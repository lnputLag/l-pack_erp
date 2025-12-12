using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Выбор обоснования подтверждения образца
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleSetConfirmation : UserControl
    {
        public SampleSetConfirmation()
        {
            InitializeComponent();

            InitForm();
            SetDefaults();
        }

        #region Common

        /// <summary>
        /// Форма
        /// </summary>
        FormHelper Form { get; set; }
        /// <summary>
        /// Окно редактирования примечания
        /// </summary>
        public Window Window { get; set; }
        /// <summary>
        /// Название окна получателя сообщения
        /// </summary>
        public string ReceiverName;

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
                SenderName = "SampleConfirmation",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        #endregion

        public void SetDefaults()
        {
            Form.SetDefaults();
            Reason.Items = SampleConfirmationReasons.Items;
            Reason.SelectedItem = SampleConfirmationReasons.Items.GetEntry("0");
            ReceiverName = "";
        }

        private void InitForm()
        {
            Form = new FormHelper();
            //список полей формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="LIMIT_QTY_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="REASON",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Reason,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Note,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STATUS",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="DT_COMPLETED",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        public void Edit(Dictionary<string, string> values)
        {
            Form.SetValues(values);
            Show();
        }

        public void Show()
        {
            string title = "Обоснование подтверждения образца";

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

        private void Save()
        {
            var v = Form.GetValues();
            int reasonKey = v.CheckGet("REASON").ToInt();
            if (reasonKey == 0)
            {
                Form.SetStatus("Выберите обоснование", 1);
            }
            else if ((reasonKey == 4) && string.IsNullOrEmpty(v.CheckGet("NOTE")))
            {
                Form.SetStatus("Заполните примечание", 1);
            }
            else
            {
                //отправляем сообщение с выбранным значением
                /*
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "PreproductionSample",
                    ReceiverName = ReceiverName,
                    SenderName = "SampleConfirmation",
                    Action = "SetConfirmation",
                    ContextObject = v,
                });
                */
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "PreproductionSample",
                    ReceiverName = ReceiverName,
                    SenderName = "SampleConfirmation",
                    Action = "SetConfirmation",
                    ContextObject = v,
                });
                Close();
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
