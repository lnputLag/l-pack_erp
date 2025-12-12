using Client.Common;    
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// диалоговое окно для тачскринов
    /// (применяется для встроенных режимов)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-07-25</released>
    /// <changed>2022-07-25</changed>
    public partial class DialogTouch : UserControl
    {
        public DialogTouch(string message, string title = "", string description = "", DialogWindowButtons buttons = DialogWindowButtons.OK)
        {
            InitializeComponent();

            AutohideTimeoutInterval = 3000;
            DialogResult =false;
            ResultButton= DialogResultButton.None;
            
            InitForm();

            if(buttons == DialogWindowButtons.OKAutohide)
            {
                AutohideTimeout = new Timeout(
                    1,
                    () => {
                        Destroy();
                        Close();
                    }
                );
                AutohideTimeout.SetIntervalMs(AutohideTimeoutInterval);
                AutohideTimeout.Run();
                buttons = DialogWindowButtons.OK;
            }
            InitButtons(buttons);

            var v=new Dictionary<string,string>();
            {
                v.CheckAdd("TITLE", title);
                v.CheckAdd("MESSAGE", message);
                v.CheckAdd("DESCRIPTION", description);
            }
            Form.SetValues(v);
            Show();
        }
        
        private int AutohideTimeoutInterval { get; set; }
        private Timeout AutohideTimeout { get; set; }

        /// <summary>
        /// Форма
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Нажатая кнопка 
        /// </summary>
        public DialogResultButton ResultButton { get; set; }

        /// <summary>
        /// результат
        /// </summary>
        public bool DialogResult { get; set; }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        public void InitForm()
        {
            Form=new FormHelper();
            //список колонок формы
            var fields=new List<FormHelperField>()
            {                
                new FormHelperField()
                { 
                    Path="TITLE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Title,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },  
                new FormHelperField()
                { 
                    Path="MESSAGE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Message,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },    
                new FormHelperField()
                { 
                    Path="DESCRIPTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Description,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },    
            };

            Form.SetFields(fields);
            Form.StatusControl=FormStatus;
            Form.SetDefaults();
        }

        /// <summary>
        /// инициализация кнопок
        /// </summary>
        public void InitButtons(DialogWindowButtons buttons)
        {
            YesButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Collapsed;
            NoButton.Visibility = Visibility.Collapsed;

            switch (buttons)
            {
                case DialogWindowButtons.OK:
                    YesButton.Content = "OK";
                    YesButton.Visibility = Visibility.Visible;
                    break;

                case DialogWindowButtons.YesNo:
                    YesButton.Content = "Да";
                    CancelButton.Content = "Нет";
                    YesButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                    break;

                case DialogWindowButtons.OKCancel:
                    YesButton.Content = "OK";
                    CancelButton.Content = "Отмена";
                    YesButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                    break;

                case DialogWindowButtons.NoYes:
                    YesButton.Content = "Да";
                    CancelButton.Content = "Нет";
                    YesButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;

                    CancelButton.Style= (Style)CancelButton.FindResource("TouchFormButtonPrimary");
                    YesButton.Style = (Style)YesButton.FindResource("TouchFormButton");
                    break;

                case DialogWindowButtons.YesNoCancel:
                    YesButton.Content = "Да";
                    NoButton.Content = "Нет";
                    CancelButton.Content = "Отмена";
                    YesButton.Visibility = Visibility.Visible;
                    NoButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                    break;

                case DialogWindowButtons.RetryCancel:
                    YesButton.Content = "Повторить";
                    CancelButton.Content = "Отмена";
                    YesButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                    break;

                case DialogWindowButtons.None:
                    break;
            }

        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="",
                ReceiverName = "",
                SenderName = "DialogTouch",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);   
            
            if(AutohideTimeout!=null)
            {
                AutohideTimeout.Finish();
            }            
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch(e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
            }
        }

        public delegate void OnCompleteDelegate(DialogResultButton resultButton);
        public OnCompleteDelegate OnComplete;
        public virtual void OnCompleteAction(DialogResultButton resultButton)
        {

        }

        public delegate void OnCloseDelegate();
        public OnCloseDelegate OnClose;
        public virtual void OnCloseAction()
        {

        }

        public void Show()
        {
            Central.WM.Show($"DialogTouch","",true,"add",this);
        }

        public void Close()
        {
            Central.WM.Close($"DialogTouch");
            if(OnClose!=null)
            {
                OnClose.Invoke();
            }
        }
            
        private void AnswerOk()
        {
            DialogResult = true;
            ResultButton = DialogResultButton.Yes;
            OnComplete?.Invoke(ResultButton);
            Close();
        }

        private void AnswerCancel()
        {
            DialogResult = false;
            ResultButton = DialogResultButton.Cancel;
            OnComplete?.Invoke(ResultButton);
            Close();
        }

        private void AnswerNo()
        {
            DialogResult = true;
            ResultButton = DialogResultButton.No;
            OnComplete?.Invoke(ResultButton);
            Close();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            AnswerOk();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            AnswerCancel();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
             AnswerNo();
        }
    }
}
