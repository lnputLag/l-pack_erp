using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Debug
{
    public partial class PosterList:UserControl
    {
        public PosterList()
        {
            InitializeComponent();

            SelectedItemId = 0;
            PosterCount=0;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            SetDefaults();
        }


        private int PosterCount { get;set;}

        public void SetDefaults()
        {
            
        }

        /// <summary>
        /// датасет, содержащий данные
        /// </summary>
        public ListDataSet ItemsDS { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        public int SelectedItemId { get; set; }
        Dictionary<string, string> SelectedItem { get; set; }


        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            
        }

             
       

       

        public void ShowHelp()
        {
            //Central.ShowHelp("/doc/l-pack-erp/shipments/control/report");
        }

        
        public void Create()
        {
            PosterCount++;
            var name=$"poster_{PosterCount}";
            var label=$"Запрос {PosterCount}";

            var poster = new PosterView();
            Central.WM.AddTab(name,label,true,"add",poster);
        }

        public object Create2()
        {
            PosterCount++;
            var name=$"poster_{PosterCount}";
            var label=$"Запрос {PosterCount}";

            var poster = new PosterView();
            Central.WM.AddTab(name,label,true,"add",poster);

            return poster;
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void CreateButton_Click(object sender,RoutedEventArgs e)
        {
            Create();
        }

      
    }


}
