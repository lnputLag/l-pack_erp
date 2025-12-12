using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh.Frames
{
    public partial class WriteOfPaddons : ControlBase
    {
        public WriteOfPaddons()
        {
            InitializeComponent();

            FrameMode = 0;

            OnLoad = GridInit;

            OnUnload = ReasonGrid.Destruct;

            OnGetFrameTitle = () => $"Списание поддона {_pallet}";
            
            
            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "writre_off_pallet",
                        Enabled = true,
                        Title = "Списать",
                        ButtonUse = true,
                        Description = "Списать выбранную позицию",
                        ButtonName = "WriteOffButton",
                        Action = WriteOfPallet
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel",
                        Enabled = true,
                        Title = "Закрыть",
                        Description = "Закрыть окно без списания",
                        ButtonUse = true,
                        ButtonName = "CancelButton",
                        Action = Close
                    });
                }

                Commander.Init(this);
            }
        }

        private int _id = 0;
        private string _pallet = string.Empty;

        private void GridInit()
        {
            var column = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "#",
                    Path = "_ROWNUMBER",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2 = 2
                },
                new DataGridHelperColumn
                {
                    Header = "Описание",
                    Path = "DESCRIPTION",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    Width2 = 25
                },
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "ID_REASON",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    Width2 = 3
                }
            };
            ReasonGrid.SetColumns(column);
            ReasonGrid.SetPrimaryKey("ID_REASON");
            ReasonGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            ReasonGrid.OnLoadItems = LoadItems;
            
            ReasonGrid.Init();
        }

        private async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorMachineOperatorKsh");
            q.Request.SetParam("Action", "ListReasonForWriteOff");

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                var list = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (list != null)
                {
                    var ds = ListDataSet.Create(list, "ITEMS");
                    ReasonGrid.UpdateItems(ds);
                }
            }
        }

        private async void WriteOfPallet()
        {
            var p = new Dictionary<string, string>
            {
                { "ID_REASON", ReasonGrid.SelectedItem.CheckGet("ID_REASON") },
                { "ID_PODDONS", _id.ToString() }
            };
            
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorMachineOperatorKsh");
            q.Request.SetParam("Action", "WriteOffPaddons");
            q.Request.SetParams(p);

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                Close();
            }
            else
            {
                var dialog = new DialogWindow($"{q.Answer.Error}", "Ошибка списания");

                dialog.ShowDialog();
            }
        }

        public void SelectReason(int id, string pallet)
        {
            _id = id;
            _pallet = pallet;
            
            Show();
        }
    }
}