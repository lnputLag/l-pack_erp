using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Client.Assets.HighLighters;
using System;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Linq;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Xceed.Wpf.Toolkit;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperator
{
    /// <summary>
    /// Список простоев
    /// </summary>
    /// <author>vlasov_ea</author>   
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
                        Width = 290,
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
        public async void IdleSplit(Dictionary<string, string> idle)
        {
            if (idle == null)
                return;

            // Разрешаем разбивку только для завершенного простоя
            var toDt = idle.CheckGet("DT");
            if (string.IsNullOrEmpty(toDt))
            {
                var d = new DialogWindow("Дождитесь пока простой закончится\nЗатем его можно будет разбить.", "Простой не завершен!");
                d.ShowDialog();
                return;
            }

            int id = idle.CheckGet("IDIDLES").ToInt();
            var ctrl = new _CorrugatorMachineOperator.Elements.IdleSplit();

            // Попытка инициализации из выбранной строки
            DateTime? fromTime = TryParseDateTime(idle.CheckGet("FROMDT"));
            DateTime? toTime = TryParseDateTime(idle.CheckGet("TODT"));

            // Если данных нет, запрашиваем подробности
            if (!fromTime.HasValue || !toTime.HasValue)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Idle");
                q.Request.SetParam("Action", "Get");
                q.Request.SetParam("IDIDLES", id.ToString());

                q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() => q.DoQuery());

                if (q.Answer.Status == 0)
                {
                    try
                    {
                        var resp = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        var data = ListDataSet.Create(resp, "ITEMS");
                        
                        string fromStr = data.Items.First().CheckGet("FROMDT");
                        string toStr = data.Items.First().CheckGet("TODT");

                        fromTime = fromTime ?? TryParseDateTime(fromStr);
                        toTime = toTime ?? TryParseDateTime(toStr);
                    }
                    catch
                    {

                    }
                }
            }

            if (!fromTime.HasValue || !toTime.HasValue || fromTime.Value >= toTime.Value)
            {
                var d = new DialogWindow("Не удалось получить корректные границы простоя.", "Ошибка данных");
                d.ShowDialog();
                return;
            }

            ctrl.FromTime = fromTime.Value;
            ctrl.ToTime = toTime.Value;
            ctrl.SplitTime = ctrl.FromTime.AddTicks((ctrl.ToTime - ctrl.FromTime).Ticks / 2);
            
            var host = new Window
            {
                Title = $"Разбивка простоя {id}",
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                Owner = Application.Current?.MainWindow,
                Content = ctrl,
                ResizeMode = ResizeMode.NoResize
            };
            
            ctrl.SplitConfirmed += async (s, e) =>
            {
                const string fmt = "dd.MM.yyyy HH:mm:ss";

                var qSplit = new LPackClientQuery();
                qSplit.Request.SetParam("Module", "Production");
                qSplit.Request.SetParam("Object", "Idle");
                qSplit.Request.SetParam("Action", "Split");
                qSplit.Request.SetParam("IDIDLES", id.ToString());
                qSplit.Request.SetParam("FROMDT", e.FromTime.ToString(fmt, CultureInfo.InvariantCulture));
                qSplit.Request.SetParam("TODT", e.ToTime.ToString(fmt, CultureInfo.InvariantCulture));
                qSplit.Request.SetParam("SPLIT_DTTM", e.SplitTime.ToString(fmt, CultureInfo.InvariantCulture));

                qSplit.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
                qSplit.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() => qSplit.DoQuery());

                if (qSplit.Answer.Status == 0)
                {
                    host.DialogResult = true;
                    host.Close();
                    IdleGrid.LoadItems();
                }
            };
            
            ctrl.Cancelled += (s, e) =>
            {
                host.DialogResult = false;
                host.Close();
            };

            host.ShowDialog();
            
            static DateTime? TryParseDateTime(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return null;
                if (DateTime.TryParseExact(s,
                        new[] { "dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy HH:mm", "dd.MM.yyyy",
                                "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.fff" },
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeLocal, out var dt))
                {
                    return dt;
                }
                if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dt))
                {
                    return dt;
                }
                return null;
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
