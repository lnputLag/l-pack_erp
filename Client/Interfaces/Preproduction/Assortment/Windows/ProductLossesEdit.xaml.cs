using Client.Common;
using Client.Interfaces.Main;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Окно редактирования припуска 
    /// </summary>
    public partial class ProductLossesEdit : ControlBase
    {
        public ProductLossesEdit()
        {
            InitializeComponent();

            InitForm();
            AllowCalculate = false;
        }

        /// <summary>
        /// форма
        /// </summary>
        public FormHelper Form { get; set; }
        /// <summary>
        /// Окно редактирования
        /// </summary>
        public Window Window { get; set; }
        /// <summary>
        /// Имя вкладки, куда происходит возврат при закрытии этой вкладки
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// Идентификатор изделия
        /// </summary>
        public int ProductId { get; set; }
        /// <summary>
        /// Максимальное количество диапазона
        /// </summary>
        public int UpperLimit { get; set; }
        /// <summary>
        /// Флаг разрешает пересчитывать припуски. Пока идет загрузка двнных - false
        /// </summary>
        private bool AllowCalculate;
        /// <summary>
        /// Режим редактирования. 1- меняем %, 2 - меняем количество
        /// </summary>
        public int Mode { get; set; }

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessCommand(string command, ItemMessage m = null)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "close":
                        Close();
                        break;
                    case "save":
                        Save();
                        break;
                }
            }
        }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            //список полей формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="RANGE_NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="PCT_LOSSES",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=PctLosses,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        //{ FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                        //{ FormHelperField.FieldFilterRef.IsNotZero, null },
                    },
                },
                new FormHelperField()
                {
                    Path="QTY_LOSSES",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QtyLosses,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        //{ FormHelperField.FieldFilterRef.DigitOnly, null },
                        //{ FormHelperField.FieldFilterRef.IsNotZero, null },
                    },
                },
                new FormHelperField()
                {
                    Path="APPLY_ALL",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    ControlType="CheckBox",
                    Control=ApplyAllCheckBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);

        }

        /// <summary>
        /// Точка входа редактирования окна
        /// </summary>
        /// <param name="values"></param>
        public void Edit(Dictionary<string, string> values)
        {
            Form.SetValues(values);
            UpperLimit = values.CheckGet("QTY_MAX").ToInt();
            if (Mode == 1)
            {
                QtyLosses.IsReadOnly = true;
            }
            else
            {
                PctLosses.IsReadOnly = true;
            }

            // После заполнения формы разрешаем пересчет полей припуска
            AllowCalculate = true;
            Show();
        }

        /// <summary>
        /// Отображение окна
        /// </summary>
        public void Show()
        {
            string title = $"Припуск";

            int w = (int)Width;
            int h = (int)Height;

            Window = new Window
            {
                Title = title,
                Width = w + 17,
                Height = h + 40,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,

            };

            Window.Content = new Frame
            {
                Content = this,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };


            if (Window != null)
            {
                //Window.Topmost = true;
                Window.ShowDialog();
            }
        }

        /// <summary>
        /// Сохранение припусков
        /// </summary>
        public void Save()
        {
            if (Form.Validate())
            {
                bool resume = false;
                if ((bool)ApplyAllCheckBox.IsChecked)
                {
                    var dw = new DialogWindow("Вы действительно хотите во всех диапазонах поставить одинаковое значение?", "Сохранение припусков", "", DialogWindowButtons.NoYes);
                    if ((bool)dw.ShowDialog())
                    {
                        if (dw.ResultButton == DialogResultButton.Yes)
                        {
                            resume = true;
                        }
                    }
                }
                else
                {
                    resume = true;
                }

                if (resume)
                {
                    var v = Form.GetValues();
                    v.CheckAdd("MODE", Mode.ToString());

                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Preproduction",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "LossesEdit",
                        ContextObject = v,
                    });

                    Close();
                }
            }
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Close()
        {
            var window = this.Window;
            if (window != null)
            {
                window.Close();
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            if (b != null)
            {
                var t = b.Tag.ToString();
                ProcessCommand(t);
            }
        }

        /// <summary>
        /// Обработка изменения процентов припуска
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PctLosses_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AllowCalculate && Mode == 1)
            {
                QtyLosses.Text = Math.Ceiling(UpperLimit * (PctLosses.Text.ToDouble() / 100)).ToInt().ToString();
            }
        }

        /// <summary>
        /// Обработка изменений количественного припуска
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void QtyLosses_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AllowCalculate && Mode == 2)
            {
                PctLosses.Text = ((QtyLosses.Text.ToDouble() / UpperLimit) * 100).ToString();
            }
        }
    }
}
