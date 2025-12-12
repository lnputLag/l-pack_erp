using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
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

namespace Client.Interfaces.Service
{
    /// <summary>
    /// управление воротами на литой таре
    /// </summary>
    /// <author>greshnyh_ni</author>
    public partial class GateManagement : UserControl
    {
        public GateManagement()
        {
            InitializeComponent();
            ProcessPermissions();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            TabName = "GateManagement";
            ControlPlcConnectionClient = null;

            Form = null;
            FormInit();
            CarGridInit();

            ButtonTimer = new Timeout(
             3,
             () =>
             {
                 ButtonPush();
             },
             true,
             false
         );
            ButtonTimer.Finish();

        }

        #region Common

        public FormHelper Form { get; set; }

        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        public List<DataGridHelperColumn> Columns { get; private set; }


        /// <summary>
        /// Таймер задержки повторного нажатия кнопки
        /// </summary>
        public Timeout ButtonTimer { get; set; }

        /// <summary>
        /// Массив кнопок для их сброса в первоначальное состояние
        /// </summary>
        private bool[] buttons = new bool[2];

        private WebClient ControlPlcConnectionClient { get; set; }

        /// <summary>
        ///  Ip адрес ворот
        /// </summary>
       string IpGate =  "172.17.37.21";
      //  string IpGate = "192.168.21.84";

        #endregion

        /// <summary>
        /// проверка доступа
        /// </summary>
        public void ProcessPermissions()
        {
            string role = "[erp]molded_contr_security";

            var mode = Central.Navigator.GetRoleLevel(role);
            var userAccessMode = mode;

            switch (mode)
            {
                case Role.AccessMode.Special:
                    {
                    }
                    break;

                case Role.AccessMode.FullAccess:
                    {
                        OpenGateButton.IsEnabled = true;
                        CloseGateButton.IsEnabled = true;
                    }
                    break;

                case Role.AccessMode.ReadOnly:
                    {
                        OpenGateButton.IsEnabled = false;
                        CloseGateButton.IsEnabled = false;
                    }
                    break;
            }

        }


        /// <summary>
        // инициализация компонентов формы
        /// </summary>
        public void FormInit()
        {
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Destroy()
        {
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            ButtonTimer.Finish();
            CarsGrid.Destruct();
        }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {

        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="obj">сообщение</param>
        private void ProcessMessages(ItemMessage obj)
        {
            if (obj.ReceiverGroup.IndexOf("Service") > -1)
            {
                if (obj.ReceiverName.IndexOf(TabName) > -1)
                {
                    switch (obj.Action)
                    {
                        case "Refresh":
                            CarsGrid.LoadItems();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// инициализация таблицы со списком машин
        /// </summary>
        private void CarGridInit()
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
                        ColumnType=ColumnTypeRef.String,
                        Width = 70,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Машина",
                        Path="CAR",
                        Doc="Машина",
                        ColumnType=ColumnTypeRef.String,
                        Width = 240,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Телефон",
                        Path="PHONE",
                        Doc="Телефон",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth=100,
                        Width = 100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Поставщик/покупатель",
                        Path="NAME",
                        Doc="Поставщик",
                        ColumnType=ColumnTypeRef.String,
                        Width = 200,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание",
                        Path="NOTE",
                        Doc="Примечание",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=250,
                        MaxWidth=540,
                    },
                    new DataGridHelperColumn()
                    {
                    Header="",
                    Path="",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                    },
                };

                // CarsGrid.SelectItemMode = 2;
                //CarsGrid.SearchText = CarSearchBox;
                CarsGrid.PrimaryKey = "ID";
                CarsGrid.SetColumns(columns);

                CarsGrid.AutoUpdateInterval = 60;
                CarsGrid.Label = "Cars";
                CarsGrid.Init();
                //данные грида
                CarsGrid.OnLoadItems = CarsGridLoad;
                CarsGrid.Run();
            }

        }


        /// <summary>
        /// Загрузка данных из БД в таблицу списка машин
        /// </summary>
        private async void CarsGridLoad()
        {
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Transport");
                q.Request.SetParam("Object", "Access");
                q.Request.SetParam("Action", "ListCarMoldedGate");

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


        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            CarsGrid.LoadItems();
        }

        private void ButtonPush()
        {
            for (int i = 0; i < 2; i++)
            {
                if (!buttons[i])
                {
                    buttons[i] = true;
                    switch (i)
                    {
                        //Открыть
                        case 0:
                            {
                                OpenGateButton.IsEnabled = true;
                                DoSendCommandHttp(IpGate, 1, 0);
                            }
                            break;

                        //Pfrhsnm
                        case 1:
                            {
                                CloseGateButton.IsEnabled = true;
                                DoSendCommandHttp(IpGate, 2, 0);
                            }
                            break;
                    }
                    ButtonTimer.Finish();
                }
            }
        }

        /// <summary>
        /// открытие ворот
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenGateButton_Click(object sender, RoutedEventArgs e)
        {
            buttons[0] = false;
            OpenGateButton.IsEnabled = false;
            OpenGate();
            ButtonTimer.Run();
        }

        /// <summary>
        ///  закрыть ворота
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseGateButton_Click(object sender, RoutedEventArgs e)
        {
            buttons[1] = false;
            CloseGateButton.IsEnabled = false;
            CloseGate();
            ButtonTimer.Run();
        }

        /// <summary>
        ///  Открыть ворота 
        ///  реле 2 
        /// http://192.168.21.84/cmd.cgi?cmd=REL,1,1
        /// </summary>
        private void OpenGate()
        {
            DoSendCommandHttp(IpGate, 2, 1);
        }

        /// <summary>
        /// Закрыть ворота
        ///  реле 1
        /// </summary>
        private void CloseGate()
        {
            DoSendCommandHttp(IpGate, 1, 1);
        }

        /// <summary>
        /// запрос к плате по HTTP
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string SendHttpRequest(string ipLaurent, string url)
        {
            var result = "";
            var report = "";
            var status = 0;
            var profiler = new Profiler("SendHttpRequest");

            bool repeat = true;
            int step = 1;
            int maxAttempts = 2;
            while (repeat)
            {
                {
                    try
                    {
                        {
                            if (ControlPlcConnectionClient == null)
                            {
                                ControlPlcConnectionClient = new WebClient();
                                NetworkCredential myCreds = new NetworkCredential("admin", "Laurent");
                                ControlPlcConnectionClient.Credentials = myCreds;
                                report = $"{report} ({step})client_created";
                            }
                        }

                        {
                            if (ControlPlcConnectionClient != null)
                            {
                                //ControlPlcConnectionClient.Timeout = 1000;
                                result = ControlPlcConnectionClient.DownloadString(url);
                                report = $"{report} ({step})client_sent";
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (ControlPlcConnectionClient != null)
                        {
                            ControlPlcConnectionClient.Dispose();
                        }
                        ControlPlcConnectionClient = null;
                    //    LogMsg($"SendHttpRequest error. Ip=[{ipLaurent}] url=[{url}] description=[{e.ToString()}]");
                        report = $"{report} ({step})client_error";
                        status = 1;
                        //    ValueReportStr = $"SendHttpRequest error. Ip=[{ipLaurent}] url=[{url}] description=[{e.ToString()}].";
                    }
                }

                if (!result.IsNullOrEmpty())
                {
                    repeat = false;
                    report = $"{report} ({step})result_ok";
                }
                else
                {
                    report = $"{report} ({step})result_empty";
                    status = 2;
                }

                step++;
                if (step > maxAttempts)
                {
                    repeat = false;
                }
            }

            var time = (int)profiler.GetDelta();
         //   LogMsg($"        SendHttpRequest. machine=[{ipLaurent}] url=[{url}] status=[{status}] time=[{time}]");
         //   LogMsg($"            report=[{report}]");

            return result;
        }


        /// </summary>
        /// <param name="machine"></param>
        /// <param name="reelNumber"></param>
        /// <param name="state"></param>
        private void DoSendCommandHttp(string ipLaurent, int reelNumber, int state)
        {
//            LogMsg($"        DoSendCommandHTTP. machine=[{ipLaurent}] reelNumber=[{reelNumber}] state=[{state}]");

            // http://192.168.21.84/cmd.cgi?cmd=REL,4,1

            var url = $"http://{ipLaurent}/cmd.cgi?cmd=REL,{reelNumber},{state}";

            var report = "";

            bool repeat = true;
            int step = 1;
            int maxAttempts = 3;
            while (repeat)
            {
                var content = SendHttpRequest(ipLaurent, url);
                var checkResult = false;

                if (!content.IsNullOrEmpty())
                {
                    checkResult = CheckRelayState(ipLaurent, reelNumber);
                    if (checkResult)
                    {
                        repeat = false;
                    }
                }

                report = $"{report} *) step=[{step}] result=[{checkResult}] content=[{content}]";

                step++;
                if (step >= maxAttempts)
                {
                    repeat = false;
                }
            }

//            LogMsg($"        DoSendCommandHTTP. report=[{report}]");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="machine"></param>
        /// <param name="reelNumber"></param>
        /// <returns></returns>
        private bool CheckRelayState(string ipLaurent, int reelNumber)
        {

            bool result = true;

            return result;
        }

    }
}
