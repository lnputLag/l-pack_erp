using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction.Rig.RigMonitorKsh.Elements;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Client.Interfaces.Preproduction.Rig.RigMonitorKsh.Tabs
{
    /// <summary>
    /// Interaction logic for RigMonitor.xaml
    /// </summary>
    public partial class RigMonitor : ControlBase
    {
        public RigMonitor()
        {
            ControlTitle = "Монитор оснастка";
            RoleName = "[erp]rig_monitor";
            InitializeComponent();

            OnLoad = () =>
            {
                Init();
            };

            {
                Commander.Add(new CommandItem
                {
                    Name = "refresh_grid_task",
                    Group = "main",
                    Enabled = true,
                    ButtonUse = true,
                    ButtonControl = Refresh,
                    MenuUse = false,
                    Action = UpdateItems
                });

                Commander.Init(this);
            }
        }

        /// <summary>
        /// Получение свежой инфы
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private async void UpdateItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "RigMonitorKsh");
            q.Request.SetParam("Action", "SelectPlan");
            q.Request.SetParam("FACT_ID", FactoryId.ToString());

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(q.Answer.Data);

                if (result != null)
                {
                    foreach (var item in GridList)
                    {
                        item.Value.LoadItemsInGrid(result.Where(x => x.CheckGet("ID_ST").ToInt() == item.Key).ToList());
                    }
                }
            }
        }

        /// <summary>
        /// Список гридов которые будут инициализированны
        /// </summary>
        private Dictionary<int, RigGrid> GridList { get; set; }
        private List<(int Id, string MachineName)> MachineList { get; set; }
        private const int FactoryId = 2; 

        private void Init()
        {
            GridList = new Dictionary<int, RigGrid>();
            MachineList = GetMachine();

            if (MachineList.Count > 0)
            {
                foreach (var m in MachineList)
                {
                    GridList.Add(m.Id, new RigGrid(m.Id, m.MachineName)
                    {
                        RoleName = RoleName,
                        ParentFrame = FrameName,
                        FactoryId = FactoryId
                    });
                }
            }

            // Разметка

            MainGrid.Children.Clear();
            MainGrid.RowDefinitions.Clear();
            MainGrid.ColumnDefinitions.Clear();

            int columns = 2;
            int rows = 3;

            for (int c = 0; c < columns; c++)
                MainGrid.ColumnDefinitions.Add(new ColumnDefinition());

            for (int r = 0; r < rows; r++)
                MainGrid.RowDefinitions.Add(new RowDefinition());

            int i = 0;
            foreach (var kvp in GridList)
            {
                var rigGrid = kvp.Value;

                int row = i / columns;
                int column = i % columns;

                Grid.SetRow(rigGrid, row);  
                Grid.SetColumn(rigGrid, column);

                MainGrid.Children.Add(rigGrid);
                i++;
            }

            UpdateItems();
        }


        private List<(int Id, string MachineName)> GetMachine()
        {
            List<(int Id, string MachineName)> list = new List<(int Id, string MachineName)>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "RigMonitorKsh");
            q.Request.SetParam("Action", "ListMachine");

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(q.Answer.Data);

                if (result != null)
                {
                    foreach (var m in result)
                    {
                        list.Add((m.CheckGet("ID_ST").ToInt(), m.CheckGet("NAME2")));
                    }
                }

            }

            return list;
        }
    }
}
