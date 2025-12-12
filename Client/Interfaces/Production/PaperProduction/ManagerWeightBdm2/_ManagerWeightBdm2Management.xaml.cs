using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Client.Interfaces.Main;
using System.Windows.Media;
using Client.Assets.HighLighters;
using System.ComponentModel;
using System.Linq;
using static Client.Interfaces.Main.FormDialog;
using static DevExpress.Mvvm.Native.TaskLinq;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Интерфейс для управления весовой на БДМ2 (для контролеров)
    /// <author>Грешных Н.И.</author>
    /// <version>9</version>
    /// <change>2025-06-17</change>
    /// </summary>
    public partial class ManagerWeightBdm2List : UserControl
    {
        public ManagerWeightBdm2List()
        {
            InitializeComponent();
            if (Central.InDesignMode())
            {
                return;
            }
            Form = null;

            TabName = "CarbageList";
            GridMasterID = 0;

            FormInit();
            SetDefaults();
            MasterGridInit();
            DetailGridInit();
            LogGridInit();
            CarbageGridInit();

            // получение прав пользователя
            ProcessPermissions();

            ControlEnable(!ReadOnly);
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            DateTimeout = new Timeout(
             1,
             () =>
             {
                 GetDataManagerWeight();
             },
             true,
             true
         );
            DateTimeout.Run();

            LogTableName = "manager_weight_report";

            // Признак отладки false - БДМ2, true -БДМ1
            DebugFlag = false;

            if (DebugFlag)
            {
                // БДМ1
                IdBarrierInput = 1;
                IdBarrierOutput = 2;
                IdTabloIn = 1;
                IdTabloOut = 2;
                IdScales = 1;
                TotalMinuts = 1;
            }
            else
            {
                // БДМ2
                IdBarrierInput = 3;
                IdBarrierOutput = 4;
                IdTabloIn = 3;
                IdTabloOut = 4;
                IdScales = 2;
                TotalMinuts = 10;
            }

            Bdm2ManagerMsg.Text = "";
            UserName = Central.User.Name;

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

            LogTimer = new Timeout(
             600,
             () =>
             {
                 LogGridLoadItems();
             },
             true,
             false
         );
            LogTimer.Run();

            WeightLog.Items.Clear();
            CountWeightLog = 0;

            //это первый запуск
            FirstRun = true;
            LogGridLoadItems();

            TestButton.Visibility = Visibility.Collapsed;
            if (Central.DebugMode)
            {
                TestButton.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;

        /// <summary>
        /// Признак первого запуска
        /// </summary>
        bool FirstRun { get; set; } = false;
        /// <summary>
        /// Признак отладки false - БДМ2, true -БДМ1
        /// </summary>
        bool DebugFlag { get; set; }
        /// <summary>
        /// время в минутах, сколько нет ответа от агента
        /// </summary>
        int TotalMinuts { get; set; }

        public FormHelper Form { get; set; }

        /// <summary>
        /// данные из выбранной в гриде машин строки
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// данные из выбранной в гриде машин с отходами строки
        /// </summary>
        Dictionary<string, string> SelectedItemCarbage { get; set; }

        /// <summary>
        /// ID выбранной машины из списка машин
        /// </summary>
        int GridMasterID { get; set; }
        /// <summary>
        /// статус выбранной машины
        /// </summary>
        int IdStatus { get; set; }
        /// <summary>
        /// Признак загрузки списка машин
        /// </summary>
        bool GridMasterLoaded { get; set; } = false;
        /// <summary>
        //// таймер получения данных по работе весовой
        /// </summary>
        private Timeout DateTimeout { get; set; }
        /// <summary>
        /// Признак ограниченного доступа к управлению весовой (только просмотр)
        /// </summary>
        bool ReadOnly { get; set; } = false;
        /// <summary>
        /// Имя папки верхнего уровня, в которой хранится лог файл по работе агента
        /// </summary>
        public string LogTableName { get; set; }

        /// <summary>
        /// датасет, содержащий данные по  работе агента
        /// </summary>
        public ListDataSet ItemsLogDS { get; set; }

        /// <summary>
        /// ID шлагбаума на въезд
        /// Id = 1 для БДМ1, 3- для БДМ2
        /// </summary>
        int IdBarrierInput { get; set; }

        /// <summary>
        /// ID шлагбаума на выезд
        /// Id = 2 для БДМ1, 4- для БДМ2
        /// </summary>
        int IdBarrierOutput { get; set; }
        /// <summary>
        /// ID табло на въезд
        /// Id = 1 для БДМ1, 3- для БДМ2
        /// </summary>
        int IdTabloIn { get; set; }
        /// <summary>
        /// ID табло на выезд
        /// Id = 2 для БДМ1, 4- для БДМ2
        /// </summary>
        int IdTabloOut { get; set; }
        /// <summary>
        /// ID весов
        /// Id = 1 для БДМ1, 2- для БДМ2
        /// </summary>
        int IdScales { get; set; }

        /// <summary>
        /// ФИО пользователя
        /// </summary>
        public string UserName { get; set; }
        public List<DataGridHelperColumn> Columns { get; private set; }

        /// <summary>
        /// Таймер задержки повторного нажатия кнопки
        /// </summary>
        public Timeout ButtonTimer { get; set; }

        /// <summary>
        /// Массив кнопок для их сброса в первоначальное состояние
        /// </summary>
        private bool[] buttons = new bool[20];

        /// <summary>
        /// Список групп, в которые входит пользователь
        /// </summary>
        private List<string> UserGroupsBdm { get; set; }

        /// <summary>
        /// количество строк в логе веса
        /// </summary>
        int CountWeightLog { get; set; }

        /// <summary>
        /// Таймер обновления информации по логам работы программы
        /// </summary>
        public Timeout LogTimer { get; set; }

        private void ProcessMessages(ItemMessage m)
        {
            if (m.ReceiverGroup.IndexOf("Carbage") > -1)
            {
                if (m.ReceiverName.IndexOf("CarbageList") > -1)
                {
                    switch (m.Action)
                    {
                        case "Refresh":
                            GridCarbage.LoadItems();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            /*
                в режиме только для чтения отключается возможность 
                манипулировать со шлагбаумом, запустить принудительно машину на весовую,
                изменить статус весовой и др. ограничения.
             */


            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel("[erp]bdm_2_manager_weight");
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:

                    break;

                case Role.AccessMode.FullAccess:
                    {
                        BarrierInputButton.IsEnabled = true;
                        OpenInAndZeroButton.IsEnabled = true;
                        AutoInButton.IsEnabled = true;
                        AutoCloseInButton.IsEnabled = true;

                        BarrierOutputButton.IsEnabled = true;
                        OpenOutAndZeroButton.IsEnabled = true;
                        AutoOutButton.IsEnabled = true;
                        AutoCloseOutButton.IsEnabled = true;

                        InputLaunchToWeightButton.IsEnabled = true;
                        OutputLaunchToWeightButton.IsEnabled = true;

                        AddButton.IsEnabled = true;

                        ReadOnly = false;
                    }
                    break;

                case Role.AccessMode.ReadOnly:
                    {
                        BarrierInputButton.IsEnabled = false;
                        OpenInAndZeroButton.IsEnabled = false;
                        AutoInButton.IsEnabled = false;
                        AutoCloseInButton.IsEnabled = false;

                        BarrierOutputButton.IsEnabled = false;
                        OpenOutAndZeroButton.IsEnabled = false;
                        AutoOutButton.IsEnabled = false;
                        AutoCloseOutButton.IsEnabled = false;
                        InputLaunchToWeightButton.IsEnabled = false;
                        OutputLaunchToWeightButton.IsEnabled = false;

                        AddButton.IsEnabled = false;

                        ReadOnly = true;
                    }
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

            if (GridMaster != null && GridMaster.Menu != null && GridMaster.Menu.Count > 0)
            {
                foreach (var manuItem in GridMaster.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }


            /// LoadUserGroup();
            // включение разрешений на управлением шлагбаумом и весовой
            ///ReadOnly = !UserGroupsBdm.Contains("controller_bdm_2");
        }

        /// <summary>
        /// Получение списка групп, в которые входит пользователь
        /// </summary>
        private void LoadUserGroup()
        {
            UserGroupsBdm = new List<string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Accounts");
            q.Request.SetParam("Object", "Group");
            q.Request.SetParam("Action", "ListByUser");
            q.Request.SetParam("ID", Central.User.EmployeeId.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var employeeGroups = ListDataSet.Create(result, "ITEMS");
                    if (employeeGroups.Items.Count > 0)
                    {
                        foreach (var item in employeeGroups.Items)
                        {
                            if (item.CheckGet("ID").ToInt() != 1)
                            {
                                if (item.CheckGet("IN_GROUP").ToBool())
                                {
                                    string groupCode = item.CheckGet("CODE");
                                    if (!string.IsNullOrEmpty(groupCode))
                                    {
                                        UserGroupsBdm.Add(groupCode);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        // инициализация компонентов формы
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
                    Path="DATE_FROM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=FromDate,
                    Default=DateTime.Now.ToString("dd.MM.yyyy"),
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DATE_TO",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ToDate,
                    Default=DateTime.Now.AddDays(1).ToString("dd.MM.yyyy"),
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="GARBAGE_CAR",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Default="1",
                    Control=GarbageCar,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange=(FormHelperField f, string v)=>
                    {
                        //EmployeeGrid.UpdateItems();
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "ProductionPm",
                        Object = "ManagerWeightBdm2",
                        Action = "GarbageCarList",
                        AnswerSectionKey="ITEMS",
                        OnComplete = (FormHelperField f,ListDataSet ds) =>
                        {
                            //var row = new Dictionary<string, string>()
                            //{
                            //    {"ID", "0" },
                            //    {"NAME_CAR", "Все" },
                            //};
                            //ds.ItemsPrepend(row);
                            var list=ds.GetItemsList("CHGC_ID","NAME_CAR");
                            var c=(SelectBox)f.Control;
                            if(c != null)
                            {
                                c.Items=list;
                                GarbageCar.SetSelectedItemByKey("3.0");
                            }
                        },
                    },
                },

            };
            Form.SetFields(fields);
            Form.SetDefaults();
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            // получение прав пользователя
            // ProcessPermissions();

            //значения полей по умолчанию
            {
                {
                    var list = new Dictionary<string, string>();
                    list.Add("0", "Общий");
                    list.Add("1", "Камера на въезд");
                    list.Add("2", "Камера на выезд");
                    list.Add("3", "Шлагбаум на въезд");
                    list.Add("4", "Шлагбаум на выезд");
                    list.Add("5", "Вывоз отходов");
                    TypeLog.Items = list;
                    TypeLog.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                }

            }
            EditButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;

        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "PaperProduction",
                ReceiverName = "",
                SenderName = "ManagerWeightBdm2",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            DateTimeout.Finish();
            GridMaster.Destruct();
            GridDetail.Destruct();
            GridCarbage.Destruct();
            LogGrid.Destruct();
        }

        /// <summary>
        /// инициализация грида списка машин для проезда через весовую
        /// </summary>
        private void MasterGridInit()
        {
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID_SCRAP",
                        Doc="ИД",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=44,
                        MaxWidth=44,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Машина",
                        Path="NAME",
                        Doc="Машина",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=110,
                        MaxWidth=160,
                    },
                    new DataGridHelperColumn
                    {
                        Header="регистрации",
                        Path="CREATED_DTTM",
                        Doc="Дата регистрации",
                        Group="           Дата",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        MinWidth=110,
                        MaxWidth=114,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Слот",
                        Path="UNLOADING_TIME_DTTM",
                        Doc="Дата слота",
                        Group="           Дата",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm",
                        MinWidth=98,
                        MaxWidth=98,
                    },
                    new DataGridHelperColumn
                    {
                        Header="полная",
                        Path="DT_FULL",
                        Doc="Дата полная",
                        Group="           Дата",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        MinWidth=110,
                        MaxWidth=114,
                    },
                    new DataGridHelperColumn
                    {
                        Header="пустая",
                        Path="DT_EMPTY",
                        Doc="Дата пустая",
                        Group="           Дата",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        MinWidth=110,
                        MaxWidth=114,
                    },
                    new DataGridHelperColumn
                    {
                        Header="полная",
                        Path="WEIGHT_FULL",
                        Doc="Вес полная",
                        Group="                Вес",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=60,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="пустая",
                        Path="WEIGHT_EMPTY",
                        Doc="Вес пустая",
                        Group="                Вес",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=60,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="брутто",
                        Path="WEIGHT_BEFORE_BRUTTO",
                        Doc="брутто",
                        Group="   На весах перед",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=60,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="нетто",
                        Path="WEIGHT_BEFORE_NETTO",
                        Doc="нетто",
                        Group="   На весах перед",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=60,
                        MaxWidth=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="STATUS",
                        Doc="Статус",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=160,
                        MaxWidth=160,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Площадка",
                        Path="AREA",
                        Doc="Площадка",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=40,
                        MaxWidth=75,
                    },
                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=2,
                        MaxWidth=2000,
                    },

                };

                GridMaster.SetColumns(columns);
                GridMaster.PrimaryKey = "ID_SCRAP";
                GridMaster.Label = "Master";
                GridMaster.UseSorting = false;
                GridMaster.AutoUpdateInterval = 10;
                GridMaster.UseRowHeader = true;
                GridMaster.SelectItemMode = 0;

                GridMaster.Init();

                //контекстное меню
                GridMaster.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "Launch",
                        new DataGridContextMenuItem()
                        {
                            Header="Запустить на весовую",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                               LaunchCar();
                            },
                        }
                    },
                    {
                       "updateTable",
                        new DataGridContextMenuItem(){
                        Header="Обновить таблицу",
                        Action=()=>
                        {
                            CarsGridLoadItems();
                        }
                    }},
                };

                {

                    //при выборе строки в гриде, обновляются актуальные действия для записи
                    GridMaster.OnSelectItem = (Dictionary<string, string> selectedItem) =>
                    {
                        if (selectedItem.Count > 0)
                        {
                            SelectedItem = selectedItem;
                            GridMasterID = SelectedItem.CheckGet("ID_SCRAP").ToInt();
                            // отображение детальной информации по машине
                            CarDetailGridLoadItems();

                            var idStatus = SelectedItem.CheckGet("ID_STATUS").ToInt();
                            GridMaster.Menu["Launch"].Enabled = false;
                            // доступность меню запуска машины на весовую
                            switch (idStatus)
                            {
                                case 1:
                                case 3:
                                case 4:
                                case 11:
                                case 16:
                                case 19:
                                case 41:
                                case 43:
                                case 44:
                                    {
                                        if (!ReadOnly)
                                            GridMaster.Menu["Launch"].Enabled = true;
                                        else
                                            GridMaster.Menu["Launch"].Enabled = false;
                                    }
                                    break;
                            }
                        }
                    };

                }

                //данные грида
                GridMaster.OnLoadItems = CarsGridLoadItems;

                GridMaster.Run();
                GridMasterLoaded = true;
            }

            //фокус ввода           
            GridMaster.Focus();
        }

        /// <summary>
        /// загрузка данных грида списка машин для проезда через весовую
        /// </summary>
        private async void CarsGridLoadItems()
        {
            var p = new Dictionary<string, string>();
            {
                if (Area2RadioButton.IsChecked == true)
                {
                    p.Add("ID_ST1", "1716");
                    p.Add("ID_ST2", "1716");
                }
                else
                {
                    p.Add("ID_ST1", "716");
                    p.Add("ID_ST2", "2716");
                }

                if (NumberDay.Text.IsNullOrEmpty())
                {
                    p.Add("NUMBERDAY", "1");
                }
                else
                {
                    p.Add("NUMBERDAY", NumberDay.Text.ToString());
                }
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "PaperProduction");
            q.Request.SetParam("Object", "ManagerWeightBdm2");
            q.Request.SetParam("Action", "ListCarRegistered");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                GridMaster.UpdateItemsAnswer(q.Answer, "ITEMS");
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// инициализация грида подробной информации по машине
        /// </summary>
        private void DetailGridInit()
        {
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="№",
                        Path="ID",
                        Doc="ИД",
                        ColumnType=ColumnTypeRef.Integer,
                        Width = 60,
                        Visible = false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="ID_SCRAP",
                        Doc="Ид",
                        ColumnType=ColumnTypeRef.Integer,
                        Visible = false,
                        Width = 60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата изменения",
                        Path="DT_CHANGED",
                        Doc="Дата изменения",
                        //Group="Дата",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        MinWidth=114,
                        MaxWidth=114,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Событие",
                        Path="STATUS",
                        Doc="Событие",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=250,
                        MaxWidth=500,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Описание",
                        Path="TEXT",
                        Doc="Описание",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=90,
                        MaxWidth=90,
                    },

                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=2,
                        MaxWidth=2000,
                    },

                };

                GridDetail.PrimaryKey = "ID_SCRAP";
                GridDetail.SetColumns(columns);
                GridDetail.Label = "Detail";
                GridDetail.UseSorting = false;
                GridDetail.UseRowHeader = false;
                GridDetail.SelectItemMode = 1;

                GridDetail.Init();
                //данные грида
                GridDetail.OnLoadItems = CarDetailGridLoadItems;
                GridDetail.Run();
            }

        }

        /// <summary>
        /// загрузка данных грида детальной информации по машине
        /// </summary>
        private async void CarDetailGridLoadItems()
        {
            if (GridMasterID == 0)
                return;

            var p = new Dictionary<string, string>();
            {
                p.Add("ID_SCRAP", GridMasterID.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "PaperProduction");
            q.Request.SetParam("Object", "ManagerWeightBdm2");
            q.Request.SetParam("Action", "ListCarDetail");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                GridDetail.UpdateItemsAnswer(q.Answer, "ITEMS");
                GridDetail.SetSelectToFirstRow();
            }
            else
            {
                //   q.ProcessError();
            }
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
                        MinWidth=114,
                        MaxWidth=114,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Описание",
                        Path="MESSAGE",
                        Doc="Описание",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=600,
                        MaxWidth=600,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="ID_SCRAP",
                        Doc="Ид",
                        ColumnType=ColumnTypeRef.Integer,
                        Visible = true,
                        MinWidth=40,
                        MaxWidth=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=2,
                        MaxWidth=2000,
                    },
                };

                LogGrid.PrimaryKey = "ID";
                LogGrid.SetColumns(Columns);
                LogGrid.SetSorting("ON_DATE", ListSortDirection.Descending);
                LogGrid.SearchText = SearchText;
                LogGrid.AutoUpdateInterval = 0;
                LogGrid.UseSorting = false;
                LogGrid.UseRowHeader = false;
                //GridLog.SelectItemMode = 1;

                LogGrid.Run();
                //данные грида
                LogGrid.OnLoadItems = LogGridLoadItems;
                LogGrid.Init();
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
                var dir = "";

                switch (TypeLog.SelectedItem.Key)
                {
                    case "0":
                        {
                            dir = "general";
                        }
                        break;
                    case "1":
                        {
                            dir = "camera_input";
                        }
                        break;
                    case "2":
                        {
                            dir = "camera_output";
                        }
                        break;
                    case "3":
                        {
                            dir = "barrier_input";
                        }
                        break;
                    case "4":
                        {
                            dir = "barrier_output";
                        }
                        break;
                    case "5":
                        {
                            dir = "carbage_log";
                        }
                        break;
                }

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("TABLE_NAME", LogTableName);
                    p.CheckAdd("TABLE_DIRECTORY", dir);
                    // 1=global,2=local,3=net
                    p.CheckAdd("STORAGE_TYPE", "3");
                    p.CheckAdd("DATE_FROM", DateFrom.Text + " 00:00:00");
                    p.CheckAdd("DATE_TO", DateTo.Text + " 00:00:00");
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Service");
                q.Request.SetParam("Object", "LiteBase");
                //FIXME: нужно стараться не использовать функций-клонов с цифровыми именами
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
                            LogGrid.UpdateItems(ds);
                        }
                    }
                }
                else
                {
                    //   q.ProcessError();
                }

            }

        }

        /// <summary>
        /// инициализация грида информации по взвешенным машина с отходами
        /// </summary>
        private void CarbageGridInit()
        {
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="№",
                        Path="RN",
                        Doc="№",
                        ColumnType=ColumnTypeRef.Integer,
                         Width2 = 2,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="CHGA_ID",
                        Doc="ИД",
                        ColumnType=ColumnTypeRef.Integer,
                         Width2 = 6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Машина",
                        Path="NOMER_CAR",
                        Doc="Машина",
                        ColumnType=ColumnTypeRef.String,
                         Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="регистрации",
                        Path="CREATED_DTTM",
                        Doc="Дата регистрации",
                        Group="Дата",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                         Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="полная",
                        Path="FULL_DTTM",
                        Doc="Дата полная",
                        Group="Дата",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="пустая",
                        Path="EMPTY_DTTM",
                        Doc="Дата пустая",
                        Group="Дата",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="полная",
                        Path="WEIGHT_FULL",
                        Doc="Вес полная",
                        Group="Вес",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                        Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                        {
                            {
                                StylerTypeRef.ForegroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // ручная правка веса полной
                                    if (row.ContainsKey("AUTO_INPUT"))
                                    {
                                        if (row["AUTO_INPUT"].ToInt() == 1 || row["AUTO_INPUT"].ToInt() == 3)
                                        {
                                            color = HColor.RedFG;
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },

                    },
                    new DataGridHelperColumn
                    {
                        Header="пустая",
                        Path="WEIGHT_EMPTY",
                        Doc="Вес пустая",
                        Group="Вес",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 8,
                        Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                        {
                            {
                                StylerTypeRef.ForegroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // ручная правка веса пустой
                                    if (row.ContainsKey("AUTO_INPUT"))
                                    {
                                        if (row["AUTO_INPUT"].ToInt() == 2 || row["AUTO_INPUT"].ToInt() == 3)
                                        {
                                            color = HColor.RedFG;
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="отходы",
                        Path="WEIGHT_FACT",
                        Doc="Вес фактический",
                        Group="Вес",
                        ColumnType=ColumnTypeRef.Integer,
                         Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Без отходов",
                        Path="CARBAGE_EMPTY_FLAG",
                        Doc="Пустой рейс",
                        Group="Вес",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2 = 10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Контролер",
                        Path="NAME",
                        Doc="Контролер",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="STATUS",
                        Doc="Статус",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 22,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Описание",
                        Path="DESCRIPTION",
                        Doc="Описание",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 20,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Участок",
                        Path="REGION",
                        Doc="Участок",
                        ColumnType=ColumnTypeRef.String,
                         Width2 = 12,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Полигон",
                        Path="LANDFILL",
                        Doc="Полигон",
                        ColumnType=ColumnTypeRef.String,
                         Width2 = 16,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ контейнера",
                        Path="CONTAINER_NUM",
                        Doc="№ контейнера",
                        ColumnType=ColumnTypeRef.String,
                         Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Бригада",
                        Path="BRIGADE",
                        Doc="Бригада",
                        ColumnType=ColumnTypeRef.String,
                         Width2 = 8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="изменение веса",
                        Path="AUTO_INPUT",
                        Doc="Изменение",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2 = 2,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Пояснение для ОЭБ",
                        Path="NOTE",
                        Doc="Описание",
                        ColumnType=ColumnTypeRef.String,
                        Width2 = 20,
                    },
                };

                GridCarbage.SetColumns(columns);
                GridCarbage.SetPrimaryKey("CHGA_ID");
                GridCarbage.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                GridCarbage.AutoUpdateInterval = 30;
                GridCarbage.Init();
                //контекстное меню
                GridCarbage.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "HistoryView",
                        new DataGridContextMenuItem()
                        {
                            Header="История изменений",
                            Tag = "history_mode_full_access",
                            Action=()=>
                            {
                               HistoryGarbageCar();
                            },
                        }
                    },
                };


                //при выборе строки в гриде, обновляются актуальные действия для записи
                GridCarbage.OnSelectItem = (Dictionary<string, string> selectedItem) =>
                    {
                        GridCarbage.Menu["HistoryView"].Enabled = false;
                        if (selectedItem.Count > 0)
                        {
                            SelectedItemCarbage = selectedItem;
                            // var IdCarbage = SelectedItemCarbage.CheckGet("ID").ToInt();
                            GridCarbage.Menu["HistoryView"].Enabled = true;
                        }
                    };


                //данные грида
                GridCarbage.OnLoadItems = CarbageGridLoadItems;
                GridCarbage.Run();
            }
        }

        /// <summary>
        /// загрузка данных грида истории вывоза мусора
        /// </summary>
        private async void CarbageGridLoadItems()
        {

            var p = new Dictionary<string, string>();
            {
                p.Add("FROM_DT", FromDate.Text + " 00:00:00");
                p.Add("TO_DT", ToDate.Text + " 23:59:59");
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "PaperProduction");
            q.Request.SetParam("Object", "ManagerWeightBdm2");
            q.Request.SetParam("Action", "ListCarbageCar");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                //GridCarbage.UpdateItemsAnswer(q.Answer, "ITEMS");

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    GridCarbage.UpdateItems(ds);
                }

            }
            else
            {
                // q.ProcessError();
            }
        }

        /// <summary>
        /// загрузка лог файлов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadLogButton_Click(object sender, RoutedEventArgs e)
        {
            FirstRun = false;
            buttons[11] = false;
            LoadLogButton.IsEnabled = false;
            LogGridLoadItems();
            ButtonTimer.Run();
        }

        /// <summary>
        /// Установка весовой в первоначальное состояние (свободна)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var dw = new DialogWindow("Вы действительно хотите сбросить состояние весовой?", "Управление весовой", "\nБудет установлено состояние весовой - свободна \n все шлагбаумы закрыты.", DialogWindowButtons.NoYes);
            if (dw.ShowDialog() == true)
            {
                buttons[9] = false;
                ResetButton.IsEnabled = false;
                ResetWeight();
                ButtonTimer.Run();
            }
        }

        /// <summary>
        ////вывод сообщения на внешнее табло
        /// </summary>
        private void SendMessageTabloIn()
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            p.CheckAdd("ID", IdTabloIn.ToString());
            p.CheckAdd("TEXT", "   " + TabloMessage1.Text);

            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Devices");
                q.Request.SetParam("Object", "Panel");
                q.Request.SetParam("Action", "Insert");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status != 0)
                {
                    //   q.ProcessError();
                }
            }
        }

        /// <summary>
        ////вывод сообщения на внутреннее табло
        /// </summary>
        private void SendMessageTabloOut()
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            p.CheckAdd("ID", IdTabloOut.ToString());
            p.CheckAdd("TEXT", "   " + TabloMessage2.Text);

            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Devices");
                q.Request.SetParam("Object", "Panel");
                q.Request.SetParam("Action", "Insert");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status != 0)
                {
                    // q.ProcessError();
                }
            }

        }

        /// <summary>
        /// вывести сообщение на табло
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowMessageTabloButton_Click(object sender, RoutedEventArgs e)
        {
            if (TabloMessage1.Text.Trim().Length > 0)
            {
                SendMessageTabloIn();
                System.Threading.Thread.Sleep(1500);
            }
            if (TabloMessage2.Text.Trim().Length > 0)
            {
                SendMessageTabloOut();
            }
        }

        /// <summary>
        /// если выбрана площадка БДМ2, то все контролы доступны в зависимости от прав доступа
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Area2RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            ControlEnable(!ReadOnly);
            CarsGridLoadItems();
        }

        /// <summary>
        /// если выбрана площадка ВСЕ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Area1RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            //   ControlEnable(false);
            ControlEnable(!ReadOnly);
            CarsGridLoadItems();
        }

        /// <summary>
        /// если vid true, то контролы в GroupBox доступны
        /// </summary>
        private void ControlEnable(bool vid)
        {
            if (GridMasterLoaded)
            {
                GroupBoxIn.IsEnabled = vid;
                GroupBoxOut.IsEnabled = vid;
                GroupBoxManagement.IsEnabled = vid;
                GroupBoxWeight.IsEnabled = vid;
                GroupBoxTablo.IsEnabled = vid;
                GridMaster.Menu["Launch"].Enabled = vid;
            }
        }

        /// <summary>
        /// получаем данные по работе весовой
        /// </summary>
        private async void GetDataManagerWeight()
        {
            bool resume = true;

            try
            {
                Dictionary<string, string> p = new Dictionary<string, string>();
                p.CheckAdd("ID", "33");

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
                        // означает что есть данные
                        if (ds.Items.Count > 0)
                        {
                            CameraInput.Content = ds.Items[0].CheckGet("INPUT_CAR_NUMBER").ToString().ToLower();
                            DtTmInput.Content = ds.Items[0].CheckGet("INPUT_CAR_DTTM").ToString();

                            CameraOut.Content = ds.Items[0].CheckGet("OUTPUT_CAR_NUMBER").ToString().ToLower();
                            DtTmOutput.Content = ds.Items[0].CheckGet("OUTPUT_CAR_DTTM").ToString();
                            WeihtCar.Content = ds.Items[0].CheckGet("WEIGHT_CAR").ToString();

                            if (CountWeightLog > 300)
                            {
                                WeightLog.Items.Clear();
                                CountWeightLog = 0;
                            }

                            if (!ds.Items[0].CheckGet("WEIGHT_CAR_DINAMIC").ToString().IsNullOrEmpty())
                            {
                                var weihtCarDinamic = ds.Items[0].CheckGet("WEIGHT_CAR_DINAMIC").ToString();
                                WeightLog.Items.Insert(0, $"{weihtCarDinamic} кг.");
                                CountWeightLog++;
                            }

                            if (ds.Items[0].CheckGet("MANAGER_STATUS").ToString() == "0")
                            {
                                StartButton.Content = "Старт";
                                Status.Foreground = HColor.RedFG.ToBrush();
                                Status.Content = "Занята";
                            }
                            else if (ds.Items[0].CheckGet("MANAGER_STATUS").ToString() == "1")
                            {
                                StartButton.Content = "Стоп";
                                Status.Foreground = HColor.GreenFG.ToBrush();
                                Status.Content = "Свободна";
                            }

                            SensorInCheckBox.IsChecked = ds.Items[0].CheckGet("MANAGER_SENSOR_IN").ToBool();
                            SensorCenterCheckBox.IsChecked = ds.Items[0].CheckGet("MANAGER_SENSOR_CENTER").ToBool();
                            SensorOutCheckBox.IsChecked = ds.Items[0].CheckGet("MANAGER_SENSOR_OUT").ToBool();

                            TabloMessage1.IsEnabled = ds.Items[0].CheckGet("MANAGER_TABLO_IN_STATUS").ToBool();
                            TabloMessage2.IsEnabled = ds.Items[0].CheckGet("MANAGER_TABLO_OUT_STATUS").ToBool();
                            Bdm2ManagerMsg.Text = ds.Items[0].CheckGet("MANAGER_MSG").ToString();

                            var today = DateTime.Now;
                            var endAgent = ds.Items[0].CheckGet("MANAGER_DTTM").ToDateTime();
                            TimeSpan rez = today - endAgent;

                            if (rez.TotalMinutes > TotalMinuts)
                            {
                                Bdm2ManagerDttmValue.Text = endAgent.ToString() + $". Нет данных более {TotalMinuts} минуты.";
                                ControlEnable(false);
                            }
                            else
                            {
                                Bdm2ManagerDttmValue.Text = endAgent.ToString();
                                //if ((bool)Area1RadioButton.IsChecked)
                                {
                                    //   ControlEnable(false);
                                }
                                //else
                                {
                                    ControlEnable(!ReadOnly);
                                }
                            }

                        }
                    }
                }

            }
            catch (Exception ex)
            {

            }

        }

        /// <summary>
        /// окно с Отладочной информацией
        /// </summary>
        private void ShowInfo()
        {
            var t = "Отладочная информация";
            var m = Central.MakeInfoString();
            var i = new ErrorTouch();
            i.Show(t, m);
        }

        /// <summary>
        /// обновим данные по списку машин при изменении количества дней для показа
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NumberDay_IsKeyboardFocusedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            CarsGridLoadItems();
        }

        ///
        /// Весы
        ///
        /// 
        /// <summary>
        /// обнуляем весы
        /// </summary>
        private void WeightZero()
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            p.CheckAdd("ID", IdScales.ToString());
            p.CheckAdd("WEIGT", "-2");
            p.CheckAdd("TICKS", DateTime.Now.Ticks.ToString());
            p.CheckAdd("USER_NAME", UserName);
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Devices");
                q.Request.SetParam("Object", "Scales");
                q.Request.SetParam("Action", "Insert");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status != 0)
                {
                    //   q.ProcessError();
                }
            }
        }

        /// <summary>
        /// нажали кнопку "обнулить весы"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WeightToZeroButton_Click(object sender, RoutedEventArgs e)
        {
            buttons[10] = false;
            WeightToZeroButton.IsEnabled = false;
            WeightZero();
            ButtonTimer.Run();
        }

        /// <summary>
        //// сброс состояния весовой в первоначальный (шлагбаумы закрыты, весовая свободна)
        /// </summary>
        private void ResetWeight()
        {
            // ID машины или 0 для сброса состояния весовой 
            string idScrap = "0";

            Dictionary<string, string> p = new Dictionary<string, string>();
            p.CheckAdd("ID", "111");
            p.CheckAdd("ID_SCRAP", idScrap);
            p.CheckAdd("CMD", "2");
            p.CheckAdd("ID_STATUS", "0");
            p.CheckAdd("USER_NAME", UserName);
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Devices");
                q.Request.SetParam("Object", "ManagerWeight");
                q.Request.SetParam("Action", "Insert");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status != 0)
                {
                    //    q.ProcessError();
                }
            }
        }

        /// <summary>
        //// отключение логики работы агента
        /// </summary>
        private void StopWeight()
        {
            string idScrap = "0";

            Dictionary<string, string> p = new Dictionary<string, string>();
            // 1- БДМ1, 2- БДМ2
            p.CheckAdd("ID", "111");
            p.CheckAdd("ID_SCRAP", idScrap);
            p.CheckAdd("CMD", "0");
            p.CheckAdd("ID_STATUS", "0");
            p.CheckAdd("USER_NAME", UserName);
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Devices");
                q.Request.SetParam("Object", "ManagerWeight");
                q.Request.SetParam("Action", "Insert");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status != 0)
                {
                    //  q.ProcessError();
                }
            }
        }

        /// <summary>
        //// включение логики работы агента
        /// </summary>
        private void StartWeight()
        {
            string idScrap = "0";

            Dictionary<string, string> p = new Dictionary<string, string>();
            // 1- БДМ1, 2- БДМ2
            p.CheckAdd("ID", "111");
            p.CheckAdd("ID_SCRAP", idScrap);
            p.CheckAdd("CMD", "1");
            p.CheckAdd("ID_STATUS", "0");
            p.CheckAdd("USER_NAME", UserName);
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Devices");
                q.Request.SetParam("Object", "ManagerWeight");
                q.Request.SetParam("Action", "Insert");
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status != 0)
                {
                    //  q.ProcessError();
                }
            }
        }

        /// <summary>
        /// старт/стоп работы логики агента на весовой
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            buttons[8] = false;
            StartButton.IsEnabled = false;

            if (Status.Content == "Свободна")
            {
                StopWeight();
            }
            else
            {
                StartWeight();
            }
            ButtonTimer.Run();
        }

        /// <summary>
        /// Принудительно запустить машину на весовую 
        /// </summary>
        private void LaunchCar()
        {
            string msg = "";
            Dictionary<string, string> p = new Dictionary<string, string>();

            msg += $"Вы действительно хотите запустить";
            msg += $"\nмашину {SelectedItem["NAME"]}";

            var idStatus = SelectedItem.CheckGet("ID_STATUS").ToInt();
            switch (idStatus)
            {
                case 1:
                case 11:
                case 41:
                    {
                        msg += $"\nпо  направлению на завод?";
                        p.CheckAdd("CMD", "3");
                    }
                    break;
                case 3:
                case 4:
                case 16:
                case 19:
                case 43:
                case 44:
                    {
                        msg += $"\nпо  направлению с завода?";
                        p.CheckAdd("CMD", "4");
                    }
                    break;
            }

            var d = new DialogWindow($"{msg}", "Всё верно?", "", DialogWindowButtons.NoYes);

            d.ShowDialog();
            if (d.DialogResult == true)
            {
                // ID машины или 0 для сброса состояния весовой 
                string idScrap = SelectedItem["ID_SCRAP"];

                p.CheckAdd("ID", "111");
                p.CheckAdd("ID_SCRAP", idScrap);
                p.CheckAdd("ID_STATUS", idStatus.ToString());
                p.CheckAdd("USER_NAME", UserName);
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Devices");
                    q.Request.SetParam("Object", "ManagerWeight");
                    q.Request.SetParam("Action", "Insert");
                    q.Request.SetParams(p);

                    q.DoQuery();

                    if (q.Answer.Status != 0)
                    {
                        //   q.ProcessError();
                    }
                }
            }
        }

        ///
        /// Работа со шлагбаумом
        /// 

        /// <summary>
        /// открываем внешний шлагбаум на 40 сек., затем закрываем его   
        /// </summary>
        private void AutoOpenIn()
        {
            string idScrap = SelectedItem["ID_SCRAP"];

            Dictionary<string, string> p = new Dictionary<string, string>();
            p.CheckAdd("ID", IdBarrierInput.ToString());
            p.CheckAdd("CMD", "1");
            p.CheckAdd("ID_SCRAP", idScrap);
            p.CheckAdd("USER_NAME", UserName);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Devices");
            q.Request.SetParam("Object", "Barrier");
            q.Request.SetParam("Action", "Insert");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status != 0)
            {
                //   q.ProcessError();
            }
        }

        /// <summary>
        /// открываем внешний шлагбаум на 40 сек., затем закрываем его и обнуляем весы  
        /// </summary>
        private void OpenInAndZero()
        {
            WeightZero();
            AutoOpenIn();
        }

        /// <summary>
        /// открываем внешний шлагбаум в режиме не закрывать
        /// </summary>
        private void AutoIn()
        {
            string idScrap = SelectedItem["ID_SCRAP"];

            Dictionary<string, string> p = new Dictionary<string, string>();
            p.CheckAdd("ID", IdBarrierInput.ToString());
            p.CheckAdd("CMD", "2");
            p.CheckAdd("ID_SCRAP", idScrap);
            p.CheckAdd("USER_NAME", UserName);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Devices");
            q.Request.SetParam("Object", "Barrier");
            q.Request.SetParam("Action", "Insert");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status != 0)
            {
                //  q.ProcessError();
            }

        }

        /// <summary>
        /// внешний шлагбаум закрыть
        /// </summary>
        private void AutoCloseIn()
        {
            string idScrap = SelectedItem["ID_SCRAP"];

            Dictionary<string, string> p = new Dictionary<string, string>();
            p.CheckAdd("ID", IdBarrierInput.ToString());
            p.CheckAdd("CMD", "3");
            p.CheckAdd("ID_SCRAP", idScrap);
            p.CheckAdd("USER_NAME", UserName);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Devices");
            q.Request.SetParam("Object", "Barrier");
            q.Request.SetParam("Action", "Insert");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status != 0)
            {
                //  q.ProcessError();
            }
        }

        /// <summary>
        /// открываем внутренний шлагбаум на 40 сек., затем закрываем его   
        /// </summary>
        private void AutoOpenOut()
        {
            string idScrap = SelectedItem["ID_SCRAP"];

            Dictionary<string, string> p = new Dictionary<string, string>();
            p.CheckAdd("ID", IdBarrierOutput.ToString());
            p.CheckAdd("CMD", "11");
            p.CheckAdd("ID_SCRAP", idScrap);
            p.CheckAdd("USER_NAME", UserName);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Devices");
            q.Request.SetParam("Object", "Barrier");
            q.Request.SetParam("Action", "Insert");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                OpenOutAndZeroButton.IsEnabled = true;
            }
            else
            {
                //   q.ProcessError();
            }
        }

        /// <summary>
        /// открываем внутренний шлагбаум на 40 сек., затем закрываем его и обнуляем весы  
        /// </summary>
        private void OpenOutAndZero()
        {
            WeightZero();
            AutoOpenOut();
        }

        /// <summary>
        /// открываем внутренний шлагбаум в режиме не закрывать
        /// </summary>
        private void AutoOut()
        {
            string idScrap = SelectedItem["ID_SCRAP"];

            Dictionary<string, string> p = new Dictionary<string, string>();
            p.CheckAdd("ID", IdBarrierOutput.ToString());
            p.CheckAdd("CMD", "12");
            p.CheckAdd("ID_SCRAP", idScrap);
            p.CheckAdd("USER_NAME", UserName);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Devices");
            q.Request.SetParam("Object", "Barrier");
            q.Request.SetParam("Action", "Insert");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                AutoOutButton.Style = (Style)AutoOutButton.TryFindResource("Button");
            }
            else
            {
                //   q.ProcessError();
            }
        }

        /// <summary>
        /// внутренний шлагбаум закрыть
        /// </summary>
        private void AutoCloseOut()
        {
            string idScrap = SelectedItem["ID_SCRAP"];

            Dictionary<string, string> p = new Dictionary<string, string>();
            p.CheckAdd("ID", IdBarrierOutput.ToString());
            p.CheckAdd("CMD", "13");
            p.CheckAdd("ID_SCRAP", idScrap);
            p.CheckAdd("USER_NAME", UserName);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Devices");
            q.Request.SetParam("Object", "Barrier");
            q.Request.SetParam("Action", "Insert");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                AutoCloseOutButton.Style = (Style)AutoCloseOutButton.TryFindResource("Button");
            }
            else
            {
                //  q.ProcessError();
            }
        }

        /// <summary>
        /// Управление внешним шлагбаумом
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BarrierInputButton_Click(object sender, RoutedEventArgs e)
        {
            string tag = (sender as Button).Tag.ToString();

            if (SelectedItem != null)
            {
                switch (tag)
                {
                    //Открыть на 40 сек    
                    case "auto_open_in":
                        {
                            buttons[0] = false;
                            BarrierInputButton.IsEnabled = false;
                            // открываем шлагбаум на 40 сек., затем закрываем его   
                            AutoOpenIn();
                        }
                        break;

                    //Открыть на  40 сек. с обнулением весов
                    case "open_in_and_zero":
                        {
                            buttons[1] = false;
                            OpenInAndZeroButton.IsEnabled = false;
                            OpenInAndZero();
                        }
                        break;

                    //открыть и не закрывать
                    case "auto_in":
                        {
                            buttons[2] = false;
                            AutoInButton.IsEnabled = false;
                            AutoIn();
                        }
                        break;

                    // закрыть
                    case "auto_close_in":
                        {
                            buttons[3] = false;
                            AutoCloseInButton.IsEnabled = false;
                            AutoCloseIn();
                        }
                        break;
                }
                ButtonTimer.Run();
            }
        }

        /// <summary>
        /// Управление внутренним шлагбаумом
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BarrierOutputButton_Click(object sender, RoutedEventArgs e)
        {
            string tag = (sender as Button).Tag.ToString();

            if (SelectedItem != null)
            {

                switch (tag)
                {
                    //Открыть на 40 сек    
                    case "auto_open_out":
                        {
                            buttons[4] = false;
                            BarrierOutputButton.IsEnabled = false;
                            // открываем шлагбаум на 40 сек., затем закрываем его   
                            AutoOpenOut();
                        }
                        break;

                    //Открыть на  40 сек. с обнулением весов
                    case "open_out_and_zero":
                        {
                            buttons[5] = false;
                            OpenOutAndZeroButton.IsEnabled = false;
                            OpenOutAndZero();
                        }
                        break;

                    //открыть и не закрывать
                    case "auto_out":
                        {
                            buttons[6] = false;
                            AutoOutButton.IsEnabled = false;
                            AutoOut();
                        }
                        break;

                    // закрыть
                    case "auto_close_out":
                        {
                            buttons[7] = false;
                            AutoCloseOutButton.IsEnabled = false;
                            AutoCloseOut();
                        }
                        break;

                    default:
                        break;
                }
                ButtonTimer.Run();
            }
        }

        private void ExportToExcelButton_OnClick(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void TestButton_OnClick(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            var msg = $"Test {today}";
            bool result = true;

            var tableDirectory = "general";

            var ReportWorking = new Dictionary<string, string>();

            ReportWorking.CheckAdd("MESSAGE", msg);
            ReportWorking.CheckAdd("ID_SCRAP", "");

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ITEMS", JsonConvert.SerializeObject(ReportWorking));
                p.CheckAdd("TABLE_NAME", "manager_weight_report");
                p.CheckAdd("TABLE_DIRECTORY", tableDirectory);
                // 1=global,2=local,3=net
                p.CheckAdd("STORAGE_TYPE", "3");
                p.CheckAdd("PRIMARY_KEY", "ID");
                p.CheckAdd("PRIMARY_KEY_VALUE", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ffffff"));

            }


            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Service");
            q.Request.SetParam("Object", "LiteBase");
            q.Request.SetParam("Action", "SaveData");
            q.Request.SetParams(p);

            q.Request.Timeout = 1000;
            q.Request.Attempts = 1;
            q.DoQuery();

            if (q.Answer.Status != 0)
            {
                result = false;
            }
        }



        private async void ExportToExcel()
        {
            var list = LogGrid.Items;

            buttons[12] = false;
            ExportButton.IsEnabled = false;
            ButtonTimer.Run();
            var eg = new ExcelGrid();
            eg.SetColumnsFromGrid(Columns);
            eg.Items = list;
            await Task.Run(() =>
            {
                eg.Make();
            });
        }

        private void ButtonPush()
        {

            for (int i = 0; i < 20; i++)
            {
                if (!buttons[i])
                {
                    buttons[i] = true;
                    switch (i)
                    {
                        //Открыть на 40 сек    
                        case 0:
                            {
                                BarrierInputButton.IsEnabled = true;
                            }
                            break;

                        //Открыть на  40 сек. с обнулением весов
                        case 1:
                            {
                                OpenInAndZeroButton.IsEnabled = true;
                            }
                            break;

                        //открыть и не закрывать
                        case 2:
                            {
                                AutoInButton.IsEnabled = true;
                            }
                            break;

                        // закрыть
                        case 3:
                            {
                                AutoCloseInButton.IsEnabled = true;
                            }
                            break;

                        //Открыть на 40 сек    
                        case 4:
                            {
                                BarrierOutputButton.IsEnabled = true;
                            }
                            break;

                        //Открыть на  40 сек. с обнулением весов
                        case 5:
                            {
                                OpenOutAndZeroButton.IsEnabled = true;
                            }
                            break;

                        //открыть и не закрывать
                        case 6:
                            {
                                AutoOutButton.IsEnabled = true;
                            }
                            break;

                        // закрыть
                        case 7:
                            {
                                AutoCloseOutButton.IsEnabled = true;
                            }
                            break;
                        // старт/стоп логика егента
                        case 8:
                            {
                                StartButton.IsEnabled = true;
                            }
                            break;
                        // сброс весовой
                        case 9:
                            {
                                ResetButton.IsEnabled = true;
                            }
                            break;
                        // сброс веса
                        case 10:
                            {
                                WeightToZeroButton.IsEnabled = true;
                            }
                            break;
                        // загрузка логов
                        case 11:
                            {
                                LoadLogButton.IsEnabled = true;
                            }
                            break;
                        // выгрузка логов в Ecxel
                        case 12:
                            {
                                ExportButton.IsEnabled = true;
                            }
                            break;
                        // удаление машины с мусором
                        case 13:
                            {
                                if ((!ReadOnly) && (SelectedItemCarbage.CheckGet("CHGA_ID").ToInt() > 0))

                                {
                                    DeleteButton.IsEnabled = true;
                                }
                                else
                                {
                                    DeleteButton.IsEnabled = false;
                                }


                            }
                            break;
                        // редактирование машины с мусором
                        case 14:
                            {
                                if ((!ReadOnly) && (SelectedItemCarbage.CheckGet("CHGA_ID").ToInt() > 0))
                                {
                                    EditButton.IsEnabled = true;
                                }
                                else
                                {
                                    EditButton.IsEnabled = false;
                                }

                            }
                            break;
                        // обновить список машин с мусором
                        case 15:
                            {
                                RefreshButton.IsEnabled = true;
                            }
                            break;
                        // отчет
                        case 16:
                            {
                                ReportButton.IsEnabled = true;
                            }
                            break;
                        // запуск машины с отходами на весовую со стороны улицы
                        case 17:
                            {
                                if (!ReadOnly)
                                    InputLaunchToWeightButton.IsEnabled = true;
                                else
                                    InputLaunchToWeightButton.IsEnabled = false;
                            }
                            break;
                        // запуск машины с отходами на весовую со стороны завода
                        case 18:
                            {
                                if (!ReadOnly)
                                    OutputLaunchToWeightButton.IsEnabled = true;
                                else
                                    OutputLaunchToWeightButton.IsEnabled = false;
                            }
                            break;


                    }
                    ButtonTimer.Finish();
                }
            }
        }

        /// <summary>
        /// карточка машины с мусором для редактирования
        /// </summary>
        private async void CarbageEdit()
        {
            int chgaId = SelectedItemCarbage.CheckGet("CHGA_ID").ToInt();

            var CarbageForm = new CarbageForm();
            CarbageForm.ReceiverName = TabName;

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("CHGA_ID", chgaId.ToString());
            }

            CarbageForm.Edit(p);
        }


        /// <summary>
        /// редактирование машины
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            buttons[14] = false;
            EditButton.IsEnabled = false;
            CarbageEdit();
            ButtonTimer.Run();
        }


        /// <summary>
        /// удаляем машину с мусором
        /// </summary>
        private async void CarbageDelete()
        {
            int chgaId = SelectedItemCarbage.CheckGet("CHGA_ID").ToInt();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "PaperProduction");
            q.Request.SetParam("Object", "ManagerWeightBdm2");
            q.Request.SetParam("Action", "CarbageDelete");
            q.Request.SetParam("ID", chgaId.ToString());
            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // вернулся не пустой ответ, обновим таблицу
                    // GridBox
                    //   GridCarbage.UpdateItemsAnswer(q.Answer, "ITEMS");

                    // GridBox4
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        GridCarbage.UpdateItems(ds);
                    }

                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// удаление машины
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            buttons[13] = false;
            DeleteButton.IsEnabled = false;

            int chgaId = SelectedItemCarbage.CheckGet("CHGA_ID").ToInt();
            if (chgaId > 0)
            {
                var dw = new DialogWindow($"Вы действительно хотите удалить машину [{SelectedItemCarbage.CheckGet("NOMER_CAR")}]?", "Удаление машины", "Подтверждение удаления машины из списка", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    CarbageDelete();
                    CarbageGridLoadItems();
                }
            }
            ButtonTimer.Run();
        }

        /// <summary>
        /// обновить список машин с мусором
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            buttons[15] = false;
            RefreshButton.IsEnabled = false;
            CarbageGridLoadItems();
            int chgaId = SelectedItemCarbage.CheckGet("CHGA_ID").ToInt();

            if ((chgaId > 0) && !ReadOnly)
            {
                EditButton.IsEnabled = true;
                DeleteButton.IsEnabled = true;
            }
            else
            {
                EditButton.IsEnabled = false;
                DeleteButton.IsEnabled = false;
            }

            ButtonTimer.Run();
        }

        /// <summary>
        /// Отчет по машинам с мусором за выбранную дату
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            buttons[16] = false;
            ReportButton.IsEnabled = false;
            CarbageReport();
            ButtonTimer.Run();
        }

        /// <summary>
        /// Деактивация контроллов
        /// </summary>
        public void DisableControls()
        {
            UserGridToolbar.IsEnabled = false;
            GridCarbage.IsEnabled = false;
        }

        /// <summary>
        /// Активация контроллов
        /// </summary>
        public void EnableControls()
        {
            UserGridToolbar.IsEnabled = true;
            GridCarbage.IsEnabled = true;
        }


        /// <summary>
        /// Отчет по машинам с мусором
        /// </summary>
        private async void CarbageReport()
        {
            DisableControls();

            if (Form.Validate())
            {
                var list = GridCarbage.Items;
                var listString = JsonConvert.SerializeObject(list);

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("DATA_LIST", listString);
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "PaperProduction");
                q.Request.SetParam("Object", "ManagerWeightBdm2");
                q.Request.SetParam("Action", "CreateGarbageExcelReportAll");
                q.Request.SetParams(p);

                q.Request.Timeout = 25000;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    Central.OpenFile(q.Answer.DownloadFilePath);
                }
                else
                {
                    q.ProcessError();
                }
            }
            EnableControls();
        }


        /// <summary>
        /// Принудительно запустить машину на весовую в сторону завода
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InputLaunchToWeightButton_Click(object sender, RoutedEventArgs e)
        {
            if (GarbageCar != null && GarbageCar.SelectedItem.Key != null)
            {
                buttons[17] = false;
                InputLaunchToWeightButton.IsEnabled = false;

                int chgaId = GarbageCar.SelectedItem.Key.ToInt();
                string msg = "";
                Dictionary<string, string> p = new Dictionary<string, string>();

                msg += $"Вы действительно хотите запустить";
                msg += $"\nмашину {GarbageCar.SelectedItem.Value}";
                msg += $"\nна весовую со стороны УЛИЦЫ?";
                p.CheckAdd("CMD", "33");

                var d = new DialogWindow($"{msg}", "Всё верно?", "", DialogWindowButtons.NoYes);

                d.ShowDialog();
                if (d.DialogResult == true)
                {
                    // запускаем на завод
                    p.CheckAdd("ID", "111");
                    p.CheckAdd("ID_SCRAP", chgaId.ToString());
                    p.CheckAdd("ID_STATUS", "32");
                    p.CheckAdd("USER_NAME", UserName);
                    {
                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Devices");
                        q.Request.SetParam("Object", "ManagerWeight");
                        q.Request.SetParam("Action", "Insert");
                        q.Request.SetParams(p);

                        q.DoQuery();

                        if (q.Answer.Status != 0)
                        {
                            //   q.ProcessError();
                        }
                    }

                    CarbageGridLoadItems();
                }
                ButtonTimer.Run();
            }
        }

        /// <summary>
        /// Принудительно запустить машину на весовую из завода
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OutputLaunchToWeightButton_Click(object sender, RoutedEventArgs e)
        {
            if (GarbageCar != null && GarbageCar.SelectedItem.Key != null)
            {
                buttons[18] = false;
                OutputLaunchToWeightButton.IsEnabled = false;

                int chgaId = GarbageCar.SelectedItem.Key.ToInt();
                string msg = "";
                Dictionary<string, string> p = new Dictionary<string, string>();

                msg += $"Вы действительно хотите запустить";
                msg += $"\nмашину {GarbageCar.SelectedItem.Value}";
                msg += $"\nна весовую со стороны ЗАВОДА?";
                p.CheckAdd("CMD", "43");

                var d = new DialogWindow($"{msg}", "Всё верно?", "", DialogWindowButtons.NoYes);

                d.ShowDialog();
                if (d.DialogResult == true)
                {
                    // выпускаем с завода
                    p.CheckAdd("ID", "111");
                    p.CheckAdd("ID_SCRAP", chgaId.ToString());
                    p.CheckAdd("ID_STATUS", "11");
                    p.CheckAdd("USER_NAME", UserName);
                    {
                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Devices");
                        q.Request.SetParam("Object", "ManagerWeight");
                        q.Request.SetParam("Action", "Insert");
                        q.Request.SetParams(p);

                        q.DoQuery();

                        if (q.Answer.Status != 0)
                        {
                            //   q.ProcessError();
                        }
                    }

                    CarbageGridLoadItems();

                }
                ButtonTimer.Run();
            }

        }

        /// <summary>
        ///  добавляем вручную машину с отходами
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            //            buttons[14] = false;
            //            EditButton.IsEnabled = false;
            CarbageAdd();
            //            ButtonTimer.Run();
        }

        /// <summary>
        /// карточка машины с мусором для редактирования
        /// </summary>
        private async void CarbageAdd()
        {

            if (GarbageCar != null && GarbageCar.SelectedItem.Key != null)
            {
                int chgcId = GarbageCar.SelectedItem.Key.ToInt();
                var CarbageAddForm = new CarbageAddForm();
                CarbageAddForm.ReceiverName = TabName;

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("CHGC_ID", chgcId.ToString());
                }

                CarbageAddForm.Edit(p);
            }

        }

        /// <summary>
        /// просмотр истории изменений по выбранной машине 
        /// </summary>
        private void HistoryGarbageCar()
        {
            var IdCarbage = SelectedItemCarbage.CheckGet("CHGA_ID").ToInt();
            var carbageHistoryForm = new CarbageHistoryForm();
            carbageHistoryForm.ReceiverName = TabName;

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID", IdCarbage.ToString());
            }

            carbageHistoryForm.Edit(p);

        }



    }
}
