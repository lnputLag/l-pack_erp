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


namespace Client.Interfaces.Service
{
    /// <summary>
    /// Ожидаемые автомобили, автомобили которым доступ разрешен
    /// </summary>
    /// <author>eletskikh_ya</author>
    /// <author>greshnyh_ni</author>
    public partial class ExpectedCarList : UserControl
    {
        public ExpectedCarList()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            TabName = "ExpectedCarList";
            LogTableName = "manager_sgp_report";
            Form = null;
            FormInit();
            CarGridInit();
            LogGridInit();

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

            DateTimeout = new Timeout(
             3,
             () =>
             {
                 GetDataManagerWeight();
             },
             true,
             true
         );
            DateTimeout.Run();

            ProcessPermissions();
        }

        #region Common

        public string RoleName = "[erp]access_transport";

        public FormHelper Form { get; set; }

        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// датасет, содержащий данные по  работе агента
        /// </summary>
        public ListDataSet ItemsLogDS { get; set; }

        public List<DataGridHelperColumn> Columns { get; private set; }

        /// <summary>
        /// Имя папки верхнего уровня, в которой хранится лог файл по работе агента
        /// </summary>
        private string LogTableName { get; set; }

        /// <summary>
        /// Таймер задержки повторного нажатия кнопки
        /// </summary>
        public Timeout ButtonTimer { get; set; }

        /// <summary>
        /// Массив кнопок для их сброса в первоначальное состояние
        /// </summary>
        private bool[] buttons = new bool[5];

        /// <summary>
        //// таймер получения данных по работе весовой
        /// </summary>
        private Timeout DateTimeout { get; set; }


        #endregion

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// инициализация компонентов формы
        /// </summary>
        public void FormInit()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="FROM_DATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DateFrom,
                    Default=DateTime.Now.ToString("dd.MM.yyyy"),
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TO_DATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DateTo,
                    Default=DateTime.Now.AddDays(1).ToString("dd.MM.yyyy"),
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SearchText,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ITEM_SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ItemsSearchBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

            };
            Form.SetFields(fields);
            Form.SetDefaults();
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
            DateTimeout.Finish();
            CarsGrid.Destruct();
            GridLog.Destruct();
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
                        Header="Цель визита",
                        Path="DESCRIPTION",
                        Doc="Тип",
                        ColumnType=ColumnTypeRef.String,
                        Width = 220,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Машина",
                        Path="MARKA",
                        Doc="Машина",
                        ColumnType=ColumnTypeRef.String,
                        Width = 140,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер",
                        Path="NUM",
                        Doc="Номер",
                        ColumnType=ColumnTypeRef.String,
                        Width = 150,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Описание",
                        Path="DRIVERNAME",
                        Doc="Описание",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=200,
                        MaxWidth=540,
                        Width = 250,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Телефон",
                        Path="DRIVERPHONE",
                        Doc="Телефон",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=100,
                        MaxWidth=100,
                        Width = 100,
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
                CarsGrid.PrimaryKey = "NUMBER";
                CarsGrid.SetColumns(columns);
                CarsGrid.SearchText = ItemsSearchBox;
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
                        CarsGrid.UpdateItems(ds);
                    }
                }
            }
        }


        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            CarsGrid.LoadItems();
        }

        /// <summary>
        /// инициализация грида подробной информации по работе агента
        /// </summary>
        private void LogGridInit()
        {
            {
                //колонки грида
                Columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Дата",
                        Path="ON_DATE",
                        Doc="Дата записи",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
//                        Width2 = 40,
                        MinWidth=110,
                        MaxWidth=114,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Описание",
                        Path="MESSAGE",
                        Doc="Описание",
                        ColumnType=ColumnTypeRef.String,
                        //Width2 = 600,
                        MinWidth=110,
                        MaxWidth=1800,
                    },
                };

                GridLog.SetColumns(Columns);

                /// GridLog.SetPrimaryKey("ID");
                /// GridLog.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

                GridLog.SetSorting("ON_DATE", ListSortDirection.Descending);
                GridLog.SearchText = SearchText;
                GridLog.AutoUpdateInterval = 0;


                GridLog.PrimaryKey = "ID";
                GridLog.UseRowHeader = true;
                GridLog.SelectItemMode = 0;

                //данные грида
                GridLog.OnLoadItems = LogGridLoadItems;
                GridLog.Init();
                GridLog.Run();

            }
        }

        /// <summary>
        /// загрузка данных грида детальной информации по работе агента
        /// </summary>
        private async void LogGridLoadItems()
        {

            bool resume = true;

            var f = DateFrom.Text.ToDateTime();
            var t = DateTo.Text.ToDateTime();

            if (resume)
            {
                if (DateTime.Compare(f, t) > 0)
                {
                    var msg = "Дата начала должна быть меньше даты окончания.";
                    var d = new DialogWindow($"{msg}", "Проверка данных");
                    d.ShowDialog();
                    resume = false;
                }
            }

            if (resume)
            {
                var dir = "general";

                var p = new Dictionary<string, string>();
                {
                    p.Add("TABLE_NAME", LogTableName);
                    p.Add("TABLE_DIRECTORY", dir);
                    p.Add("DATE_FROM", DateFrom.Text + " 00:00:00");
                    p.Add("DATE_TO", DateTo.Text + " 00:00:00");
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Service");
                q.Request.SetParam("Object", "LiteBase");
                q.Request.SetParam("Action", "List2");
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
                        {
                            var ds = ListDataSet.Create(result, LogTableName);
                            GridLog.UpdateItems(ds);
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }

            }

        }

        /// <summary>
        /// загрузка лог файлов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogGridLoadItems();
        }


        /// <summary>
        /// чтение данных от агента по управлению шлагбаумом на СГП
        /// </summary>
        private async void GetDataManagerWeight()
        {
            try
            {
                Dictionary<string, string> p = new Dictionary<string, string>();
                p.CheckAdd("ID", "55");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Devices");
                q.Request.SetParam("Object", "ManagerWeight");
                q.Request.SetParam("Action", "Get");
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
                        if (ds.Items.Count > 0)
                        {
                            // означает что есть данные
                            CameraInput.Content = ds.Items[0].CheckGet("INPUT_CAR_NUMBER").ToString();
                            DtTmInput.Content = ds.Items[0].CheckGet("INPUT_CAR_DTTM").ToString();

                            var today = DateTime.Now;
                            var endAgent = ds.Items[0].CheckGet("MANAGER_DTTM").ToDateTime();
                            TimeSpan rez = today - endAgent;

                            if (rez.TotalMinutes > 1)
                            {
                                ManagerDttmValue.Text = endAgent.ToString() + $". Нет данных более одной минуты.";
                                OpenGateButton.IsEnabled = false;
                            }
                            else
                            {
                                ManagerDttmValue.Text = endAgent.ToString();
                                if (OpenAndNoCloseGateButton.IsEnabled)
                                    OpenGateButton.IsEnabled = true;

                                ProcessPermissions();
                            }

                            ManagerMsg.Content = ds.Items[0].CheckGet("MANAGER_MSG").ToString();
                        }
                    }
                }

            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// Экспорт логов в Excel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ExportToExcelButton_OnClick(object sender, RoutedEventArgs e)
        {
            // GridLog.ItemsExportExcel();


            var list = GridLog.Items;

            var eg = new ExcelGrid();
            eg.SetColumnsFromGrid(Columns);
            eg.Items = list;
            await Task.Run(() =>
            {
                eg.Make();
            });

        }


        /// <summary>
        /// открытие вручную шлагбаума
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenGateButton_Click(object sender, RoutedEventArgs e)
        {
            buttons[0] = false;
            OpenGateButton.IsEnabled = false;
            // открываем шлагбаум на 20 сек., затем закрываем его   
            AutoOpenOut();
            ButtonTimer.Run();
        }

        /// <summary>
        /// открываем шлагбаум на 20 сек., затем закрываем его   
        /// </summary>
        private void AutoOpenOut()
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            p.CheckAdd("ID", "5");
            p.CheckAdd("CMD", "1");
            p.CheckAdd("ID_SCRAP", "0");
            p.CheckAdd("USER_NAME", Central.User.Name);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Devices");
            q.Request.SetParam("Object", "Barrier");
            q.Request.SetParam("Action", "Insert");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {

                // OpenGateButton.IsEnabled = true;
            }
            else
            {
                q.ProcessError();
            }
        }

        private void ButtonPush()
        {
            for (int i = 0; i < 5; i++)
            {
                if (!buttons[i])
                {
                    buttons[i] = true;
                    switch (i)
                    {
                        //Открыть на 40 сек    
                        case 0:
                            {
                                if (OpenAndNoCloseGateButton.IsEnabled)
                                    OpenGateButton.IsEnabled = true;
                                ProcessPermissions();
                            }
                            break;

                    }
                    ButtonTimer.Finish();
                }
            }
        }


        /// <summary>
        /// открываем  и не закрываем его   
        /// </summary>
        private void AutoOut()
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            p.CheckAdd("ID", "5");
            p.CheckAdd("CMD", "12");
            p.CheckAdd("ID_SCRAP", "0");
            p.CheckAdd("USER_NAME", Central.User.Name);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Devices");
            q.Request.SetParam("Object", "Barrier");
            q.Request.SetParam("Action", "Insert");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {

                // OpenGateButton.IsEnabled = true;
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// открываем шлагбаум и не закрываем его   
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenAndNoCloseGateButton_Click(object sender, RoutedEventArgs e)
        {
            OpenAndNoCloseGateButton.IsEnabled = false;
            OpenGateButton.IsEnabled = false;
            CloseGateButton.IsEnabled = true;
            AutoOut();
        }

        /// <summary>
        /// открываем  и не закрываем его   
        /// </summary>
        private void AutoCloseOut()
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            p.CheckAdd("ID", "5");
            p.CheckAdd("CMD", "13");
            p.CheckAdd("ID_SCRAP", "0");
            p.CheckAdd("USER_NAME", Central.User.Name);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Devices");
            q.Request.SetParam("Object", "Barrier");
            q.Request.SetParam("Action", "Insert");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {

                // OpenGateButton.IsEnabled = true;
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// /закрываем шлагбаум
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseGateButton_Click(object sender, RoutedEventArgs e)
        {
            OpenAndNoCloseGateButton.IsEnabled = true;
            OpenGateButton.IsEnabled = true;
            CloseGateButton.IsEnabled = false;

            AutoCloseOut();
        }


    }
}
