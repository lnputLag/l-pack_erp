using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// просмотрщик отчетов
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-06-23</released>
    /// <changed>2022-06-23</changed>
    public partial class ReportViewer : UserControl
    {
        public ReportViewer()
        {
            FrameName="ReportViewer";
            
            InitializeComponent();
            
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();

            Content="";
        }

        public string Content { get; set; }
        public string FrameName { get; set; }

        public void Init()
        {
            ReportText.Text=Content;
        }

        public void SetDefaults()
        {
        }

        public void Destroy()
        {
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        private void ProcessMessages(ItemMessage m)
        {
        }

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

        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode=2;

            var frameName=GetFrameName();
            Central.WM.Show(frameName,"Просмотр отчета",true,"add",this);
        }

        public void Close()
        {
            var frameName=GetFrameName();
            Central.WM.Close(frameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        public string GetFrameName()
        {
            string result="";
            result=$"{FrameName}";
            return result;
        }

        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }

        private void CancelButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Close();
        }
    }
}
