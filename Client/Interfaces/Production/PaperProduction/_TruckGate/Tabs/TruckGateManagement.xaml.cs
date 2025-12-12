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

namespace Client.Interfaces.Production.PaperProduction
{
    /// <summary>
    /// Interaction logic for TruckGateManagement.xaml
    /// </summary>
    public partial class TruckGateManagement : UserControl
    {
        public TruckGateManagement()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
        }

        private void ProcessMessages(ItemMessage obj)
        {
            
        }
    }
}
