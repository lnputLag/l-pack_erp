using System;
using System.Windows;
using Client.Interfaces.Main;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperator.Frames
{
    /// <summary>
    /// Окно с уведомление о пожаре
    /// </summary>
    public partial class FireInformatio : ControlBase
    {
        public FireInformatio()
        {
            InitializeComponent();
            FrameMode = 2;

            OnGetFrameTitle = () =>
            {
                var result = $"Пожарная тревога - {_placeFire.ToUpper()}";

                return result;
            };
        }

        private string _placeFire { get; set; }

        public string GetPlaceFire => _placeFire;

        public void ShowWindow(string text)
        {
            _placeFire = text;
    
            NotificationTime.Text = DateTime.Now.ToString("F");
            MainText.Text = $"Пожар {_placeFire}";
            
            Window window = new Window
            {
                Title = $"Пожарная тревога - {_placeFire.ToUpper()}",
                Content = this,
                Width = 500,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.CanResize
            };
            
            window.Show();
        }
    }
}
