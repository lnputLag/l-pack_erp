using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Interaction logic for CorrugatorMachineReportIndicators.xaml
    /// </summary>
    public partial class CorrugatorMachineReportIndicators : ControlBase
    {
        public CorrugatorMachineReportIndicators()
        {
            InitializeComponent();

            ControlTitle = "Показатели ГА";

            OnLoad = () =>
            {
                InitializeComponent();

                SetDefaults();
                GridInit();
            };

            OnUnload = () =>
            {
                Grid.Destruct();

            };

            //OnMessage = (ItemMessage msg) =>
            //{
            //    if (msg.ReceiverGroup == "PreproductionSample")
            //    {
            //        if (msg.ReceiverName == ControlName)
            //        {
            //            ReceivedData.Clear();
            //            if (msg.ContextObject != null)
            //            {
            //                ReceivedData = (Dictionary<string, string>)msg.ContextObject;
            //            }
            //            ProcessCommand(msg.Action);
            //        }
            //    }
            //};

            OnFocusGot = () =>
            {
                Grid.ItemsAutoUpdate = true;
                Grid.Run();
            };

            OnFocusLost = () =>
            {
                Grid.ItemsAutoUpdate = false;
            };

            OnKeyPressed = (e) =>
            {
                if (!e.Handled)
                {
                    switch (e.Key)
                    {
                        case Key.F1:
                            ProcessCommand("help");
                            e.Handled = true;
                            break;
                        case Key.F5:
                            Grid.LoadItems();
                            e.Handled = true;
                            break;

                        case Key.Home:
                            Grid.SetSelectToFirstRow();
                            e.Handled = true;
                            break;

                        case Key.End:
                            Grid.SetSelectToLastRow();
                            e.Handled = true;
                            break;
                    }
                }

                if (!e.Handled)
                {
                    Grid.ProcessKeyboardEvents(e);
                }
            };
        }

        private void ProcessCommand(string v)
        {
            
        }

        private async void LoadItems()
        {
            var p = new Dictionary<string, string>
            {
                ["ID_ST"] = Machines.SelectedItem.Key.ToInt().ToString(),
                ["FROM_DATE"] = FromDate.Text,
                ["TO_DATE"] = ToDate.Text,
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Report");
            q.Request.SetParam("Action", "ReportIndicators");

            q.Request.SetParams(p);

         
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
                    Grid.UpdateItems(ds);
                }
            }

        }

        private void GridInit()
        {
            
        }

        private void SetDefaults()
        {
            var list = new Dictionary<string, string>();
            list.Add("0", "Все");
            list.Add("2", "ГА-1");
            list.Add("21", "ГА-2");
            list.Add("22", "ГА-3");
            Machines.Items = list;
            Machines.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");

            ShowButton.Style = (Style)ShowButton.TryFindResource("Button");

            var start = DateTime.Now;

            int hour = start.Hour;

            if (hour > 12)
            {
                start = new DateTime(start.Year, start.Month, start.Day, 20, 0, 0);
            }
            else
            {
                start = new DateTime(start.Year, start.Month, start.Day, 8, 0, 0);
            }

            FromDate.EditValue = start;
            ToDate.EditValue = start.AddHours(12);
        }

        private void Types_SelectedItemChanged(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {

        }

        private void LeftShiftButton_Click(object sender, RoutedEventArgs e)
        {
            if (FromDate.EditValue is DateTime start)
            {
                start = start.AddHours(-12);
                int hour = start.Hour;

                if (hour > 12)
                {
                    start = new DateTime(start.Year, start.Month, start.Day, 20, 0, 0);
                }
                else
                {
                    start = new DateTime(start.Year, start.Month, start.Day, 8, 0, 0);
                }

                FromDate.EditValue = start;
                ToDate.EditValue = start.AddHours(12);

                LoadItems();
            }
        }

        

        private void RightShiftButton_Click(object sender, RoutedEventArgs e)
        {
            if (FromDate.EditValue is DateTime start)
            {
                start = start.AddHours(12);
                int hour = start.Hour;

                if (hour > 12)
                {
                    start = new DateTime(start.Year, start.Month, start.Day, 20, 0, 0);
                }
                else
                {
                    start = new DateTime(start.Year, start.Month, start.Day, 8, 0, 0);
                }

                FromDate.EditValue = start;
                ToDate.EditValue = start.AddHours(12);

                LoadItems();
            }
        }
    }
}
