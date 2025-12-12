using Client.Common;
using Client.Interfaces.Main;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls; 
using Newtonsoft.Json;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Xceed.Wpf.Toolkit;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh
{
    /// <summary>
    /// Список простоев
    /// </summary>
    /// <author>volkov_as</author>   
    public partial class IdleList : UserControl
    {
        public IdleList()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        public void Init()
        {
            IdleGridInit();
            IdleGridLoadItems();
        }

        public Dictionary<string, string> SelectedIdleItem { get; set; }

        public static bool ShowOld { get; set; }

        /// <summary>
        /// инициализация грида IdleGrid
        /// </summary>
        public void IdleGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                     new DataGridHelperColumn
                    {
                        Header="Начало простоя",
                        Path="FROMDT_TIME",
                        Doc="Начало простоя",
                        ColumnType=ColumnTypeRef.String,
                        Width = 50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продолжительность",
                        Path="DT",
                        Doc="Время простоя",
                        ColumnType=ColumnTypeRef.String,
                        Width = 50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Причина",
                        Path="NAME",
                        Doc="Причина простоя",
                        ColumnType=ColumnTypeRef.String,
                        Width = 90,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Описание",
                        Path="REASON",
                        Doc="Описание простоя",
                        ColumnType=ColumnTypeRef.String,
                        Width = 190,
                    },
                };

                IdleGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    { "Change", new DataGridContextMenuItem(){
                        Header="Изменить",
                        Action=()=>
                        {
                            IdleReasonEditShow(SelectedIdleItem);
                        }
                    }},
                    { "Split", new DataGridContextMenuItem(){
                        Header="Разбить",
                        Action=()=>
                        {
                            IdleSplit(SelectedIdleItem);
                        }
                    }},
                };

                IdleGrid.UseRowHeader = false;
                IdleGrid.PrimaryKey = "IDIDLES";
                IdleGrid.SetColumns(columns);

                //при выборе строки в гриде, обновляются актуальные действия для записи
                IdleGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        SelectedIdleItem = selectedItem;
                    }
                };

                // двойной клик по строке
                IdleGrid.OnDblClick = IdleReasonEditShow;

                //данные грида
                IdleGrid.OnLoadItems = IdleGridLoadItems;

                IdleGrid.Init();
                IdleGrid.Run();
            }
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void IdleGridLoadItems()
        {
            IdleGrid.ShowSplash();
            bool resume = true;

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", CorrugatorMachineOperator.SelectedMachineId.ToString());
                p.CheckAdd("TO_DATE", CorrugatorMachineOperator.DateShiftStart.AddHours(12).ToString("dd.MM.yyyy HH:mm:ss"));
            }

            if (ShowOld)
            {
                p.CheckAdd("FROM_DATE", CorrugatorMachineOperator.DateShiftStart.AddDays(-2).ToString("dd.MM.yyyy HH:mm:ss"));
            }
            else
            {
                p.CheckAdd("FROM_DATE", CorrugatorMachineOperator.DateShiftStart.ToString("dd.MM.yyyy HH:mm:ss"));
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Idle");
                q.Request.SetParam("Action", "List");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

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
                        IdleGrid.UpdateItems(ds);
                    }
                }
            }

            IdleGrid.HideSplash();
        }
        public void IdleSplit(Dictionary<string, string> idle)
        {
            if (idle != null)
            {
                var toDt = idle.CheckGet("DT");

                if(string.IsNullOrEmpty(toDt))
                {
                    var d = new DialogWindow("Дождитесь пока простой закончится\nЗатем его можно будет разбить.", "Простой не завершен!");
                    d.ShowDialog();

                    return;
                }


                int id = idle.CheckGet("IDIDLES").ToInt();

                var idleEditForm = new FormExtend()
                {
                    FrameName = "IdleSplit",
                    ID = "IDIDLES",
                    Id = id,
                    Title = $"Разбивка простоя {id}",

                    QueryGet = new FormExtend.RequestData()
                    {
                        Module = "Production",
                        Object = "Idle",
                        Action = "Get"
                    },

                    QuerySave = new FormExtend.RequestData()
                    {
                        Module = "Production",
                        Object = "Idle",
                        Action = "Split"
                    },

                    Fields = new List<FormHelperField>()
                    {
                        new FormHelperField()
                        {
                            Path="FROMDT",
                            FieldType=FormHelperField.FieldTypeRef.String,
                            Description = "Начало старого простоя:",
                            ControlType="DateTime",
                            Width = 200,
                        },
                        new FormHelperField()
                        {
                            Path="TODT",
                            FieldType=FormHelperField.FieldTypeRef.String,
                            Description = "Конец старого простоя:",
                            ControlType="DateTime",
                            Width = 200,
                        },
                        new FormHelperField()
                        {
                            Path="SPLIT_DTTM",
                            FieldType=FormHelperField.FieldTypeRef.String,
                            Description = "Разбить по времени:",
                            ControlType="DateTime",
                            Width = 200,
                        },
                       
                    }
                };

                idleEditForm["FROMDT"].OnAfterCreate += (control) =>
                {
                    var dt = control as DateTimeUpDown;
                    dt.Format = DateTimeFormat.LongTime;
                    dt.IsReadOnly = true;
                };

                idleEditForm["TODT"].OnAfterCreate += (control) =>
                {
                    var dt = control as DateTimeUpDown;
                    dt.Format = DateTimeFormat.LongTime;
                    dt.IsReadOnly = true;
                };

                idleEditForm["SPLIT_DTTM"].Validate += (f, v) =>
                {
                    var fromDttm = (idleEditForm["FROMDT"].Control as DateTimeUpDown).Value;
                    var toDttm = (idleEditForm["TODT"].Control as DateTimeUpDown).Value;
                    var splitDttm = (idleEditForm["SPLIT_DTTM"].Control as DateTimeUpDown).Value;

                    if (splitDttm <= fromDttm
                        || splitDttm >= toDttm)
                    {
                        string errorMessage = "Время разбиения должно быть между временем начала и временем конца простоя!";

                        f.ValidateResult = false;
                        f.ValidateProcessed = true;
                        f.ValidateMessage = errorMessage;

                        var d = new DialogWindow(errorMessage, "Введите правильное время!");
                        d.ShowDialog();
                    }
                };


                idleEditForm.OnAfterGet += (result) =>
                {
                    var splitDttm = idleEditForm["SPLIT_DTTM"].Control as DateTimeUpDown;
                    var fromDttm = idleEditForm["FROMDT"].Control as DateTimeUpDown;
                    splitDttm.Value = fromDttm.Value;
                    splitDttm.Format = DateTimeFormat.LongTime;
                };

                idleEditForm.OnAfterSave += (id, result) =>
                {
                    IdleGrid.LoadItems();
                };

                idleEditForm.Show();
            }
        }

        /// <summary>
        /// Редактирование причины простоя
        /// </summary>
        private void IdleReasonEditShow(Dictionary<string, string> selectedItem)
        {
            bool canUse = MachineGroups.ContainsMachine(Central.Navigator.Address.GetLastBit(), 2);
            
            if (CorrugatorMachineOperator.IsCurrentMachineSelected || canUse)
            {
                var id = selectedItem.CheckGet("IDIDLES").ToInt();
                if (id != 0)
                {
                    var idleEdit = new IdleEdit();
                    idleEdit.Id = id;
                    idleEdit.IdMachine = CorrugatorMachineOperator.SelectedMachineId;
                    idleEdit.IdleReasonText.Text = selectedItem.CheckGet("REASON").ToString();
                    idleEdit.SelectedIdleItem = selectedItem;
                    idleEdit.OnClose = LoadItems;
                    idleEdit.Show();
                }
            }
        }

        public void LoadItems()
        {
            IdleGrid.LoadItems();
        }

    }
}
