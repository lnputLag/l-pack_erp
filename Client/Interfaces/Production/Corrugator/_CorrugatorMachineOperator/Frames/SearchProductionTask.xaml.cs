using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;

namespace Client.Interfaces.Production.Corrugator._CorrugatorMachineOperator.Frames
{
    public partial class SearchProductionTask : ControlBase
    {
        public SearchProductionTask()
        {
            InitializeComponent();

            OnLoad = GridTaskInit;
            
            OnUnload = () => GridWithTask.Destruct();
            
            
            FrameMode = 0;

            OnGetFrameTitle = () =>
            {
                var result = $"{Articul}";
                return result;
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "close",
                        Enabled = true,
                        Title = "Закрыть",
                        Description = "Закрыть форму",
                        ButtonUse = true,
                        ButtonName = "CancelButton",
                        MenuUse = false,
                        Action = Close,
                    });
                }
                
                Commander.Init(this);
            }
        }
        
        private int Id { get; set; }
        private string Articul { get; set; }

        private void GridTaskInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn()
                {
                    Header = "ИД ПЗ",
                    Path = "ID_PZ",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 50,
                },
                new DataGridHelperColumn()
                {
                    Header = "Номер",
                    Path = "NUM",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 50,
                    MaxWidth = 50,
                },
                new DataGridHelperColumn()
                {
                    Header = "Артикул",
                    Path = "ARTIKUL",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 50,
                    MaxWidth = 50,
                },
                new DataGridHelperColumn()
                {
                    Header = "Формат",
                    Path = "WEB_WIDTH",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 50,
                },
                new DataGridHelperColumn()
                {
                    Header = "Длина",
                    Path = "LEN",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth = 50,
                    MaxWidth = 50,
                },
                new DataGridHelperColumn()
                {
                    Header = "Завершен",
                    Path = "POSTING",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.Boolean,
                    MinWidth = 50,
                    MaxWidth = 50,
                },
                new DataGridHelperColumn()
                {
                    Header = "Начать до",
                    Path = "START_BEFORE",
                    ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth = 50,
                    MaxWidth = 50,
                },
            };
            GridWithTask.SetColumns(columns);
            GridWithTask.SetPrimaryKey("ID_PZ");
            GridWithTask.AutoUpdateInterval = 0;
            GridWithTask.OnLoadItems = QueryExecute;
            GridWithTask.Init();
        }

        public void ShowWindow(int id, string articul)
        {
            Id = id;
            Articul = articul;
            SearchLabel.Content = $"Поиск осуществлен по артикулу: {articul}";
            
            Show();
        }

        private async void QueryExecute()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorMachineOperator");
            q.Request.SetParam("Action", "SearchProductionTask");
            q.Request.SetParam("ID", Id.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    GridWithTask.UpdateItems(ds);
                }
            }
        }
    }
}