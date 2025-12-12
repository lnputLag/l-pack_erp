using Client.Common;
using Client.Interfaces.Main;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Client.Interfaces.Preproduction.Rig.RigMonitorKsh.Elements
{
    /// <summary>
    /// Interaction logic for RigGrid.xaml
    /// </summary>
    public partial class RigGrid : UserControl
    {
        public RigGrid(int machineId, string machineName)
        {
            InitializeComponent();

            MachineId = machineId;
            MachineName = machineName;
            FrameName = "RigGrid";

            MachineNameTextBox.Text = machineName;

            Init(1, ClicheGrid);
            Init(2, ShtanzGrid);
        }

        public string RoleName { get; set; }
        public int FactoryId { get; set; }
        public string ParentFrame { get; set; }
        private string ControlName = "Монитор оснастки";
        private string FrameName { get; set; }
        public int MachineId { get; set; }
        private string MachineName { get; set; }

        public List<Dictionary<string, string>> Items = new List<Dictionary<string, string>>();

        public void LoadItemsInGrid(List<Dictionary<string, string>> items)
        {
            if (items != null && items.Count > 0)
            {
                var cliche = new ListDataSet();
                cliche.Init();
                var shtanz = new ListDataSet();
                shtanz.Init();

                foreach (var item in items)
                {
                    if (item.CheckGet("PRINTING").ToInt() == 1)
                    {
                        cliche.AddItem(item);
                    }

                    if (item.CheckGet("SHTANZ_KEY").ToInt() == 1)
                    {
                        shtanz.AddItem(item);
                    }
                }

                ClicheGrid.UpdateItems(cliche);
                ShtanzGrid.UpdateItems(shtanz);
            }
        }

        /// <summary>
        /// 1 - cliche, 2 - shtanz
        /// </summary>
        /// <param name="key"></param>
        private void Init(int key, GridBox4 grid)
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "№ ПЗ",
                    Path = "NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 13,
                },
                new DataGridHelperColumn
                {
                    Header = "Штанцформа",
                    Path = "SHTANZ",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 13,
                    Visible = key == 2
                },
                new DataGridHelperColumn
                {
                    Header = "Клише 1",
                    Path = "CLICHE_1",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 13,
                    Visible = key == 1
                },
                new DataGridHelperColumn
                {
                    Header = "Клише 2",
                    Path = "CLICHE_2",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 13,
                    Visible = key == 1
                },
                new DataGridHelperColumn
                {
                    Header = "Клише 3",
                    Path = "CLICHE_3",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 13,
                    Visible = key == 1
                },
                new DataGridHelperColumn
                {
                    Header = "Клише 4",
                    Path = "CLICHE_4",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 13,
                    Visible = key == 1
                },
                new DataGridHelperColumn
                {
                    Header = "Клише 5",
                    Path = "CLICHE_5",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 13,
                    Visible = key == 1
                },
            };
            grid.SetColumns(columns);
            grid.AutoUpdateInterval = 0;
            grid.ItemsAutoUpdate = false;
            grid.SetPrimaryKey("NUM");

            grid.Init();
            grid.Run();
        }
    }
}
