using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// 
    /// </summary>
    public partial class SplashControl : UserControl
    {

        public string Message
        {
            get => MessageBlock.Text;
            set => MessageBlock.Text = value;
        }

        public bool Visible
        {
            get => Splash.Visibility == Visibility.Visible;
            set => Splash.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

       

        public SplashControl()
        {
            InitializeComponent();
        }

        
    }
}
