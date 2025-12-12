using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// фрейм для отображения ошибок
    /// (применяется для встроенных режимов)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-04-29</released>
    /// <changed>2022-06-29</changed>
    public partial class ErrorTouch : UserControl
    {
        public ErrorTouch()
        {
            InitializeComponent();
            InitForm();
        }
        
        /// <summary>
        /// Форма
        /// </summary>
        public FormHelper Form { get; set; }

        private Timeout AutoCloseTimeout{ get; set; }

        public delegate void OnCloseDelegate();
        public OnCloseDelegate OnClose;
        public virtual void OnCloseTemplate()
        {
        }

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
            };

            Form.SetFields(fields);
            Form.StatusControl=FormStatus;
            Form.SetDefaults();
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="Production",
                ReceiverName = "",
                SenderName = "ErrorTouch",
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
            switch(e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
            }
        }


        public void Show(string title, string message, int delay=0)
        {
            var v=new Dictionary<string,string>();
            v.CheckAdd("TITLE",title);
            v.CheckAdd("MESSAGE",message);
            Form.SetValues(v);

            Central.WM.Show($"ErrorTouch","Ошибка",true,"add",this);
            Central.WM.SetActive($"ErrorTouch",true);

            if (delay > 0)
            {
                AutoCloseTimeout=new Timeout(
                    delay,
                    () =>
                    {
                        Close();
                    }
                );
                AutoCloseTimeout.Run();
            }
        }

        public void Close()
        {
            OnClose?.Invoke();
            Central.WM.Close($"ErrorTouch");
        }
       
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}
