using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.Utils.Filtering.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Client.Interfaces.Preproduction.PlannedDowntime.Frames
{
    /// <summary>
    /// Интерфейс для создания шаблонов плановых простоев
    /// </summary>
    public partial class PatternCreateFrame : ControlBase
    {
        public PatternCreateFrame()
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
                if (IdPattern == 0)
                {
                    return "Создание типового простоя";
                }

                return $"Ред. типового простоя {IdPattern}";
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
        private bool IsEdit { get; set; }
        private int IdPattern { get; set; }
        private Dictionary<string, string> Pattern = new Dictionary<string, string>();

        public void Create(int factId)
        {
            FactId = factId;

            LoadItems();

            Show();
        }

        public void Edit(int id, int factId, Dictionary<string, string> pattern)
        {
            FactId = factId;
            IdPattern = id;
            Pattern = pattern;
            IsEdit = true;

            if (!pattern.CheckGet("DTTM_START").IsNullOrEmpty())
            {
                VariablePattern.SelectedIndex = 2;
                StartDowntimeHourInterval.Text = pattern.CheckGet("HOUR_START").ToInt().ToString();
                StartDowntimeMinutInterval.Text = pattern.CheckGet("MINUTE_START").ToInt().ToString();
                DowntimeInterval.Text = pattern.CheckGet("DOWNTIME").ToInt().ToString();
                StartDateDowntimeInterval.Text = pattern.CheckGet("DTTM_START").ToDateTime().ToString("dd.MM.yyyy");
                StartEveryHourDowntimeInterval.Text = pattern.CheckGet("REPEAT_HOURS").ToInt().ToString();
            }

            if (!pattern.CheckGet("DAY_OF_WEEK").IsNullOrEmpty())
            {
                VariablePattern.SelectedIndex = 0;
                StartDowntimeHourWeek.Text = pattern.CheckGet("HOUR_START").ToInt().ToString();
                StartDowntimeMinutWeek.Text = pattern.CheckGet("MINUTE_START").ToInt().ToString();
                DowntimeWeek.Text = pattern.CheckGet("DOWNTIME").ToInt().ToString();
                NumberDayWeek.Text = pattern.CheckGet("DAY_OF_WEEK").ToInt().ToString();
            }

            if (!pattern.CheckGet("DAY_OF_MONTH").IsNullOrEmpty())
            {
                VariablePattern.SelectedIndex = 1;
                StartDowntimeHourMonth.Text = pattern.CheckGet("HOUR_START").ToInt().ToString();
                StartDowntimeMinutMonth.Text = pattern.CheckGet("MINUTE_START").ToInt().ToString();
                DowntimeMonth.Text = pattern.CheckGet("DOWNTIME").ToInt().ToString();
                NumberDayMonth.Text = pattern.CheckGet("DAY_OF_WEEK").ToInt().ToString();
            }

            LoadItems();

            Show();
        }

        private async void Save()
        {
            var p = new Dictionary<string, string>
            {
                { "ID_ST", MachineGridBox.SelectedItem.CheckGet("ID_ST") },
                { "ID_IDLE_DETAILS", ReasonGridBox.SelectedItem.CheckGet("ID_IDLE_DETAILS") }
            };

            if (IsEdit)
            {
                p.Add("DOSC_ID", IdPattern.ToString());
            }

            var selectTabIntex = VariablePattern.SelectedIndex;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PlannedDowntime");
            q.Request.SetParam("Action", "PatternSave");

            switch (selectTabIntex)
            {
                case 0:
                    if (NumberDayWeek.Text.IsNullOrEmpty() || StartDowntimeHourWeek.Text.IsNullOrEmpty() ||
                        StartDowntimeMinutWeek.Text.IsNullOrEmpty() || DowntimeWeek.Text.IsNullOrEmpty())
                    {
                        FormStatus.Text = "Требуется заполнить все поля";
                        return;
                    }

                    p.Add("DAY_OF_WEEK", NumberDayWeek.Text);
                    p.Add("HOUR_START", StartDowntimeHourWeek.Text);
                    p.Add("MINUTE_START", StartDowntimeMinutWeek.Text);
                    p.Add("DOWNTIME", DowntimeWeek.Text);
                    break;
                case 1:
                    if (NumberDayMonth.Text.IsNullOrEmpty() || StartDowntimeHourMonth.Text.IsNullOrEmpty() ||
                        StartDowntimeMinutMonth.Text.IsNullOrEmpty() || DowntimeMonth.Text.IsNullOrEmpty())
                    {
                        FormStatus.Text = "Требуется заполнить все поля";
                        return;
                    }

                    p.Add("DAY_OF_MONTH", NumberDayMonth.Text);
                    p.Add("HOUR_START", StartDowntimeHourMonth.Text);
                    p.Add("MINUTE_START", StartDowntimeMinutMonth.Text);
                    p.Add("DOWNTIME", DowntimeMonth.Text);
                    break;
                case 2:
                    if (StartDateDowntimeInterval.Text.IsNullOrEmpty() || StartDowntimeHourInterval.Text.IsNullOrEmpty() ||
                        StartDowntimeMinutInterval.Text.IsNullOrEmpty() || DowntimeInterval.Text.IsNullOrEmpty() ||
                        StartDateDowntimeInterval.Text.IsNullOrEmpty())
                    {
                        FormStatus.Text = "Требуется заполнить все поля";
                        return;
                    }

                    p.Add("START_DTTM", StartDateDowntimeInterval.Text);
                    p.Add("REPEAT_HOURS", StartEveryHourDowntimeInterval.Text);
                    p.Add("HOUR_START", StartDowntimeHourInterval.Text);
                    p.Add("MINUTE_START", StartDowntimeMinutInterval.Text);
                    p.Add("DOWNTIME", DowntimeInterval.Text);
                    break;
            }

            q.Request.SetParams(p);

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverName = "PatternsDowntimeGrid",
                    Action = "pattern_downtime_grid_refresh"
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

                    if (IsEdit)
                    {
                        await Task.Run(async () =>
                        {
                            while (MachineGridBox.Items.Count == 0 && ReasonGridBox.Items.Count == 0)
                            {
                                await Task.Delay(100);
                            }
                        });

                        MachineGridBox.SelectRowByKey(Pattern.CheckGet("ID_ST").ToInt().ToString());
                        ReasonGridBox.SelectRowByKey(Pattern.CheckGet("ID_IDLE_DETAILS").ToInt().ToString());
                    }
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

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static bool IsTextAllowed(string text)
        {
            foreach (char c in text)
            {
                if (!char.IsDigit(c))
                    return false;
            }
            return true;
        }

    }
}
