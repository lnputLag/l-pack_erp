using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
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

namespace Client.Interfaces.Preproduction.PlannedDowntime.Frames
{
    /// <summary>
    /// Interaction logic for DowntimeCreateFrame.xaml
    /// </summary>
    public partial class DowntimeCreateFrame : ControlBase
    {
        public DowntimeCreateFrame()
        {
            InitializeComponent();

            RoleName = "[erp]planned_downtime";

            OnLoad = () =>
            {
                MachineGridBoxInit();
                ReasonGridBoxInit();
            };

            OnUnload = () =>
            {
                MachineGridBox.Destruct();
                ReasonGridBox.Destruct();
            };

            FrameMode = 0;
            OnGetFrameTitle = () =>
            {
                return "Создание нового простоя";
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "save",
                        Enabled = true,
                        Title = "Сохранить",
                        ButtonUse = true,
                        ButtonControl = SaveButton,
                        AccessLevel = Common.Role.AccessMode.FullAccess,
                        Action = Save
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel",
                        Enabled = true,
                        Title = "Закрыть",
                        ButtonUse = true,
                        ButtonControl = CancelButton,
                        Action = Close
                    });
                }

                Commander.Init(this);
            }
        }

        private int FactId { get; set; }

        public void Create(int factId)
        {
            FactId = factId;

            LoadItems();

            Show();
        }

        private async void Save()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PlannedDowntime");
            q.Request.SetParam("Action", "Create");
            q.Request.SetParam("ID_ST", MachineGridBox.SelectedItem.CheckGet("ID_ST"));
            q.Request.SetParam("ID_IDLE_DETAILS", ReasonGridBox.SelectedItem.CheckGet("ID_IDLE_DETAILS"));
            q.Request.SetParam("DTTM_START", StartDowntime.Text.ToString());
            q.Request.SetParam("DTTM_END", EndDowntime.Text.ToString());

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<int>(q.Answer.Data);

                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverName = "DowntimeGrid",
                    Action = "downtime_grid_refresh",
                    Message = $"{result}"
                });

                Close();
            }
        }

        private async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PlannedDowntime");
            q.Request.SetParam("Action", "ListMachineAndReason");
            q.Request.SetParam("FACT_ID", FactId.ToString());

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null)
                {
                    var l1 = ListDataSet.Create(result, "ITEMS");
                    var l2 = ListDataSet.Create(result, "REASON");

                    MachineGridBox.UpdateItems(l1);
                    ReasonGridBox.UpdateItems(l2);
                }
            } 
            else
            {
                var d = new DialogWindow("Во время запроса произошла ошибка", "Ошибка");
                d.ShowDialog();
                Close();
            }
        }

        private void MachineGridBoxInit()
        {
            var column = new List<DataGridHelperColumn> 
            {
                new DataGridHelperColumn
                {
                    Header = "Название",
                    Path = "NAME2",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "ID_ST",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2 = 6,
                },
            };
            MachineGridBox.SetColumns(column);
            MachineGridBox.SetPrimaryKey("ID_ST");
            MachineGridBox.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            MachineGridBox.AutoUpdateInterval = 0;
            MachineGridBox.ItemsAutoUpdate = false;
            MachineGridBox.SearchText = SearchText;
            MachineGridBox.Init();
        }

        private void ReasonGridBoxInit()
        {
            var column = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Вид простоя",
                    Path = "DESCRIPTION",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 30,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "ID_IDLE_DETAILS",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2 = 6,
                },
            };
            ReasonGridBox.SetColumns(column);
            ReasonGridBox.SetPrimaryKey("ID_IDLE_DETAILS");
            ReasonGridBox.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ReasonGridBox.AutoUpdateInterval = 0;
            ReasonGridBox.ItemsAutoUpdate = false;
            ReasonGridBox.Init();
        }
    }
}
