using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Debug
{
    /// <summary>
    /// Логика взаимодействия для ShipmentClicheView.xaml.
    /// </summary>
    public partial class UrlParams:UserControl
    {
        /// <summary>
        /// тестирование параметров URL        
        /// </summary>
        public UrlParams()
        {
            InitializeComponent();
            NavigationProcessed=false;
            NavigationParams=new Dictionary<string, string>();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this,ProcessMessages);

            SetDefaults();            
            Central.Dbg("Construct");
            Init();
        }


        /// <summary>
        /// инициализация интерфейса
        /// </summary>
        public void Init()
        {
            Central.Dbg($"....Init processed={NavigationProcessed} count={NavigationParams.Count}");

            ParametersBlock.Text="11111";

            //if(!NavigationProcessed)
            {
                NavigationProcessed=true;

                if(NavigationParams.Count>0)
                {
                    string text="";
                    text=$"{text}\nparameters:";
                    foreach(KeyValuePair<string,string> i in NavigationParams)
                    {
                        text=$"{text}\n{i.Key}={i.Value}";
                    }
                    ParametersBlock.Text=text;
                    Central.Dbg($"....Init (1) text={text}");
                }
                else
                {
                    ParametersBlock.Text="no parameters";
                    Central.Dbg($"....Init (2) text=no parameters");
                }

            }
        }
        
        /// <summary>
        /// флаг отработки навигации
        /// </summary>
        private bool NavigationProcessed { get;set;}
        private Dictionary<string,string> NavigationParams { get;set;}

        public void SetDefaults()
        {
            //ParametersBlock.Text="";
        }
             

        /// <summary>
        /// Остановка вспомогательных процессов при закрытии интерфейса.
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="ProductionTask",
                ReceiverName = "",
                SenderName = "ProductionTaskListView",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        
        /// <summary>
        /// обработчик сообщений.
        /// </summary>
        /// <param name="m">сообщение.</param>
        private void ProcessMessages(ItemMessage m)
        {
            
        }

       

        /// <summary>
        /// The ShowHelp.
        /// </summary>
        public void ShowHelp()
        {
            //Central.ShowHelp("/doc/l-pack-erp/shipments/control/equipments/shipmentcliche");
        }

        /// <summary>
        /// обработчик системы навигации по URL.
        /// </summary>
        public void ProcessNavigation()
        {
            Central.Dbg("ProcessNavigation");
            NavigationParams=Central.Navigator.Address.Params;                                   
            System.Threading.Thread.Sleep(1000);
            Init();
        }

       

        /// <summary>
        /// The HelpButton_Click.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/>.</param>
        private void HelpButton_Click(object sender,RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void InitButton_Click(object sender,RoutedEventArgs e)
        {
            Init();
        }
    }
}
