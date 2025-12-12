using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
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
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.PaperProduction
{
    /// <summary>
    /// Interaction logic for TruckGateConfiguration.xaml
    /// </summary>
    public partial class TruckGateConfiguration : UserControl
    {
        private Dictionary<string, string> gateSelectedItem;
        private Dictionary<string, string> deviceSelectedItem;

        public TruckGateConfiguration()
        {
            InitializeComponent();

            deviceConfig = new DeviceConfig();

            GateGridInit();
            DeviceGridInit();
            CarGridInit();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
        }

        private void CarGridInit()
        {
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Тип",
                        Path="TYPE",
                        Doc="Тип",
                        ColumnType=ColumnTypeRef.String,
                        Width = 80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер",
                        Path="NUMBER",
                        Doc="Номер",
                        ColumnType=ColumnTypeRef.String,
                        Width = 80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Описание",
                        Path="DESCRIPTION",
                        Doc="Описание",
                        ColumnType=ColumnTypeRef.String,
                        Width = 140,
                    },
                };

                CarsGrid.SelectItemMode = 2;

                //CarsGrid.SearchText = CarSearchBox;

                CarsGrid.PrimaryKey = "ID";
                CarsGrid.SetColumns(columns);

                CarsGrid.Label = "Cars";
                CarsGrid.Init();

                //данные грида
                CarsGrid.OnLoadItems = CarsGridLoad;

                CarsGrid.Run();
            }

        }

        private async void CarsGridLoad()
        {
            if (gateSelectedItem != null)
            {
                var p = new Dictionary<string, string>();
                {
                    p.Add("ID", gateSelectedItem.CheckGet("ID"));
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Transport");
                q.Request.SetParam("Object", "Access");
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
                        CarsGrid.UpdateItems(ds);
                    }
                }
            }
        }

        private void DeviceGridInit()
        {
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID",
                        Doc="ИД",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="NAME",
                        Doc="Наименование",
                        ColumnType=ColumnTypeRef.String,
                        Width = 140,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тип",
                        Path="TYPENAME",
                        Doc="Тип Оборудования",
                        ColumnType=ColumnTypeRef.String,
                        Width = 140,
                    },
                };

                DeviceGrid.SelectItemMode = 2;

                DeviceGrid.SearchText = CarSearchBox;

                DeviceGrid.PrimaryKey = "ID";
                DeviceGrid.SetColumns(columns);

                DeviceGrid.Label = "Devices";
                DeviceGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                DeviceGrid.OnSelectItem = selectedItem =>
                {
                    UpdateDeviceAction(selectedItem);
                };

                //данные грида
                DeviceGrid.OnLoadItems = DeviceGridLoad;

                DeviceGrid.Run();
            }
        }

        private void DeviceGridLoad()
        {
            if(gateSelectedItem!=null)
            {
                int idgate = gateSelectedItem.CheckGet("ID").ToInt();

                DeviceGrid.UpdateItems(deviceConfig.GetGateDevices(idgate));
            }
        }

        private void UpdateDeviceAction(Dictionary<string, string> selectedItem)
        {
            OpenGateButton.IsEnabled = false;
            CloseGateButton.IsEnabled = false;
            ForceOpenGateButton.IsEnabled = false;



            ScaleButton.IsEnabled = false;
            TabloButton.IsEnabled = false;
            ScaleClearButton.IsEnabled = false;

            deviceSelectedItem = selectedItem;

            if(deviceSelectedItem!=null)
            {
                int type = deviceSelectedItem.CheckGet("TYPE").ToInt();

                //Unknow = 0, Barrier = 1, Camera = 2, Panel = 3, Laurent = 4, Scales = 5
                if (type == 1)
                {
                    OpenGateButton.IsEnabled = true;
                    CloseGateButton.IsEnabled = true;
                    ForceOpenGateButton.IsEnabled = true;
                }
                else if(type == 3)
                {
                    TabloButton.IsEnabled= true;
                }
                else if(type==5)
                {
                    ScaleButton.IsEnabled = true;
                    ScaleClearButton.IsEnabled=true;
                }

            }
        }

        private DeviceConfig deviceConfig { get; set; }

        private void GateGridInit()
        {
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID",
                        Doc="ИД",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="NAME",
                        Doc="Наименование",
                        ColumnType=ColumnTypeRef.String,
                        Width = 140,
                    },
                };

                GateGrid.SelectItemMode = 2;

                GateGrid.SearchText = CarSearchBox;

                GateGrid.PrimaryKey = "ID";
                GateGrid.SetColumns(columns);

                GateGrid.Label = "GATES";
                GateGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                GateGrid.OnSelectItem = selectedItem =>
                {
                    UpdateGateAction(selectedItem);
                    
                };

                //данные грида
                GateGrid.OnLoadItems = GateGridLoad;

                GateGrid.Run();
            }
        }

        private void UpdateGateAction(Dictionary<string, string> selectedItem)
        {
            gateSelectedItem = selectedItem;
            DeviceGrid.LoadItems();
            CarsGrid.LoadItems();
        }

        private void GateGridLoad()
        {
            GateGrid.UpdateItems(deviceConfig.GetGatesList());
        }

        private void ProcessMessages(ItemMessage obj)
        {
            //throw new NotImplementedException();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void OpenGateButton_Click(object sender, RoutedEventArgs e)
        {
            if(deviceSelectedItem!=null)
            {
                string idBarrier = deviceSelectedItem.CheckGet("ID");

                Dictionary<string, string> p = new Dictionary<string, string>();
                p.CheckAdd("ID", idBarrier);
                p.CheckAdd("CMD", "1");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Devices");
                q.Request.SetParam("Object", "Barrier");
                q.Request.SetParam("Action", "Insert");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {

                }
            }
        }

        private void ScaleButton_Click(object sender, RoutedEventArgs e)
        {
            if (deviceSelectedItem != null)
            {
                string idScales = deviceSelectedItem.CheckGet("ID");

                Dictionary<string, string> p = new Dictionary<string, string>();
                p.CheckAdd("ID", idScales);
                p.CheckAdd("WEIGT", "-1");
                p.CheckAdd("TICKS", DateTime.Now.Ticks.ToString()) ;

                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Devices");
                    q.Request.SetParam("Object", "Scales");
                    q.Request.SetParam("Action", "Insert");
                    q.Request.SetParams(p);

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {

                    }
                }

               
            }

        }

        private void ScaleClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (deviceSelectedItem != null)
            {
                string idScales = deviceSelectedItem.CheckGet("ID");

                Dictionary<string, string> p = new Dictionary<string, string>();
                p.CheckAdd("ID", idScales);
                p.CheckAdd("WEIGT", "-2");
                p.CheckAdd("TICKS", DateTime.Now.Ticks.ToString());

                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Devices");
                    q.Request.SetParam("Object", "Scales");
                    q.Request.SetParam("Action", "Insert");
                    q.Request.SetParams(p);

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {

                    }
                }


            }

        }

        private void TabloButton_Click(object sender, RoutedEventArgs e)
        {
            if (deviceSelectedItem != null)
            {
                string idScales = deviceSelectedItem.CheckGet("ID");

                Dictionary<string, string> p = new Dictionary<string, string>();
                p.CheckAdd("ID", idScales);
                p.CheckAdd("TEXT", TabloTextBox.Text);
               
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Devices");
                    q.Request.SetParam("Object", "Panel");
                    q.Request.SetParam("Action", "Insert");
                    q.Request.SetParams(p);

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {

                    }
                }
            }
        }

        private void CloseGateButton_Click(object sender, RoutedEventArgs e)
        {
            if (deviceSelectedItem != null)
            {
                string idBarrier = deviceSelectedItem.CheckGet("ID");

                Dictionary<string, string> p = new Dictionary<string, string>();
                p.CheckAdd("ID", idBarrier);
                p.CheckAdd("CMD", "3");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Devices");
                q.Request.SetParam("Object", "Barrier");
                q.Request.SetParam("Action", "Insert");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {

                }
            }

        }

        private void ForceOpenGateButton_Click(object sender, RoutedEventArgs e)
        {
            if (deviceSelectedItem != null)
            {
                string idBarrier = deviceSelectedItem.CheckGet("ID");

                Dictionary<string, string> p = new Dictionary<string, string>();
                p.CheckAdd("ID", idBarrier);
                p.CheckAdd("CMD", "2");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Devices");
                q.Request.SetParam("Object", "Barrier");
                q.Request.SetParam("Action", "Insert");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {

                }
            }

        }
    }
}
