using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Окно указания причин отмены выполнения задания
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleTaskStopReason : UserControl
    {
        /// <summary>
        /// Окно указания причин отмены выполнения задания
        /// </summary>
        public SampleTaskStopReason()
        {
            InitializeComponent();

            InitForm();
            SetDefaults();
        }

        public Dictionary<string, string> TaskValues { get; set; }
        /// <summary>
        /// форма
        /// </summary>
        public FormHelper Form { get; set; }
        /// <summary>
        /// Окно редактирования примечания
        /// </summary>
        public Window Window { get; set; }
        /// <summary>
        /// Название получателя сообщения
        /// </summary>
        public string ReceiverName;

        #region Common
        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "PreproductionSample",
                ReceiverName = "",
                SenderName = "SampleTaskStopReason",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;

                case Key.Enter:
                    Save();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void SetDefaults()
        {
            Form.SetDefaults();
            TaskValues = new Dictionary<string, string>();
        }
        #endregion

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
                    Path="RAW_ABSENT",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=RawAbsentCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DRAWING_INCORRECT",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=DrawingIncorrectCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PROFILE_INCORRECT",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=ProfileIncorrectCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STOP_REASON_TEXT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=StopReasonText,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        public void Show()
        {
            string title = "Причины отмены";

            Window = new Window
            {
                Title = title,
                Width = this.Width + 24,
                Height = this.Height + 40,
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
                Window.Topmost = true;
                Window.ShowDialog();
            }
        }

        public void Close()
        {
            var window = this.Window;
            if (window != null)
            {
                window.Close();
            }
            Destroy();
        }

        public void Save()
        {
            List<string> reasons = new List<string>();
            if ((bool)RawAbsentCheckBox.IsChecked)
            {
                reasons.Add((string)RawAbsentCheckBox.Content);
                TaskValues.CheckAdd("RAW_MISSING", "1");
            }

            if ((bool)DrawingIncorrectCheckBox.IsChecked)
            {
                reasons.Add((string)DrawingIncorrectCheckBox.Content);
                TaskValues.CheckAdd("REVISION", "1");
            }

            if ((bool)ProfileIncorrectCheckBox.IsChecked)
            {
                reasons.Add((string)ProfileIncorrectCheckBox.Content);
                TaskValues.CheckAdd("REVISION", "1");
            }

            if (!string.IsNullOrEmpty(StopReasonText.Text))
            {
                reasons.Add(StopReasonText.Text);
            }

            if (reasons.Count > 0)
            {
                TaskValues.Add("NOTE", string.Join(". ", reasons));

                //отправляем сообщение с текстом причины
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "PreproductionSample",
                    ReceiverName = ReceiverName,
                    SenderName = "SampleTaskStopReason",
                    Action = "SetReason",
                    ContextObject = TaskValues,
                });
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "PreproductionSample",
                    ReceiverName = ReceiverName,
                    SenderName = "SampleTaskStopReason",
                    Action = "SetReason",
                    ContextObject = TaskValues,
                });
                Close();
            }
            else
            {
                Form.SetStatus("Выберите или напишите причину", 1);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
