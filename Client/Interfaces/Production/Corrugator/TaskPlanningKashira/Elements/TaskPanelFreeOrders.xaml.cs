using Client.Common;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Production.Corrugator.TaskPlanningKashira
{
    /// <summary>
    /// Interaction logic for TaskPanelFreeOrders.xaml
    /// </summary>
    public partial class TaskPanelFreeOrders : UserControl, ITaskPanel
    {
        public TaskPanelFreeOrders()
        {
            InitializeComponent();
        }

        private event ITaskPanel.FindFormat onFormat;
        private event ITaskPanel.FindFormat onHoursFilter;


        public string Description { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Kpd { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public TextBox SearchBox => SearchText;

        public string NameStanok { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        event ITaskPanel.FindFormat ITaskPanel.OnFindFormat
        {
            add
            {
                onFormat += value;
            }

            remove
            {
                onFormat-= value;
            }
        }


        event ITaskPanel.FindFormat ITaskPanel.OnHoursFilter
        {
            add
            {
                onHoursFilter += value;
            }

            remove
            {
                onHoursFilter -= value;
            }
        }

        private void ButtonFilterShortOrders_Click(object sender, RoutedEventArgs e)
        {
            int format = Settings.ShortBlockSize;
            onFormat?.Invoke(format);
        }

        private void HoursText_TextChanged(object sender, TextChangedEventArgs e)
        {
            int format = 0;

            if(!int.TryParse(HoursText.Text, out format))
            {
                format = 0;
            }

            onHoursFilter?.Invoke(format);
        }

        private void ButtonShortOrders_Click(object sender, RoutedEventArgs e)
        {
            Central.WM.CheckAddTab<ShortOrderManager>("ShortOrderManager", "Короткие ПЗ на ГА", true, "main");
            Central.WM.SetActive("ShortOrderManager");

        }

        public void SetPlanData(int totalLenght, TimeSpan duration)
        {
            
        }

        public void SetMachineData(string CurrentProdTask, double CurrentProgress, string CurrentProgressTask, string TotalTaskLength, string AvgSpeed)
        {
            
        }

        public void Update(Dictionary<string, string> item)
        {
            if (item != null)
            {
                ArticulText.Content = item.CheckGet(TaskPlaningDataSet.Dictionary.Artikul);
            }
            else
            {
                ArticulText.Content = "";
            }
        }
    }
}
