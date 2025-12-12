using Client.Common;
using Client.Interfaces.Production;
using Client.Interfaces.Production.CreatingTasks;
using Client.Interfaces.Production.ProcessingMachines;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Client.Interfaces.Debug
{
    public partial class LinkMapView : UserControl
    {
        public LinkMapView()
        {
            InitializeComponent();

            LogUpdateTimer = new Common.Timeout(
                   60,
                   () => {
                       DoLogUpdate();
                   },
                   true,
                   false
                );

            LogUpdateTimer.SetIntervalMs(1000);
            LogUpdateTimer.Run();

        }
        private Common.Timeout LogUpdateTimer { get; set; }

        private void Shipments_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/shipments/control";
            Central.Navigator.ProcessURL(url);
        }

        private void ShipmentsList_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/shipments/control/list";
            Central.Navigator.ProcessURL(url);
        }

        private void ShipmentsPlan_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/shipments/control/plan";
            Central.Navigator.ProcessURL(url);
        }

        private void ShipmentsMonitor_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/shipments/control/monitor";
            Central.Navigator.ProcessURL(url);
        }

        private void ShipmentsReport_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/shipments/control/report";
            Central.Navigator.ProcessURL(url);
        }

        private void ShipmentsEquipment_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/shipments/control/equipment";
            Central.Navigator.ProcessURL(url);
        }

        private void ShipmentsEquipmentSamples_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/shipments/control/equipment/samples";
            Central.Navigator.ProcessURL(url);
        }

        private void ShipmentsEquipmentClishe_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/shipments/control/equipment/clishe";
            Central.Navigator.ProcessURL(url);
        }

        private void ShipmentsEquipmentShtanzforms_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/shipments/control/equipment/shtanzforms";
            Central.Navigator.ProcessURL(url);
        }

      
        private void Pt_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/production/production_tasks";
            Central.Navigator.ProcessURL(url);
        }


        private void StockPallets2Button_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/stock/pallets";
            Central.Navigator.ProcessURL(url);
        }

        private void StockPalletsInButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/stock/pallets/receipt";
            Central.Navigator.ProcessURL(url);
        }

        private void StockPalletsReturnableButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/stock/pallets/returnable";
            Central.Navigator.ProcessURL(url);
        }

        private void StockPalletsOutButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/stock/pallets/expenditure";
            Central.Navigator.ProcessURL(url);
        }

        private void StockPalletsButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/stock/pallets";
            Central.Navigator.ProcessURL(url);
        }

        private void StockOperationsProductsButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/stock/stock_operations/products";
            Central.Navigator.ProcessURL(url);
        }

        private void StockOperationsWriteOffButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/stock/stock_operations/writeoff";
            Central.Navigator.ProcessURL(url);
        }

        private void StockOperationsEquipmentButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/stock/stock_operations/complectation";
            Central.Navigator.ProcessURL(url);
        }

        private void StockOperationsButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/stock/stock_operations";
            Central.Navigator.ProcessURL(url);
        }

        private void StockForkliftdriversButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/stock/forkliftdrivers";
            Central.Navigator.ProcessURL(url);
        }

        private void StockForkliftdriversLogButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/stock/forkliftdrivers/log";
            Central.Navigator.ProcessURL(url);
        }

        private void StockForkliftdriversListButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/stock/forkliftdrivers/list";
            Central.Navigator.ProcessURL(url);
        }

        private void ProductionTasksCreate_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            //FIXME:
            var i = new ProductionTaskCMCreateInterface();
        }

        private void ProductionRollsPt_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            //FIXME:
            var i = new RollRegistrationInterface();
        }

        private void ProductionPtProcessing_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            //FIXME:
            var i = new ProductionTaskPRDiagramInterface();
        }

        private void ShipmentsEdm_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/shipments/edm";
            Central.Navigator.ProcessURL(url);
        }

        private void ShipmentsControl_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/shipments/control";
            Central.Navigator.ProcessURL(url);
        }

        private void Service_Click(object sender,System.Windows.RoutedEventArgs e)
        {

        }

        private void ServiceAccounts_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/service/users_ctl";
            Central.Navigator.ProcessURL(url);
        }

        private void ServiceMessages_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/service/messages";
            Central.Navigator.ProcessURL(url);
        }

        private void DocUpdate_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/documentation/update";
            Central.Navigator.ProcessURL(url);
        }

        private void DocManual_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/documentation/manual";
            Central.Navigator.ProcessURL(url);
        }

        private void DocAbout_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/documentation/about";
            Central.Navigator.ProcessURL(url);
        }

        private void DebugTools_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/debug/tools";
            Central.Navigator.ProcessURL(url);
        }

        private void DebugToolsMap_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/debug/tools/map";
            Central.Navigator.ProcessURL(url);
        }

        private void DebugToolsQueries_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/debug/tools/queries";
            Central.Navigator.ProcessURL(url);
        }

        private void DebugToolsLog_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/debug/tools/log";
            Central.Navigator.ProcessURL(url);
        }

        private void DocVersions_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            string url = "l-pack://l-pack_erp/documentation/versions";
            Central.Navigator.ProcessURL(url);
        }

        private void DebugSizeF11_Click(object sender,System.Windows.RoutedEventArgs e)
        {

        }

        private void DebugSizeMax_Click(object sender,System.Windows.RoutedEventArgs e)
        {

        }

        private void SetModeDebugButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="Main",
                ReceiverName = "MainWindow",
                SenderName = "LinkMap",
                Action = "SetMode",
                Message="Debug",
            });
        }

        private void SetModeReleaseButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="Main",
                ReceiverName = "MainWindow",
                SenderName = "LinkMap",
                Action = "SetMode",
                Message="Release",
            });
        }

        private void CloseButon_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Central.WM.Close($"Debug");
        }

        private void TabsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Central.WM.DebugShow(1);
        }

        private void TabsButton2_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Central.WM.DebugShow(2);
        }

        private void Tab1Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
             var b=(Button)sender;
             if (b!=null)
             {
                var t=b.Tag.ToString();
                Central.WM.SetActive(t);              
             }
        }

        private void DoHopButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Central.LPackClient.ChangeServer();
        }

        private void GCButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            GC.Collect();
        }

        private void DoLogUpdate()
        {
            try
            {
                if(Central.Parameters != null)
                {
                    var s = "";

                   
                    s = s.Append($"HostUserId=[{Central.User.HostUserId.ToString()}]", true);
                    s = s.Append($"HopMode=[{Central.Parameters.HopMode.ToString()}]", true);
                    s = s.Append($"RequestAttemptsFixMode=[{Central.Parameters.RequestAttemptsFixMode.ToString()}]", true);
                    s = s.Append($"HopControlIntervalSlow=[{Central.Parameters.HopControlIntervalSlow.ToString()}]", true);
                    s = s.Append($"HopControlIntervalFast=[{Central.Parameters.HopControlIntervalFast.ToString()}]", true);
                    s = s.Append($" ", true);
                    s = s.Append($"DoSystemRequestFaultLimit=[{Central.Parameters.DoSystemRequestFaultLimit.ToString()}]", true);
                    s = s.Append($"QueryRepeatLimitTime=[{Central.Parameters.QueryRepeatLimitTime.ToString()}]", true);
                    s = s.Append($"QueryRepeatDelay=[{Central.Parameters.QueryRepeatDelay.ToString()}]", true);
                    s = s.Append($"WaitRepeatLimitTime=[{Central.Parameters.WaitRepeatLimitTime.ToString()}]", true);
                    s = s.Append($"WaitRepeatDelay=[{Central.Parameters.WaitRepeatDelay.ToString()}]", true);
                    s = s.Append($" ", true);
                    s = s.Append($"StatusBarUpdateInterval=[{Central.Parameters.StatusBarUpdateInterval.ToString()}]", true);
                    s = s.Append($"PollInterval=[{Central.Parameters.PollInterval.ToString()}]", true);
                    s = s.Append($" ", true);
                    s = s.Append($"RequestGridTimeout=[{Central.Parameters.RequestGridTimeout.ToString()}]", true);
                    s = s.Append($"RequestGridAttempts=[{Central.Parameters.RequestAttemptsDefault.ToString()}]", true);
                    s = s.Append($"RequestMinimumTimeout=[{Central.Parameters.RequestTimeoutMin.ToString()}]", true);
                    s = s.Append($"RequestDeaultTimeout=[{Central.Parameters.RequestTimeoutDefault.ToString()}]", true);
                    s = s.Append($"RequestSystemTimeout=[{Central.Parameters.RequestTimeoutSystem.ToString()}]", true);
                    s = s.Append($"RequestGridCuttingTimeout=[{Central.Parameters.RequestTimeoutMax.ToString()}]", true);
                    
                    LogTimings.Text = s;
                }

                {
                    var s = "";
                    
                    s = s.Append($"ConnectingTimeout=[{Central.LPackClient.ConnectingTimeout.ToString()}]", true);
                    s = s.Append($"Sys Interval=[{Central.Parameters.HopControlInterval.ToString()}]", true);
                    s = s.Append($"ConnectingHopping=[{Central.LPackClient.ConnectingHopping.ToString()}]", true);
                    s = s.Append($"ConnectingInProgress=[{Central.LPackClient.ConnectingInProgress.ToString()}]", true);
                    s = s.Append($"Online=[{Central.LPackClient.OnlineMode.ToString()}]", true);
                    s = s.Append($"Server=[{Central.LPackClient.CurrentConnection.Host.ToString()}]", true);
                    s = s.Append($"Sid=[{Central.LPackClient.Session.Sid.ToString()}]", true);
                    s = s.Append($"Token=[{Central.LPackClient.Session.Token.ToString()}]", true);
                    
                    LogConnection.Text = s;
                }

                {
                    LogStatus.Text = Central.LPackClient.CurrentConnection.DebugStatusString.ToString();
                    LogStatusHistory.Text= Central.LPackClient.CurrentConnection.DebugStatusStringLog.ToString();
                    LogStatusHistory.ScrollToEnd();
                }

                {
                    LogStatus2.Text = Central.LPackClient.CurrentConnection.DebugStatusString2.ToString();
                    LogStatus2History.Text = Central.LPackClient.CurrentConnection.DebugStatusString2Log.ToString();
                    LogStatus2History.ScrollToEnd();
                }
            }
            catch(Exception e)
            {

            }

        }

        private void NewWinButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var p = new Dictionary<string, string>();
            p.CheckAdd("width","900");
            p.CheckAdd("height", "720");
            p.CheckAdd("no_modal", "1");
            Central.WM.FrameMode = 2;
            Central.WM.Show("tools", $"Инструменты", true, "main", this, "top", p);
            
        }
    }
}
