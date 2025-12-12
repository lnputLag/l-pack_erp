using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using NPOI.SS.Formula.Functions;
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
using System.Windows.Shapes;

namespace Client.Interfaces.Preproduction.PlannedDowntime.Elements
{
    /// <summary>
    /// Interaction logic for DowntimeEdit.xaml
    /// </summary>
    public partial class DowntimeEdit : ControlBase
    {
        public DowntimeEdit()
        {
            downtimeId = 0;

            InitializeComponent();

            FrameMode = 2;
            OnGetFrameTitle = () =>
            {
                return $"Редактирование простоя №{downtimeId}";
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
                        ButtonName = "SaveButton",
                        Action = Save
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel",
                        Enabled = true,
                        Title = "отмена",
                        ButtonUse = true,
                        ButtonName = "CancelButton",
                        Action = Close
                    });
                }
                Commander.Init(this);
            }
        }

        private int downtimeId { get; set; }
        private string oldValueStartDate { get; set; }
        private string oldValueEndDate { get; set; }

        private async void Save()
        {
            bool resume = true;

            {
                var f = StartDowntime.Text.ToDateTime();
                var t = EndDowntime.Text.ToDateTime();

                if (DateTime.Compare(f, t) > 0)
                {
                    Status.Text = "Дата начала периода не может быть\nбольше даты окончания периода";
                    Status.Visibility = Visibility.Visible;
                    resume = false;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "PlannedDowntime");
                q.Request.SetParam("Action", "Save");
                q.Request.SetParam("DT_START", StartDowntime.Text);
                q.Request.SetParam("DT_END", EndDowntime.Text);
                q.Request.SetParam("DOPL_ID", downtimeId.ToString());

                await Task.Run(() => q.DoQuery());

                if (q.Answer.Status == 0)
                {
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverName = "DowntimeGrid",
                        Action = "downtime_grid_refresh"
                    });

                    Close();
                } 
                else
                {
                    Status.Visibility = Visibility.Visible;
                    Status.Text = "Ошибка при сохранении";
                }
            }
        }

        public void Edit(int downtimeId, string startDate, string endDate)
        {
            this.downtimeId = downtimeId;
            this.oldValueStartDate = startDate;
            this.oldValueEndDate = endDate;

            Width = 270;
            Height = 135;

            StartDowntime.Text = startDate;
            EndDowntime.Text = endDate;

            var windowParameters = new Dictionary<string, string>
            {
                { "no_resize", "1" },
                { "no_maximize", "1" },
                { "no_minimize", "1" },
                { "center_screen", "1" }
            };

            Central.WM.FrameMode = FrameMode;
            var frameName = GetFrameName();
            var frameTitle = FrameTitle;

            if (OnGetFrameTitle != null)
            {
                frameTitle = OnGetFrameTitle.Invoke();
                frameName = GetFrameName();
            }

            Central.WM.Show(frameName, frameTitle, true, "add", this, p: windowParameters);
        }
    }
}
