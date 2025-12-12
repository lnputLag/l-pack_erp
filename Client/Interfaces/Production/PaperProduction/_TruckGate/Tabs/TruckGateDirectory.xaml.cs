using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace Client.Interfaces.Production.PaperProduction
{
    /// <summary>
    /// Интерфейс для отображения состояния датчиков
    /// 
    /// </summary>
    public partial class TruckGateDirectory : UserControl
    {
        public TruckGateDirectory()
        {
            InitializeComponent();

            CarNumbersGridInit();
            PositionGridInit();
            ScaleGridInit();
            BarrierGridInit();
            PanelGridInit();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
        }

        private void PanelGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID",
                        Doc="ИД камеры",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Сообщение",
                        Path="TEXT",
                        Doc="Сообщение",
                        ColumnType=ColumnTypeRef.String,
                        Width = 140,
                    },
                };

                PanelGrid.SelectItemMode = 2;

                PanelGrid.SearchText = CarSearchBox;

                PanelGrid.PrimaryKey = "ID";
                PanelGrid.SetColumns(columns);

                PanelGrid.Label = "PanelGrid";
                PanelGrid.Init();



                //данные грида
                PanelGrid.OnLoadItems = PanelGridLoad;

                PanelGrid.Run();
            }
        }

        private async void PanelGridLoad()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Devices");
            q.Request.SetParam("Object", "Panel");
            q.Request.SetParam("Action", "List");

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
                    PanelGrid.UpdateItems(ds);
                }
            }
        }

        private void BarrierGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID",
                        Doc="ИД камеры",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Команда",
                        Path="CMD",
                        Doc="Команда",
                        ColumnType=ColumnTypeRef.String,
                        Width = 140,
                    },
                };

                BarrierGrid.SelectItemMode = 2;

                BarrierGrid.SearchText = CarSearchBox;

                BarrierGrid.PrimaryKey = "ID";
                BarrierGrid.SetColumns(columns);

                BarrierGrid.Label = "BarrierGrid";
                BarrierGrid.Init();



                //данные грида
                BarrierGrid.OnLoadItems = BarrierGridLoad;

                BarrierGrid.Run();
            }
        }

        private async void BarrierGridLoad()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Devices");
            q.Request.SetParam("Object", "Barrier");
            q.Request.SetParam("Action", "List");

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
                    BarrierGrid.UpdateItems(ds);
                }
            }
        }

        private void ScaleGridInit()
        {

            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID",
                        Doc="ИД камеры",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес",
                        Path="WEIGT",
                        Doc="Вес",
                        ColumnType=ColumnTypeRef.String,
                        Width = 140,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Время",
                        Path="TICKS",
                        Doc="Время",
                        ColumnType=ColumnTypeRef.String,
                        Width = 130,
                    },
                };

                ScaleGrid.SelectItemMode = 2;

                ScaleGrid.SearchText = CarSearchBox;

                ScaleGrid.PrimaryKey = "ID_POS";
                ScaleGrid.SetColumns(columns);

                ScaleGrid.Label = "POSITINS";
                ScaleGrid.Init();

               

                //данные грида
                ScaleGrid.OnLoadItems = ScalesGridLoad;

                ScaleGrid.Run();
            }
        }

        private async void ScalesGridLoad()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Devices");
            q.Request.SetParam("Object", "Scales");
            q.Request.SetParam("Action", "List");

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
                    ScaleGrid.UpdateItems(ds);
                }
            }
        }

        private void PositionGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID",
                        Doc="ИД камеры",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Код положения",
                        Path="DATA",
                        Doc="Код положения",
                        ColumnType=ColumnTypeRef.String,
                        Width = 140,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Время",
                        Path="DATE",
                        Doc="Время",
                        ColumnType=ColumnTypeRef.String,
                        Width = 130,
                    },
                };

                PositionGrid.SelectItemMode = 2;

                PositionGrid.SearchText = CarSearchBox;

                PositionGrid.PrimaryKey = "ID_POS";
                PositionGrid.SetColumns(columns);

                PositionGrid.Label = "POSITINS";
                PositionGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                PositionGrid.OnSelectItem = selectedItem =>
                {
                    positionSelectedItem = selectedItem;
                };

                //данные грида
                PositionGrid.OnLoadItems = PositionGridLoad;

                PositionGrid.Run();
            }
        }

        private async void PositionGridLoad()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Devices");
            q.Request.SetParam("Object", "PositionSensor");
            q.Request.SetParam("Action", "List");

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
                    PositionGrid.UpdateItems(ds);
                }
            }
        }

        private Dictionary<string, string> carGridSelectedItem;
        private Dictionary<string, string> positionSelectedItem;

        private void CarNumbersGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID",
                        Doc="ИД камеры",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер автомобиля",
                        Path="NUMBER",
                        Doc="Номер автомобиля",
                        ColumnType=ColumnTypeRef.String,
                        Width = 140,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Время",
                        Path="DATE",
                        Doc="Время",
                        ColumnType=ColumnTypeRef.String,
                        Width = 130,
                    },
                };

                CarsNumberGrid.SelectItemMode = 2;

                CarsNumberGrid.SearchText = CarSearchBox;

                CarsNumberGrid.PrimaryKey = "ID_CAR";
                CarsNumberGrid.SetColumns(columns);

                CarsNumberGrid.Label = "CarNumbers";
                CarsNumberGrid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                CarsNumberGrid.OnSelectItem = selectedItem =>
                {
                    carGridSelectedItem = selectedItem;
                };

                //данные грида
                CarsNumberGrid.OnLoadItems = CarNumbersGridLoad;
                //WarehouseGrid.OnFilterItems = RoleGridFilterItems;

                CarsNumberGrid.Run();
            }

        }

        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("TGate") > -1)
            {
               
                
            }
        }

        private async void CarNumbersGridLoad()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Devices");
            q.Request.SetParam("Object", "Camera");
            q.Request.SetParam("Action", "List");

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
                    CarsNumberGrid.UpdateItems(ds);
                }
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RefresCarsNumberButton_Click(object sender, RoutedEventArgs e)
        {
            CarsNumberGrid.LoadItems();
            PositionGrid.LoadItems();
            ScaleGrid.LoadItems();
            PanelGrid.LoadItems();
            BarrierGrid.LoadItems();
        }
    }
}
