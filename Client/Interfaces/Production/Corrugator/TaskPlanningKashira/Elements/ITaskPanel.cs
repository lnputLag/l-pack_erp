using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Client.Interfaces.Production.Corrugator.TaskPlanningKashira
{
    internal interface ITaskPanel
    {
        public delegate void FindFormat(int format);
        public event FindFormat OnFindFormat;
        public event FindFormat OnHoursFilter;

        public string Kpd
        { 
            get; set; 
        }

        public string NameStanok {  get; set; }

        public TextBox SearchBox { get; }


        public void SetPlanData(int totalLenght, TimeSpan duration);
        public void SetMachineData(string CurrentProdTask, double CurrentProgress, string CurrentProgressTask, string TotalTaskLength, string AvgSpeed);

        public void Update(Dictionary<string, string> item);

    }
}
