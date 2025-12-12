using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Учет рулонов
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2021-03-16</released>
    /// <changed>2022-06-29</changed>
    public partial class ReelControlKsh : UserControl
    {
        public ReelControlKsh()
        {
            /*
                Production>Roll>ListReelTasks

                ручные операции с рулонами
                    Production>Roll>Put
                    Production>Roll>Remove
                    Production>Roll>SetRunning
                    Production>Roll>Move
             */

            //анимация транспаранта "Поставь рулон"
            AlertBlinkTimerInterval=1;
            //обновление данных
            LoadItemsTimerInterval=5;
            
            //возврат интерфейса в исходное состояние
            ReturnTimerTimeout=30;
            //интервал проверки
            ReturnTimerInterval=5;
            //блокировка главного интерфейса
            //задержка при возврате из дркгого окна
            UnblockTimerInterval=1;
            //задержка перед появлением прогресс-бара,мс
            ProgressBarDelay=1000;

            InitializeComponent();

            Loaded+=OnLoad;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this,ProcessMessages);

            AlertBlinkState=true;
            RightReelState=0;
            LeftReelState=0;
            ActiveReel=0;

            BaseMachineId=0;
            BaseReelId=0;
            CurrentMachineId=0;
            CurrentReelId=0;
            NoPrint=false;
            FosberReelManualMode=false;
            ActionsEnabledList=true;
            ActionsEnabledCommand=true;
            ActionsEnabledDelay=true;
            Dt="";
            WaitingFlag=false;

            LeftPutEnabled=true;
            RightPutEnabled=true;
            LeftRemoveEnabled=true;
            RightRemoveEnabled=true;
            PutEnabled=true;
            LastClick=DateTime.Now;

            TaskDS = new ListDataSet();
            RollBufferDS = new ListDataSet();
            RollDS = new ListDataSet();
            PrintingDS = new ListDataSet();
            PrintingValues = new List<Dictionary<string, string>>();
            
            DataActual = false;
            DataActualTtl = 90;
            DataActualLastDate = "";
            DataActualLastTtl = 30;


            RequestTimeoutGrid =Central.Parameters.RequestTimeoutMin;
            RequestAttemptsGrid= Central.Parameters.RequestAttemptsDefault;
            RequestTimeoutAction=1000;
            RequestAttemptsAction=1;

            DebugString = "";

            MaterialWriteOffInstanceKey ="Client.Interfaces.Production.RollControl.MaterialWroteOff.InstanceId";
            
            TaskGridInit();
            RollBufferGridInit();
            SetDefaults();
            

            Loaded += RollControl_Loaded;

            if(Central.DebugMode)
            {
                ReturnTimerTimeout = 3000;
            }

            ProcessPermissions();
        }

        private void RollControl_Loaded(object sender, RoutedEventArgs e)
        {
            SetActive();
        }

        private void SetActive()
        {
            TabUpdate();
            Central.Dbg("SetActive ReelControlKsh");
            Central.WM.SelectedTab = "ReelControlKsh";
            ReturnTimerRun();
        }

        public string RoleName = "[erp]reel_control_ksh";

        /// <summary>
        /// датасет, содержащий данные
        /// </summary>
        private ListDataSet TaskDS { get; set; }
        private ListDataSet RollBufferDS { get; set; }
        private ListDataSet RollDS { get; set; }
        private ListDataSet PrintingDS { get; set; }

        /// <summary>
        /// базовый ГА
        /// </summary>
        public int BaseMachineId { get; set; }
        /// <summary>
        /// базовый раскат
        /// </summary>
        public int BaseReelId { get; set; }
        /// <summary>
        /// текущий ГА
        /// 2=ГА1 (BHS)
        /// 21=ГА2 (BHS)
        /// 22=ГА3 (Fosber)
        /// </summary>
        public int CurrentMachineId { get; set; }
        /// <summary>
        /// текущий раскат
        /// </summary>
        public int CurrentReelId { get; set; }
        /// <summary>
        /// id рулона не левом раскате
        /// </summary>
        private int LeftReelRollId { get; set; }
        /// <summary>
        /// id рулона не правом раскате
        /// </summary>
        private int RightReelRollId { get; set; }
        /// <summary>
        /// prce_id активного левого раската
        /// </summary>
        private int CurrentLeftReelPrceId { get; set; }
        /// <summary>
        /// prce_id активного правого раската
        /// </summary>
        private int CurrentRightReelPrceId { get; set; }
        /// <summary>
        /// состояние правого раската
        /// 0=неактивен,1=активен,2=пустой
        /// </summary>
        private int RightReelState { get; set; }
        /// <summary>
        /// состояние левого раската
        /// 0=неактивен,1=активен,2=пустой
        /// </summary>
        private int LeftReelState { get; set; }
        /// <summary>
        /// активный раскат
        /// 1=левый,2=правый
        /// </summary>
        private int ActiveReel { get; set; }

        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string,string> TaskGridSelectedItem { get;set;}
        /// <summary>
        /// выбранная в гриде запись
        /// </summary>
        Dictionary<string, string> SelectedBufferItem { get; set; }
        /// <summary>
        /// Параметры установки клише для печати на листах гофрокартона
        /// </summary>
        List<Dictionary<string, string>> PrintingValues { get; set; }
        /// <summary>
        /// флаг подавления печати,
        /// если поднят, то при перемещении рулона автоматическая печать ярлыка будет подавлена,
        /// нужен для отладки,
        /// по умолчанию false
        /// </summary>
        private bool NoPrint { get;set;}
        /// <summary>
        /// ручной режим для фосбера
        /// в ручном режиме можнос тавить рулоны на раскат вручную
        /// </summary>
        private bool FosberReelManualMode {get;set;}
        /// <summary>
        /// разрешение выполнения операций
        /// тулбар с кнопками операций активен, когда влаг поднят
        /// </summary>
        private bool ActionsEnabledList {get;set;}
        private bool ActionsEnabledCommand {get;set;}
        private bool ActionsEnabledDelay {get;set;}
        /// <summary>
        /// задержка перед появлением прогресс-бара,мс
        /// </summary>
        private int ProgressBarDelay {get;set;}

        private bool LeftPutEnabled {get;set;}
        private bool RightPutEnabled {get;set;}
        private bool LeftRemoveEnabled {get;set;}
        private bool RightRemoveEnabled {get;set;}
        private bool PutEnabled {get;set;}
        private DateTime LastClick {get;set;}
        private string Dt{get;set;}
        private bool WaitingFlag {get;set;}

        private int RequestTimeoutGrid {get;set;}
        private int RequestAttemptsGrid {get;set;}
        private int RequestTimeoutAction {get;set;}
        private int RequestAttemptsAction {get;set;}

        private string MaterialWriteOffInstanceKey {get;set;}
        /// <summary>
        /// флаг актуальности данных
        /// </summary>
        private bool DataActual { get; set; }
        /// <summary>
        /// Ttl, после которого данные считаются устаревшими, сек.
        /// </summary>
        private int DataActualTtl {get;set;}
        /// <summary>
        /// дата получения последний актуальных данных
        /// </summary>
        private string DataActualLastDate { get; set; }
        /// <summary>
        /// Ttl после которого появится транспарант "нет данных АСУТП"
        /// </summary>
        private int DataActualLastTtl { get; set; }

        private string DebugString { get; set; }

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
                    BurgerRestore.IsEnabled = true;
                    break;

                case Role.AccessMode.FullAccess:
                    BurgerRestore.IsEnabled = true;
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    BurgerRestore.IsEnabled = false;
                    break;
            }

            //UIUtil.SetFrameworkElementEnabledByTagAccessMode(this.Content as DependencyObject, Acl.AccessMode.ReadOnly);

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
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage obj)
        {
            //Group 
            if (obj.ReceiverGroup.IndexOf("Production") > -1)
            {
                if (obj.ReceiverName.IndexOf("ReelControlKsh") > -1)
                {
                    switch (obj.Action)
                    {
                        case "Refresh":
                            LoadItems(0);                            
                            break;
                            
                        case "Loaded":
                            Render();
                            break;

                        case "MoveRoll":
                            var o=(Dictionary<string,string>)obj.ContextObject;
                            MoveRoll(o);
                            break;
                    }
                }
            }

            //Broadcast
            {
                {
                    switch (obj.Action)
                    {
                        case "Resized":
                            Render();
                            break;

                        case "Initialized":
                            Init();
                            break;
                    }
                }
            }

            if(obj.Action=="Closed")
            {
                if(
                    obj.SenderName=="DocTouch"
                    || obj.SenderName=="RollControlHelp"
                    || obj.SenderName=="RollPrintLabel"                    
                    || obj.SenderName=="ErrorTouch"
                    || obj.SenderName=="RollCorrectWeight"
                    || obj.SenderName=="RollMaterialWriteOff"
                    || obj.SenderName=="RollReceiptViewer"
                    || obj.SenderName=="RollRemove"
                    || obj.SenderName=="RollRestore"
                    || obj.SenderName=="RollSetDefect"
                    || obj.SenderName=="Settings"
                    || obj.SenderName == "LogTouch"
                )
                {
                    BlockInterface();
                    SetActive();
                }
            }
        }

        /// <summary>
        /// отображение статьи в справочной системе
        /// </summary>
        public void ShowHelp()
        {            
            //Central.ShowHelp($"/doc/l-pack-erp/production/roll_control");
            var h=new RollControlHelp();
            h.Init();
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="Production",
                ReceiverName = "ReelControlKsh",
                SenderName = "ReelControlKsh",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            TaskGrid.Destruct();
            RollBufferGrid.Destruct();

            AlertBlinkStop();
            LoadItemsStop();
            ReturnTimerStop();
        }

        /// <summary>
        /// инициализация 
        /// </summary>
        public void Init()
        {
            ReturnInterface(true);
            LoadItemsRun();   
            AlertBlinkRun();
            Render();
            ReturnTimerRun();
        }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {
            BaseMachineId=23;
            BaseReelId=1;

            SetPrceId(BaseMachineId, BaseReelId);

            //параметры запуска
            var p =Central.Navigator.Address.Params;

            var machineId=p.CheckGet("machine_id").ToInt();
            if(machineId!=0)
            {
                if(machineId.ContainsIn(23))
                {
                    BaseMachineId=machineId;
                }
            }

            var reelId=p.CheckGet("reel_number").ToInt();
            if(reelId!=0)
            {
                BaseReelId=reelId;
            }

            Init();
        }

        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            var x = e.Key.ToString();

            
            var code = Central.WM.GetScannerInput();
            if (!code.IsNullOrEmpty())
            {
                //if(Central.DebugMode)
                {
                    Mem.Text = code;
                    //System.Threading.Thread.Sleep(500);
                }
                GetMaterial(code);                
            }

            {
                var m = "";
                if(!code.IsNullOrEmpty())
                {
                    m = m.Append($"SCANNER:[{code}]");
                }
                if(!x.IsNullOrEmpty())
                {
                    m = m.Append($"KEY:[{x}]");
                }

                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "Production",
                    ReceiverName = "LogTouch",
                    SenderName = "ReelControlKsh",
                    Action = "KeyPressed",
                    Message = $"{x}",
                });
            }
            

            switch (e.Key)
            {
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Enter:
                    break;
            }

            Central.Dbg($"{x}");
        }

        /// <summary>
        /// вычисление размеров сложных блоков
        /// </summary>
        public void Render()
        {
            TaskGrid.Width=TaskGridContainer.ActualWidth;
            TaskGrid.Height=TaskGridContainer.ActualHeight;

            RollBufferGrid.Width=GridBoxContainer.ActualWidth;
            RollBufferGrid.Height=GridBoxContainer.ActualHeight-RollWetList.ActualHeight;

            SetActive();

            if(Central.DebugMode)
            {
                BurgerDebug.Visibility=Visibility.Visible;
            }else{
                BurgerDebug.Visibility=Visibility.Collapsed;
            }
        }
        
        /// <summary>
        /// формирует уникальный идентификатор фрейма
        /// </summary>
        /// <returns></returns>
        public string GetFrameName()
        {
            string result = "";
            result = $"ReelControlKsh";
            result=result.MakeSafeName();
            return result;
        }

        private void TabUpdate()
        {
            var frameName = GetFrameName();
            Central.WM.SetActive(frameName);
            Central.WM.SelectedTab = frameName;
        }

        private void OnLoad(object sender,RoutedEventArgs e)
        {
            TabUpdate();

            //отправляем сообщение о загрузке интерфейса
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="Production",
                ReceiverName = "ReelControlKsh",
                SenderName = "ReelControlKsh",
                Action = "Loaded",
            });

        }

        /// <summary>
        /// инициализация грида "Задания"
        /// </summary>
        public void TaskGridInit()
        {
            //инициализация грида
            {
                //список колонок грида
                var columns = new List<DataGridHelperColumn>()
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Z",
                        Path="FANFOLD",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=4,
                        Doc="Бесконечный картон",
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Задание",
                        Path="NUM",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=8,
                        Doc="Номер ПЗ. Выделены задания с печатью",
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // если в доп.поле стоит отметка о печати в задании, раскрашиваем ячейку
                                    if (row.CheckGet("HAS_PRINTING").ToInt() == 1)
                                    {
                                        color=HColor.Brown;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            }
                        },
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Длина, м",
                        Path="LEN",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=7,
                        Doc="Длина задания. Выделены задания, после которых требуется разблокировать сплайсер",
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // В конце задания надо разблокировать сплайсер
                                    if ((row.CheckGet("CURR_REEL").ToInt() == 1) && (row.CheckGet("UNBLOCK_SPLAYCER").ToInt() == 1))
                                    {
                                        color=HColor.Pink;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            }
                        },
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Профиль",
                        Path="PROFIL_MACHINE",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=5,
                        Doc="Каждый профиль на своем фоне",
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    switch (row["PROFIL_NAME"])
                                    {
                                        case "В":
                                            color=HColor.Violet;
                                            break;

                                        case "С":
                                            color=HColor.Blue;
                                            break;
                                        
                                        case "Е":
                                            color=HColor.Pink;
                                            break;

                                        case "ВВ":
                                            break;

                                        case "ВС":
                                            color=HColor.Yellow;
                                            break;

                                        case "ВЕ":
                                            color=HColor.Green;
                                            break;

                                        case "ЕВ":
                                            color=HColor.Blue;
                                            break;

                                        case "ЕС":
                                            color=HColor.Orange;
                                            break;
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
                    new DataGridHelperColumn()
                    {
                        Header="Обрезь",
                        Path="OBREZ",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=6,
                        Doc="Обрезь в мм",
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Формат",
                        Path="FORMAT",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Сырье",
                        Path="RAW_NAME",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=11,
                        Doc="Выделены крашенная бумага, склеенная бумага, вес строго",
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                (Dictionary<string, string> row) =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    //Используется крашенная бумага
                                    if( row.CheckGet("PAINTED_FLAG").ToInt() == 1 )
                                    {
                                        color = HColor.Brown;
                                    }

                                    // Бумагу надо склеить
                                    if (row.CheckGet("GLUED_FLAG").ToInt() == 1)
                                    {
                                        color = HColor.YellowOrange;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                            {
                                StylerTypeRef.ForegroundColor,
                                (Dictionary<string, string> row) =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Использовать бумагу только заданной плотности
                                    if( row.CheckGet("FIXED_WEIGHT_FLAG").ToInt() == 1 )
                                    {
                                        color = HColor.RedFG;
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
                        Header="Состав",
                        Path="PARENT_QID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width2=16,
                        Doc="Для визуального выделения групп заданий с одинаковым составом",
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                (Dictionary<string, string> row) =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // Подстветка разного состава
                                    switch (row["NUM_COLOR"].ToInt() % 10)
                                    {
                                        case 1:
                                            color = HColor.Green;
                                            break;
                                        case 2:
                                            color = HColor.Violet;
                                            break;
                                        case 3:
                                            color = HColor.Blue;
                                            break;
                                        case 4:
                                            color = HColor.Orange;
                                            break;
                                        case 5:
                                            color = HColor.LightSelection;
                                            break;
                                        case 6:
                                            color = HColor.Olive;
                                            break;
                                        case 7:
                                            color = HColor.Pink;
                                            break;
                                        case 8:
                                            color = HColor.YellowOrange;
                                            break;
                                        case 9:
                                            color = HColor.VioletPink;
                                            break;
                                        case 0:
                                            color = HColor.Gray;
                                            break;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        }
                    },
                  


                    new DataGridHelperColumn
                    {
                        Header="ИД задания",
                        Path="ID_PZ",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=60,
                        MaxWidth=60,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Есть раскат в задании",
                        Path="CURR_REEL",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Крашенная бумага",
                        Path="PAINTED_FLAG",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="NUM_COLOR",
                        Path="NUM_COLOR",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Склейка",
                        Path="GLUED_FLAG",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Разблокировать сплайсер",
                        Path="UNBLOCK_SPLAYCER",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес строго",
                        Path="FIXED_WEIGHT_FLAG",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Профиль",
                        Path="PROFIL_NAME",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Гофромашина1",
                        Path="MACHINE1",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Гофромашина2",
                        Path="MACHINE2",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                };
                TaskGrid.SetColumns(columns);

                // цветовая маркировка строк
                TaskGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // Выделяем, если раскат не участвует в задании
                            if(row.CheckGet("CURR_REEL").ToInt() == 0)
                            {
                                color = HColor.Yellow;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                TaskGrid.SetSorting("_ROWNNMBER", ListSortDirection.Ascending);
                TaskGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
                TaskGrid.UseSorting = false;
                TaskGrid.AutoUpdateInterval=0;
                TaskGrid.PrimaryKey="ID_PZ";
                TaskGrid.UseRowHeader=false;
                TaskGrid.SetMode(1);
                TaskGrid.Init();

                TaskGrid.OnFilterItems = FilterItems;
                //при выборе строки в гриде, обновляются актуальные действия для записи
                TaskGrid.OnSelectItem=(Dictionary<string,string> selectedItem) =>
                {
                    TaskGridActionsUpdate(selectedItem);
                };
            }
        }

        /// <summary>
        /// инициализация грида "Рулоны"
        /// </summary>
        private void RollBufferGridInit()
        {
            var positionHidden = true;
            if (CurrentMachineId == 23)
            {
                positionHidden = false;
            }

            //список колонок грида
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Сырье",
                    Path="RAW_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=180,
                    MaxWidth=300,
                },
                new DataGridHelperColumn
                {
                    Header="Вес, кг",
                    Path="QTY",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=40,
                    MaxWidth=150,
                    Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            (Dictionary<string, string> row) =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = "";

                                //красный -- рулон заблокирован
                                if( row.CheckGet("ROLL_BLOCKED_FLAG").ToInt() == 1 )
                                {
                                    color = HColor.Red;
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
                    Header="Длина, м",
                    Path="LNGTH",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=150,
                },
                new DataGridHelperColumn
                {
                    Header="Имп.",
                    Description="Импортный рулон",
                    Path="_IMPORT_STAUS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=60,
                    MaxWidth=60,                   
                },
                new DataGridHelperColumn
                {
                    Header="Номер",
                    Path="ROLL_NUMBER",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=120,
                    MaxWidth=150,
                },
                
                new DataGridHelperColumn
                {
                    Header="Позиция",
                    Path="ROLL_POSITION",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    MinWidth=50,
                    MaxWidth=50,
                    Hidden=positionHidden,
                },
                new DataGridHelperColumn
                {
                    Header="Комментарий",
                    Path="NOTE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    MinWidth=50,
                    MaxWidth=150,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="PLACE",
                    Path="PLACE",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Дата/время",
                    Path="DT_TM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Номер рулона",
                    Path="NUM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Прошлый раскат",
                    Path="ID_REEL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="PLACE_PREV",
                    Path="PLACE_PREV",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Список влажности",
                    Path="WET_LIST",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="БДМ",
                    Path="BDM",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="IDP_ROLL",
                    Path="IDP_ROLL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ID_STATUS",
                    Path="ID_STATUS",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="NORM_ROLL",
                    Path="NORM_ROLL",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ROLL_BLOCKED_FLAG",
                    Path="ROLL_BLOCKED_FLAG",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="IMPORT_PRODUCED_DT",
                    Path="IMPORT_PRODUCED_DT",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
            };
            RollBufferGrid.SetColumns(columns);

            RollBufferGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
            {
                // определение цветов фона строк
                {
                    StylerTypeRef.BackgroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        //таблица ROLLS_STATUS

                        //В буфере для перемещения на склад
                        if(row.CheckGet("ID_STATUS").ToInt() == 4)
                        {
                            color = HColor.Blue;
                        }

                        //Рулон движется с раската на выход
                        if(row.CheckGet("ID_STATUS").ToInt() == 36)
                        {                          
                            color = HColor.Olive;
                        }

                        //Рулон просканирован на входе, данные в транспортную систему переданы
                        if(row.CheckGet("ID_STATUS").ToInt() == 33)
                        {                          
                            color = HColor.Green;
                        }

                        //Отложен в буфере для чего-либо
                        if(row.CheckGet("ID_STATUS").ToInt() == 6)
                        {
                            color = HColor.Yellow;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },

                // определение цветов шрифта строк
                {
                    StylerTypeRef.ForegroundColor,
                    row =>
                    {
                        var result=DependencyProperty.UnsetValue;
                        var color = "";

                        //розовый -- Отложен
                        //ROLLS_STATUS.ID_STATUS=3	Забраковано в Z0	В буфере для перемещения в Z0 для забраковки (обычно не наше сырьё)                        
                        if(row.CheckGet("ID_STATUS").ToInt()! == 3)
                        {
                            color = HColor.MagentaFG;
                        }

                        if (!string.IsNullOrEmpty(color))
                        {
                            result=color.ToBrush();
                        }

                        return result;
                    }
                },
            };
            
            RollBufferGrid.SetSorting("_ROWNNMBER", ListSortDirection.Ascending);
            RollBufferGrid.AutoUpdateInterval=0;
            RollBufferGrid.SetMode(1);
            RollBufferGrid.PrimaryKey="IDP_ROLL";
            RollBufferGrid.Init();

            RollBufferGrid.OnSelectItem = (Dictionary<string, string> selectedItem) =>
            {
                RollBufferGridActionsUpdate();
            };

            RollBufferGrid.OnDblClick = selectedItem =>
            {
                EditRoll();
            };
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            //при переключении раската или агрегата
            //ставится флаг, что данные актуальны
            DataActual = true;
            DataActualLastDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            UpdateDataActual();


            CorrugatedMachineButton.Content="ГА";
            CurrentSourceName.Text="";
            CurrentTaskRest.Text="0";

            {
                LeftSourceName.Text="";
                LeftSourceRest.Text="0";

                SetReelState(1,0);
            }

            {
                RightSourceName.Text="";
                RightSourceRest.Text="0";

                SetReelState(2,0);
            
            }


            TaskDS = new ListDataSet();
            TaskDS.Init();
            TaskGrid.UpdateItems(TaskDS);

            RollBufferDS = new ListDataSet();
            RollBufferDS.Init();
            RollBufferGrid.UpdateItems(RollBufferDS);
        }

        /// <summary>
        /// Установка состояния раската
        /// </summary>
        /// <param name="reelId">1=левый,2=правый</param>
        /// <param name="stateId">0=неактивный,1=активный,2=пустой,3=не используется</param>
        public void SetReelState(int reelId=0, int stateId=0, string sourceName="", int sourceRest=0, int rollIdp=0,string rollNumber="")
        {
            var dbg = false;
            if(Central.DebugMode)
            {
                dbg = true;
            }

            //установка состояния раската
            switch(reelId) { 
                //левый
                case 1:
                    {
                        LeftReelState = stateId;
                        LeftSourceName.Text = sourceName;
                        LeftSourceRest.Text = sourceRest.ToString();
                        //if(dbg)
                        //{
                        //    LeftSourceLabel.Text = rollIdp.ToString();
                        //}
                        LeftIdp.Text = rollNumber.ToString();
                    }
                    break;

                //правый
                case 2:
                    {
                        RightReelState = stateId;
                        RightSourceName.Text = sourceName;
                        RightSourceRest.Text = sourceRest.ToString();
                        //if(dbg)
                        //{
                        //    RightSourceLabel.Text = rollIdp.ToString();
                        //}
                        RightIdp.Text = rollNumber.ToString();
                    }
                    break;
            }

           

            bool emptyBuffer = false;
            // Если буфер пустой, отключим возможность установки рулона
            if (RollBufferGrid.Items == null)
            {
                emptyBuffer = true;
            }
            else
            {
                if (RollBufferGrid.Items.Count == 0)
                {
                    emptyBuffer = true;
                }
            }

            //блоки визуализации раскатов
            switch (stateId)
            {
                // 1 -- активный, рулон установлен, разматывается
                // 4 -- активный, рулона нет на раскате
                case 1:
                case 4:

                    switch (reelId)
                    {
                        //левый
                        case 1:
                            //запуск раската
                            
                            LeftReelBlock.Visibility=Visibility.Visible;
                            LeftInfoBlock.Visibility = Visibility.Collapsed;
                            LeftAlertBlock.Visibility=Visibility.Collapsed;
                            LeftReelBlock.Style=(Style)LeftReelBlock.TryFindResource("RollReelBlockActive");
                            LeftReelIcon.Style=(Style)RightReelIcon.TryFindResource("RollReelIconActive");

                            //останов оппозитного раската
                            //в один момент может быть активным только один раскат
                            if(ActiveReel==2 && (RightReelState == 1 || RightReelState == 4))
                            {
                                SetReelState(2,0);
                            }

                            ActiveReel=reelId;

                            if (stateId == 4)
                            {
                                LeftInfoBlock.Visibility = Visibility.Visible;
                            }    

                            break;

                        //правый
                        case 2:
                            //запуск раската
                            
                            RightReelBlock.Visibility=Visibility.Visible;
                            RightInfoBlock.Visibility = Visibility.Collapsed;
                            RightAlertBlock.Visibility=Visibility.Collapsed;
                            RightReelBlock.Style=(Style)LeftReelBlock.TryFindResource("RollReelBlockActive");
                            RightReelIcon.Style=(Style)RightReelIcon.TryFindResource("RollReelIconActive");
                            
                            //останов оппозитного раската
                            //в один момент может быть активным только один раскат
                            if(ActiveReel==1 && (LeftReelState == 1 || LeftReelState == 4))
                            {
                                SetReelState(1,0);
                            }

                            ActiveReel=reelId;

                            if (stateId == 4)
                            {
                                RightInfoBlock.Visibility = Visibility.Visible;
                            }

                            break;
                    }

                    break;

                //рулон не установлен, транспарант "Поставь рулон"
                case 2:

                    //AlertBlinkRun();
                    switch (reelId)
                    {
                        //левый
                        case 1:
                            LeftReelBlock.Visibility = Visibility.Collapsed;
                            LeftInfoBlock.Visibility = Visibility.Collapsed;
                            LeftAlertBlock.Visibility = Visibility.Visible;
                            LeftReelIcon.Style = (Style)RightReelIcon.TryFindResource("RollReelIconAlert");
                            break;

                        //правый
                        case 2:
                            RightReelBlock.Visibility = Visibility.Collapsed;
                            RightInfoBlock.Visibility = Visibility.Collapsed;
                            RightAlertBlock.Visibility = Visibility.Visible;
                            RightReelIcon.Style = (Style)RightReelIcon.TryFindResource("RollReelIconAlert");
                            break;
                    }

                    break;
                
                //неактивный, 0 - рулон установлен, но не используется, 3 - раскат не используется
                default:
                case 0:
                case 3:

                    switch(reelId)
                    {
                        //левый
                        case 1:
                            LeftReelBlock.Visibility=Visibility.Visible;
                            LeftInfoBlock.Visibility = Visibility.Collapsed;
                            LeftAlertBlock.Visibility=Visibility.Collapsed;
                            LeftReelBlock.Style=(Style)LeftReelBlock.TryFindResource("RollReelBlockIdle");
                            LeftReelIcon.Style=(Style)RightReelIcon.TryFindResource("RollReelIconIdle");
                            break;

                        //правый
                        case 2:
                            RightReelBlock.Visibility=Visibility.Visible;
                            RightInfoBlock.Visibility = Visibility.Collapsed;
                            RightAlertBlock.Visibility=Visibility.Collapsed;
                            RightReelBlock.Style=(Style)LeftReelBlock.TryFindResource("RollReelBlockIdle");
                            RightReelIcon.Style=(Style)RightReelIcon.TryFindResource("RollReelIconIdle");
                            break;
                    }

                    break;
            }
                      

            //кнопки установка-снятие
            //0=неактивный,1=активный,2=пустой
            switch(stateId)
            {
                //рулон установлен, активны кнопки: "Снять"             
                case 1:
                case 0:
                case 4:

                    switch(reelId)
                    {
                        //левый
                        case 1:
                            LeftPutEnabled=false;
                            //LeftPutRollButton.Style=(Style)LeftPutRollButton.TryFindResource("RollReelButtonDisabled");
                            //LeftRemoveRollButton.Style=(Style)LeftRemoveRollButton.TryFindResource("RollReelButton");
                            break;

                        //правый
                        case 2:
                            RightPutEnabled=false;
                            //RightPutRollButton.Style=(Style)RightPutRollButton.TryFindResource("RollReelButtonDisabled");
                            //RightRemoveRollButton.Style=(Style)RightRemoveRollButton.TryFindResource("RollReelButton");
                            break;
                    }

                    break;

                //рулон не установлен, активны кнопки: "Поставить"
                default:
                case 2:
                case 3:

                    switch(reelId)
                    {
                        //левый
                        case 1:
                            LeftRemoveEnabled=false;
                            //LeftPutRollButton.Style=(Style)LeftPutRollButton.TryFindResource("RollReelButton");
                            //LeftRemoveRollButton.Style=(Style)LeftRemoveRollButton.TryFindResource("RollReelButtonDisabled");
                            break;

                        //правый
                        case 2:
                            RightRemoveEnabled=false;
                            //RightPutRollButton.Style=(Style)RightPutRollButton.TryFindResource("RollReelButton");
                            //RightRemoveRollButton.Style=(Style)RightRemoveRollButton.TryFindResource("RollReelButtonDisabled");
                            break;
                    }

                    break;     
                
            }
            
            // Если буфер пустой, то кнопки "Поставить" неактивны  
            if (emptyBuffer)
            {
                switch (reelId)
                {
                    //левый
                    case 1:
                        LeftPutEnabled=false;
                        //LeftPutRollButton.Style = (Style)LeftPutRollButton.TryFindResource("RollReelButtonDisabled");
                        break;

                    //правый
                    case 2:
                        RightPutEnabled=false;
                        //RightPutRollButton.Style = (Style)RightPutRollButton.TryFindResource("RollReelButtonDisabled");
                        break;
                }

            }

            SetButtonsState();
            
        }

        /// <summary>
        /// Настройка доступности и стиля кнопок
        /// </summary>
        private void SetButtonsState()
        {
            if(LeftPutEnabled && PutEnabled)
            {
                LeftPutRollButton.Style=(Style)LeftPutRollButton.TryFindResource("RollReelButton");
                LeftPutRollButton.IsEnabled=true;
            }
            else
            {
                LeftPutRollButton.Style=(Style)LeftPutRollButton.TryFindResource("RollReelButtonDisabled");
                LeftPutRollButton.IsEnabled=false;
            }

            if(RightPutEnabled && PutEnabled)
            {
                RightPutRollButton.Style=(Style)RightPutRollButton.TryFindResource("RollReelButton");
                RightPutRollButton.IsEnabled=true;
            }
            else
            {
                RightPutRollButton.Style=(Style)RightPutRollButton.TryFindResource("RollReelButtonDisabled");
                RightPutRollButton.IsEnabled=false;
            }

            if(LeftRemoveEnabled)
            {
                LeftRemoveRollButton.Style=(Style)LeftRemoveRollButton.TryFindResource("RollReelButton");
                LeftRemoveRollButton.IsEnabled=true;
            }
            else
            {
                LeftRemoveRollButton.Style=(Style)LeftRemoveRollButton.TryFindResource("RollReelButtonDisabled");
                LeftRemoveRollButton.IsEnabled=false;
            }

            if(RightRemoveEnabled)
            {
                RightRemoveRollButton.Style=(Style)RightRemoveRollButton.TryFindResource("RollReelButton");
                RightRemoveRollButton.IsEnabled=true;
            }
            else
            {
                RightRemoveRollButton.Style=(Style)RightRemoveRollButton.TryFindResource("RollReelButtonDisabled");
                RightRemoveRollButton.IsEnabled=false;
            }

            ProcessPermissions();
        }

        
        /// <summary>
        /// таймер анимации транспаранта Поставь рулон
        /// </summary>
        private DispatcherTimer AlertBlinkTimer { get; set; }
        /// <summary>
        /// интервал анимации транспаранта Поставь рулон, сек
        /// </summary>
        private int AlertBlinkTimerInterval { get; set; }
        /// <summary>
        /// состояние транспаранта
        /// </summary>
        private bool AlertBlinkState { get; set; }
        /// <summary>
        /// запус таймера анимации
        /// </summary>
        private void AlertBlinkRun()
        {
            if(AlertBlinkTimerInterval != 0)
            {
                if(AlertBlinkTimer == null)
                {
                    AlertBlinkTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0,0,AlertBlinkTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", AlertBlinkTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("RollControl_AlertBlinkRun", row);
                    }

                    AlertBlinkTimer.Tick += (s,e) =>
                    {

                        //Central.Dbg($"AlertBlinkTimer");

                        if(AlertBlinkState)
                        {
                            AlertBlinkState=false;
                        }
                        else
                        {
                            AlertBlinkState=true;
                        }

                        if(LeftReelState==2)
                        {
                            if(AlertBlinkState)
                            {
                                LeftAlertBlock.Visibility=Visibility.Visible;
                            }
                            else
                            {
                                LeftAlertBlock.Visibility=Visibility.Collapsed;
                            }
                        }
                        else if (LeftReelState == 4)
                        {
                            if (AlertBlinkState)
                            {
                                LeftInfoBlock.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                LeftInfoBlock.Visibility = Visibility.Collapsed;
                            }
                        }

                        if(RightReelState==2)
                        {
                            if(AlertBlinkState)
                            {
                                RightAlertBlock.Visibility=Visibility.Visible;
                            }
                            else
                            {
                                RightAlertBlock.Visibility=Visibility.Collapsed;
                            }
                        }
                        else if (RightReelState == 4)
                        {
                            if (AlertBlinkState)
                            {
                                RightInfoBlock.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                RightInfoBlock.Visibility = Visibility.Collapsed;
                            }
                        }
                    };
                }

                if(AlertBlinkTimer.IsEnabled)
                {
                    AlertBlinkTimer.Stop();
                }
                AlertBlinkTimer.Start();
            }
        }
        //останов таймера анимации
        private void AlertBlinkStop()
        {
            if(AlertBlinkTimer != null)
            {
                if(AlertBlinkTimer.IsEnabled)
                {
                    AlertBlinkTimer.Stop();
                }
            }
        }

        /// <summary>
        /// таймер анимации транспаранта Поставь рулон
        /// </summary>
        private DispatcherTimer LoadItemsTimer { get; set; }
        /// <summary>
        /// интервал анимации транспаранта Поставь рулон, сек
        /// </summary>
        private int LoadItemsTimerInterval { get; set; }
        /// <summary>
        /// запусr таймера получения данных
        /// </summary>
        private void LoadItemsRun()
        {
            if(LoadItemsTimerInterval != 0)
            {
                if(LoadItemsTimer == null)
                {
                    LoadItemsTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0,0,LoadItemsTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", LoadItemsTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("ReelControlKsh_LoadItemsRun", row);
                    }

                    LoadItemsTimer.Tick += (s,e) =>
                    {
                        //Central.Dbg($"LoadItemsTimer");
                        LoadItems(0);
                    };
                }

                if(LoadItemsTimer.IsEnabled)
                {
                    LoadItemsTimer.Stop();
                }
                LoadItemsTimer.Start();
            }
        }
        //останов таймера получения данных
        private void LoadItemsStop()
        {
            if(LoadItemsTimer != null)
            {
                if(LoadItemsTimer.IsEnabled)
                {
                    LoadItemsTimer.Stop();
                }
            }
        }


        /// <summary>
        /// таймер возврата интерфейса в исходное состояние
        /// в неактивном состоянии интерфейс возвращается к базовому состоянию
        /// </summary>
        private DispatcherTimer ReturnTimer { get; set; }
        /// <summary>
        /// интервал возврата интерфейса к базовым настрокам
        /// </summary>
        private int ReturnTimerInterval { get; set; }
        private int ReturnTimerTimeout { get; set; }

        public void ReturnTimerReset()
        {
            LastClick=DateTime.Now;
        }

        /// <summary>
        /// запусr таймера возврата интерфейса в исходное состояние
        /// </summary>
        private void ReturnTimerRun()
        {
            if(
                Central.EmbdedMode
                || Central.DebugMode
            )
            {
                //Central.Dbg($"ReturnTimerRun");

                if(ReturnTimerInterval != 0)
                {
                    if(ReturnTimer == null)
                    {
                        ReturnTimer = new DispatcherTimer
                        {
                            Interval = new TimeSpan(0,0,ReturnTimerInterval)
                        };

                        {
                            var row = new Dictionary<string, string>();
                            row.CheckAdd("TIMEOUT", ReturnTimerInterval.ToString());
                            row.CheckAdd("DESCRIPTION", "");
                            Central.Stat.TimerAdd("ReelControlKsh_ReturnTimerRun", row);
                        }

                        ReturnTimer.Tick += (s,e) =>
                        {
                            //Central.Dbg($"ReturnTimer");
                            ReturnInterface();
                        };
                    }

                    if(ReturnTimer.IsEnabled)
                    {
                        ReturnTimer.Stop();
                    }
                    ReturnTimer.Start();
                }
            }
            
        }
        //останов таймера возврата интерфейса в исходное состояние
        private void ReturnTimerStop()
        {
            Central.Dbg($"ReturnTimerStop");

            if(ReturnTimer != null)
            {
                if(ReturnTimer.IsEnabled)
                {
                    ReturnTimer.Stop();
                }
                ReturnTimer=null;
            }
        }
       

        /// <summary>
        /// возврат интерфейса в исходное состояние
        /// у интерфейса есть базовое состояние: ГА и раскат, которые он отображает
        /// при переключении интерфейса в другое сосотояение оно отображается
        /// определенное время, затем интерфейс возвращается в исходное состояние
        /// любой клик на элементе сбрабывает таймер возврата:
        ///  -- кнопки переключения раската
        ///  -- кнопки выбора агрегата
        ///  -- выбор строки в гриде
        /// </summary>
        private void ReturnInterface(bool force=false)
        {
            
            //ReturnTimerStop();

            var today=DateTime.Now;
            var dt=((TimeSpan)(today-LastClick)).TotalSeconds;

            Dt=$"{dt.ToInt().ToString()}";
            //Central.Logger.Message($"ReturnTimer inner dt=[{dt}] last=[{LastClick.ToString("dd.MM.yyyy HH:mm:ss")}] today=[{today.ToString("dd.MM.yyyy HH:mm:ss")}]");
            //Central.Logger.Message($"CurrentMachineId=[{CurrentMachineId}] BaseMachineId=[{BaseMachineId}]");
            //Central.Logger.Message($"CurrentReelId=[{CurrentReelId}] BaseReelId=[{BaseReelId}]");

            if(
                dt > ReturnTimerTimeout
                || force
            )
            {
                if(
                    CurrentMachineId!=BaseMachineId
                    || CurrentReelId!=BaseReelId
                )
                {
                    CurrentMachineId=BaseMachineId;
                    CurrentReelId=BaseReelId;

                    SetDefaults();

                    CheckMachine();
                    CheckReel();
                    LoadItems(0);
                    //LoadItems(1);

                    SetPrceId(CurrentMachineId, CurrentReelId);
                }
            }

           
        }



        /// <summary>
        /// </summary>
        private DispatcherTimer UnblockTimer { get; set; }
        /// <summary>
        /// </summary>
        private int UnblockTimerInterval { get; set; }

        /// <summary>
        /// </summary>
        private void UnblockTimerRun()
        {
            //Central.Dbg($"UnblockTimerRun");

            if(UnblockTimerInterval != 0)
            {
                if(UnblockTimer == null)
                {
                    UnblockTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0,0,UnblockTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", UnblockTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("ReelControlKsh_UnblockTimerRun", row);
                    }

                    UnblockTimer.Tick += (s,e) =>
                    {
                        //Central.Dbg($"UnblockTimer");
                        UnblockInterface();
                    };
                }

                if(UnblockTimer.IsEnabled)
                {
                    UnblockTimer.Stop();
                }
                UnblockTimer.Start();
            }
        }

        private void UnblockTimerStop()
        {
            //Central.Dbg($"UnblockTimerStop");

            if(UnblockTimer != null)
            {
                if(UnblockTimer.IsEnabled)
                {
                    UnblockTimer.Stop();
                }
            }
        }

        /// <summary>
        /// задержка при возврате из другого окна
        /// </summary>
        private void BlockInterface()
        {
            TabUpdate();
            ActionsEnabledDelay =false;
            CheckControls();
            UnblockTimerRun();
        }

        private void UnblockInterface()
        {
            ActionsEnabledDelay=true;
            CheckControls();    
            UnblockTimerStop();
        }

        /// <summary>
        /// получение ID раската по machineId и reelNumber
        /// (эта функция статически рассчитывает результат без обращения к бд)
        /// (это экспериментальная функция, введена, т.к. существующий алгоритм дает сбои)
        /// </summary>
        /// <param name="reel"></param>
        /// <returns></returns>
        public int GetReelId(int machineId, int reelNumber, int reelSide)
        {
            /*
                machineId=[23]
                reelNumber=[1-5]
                reelSide=[1-2]
             */

            int result=0;

            int side=reelSide;

            if(side > 0)
            {
                result=reelNumber*2;

                /*
                if(machineId == 21)
                {
                    result=result+10;
                }

                // Fosber
                if(machineId == 22)
                {
                    result=result+16;
                }
                */
                // JS - Кашира 1
                if (machineId == 23)
                {
                    result = result + 26;
                }

                if (side == 1)
                {
                    result--;
                }
            }
            
            return result;
        }

        /// <summary>
        /// обновление PrceId для активного раската
        /// </summary>
        public async void SetPrceId(int idSt, int num)
        {

            /*
                balchugov_dv 2022-10-27_B1
                в интерфейсе управления рулонами есть такой баг:
                при опеределенных условиях перестают работать функции установки/снятия
                при отправке запросов вместо PRCE_ID отправляется 1 (неверный раскат)
                как работало:
                PRCE_ID берется из сервисного запроса (бд): на входе machine, reelNum, side
                на выходе PRCE_ID
                переделано:
                PRCE_ID вычисляется статически из тех же статических параметров
                это сделано для проверки влияния сбоя в этом запросе.
                Вообще, этот запрос надо делать один раз, выгружать список раскатов
                и хранить себе в справочнике в памяти
             
             */

            CurrentLeftReelPrceId=GetReelId(idSt, num, 1);
            CurrentRightReelPrceId=GetReelId(idSt, num, 2);
        }


        /// <summary>
        /// обновление записей
        /// </summary>
        public async void LoadItems(int delay=0)
        {           
            CheckControls();

            bool resume = true;

            if (resume)
            {
                if(CurrentMachineId==0)
                {
                    resume=false;
                }
                if(CurrentReelId==0)
                {
                    resume=false;
                }
            }

            if (resume)
            {                
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("MACHINE_ID", CurrentMachineId.ToString());
                    p.CheckAdd("REEL_NUMBER", CurrentReelId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Production");
                q.Request.SetParam("Object","Roll");
                q.Request.SetParam("Action","ListReelTasks");

                q.Request.SetParams(p);

                q.Request.Timeout = 10000;
                q.Request.Attempts= 1;

                UpdateTime(true);
                await Task.Run(() =>
                {
                    q.DoQuery();
                });
                UpdateTime(false);

                if(q.Answer.Status == 0)                
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if(result!=null)
                    {
                        TaskDS = ListDataSet.Create(result, "TASKS");
                        RollBufferDS = ListDataSet.Create(result, "BUFFER");
                        RollDS = ListDataSet.Create(result, "ROLLS");
                        PrintingDS = ListDataSet.Create(result, "PRINTING");
                        if (PrintingDS.Items.Count > 0)
                        {
                            PrintingValues = PrintingDS.Items;
                        }

                        var optionsDs=ListDataSet.Create(result, "OPTIONS");

                        //DisableControls();

                        ActionsEnabledList=false;
                        CheckControls();

                        UpdateOptions(optionsDs);
                        UpdateItems();

                        {
                            //var machineInfoDs= ListDataSet.Create(result, "MACHINE_INFO");
                            var reelInfoDs = ListDataSet.Create(result, "REEL_INFO");
                            UpdateDataActual(reelInfoDs.Items);                            
                        }
                    }
                }
            }


            ActionsEnabledList=true;
            CheckControls();
            //EnableControls(delay);
            //EnableControls();
        }

        public void UpdateDataActual(List<Dictionary<string, string>> items)
        {
            var reelData = new Dictionary<string, string>();
            DebugString = "";

            if(items.Count > 0)
            {
                foreach(Dictionary<string, string> row in items)
                {
                    /*
                     "NUMBER":"1","MACHINE_ID":"2","MACHINE_NUMBER":"1"
                     */
                    if(
                        row.CheckGet("MACHINE_ID").ToInt() == CurrentMachineId
                        && row.CheckGet("NUMBER").ToInt() == CurrentReelId
                    )
                    {
                        reelData = row;
                    }
                }
            }

            DebugString = DebugString.Append($"{reelData.CheckGet("MACHINE_ID")}~{reelData.CheckGet("NUMBER")} {CurrentMachineId}~{CurrentReelId}", true);

            var dataActual = true;
            if(reelData.Count > 0)
            {
                for(int k = 1; k <= 4; k++)
                {
                    if(dataActual)
                    {
                        DebugString = DebugString.Append($"{reelData.CheckGet($"SOURCE{k}_READ")} {reelData.CheckGet($"SOURCE{k}_ONDATE")}", true);

                        if(reelData.CheckGet($"SOURCE{k}_READ").ToBool())
                        {
                            var ds = reelData.CheckGet($"SOURCE{k}_ONDATE");
                            if(!ds.IsNullOrEmpty())
                            {
                                var d1 = ds.ToDateTime();
                                var d0 = DateTime.Now;
                                var dt = ((TimeSpan)(d0 - d1)).TotalSeconds;
                                if(dt > DataActualTtl)
                                {
                                    dataActual = false;
                                }
                            }
                            else
                            {
                                dataActual = false;
                            }
                        }
                        else
                        {
                            dataActual = false;
                        }
                    }
                }
            }

            if(dataActual)
            {
                DataActualLastDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            }

            if(DataActualLastTtl > 0)
            {
                if(!DataActualLastDate.IsNullOrEmpty())
                {
                    var d1 = DataActualLastDate.ToDateTime();
                    var d0 = DateTime.Now;
                    var dt = ((TimeSpan)(d0 - d1)).TotalSeconds;
                    DebugString = DebugString.Append($"{dataActual} {DataActualLastDate} {dt}", true);
                    if(dt > DataActualLastTtl)
                    {
                        DataActual = false;
                    }
                    else
                    {
                        DataActual = true;
                    }
                }
            }

            UpdateDataActual();


            var rollRotate = false;
            if(reelData.Count > 0)
            {
                var a = reelData.CheckGet($"ACTIVE").ToBool();
            }
        }

        public void UpdateDataActual()
        {
            if(DataActual)
            {
                DataUnactualBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                DataUnactualBlock.Visibility = Visibility.Visible;
            }
        }

        

        public void UpdateTime(bool waitingFlag)
        {
            WaitingFlag=waitingFlag;

            var time=DateTime.Now.ToString("HH:mm:ss");
            var w=" ";
            if(WaitingFlag)
            {
                w="w";
            }
            //var memoryUsedMb=Central.GetUsedMemory();

            Time.Text=$"{time}";
            //Mem.Text=$"{memoryUsedMb}{w}";
        }

        /// <summary>
        /// обработка параметров
        /// </summary>
        /// <param name="ds"></param>
        public void UpdateOptions(ListDataSet ds)
        {
            if(ds.Items.Count > 0)
            {
                /*
                    NAME
                    VALUE
                    DESCRIPTION
                */
                foreach(Dictionary<string, string> row in ds.Items)
                {
                    if(row.CheckGet("NAME") == "FOSBER_REEL_MANUAL_MODE")
                    {
                        FosberReelManualMode=row.CheckGet("VALUE").ToBool();
                    }
                }
            }
        }

        /// <summary>
        /// рендер данных интерфейса
        /// </summary>
        public void UpdateItems()
        {
            //по умолчанию все включены, отключаются по мере вычисления
            LeftPutEnabled=true;
            RightPutEnabled=true;
            LeftRemoveEnabled=true;
            RightRemoveEnabled=true;
            PutEnabled=true;


            var currentRest=0;
            var currentSource="";
            var currentTask=new Dictionary<string, string>();

            if(TaskDS!=null)
            {

                if(TaskGridSelectedItem!=null)
                {
                    TaskGrid.SetSelectedItem(TaskGridSelectedItem);
                }
                TaskGrid.UpdateItems(TaskDS);

                //общая информация
                {
                    currentSource=TaskDS.GetFirstItemValueByKey("RAW_NAME").ToString();
                    currentTask=TaskDS.GetFirstItem();
                }
            }

            //перемещение
            if(RollBufferDS!=null)
            {
                if(RollBufferDS.Items.Count>0)
                {
                    foreach(var row in RollBufferDS.Items)
                    {
                        var r="";
                        var d=row.CheckGet("IMPORT_PRODUCED_DT");
                        if(!d.IsNullOrEmpty())
                        {
                            var d2 = DateTime.Now;
                            var d1 = d.ToDateTime();
                            var dd = (TimeSpan)(d2 - d1);
                            var dt = (int)dd.TotalDays;

                            if(dt>0)
                            {
                                if(dt>0 && dt<=30)
                                {
                                    r="Н";
                                }

                                if(dt>=90)
                                {
                                    r="С";
                                }
                            }
                        }
                        row.CheckAdd("_IMPORT_STAUS", r);
                    }
                }
                RollBufferGrid.UpdateItems(RollBufferDS);

                bool bufferEmpty=true;
                if(RollBufferGrid.Items!=null)
                {
                    if(RollBufferGrid.Items.Count>0)
                    {
                        bufferEmpty=false;
                    }
                }

                if(bufferEmpty)
                {
                    MoveRollButton.Style=(Style)MoveRollButton.TryFindResource("RollReelButtonDisabled");
                }
                else
                {
                    MoveRollButton.Style=(Style)MoveRollButton.TryFindResource("RollReelButton");
                }
                
            }

            if(RollDS!=null)
            {
                //общая информация
                {
                    currentRest=RollDS.GetFirstItemValueByKey("ALL_LENGTH").ToInt();
                }  
                
                //по раскатам
                { 
                    foreach (var row in RollDS.Items)
                    {
                        var side=row.CheckGet("SIDE").ToInt();
                        var rollIdp=row.CheckGet("ID").ToInt();
                        var rollNumber= row.CheckGet("ROLL_NUMBER").ToString();
                        var active=row.CheckGet("ACTIVE_FLAG").ToInt();
                        string sourceName=row.CheckGet("RAW_NAME").ToString();
                        int sourceRest=row.CheckGet("ROLL_LENGTH").ToInt();
                        int id=row.CheckGet("ID").ToInt();
                        int rollStatus = row.CheckGet("ROLL_STATUS").ToInt();

                        //неактивный
                        if(string.IsNullOrEmpty(sourceName))
                        {
                            active=0;
                        }

                        //пустой
                        if(id==0)
                        {
                            active=2;
                            sourceName="";
                            sourceRest=0;
                        }

                        //если текущее задание не задействует раскат, не делаем алерт
                        if(currentTask.CheckGet("CURR_REEL").ToInt() == 0)
                        {
                            if(active==2)
                            {
                                active=3;
                            }

                        }

                        if (active == 1 && rollStatus != 8)
                        {
                            active = 4;
                        }
                                
                        /*
                            side: 1=левый,2=правый
                            active: 0=неактивный,1=активный,2=пустой,3=не используется, 4=рулон стоит на раскате, но не числится в буфере
                         */

                        SetReelState(side,active,sourceName,sourceRest,rollIdp,rollNumber);

                        //1=левый,2=правый
                        switch(side)
                        {
                            case 1:
                                LeftReelRollId=id;
                                break;

                            case 2:
                                RightReelRollId=id;
                                break;
                        }
                    }
                }

            }

            CurrentTaskRest.Text = currentRest.ToString();
            CurrentSourceName.Text = currentSource.ToString();
            //BurgerDbgShort.Header=$"M{CurrentMachineId}-R{CurrentReelId}-{FosberReelManualMode}_BM={BaseMachineId}-BR-{BaseReelId}_{Dt}";
            
            CheckFosberReel();
            RollBufferGridActionsUpdate();
            SetButtonsState();
        }


        /// <summary>
        /// обновление методов работы с выбранной записью в таблице заданий
        /// </summary>
        public void UpdateBufferActions(Dictionary<string, string> selectedItem)
        {
            SelectedBufferItem = selectedItem;
            RollWetList.Text = SelectedBufferItem.CheckGet("WET_LIST").ToString();
        }

        private void CheckControls()
        {
            if(
                ActionsEnabledList
                && ActionsEnabledCommand
                && ActionsEnabledDelay
            )
            {
                OperationsToolbar.IsEnabled=true;
                Splash.Visibility=Visibility.Collapsed;
                
                Progress.Stop();
                ProgressContainer.Visibility=Visibility.Collapsed;
            }
            else
            {
                OperationsToolbar.IsEnabled=false;
                Splash.Visibility=Visibility.Visible;

                //прогресс-бар будет показан только при выполнении ручных операций
                if(!ActionsEnabledCommand)                    
                {
                    if(ProgressBarDelay>0)
                    {
                        Progress.Start(ProgressBarDelay);
                        ProgressContainer.Visibility=Visibility.Visible;
                    }                    
                }                
            }

           
        }

        /// <summary>
        /// ожидание данных
        /// </summary>
        private void DisableControls()
        {
            ActionsEnabledCommand=false;
            CheckControls();
        }

        /// <summary>
        /// окончание ожидания данных
        /// </summary>
        private void EnableControls()
        {
            ActionsEnabledCommand=true;
            CheckControls();
        }
       

        /// <summary>
        /// фильтрация записей таблицы заданий
        /// </summary>
        public void FilterItems()
        {
            if (TaskGrid.GridItems != null)
            {
                if (TaskGrid.GridItems.Count > 0)
                {
                    var printingTask = new List<string>();
                    if (PrintingValues.Count > 0)
                    {
                        foreach (var item in PrintingValues)
                        {
                            printingTask.Add(item.CheckGet("ID_PZ"));
                        }
                    }

                    foreach (Dictionary<string, string> row in TaskGrid.GridItems)
                    {
                        // Добавим поле, в котором будет отметка о печати. По умолчанию печати нет
                        row.CheckAdd("HAS_PRINTING", "0");
                        if (printingTask.Count > 0)
                        {
                            if (printingTask.Contains(row.CheckGet("ID_PZ")))
                            {
                                row["HAS_PRINTING"] = "1";
                            }
                        }

                        // Для первого раската к профилю добавим номер рабочей машины
                        row.CheckAdd("PROFIL_MACHINE", row["PROFIL_NAME"]);
                        if ((CurrentMachineId != 21) && (CurrentReelId == 1))
                        {
                            if (row["MACHINE2"].ToInt() == 0)
                            {
                                row["PROFIL_MACHINE"] = row["PROFIL_MACHINE"] + "1";
                            }
                            else if (row["MACHINE1"].ToInt() == 0)
                            {
                                row["PROFIL_MACHINE"] = row["PROFIL_MACHINE"] + "2";
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Получение параметров печати для выбранной записи
        /// </summary>
        /// <returns></returns>
        private List<Dictionary<string, string>> GetClichePrintingParams()
        {
            var result = new List<Dictionary<string, string>>();
            if (TaskGridSelectedItem != null)
            {
                if (PrintingValues.Count > 0)
                {
                    foreach (var item in PrintingValues)
                    {
                        if (item.CheckGet("ID_PZ").ToInt() == TaskGridSelectedItem.CheckGet("ID_PZ").ToInt())
                        {
                            result.Add(item);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// обновление методов работы с выбранной записью в таблице заданий
        /// </summary>
        public void TaskGridActionsUpdate(Dictionary<string,string> selectedItem)
        {
            if(selectedItem.Count > 0)
            {
                TaskGridSelectedItem=selectedItem;

                PrintingButton.Visibility = Visibility.Hidden;

                // если в доп.поле стоит отметка о печати в задании, раскрашиваем ячейку
                if (TaskGridSelectedItem.CheckGet("HAS_PRINTING").ToInt()==1)
                { 
                    if ((CurrentMachineId == 2) && (CurrentReelId == 1))
                    {
                        PrintingButton.Visibility = Visibility.Visible;
                    }
                }
            }            
        }

        /// <summary>
        /// обновление методов работы с выбранной записью в таблице заданий
        /// </summary>
        public void RollBufferGridActionsUpdate()
        {
            var selectedItem=RollBufferGrid.SelectedItem;
            
            if (selectedItem != null)
            {
                PutEnabled=true;

                SelectedBufferItem = selectedItem;
                RollWetList.Text = SelectedBufferItem.CheckGet("WET_LIST");

                //если текущий ГА фосбер
                if(CurrentMachineId == 22)
                {
                    PutEnabled=false;

                    if(FosberReelManualMode)
                    {
                        //ручной режим
                        //можно ставить все рулоны
                        PutEnabled=true;
                    }
                    else
                    {
                        //автоматический режим
                        //можно ставить только белые рулоны
                                                
                        if(SelectedBufferItem.CheckGet("ID_STATUS").ToInt() == 33)
                        {
                            //зеленый--рулон просканирован на входе, данные в транспортную систему переданы
                            PutEnabled=false;
                        }
                        else
                        {
                            //белый
                            PutEnabled=true;
                        }
                    }

                    //if(!putButtonEnabled)
                    //{
                    //    LeftPutRollButton.Style = (Style)LeftPutRollButton.TryFindResource("RollReelButtonDisabled");
                    //    RightPutRollButton.Style = (Style)RightPutRollButton.TryFindResource("RollReelButtonDisabled");

                    //    LeftPutRollButton.IsEnabled=false;
                    //    RightPutRollButton.IsEnabled=false;
                    //}
                    //else
                    //{
                    //    LeftPutRollButton.IsEnabled=true;
                    //    RightPutRollButton.IsEnabled=true;
                    //}
                }  
                
                SetButtonsState();
            }    
        }

        private void CheckMachine(int machineId=0, int reelId=0)
        {
            if(machineId!=0)
            {
                CurrentMachineId=machineId;                
            }

            if(reelId!=0)
            {
                CurrentReelId=reelId;
            }

            switch (CurrentMachineId)
            {
                //ГА1
                default:
                case 2:
                    CorrugatedMachineButton.Content = "ГА1";                    

                    //дополнительные кнопки неактивны
                    FosberLeftButton.Visibility = Visibility.Hidden;
                    FosberRightButton.Visibility = Visibility.Hidden;

                    //раскаты 4-5 активны
                    Reel4Button.Visibility = System.Windows.Visibility.Visible;
                    Reel5Button.Visibility = System.Windows.Visibility.Visible;
                    
                    break;

                //ГА2
                case 21:
                    CorrugatedMachineButton.Content = "ГА2";                    

                    //дополнительные кнопки неактивны
                    FosberLeftButton.Visibility = Visibility.Hidden;
                    FosberRightButton.Visibility = Visibility.Hidden;

                    //раскаты 4-5 неактивны
                    Reel4Button.Visibility = System.Windows.Visibility.Hidden;
                    Reel5Button.Visibility = System.Windows.Visibility.Hidden;


                    if (
                        (CurrentReelId == 4) 
                        || (CurrentReelId == 5)
                    )
                    {
                        CurrentReelId=1;
                    }
                    break;

                //ГА3
                case 22:
                    CorrugatedMachineButton.Content = "ГА3";         
                    
                    //дополнительные кнопки активны
                    FosberLeftButton.Visibility = Visibility.Visible;
                    FosberRightButton.Visibility = Visibility.Visible;

                    //раскаты 4-5 активны
                    Reel4Button.Visibility = System.Windows.Visibility.Visible;
                    Reel5Button.Visibility = System.Windows.Visibility.Visible;

                    break;

                //ГА4 - Кашира
                case 23:
                    CorrugatedMachineButton.Content = "ГА1";

                    //дополнительные кнопки активны
                    FosberLeftButton.Visibility = Visibility.Visible;
                    FosberRightButton.Visibility = Visibility.Visible;

                    //раскаты 4-5 активны
                    Reel4Button.Visibility = System.Windows.Visibility.Visible;
                    Reel5Button.Visibility = System.Windows.Visibility.Visible;

                    break;
            }

            /*

            //на чужих агрегатах нельзя проводить операции            
            if(CurrentMachineId!=BaseMachineId)
            {
                OperationsToolbar.Visibility=Visibility.Hidden;
            }
            else
            {
                OperationsToolbar.Visibility=Visibility.Visible;
            }

            */

            CheckReel(CurrentReelId);
            //            LoadItems();
        }

        /// <summary>
        /// циклическая смена ГА
        /// </summary>
        private void ChangeMachineId()
        {
            // 2 -> 21 -> 22 -> 23 -> 2 ...

            /*
            switch (CurrentMachineId)
            {
                default:
                case 23:
                    CheckMachine(2);
                    break;
                
                case 2:
                    CheckMachine(21);
                    break;

                case 21:
                    CheckMachine(22);
                    break;

                case 22:
                    CheckMachine(23);
                    break;

            }
            */
            CheckMachine(23);
            RollBufferGridInit();
        }

        /// <summary>
        /// установка активного раската
        /// </summary>
        /// <param name="reelId"></param>
        private void CheckReel(int reelId=0)
        {            
            if(reelId!=0)
            {
                CurrentReelId = reelId;               
            }

            //все остальные в исходное состояние
            Reel1Button.Style=(Style)Reel1Button.TryFindResource("RollReelButton");
            Reel2Button.Style=(Style)Reel1Button.TryFindResource("RollReelButton");
            Reel3Button.Style=(Style)Reel1Button.TryFindResource("RollReelButton");
            Reel4Button.Style=(Style)Reel1Button.TryFindResource("RollReelButton");
            Reel5Button.Style=(Style)Reel1Button.TryFindResource("RollReelButton");

            //кнопку активного раската в активное состояние
            switch (CurrentReelId)
            {
                case 1:
                    Reel1Button.Style=(Style)Reel1Button.TryFindResource("RollReelButtonActive");
                    break;

                case 2:
                    Reel2Button.Style=(Style)Reel1Button.TryFindResource("RollReelButtonActive");
                    break;

                case 3:
                    Reel3Button.Style=(Style)Reel1Button.TryFindResource("RollReelButtonActive");
                    break;

                case 4:
                    Reel4Button.Style=(Style)Reel1Button.TryFindResource("RollReelButtonActive");
                    break;

                case 5:
                    Reel5Button.Style=(Style)Reel1Button.TryFindResource("RollReelButtonActive");
                    break;
            }
            
            CheckFosberReel();
        }

        public void CheckFosberReel()
        {
            //fosber: ручная активация раската
            if (CurrentMachineId == 23)
            {
                switch(ActiveReel)
                {
                    //левый
                    case 1:
                        FosberLeftButton.Style=(Style)Reel1Button.TryFindResource("RollReelButtonDisabled");  
                        FosberRightButton.Style=(Style)Reel1Button.TryFindResource("RollReelButton");
                        break;

                    //правый
                    case 2:
                        FosberLeftButton.Style=(Style)Reel1Button.TryFindResource("RollReelButton");  
                        FosberRightButton.Style=(Style)Reel1Button.TryFindResource("RollReelButtonDisabled");
                        break;
                }

                SettingsButton.Visibility=Visibility.Visible;

            }
            else
            {
                SettingsButton.Visibility=Visibility.Collapsed;
            }

            
            if(CurrentMachineId==22 && FosberReelManualMode)
            {
                //подсвечено красным
                SettingsButton.Style=(Style)SettingsButton.TryFindResource("RollReelButtonHot");
            }
            else
            {
                //обычный стиль
                SettingsButton.Style=(Style)SettingsButton.TryFindResource("RollReelButton");
            }

        }
              

        /// <summary>
        /// Постановка рулона на раскат
        /// </summary>
        /// <param name="pos">0 - слева, 1 - справа</param>
        private void PutRoll(int pos)
        {
            
            //if (CurrentMachineId == 22)
            //{
            //    var sideTitle = "";
            //    switch (pos)
            //    {
            //        case 0:
            //            sideTitle = "левый";
            //            break;

            //        case 1:
            //            sideTitle = "правый";
            //            break;
            //    }

            //    string msg = $"\nВНИМАНИЕ!\n\nРулон на раскат устанавливается АВТОМАТИЧЕСКИ!\nИспользуйте установку рулона вручную только в нештатных ситуациях! " +
            //        $"\n\nУстановить рулон на {sideTitle} раскат вручную?";

            //    var d = new DialogTouch($"{msg}", "Установка рулона", "", DialogWindowButtons.NoYes);

            //    d.Show();
            //    d.OnComplete = (DialogResultButton resultButton) =>
            //    {
            //        if (resultButton == DialogResultButton.Yes)
            //        {
            //            DoPutRoll(pos);
            //        }
            //    };
            //}
            //else
            //{
            //    DoPutRoll(pos);
            //}

            DoPutRoll(pos);
        }

        private async void DoPutRoll(int pos)
        {
            DisableControls();

            var prceId = 0;

            if (pos == 0)
            {
                prceId = CurrentLeftReelPrceId;
            }

            if (pos == 1)
            {
                prceId = CurrentRightReelPrceId;
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Roll");
            q.Request.SetParam("Action", "Put");

            q.Request.SetParam("ID", SelectedBufferItem["IDP_ROLL"]);
            q.Request.SetParam("SIDE", pos.ToString());
            q.Request.SetParam("PRCE_ID", prceId.ToString());

            q.Request.Timeout = RequestTimeoutAction;
            q.Request.Attempts= RequestAttemptsAction;

            UpdateTime(true);
            await Task.Run(() =>
            {
                q.DoQuery();
            });
            UpdateTime(false);

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (result.ContainsKey("Items"))
                    {
                        //TaskGrid.UpdateItems();
                        ActionsEnabledList=false;
                        LoadItems(0);
                    }
                }
            }
            else
            {
                //q.SilentErrorProcess=true;
                q.ProcessError();
            }

            EnableControls();
        }

        /// <summary>
        /// Снятие рулона с раската
        /// </summary>
        /// <param name="rollId"></param>
        /// <param name="side">1=левый,2=правый</param>
        private void RemoveRoll(int rollId, int side=0)
        {
            bool resume = true;

            if (resume)
            {
                if (rollId == 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var sideTitle = "";
                switch (side)
                {
                    case 1:
                        sideTitle = "левого";
                        break;

                    case 2:
                        sideTitle = "правого";
                        break;
                }

                string msg = "";
                msg = $"Снять рулон с {sideTitle} раската?";

                //if (CurrentMachineId == 22)
                //{
                //    msg = $"\nВНИМАНИЕ!\n\nРулон с раската снимается АВТОМАТИЧЕСКИ!\nИспользуйте снятие рулона вручную только в нештатных ситуациях! " +
                //        $"\n\nСнять рулон с {sideTitle} раската вручную?";
                //}

                var d = new DialogTouch($"{msg}", "Снятие рулона", "", DialogWindowButtons.NoYes);
                d.Show();
                d.OnComplete = (DialogResultButton resultButton) =>
                {
                    if (resultButton == DialogResultButton.Yes)
                    {
                        DoRemoveRoll(rollId, side);
                    }

                    BlockInterface();
                    SetActive();
                    
                };
            }

        }

        private async void DoRemoveRoll(int rollId, int side=0)
        {
            bool resume=true;

            if(resume)
            {
                DisableControls();

                var prceId = 0;

                if (side == 1)
                {
                    prceId = CurrentLeftReelPrceId;
                }

                if (side == 2)
                {
                    prceId = CurrentRightReelPrceId;
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Roll");
                q.Request.SetParam("Action", "Remove");

                q.Request.SetParam("ID", rollId.ToString());
                q.Request.SetParam("PRCE_ID", prceId.ToString());

                q.Request.Timeout = RequestTimeoutAction;
                q.Request.Attempts= RequestAttemptsAction;

                UpdateTime(true);
                await Task.Run(() =>
                {
                    q.DoQuery();
                });
                UpdateTime(false);

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        if (result.ContainsKey("Items"))
                        {
                            //TaskGrid.UpdateItems();
                            ActionsEnabledList=false;
                            LoadItems(0);
                        }
                    }
                }
                else
                {
                    //q.SilentErrorProcess=true;
                    q.ProcessError();
                }

                EnableControls();
            }
        }

        private async void SetRunningRoll(int side)
        {
            DisableControls();
            var prceId = 0;

            if (side == 0)
            {
                prceId = CurrentLeftReelPrceId;
            }

            if (side == 1)
            {
                prceId = CurrentRightReelPrceId;
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Roll");
            q.Request.SetParam("Action", "SetRunning");

            q.Request.SetParam("ID_REEL", CurrentReelId.ToString());
            q.Request.SetParam("SIDE", side.ToString());
            q.Request.SetParam("PRCE_ID", prceId.ToString());

            q.Request.Timeout = RequestTimeoutAction;
            q.Request.Attempts= RequestAttemptsAction;

            UpdateTime(true);
            await Task.Run(() =>
            {
                q.DoQuery();
            });
            UpdateTime(false);

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (result.ContainsKey("Items"))
                    {
                        ActionsEnabledList=false;
                        LoadItems(0);
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        private async void MoveRoll(Dictionary<string,string> v)
        {
            /*
                RollRemove:
                    ID
                    MASS
                    OPERATION
                RollSetDefect:
                    ID
                    MASS
                    OPERATION
                    REASON
                    REASON_ID                
             */
            
            DisableControls();            
            bool resume=true;

            /*
                !!! LOWERCASE
                    S = На склад
                    Z = В Z0
                    5 = 1 раскат
                    4 = 2 раскат
                    3 = 3 раскат
                    2 = 4 раскат
                    1 = 5 раскат
                    D = Смотать
                    R = Откатить
             */
            string operation=v.CheckGet("OPERATION");
            operation=operation.ToLower();
            operation=operation.Trim();

            int labelCount = v.CheckGet("LABEL_COUNT").ToInt();

            int rollId = v.CheckGet("ID").ToInt();

            if(resume)
            {
                if(rollId==0) 
                {
                    resume=false;
                }
            }

            if(resume)
            {
                if(string.IsNullOrEmpty(operation)) 
                {
                    resume=false;
                }
            }

            if(resume)
            {
                //печатаем ярлык
                //для всех операций кроме "Смотать" и "Откатить"

                var doPrinting=false;
                var faultReason="";

                switch(operation)
                {
                    case "d":
                    case "r":
                        { 
                            doPrinting=false;
                        }
                        break;

                    case "z":
                        { 
                            doPrinting=true;
                            faultReason=v.CheckGet("REASON");
                        }
                        break;

                    default:
                        { 
                            doPrinting=true;
                        }
                        break;

                }


                if(doPrinting){
                    if(!NoPrint)
                    {
                        //печать


                        bool printResult = true;

                        for (int i = 0; i < labelCount; i++)
                        {
                            printResult = PrintReceipt(2, rollId, faultReason);
                        }

                        if(!printResult)
                        {
                            { 
                                var t="Перемещение рулона";
                                var m="";
                                m=$"{m}Не удалось напечатать ярлык.";
                                m=$"{m}\nРулон не будет перемещен.";
                
                                var error=new ErrorTouch();
                                error.Show(t,m);
                            }
                            resume=false;
                        }
                    }
                    else
                    {
                        //просмотр
                        PrintReceipt(1,rollId,faultReason);
                    }
                }
            }


            if(resume)
            {
                var p=new Dictionary<string,string>();
                {
                    p.CheckAdd("ID",        SelectedBufferItem.CheckGet("IDP_ROLL"));
                    p.CheckAdd("REEL_ID",   SelectedBufferItem.CheckGet("ID_REEL"));
                    p.CheckAdd("MACHINE_ID",     CurrentMachineId.ToString());
                    p.CheckAdd("OPERATION", operation);
                }
                
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Roll");
                q.Request.SetParam("Action", "Move");

                q.Request.SetParams(p);

                q.Request.Timeout = RequestTimeoutAction;
                q.Request.Attempts= RequestAttemptsAction;

                UpdateTime(true);
                await Task.Run(() =>
                {
                    q.DoQuery();
                });
                UpdateTime(false);

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        if (result.ContainsKey("Items"))
                        {
                            //TaskGrid.UpdateItems();
                            ActionsEnabledList=false;
                            LoadItems(0);
                        }
                    }
                }
                else
                {
                    //q.SilentErrorProcess=true;
                    q.ProcessError();
                }
            }

            EnableControls();
            //EnableControls(1);

            ReturnTimerReset();
        }

        private void PrintReceiptShowDialog(int rollId=0)
        {
            if(rollId==0)
            {
                rollId = SelectedBufferItem.CheckGet("IDP_ROLL").ToInt();
            }

            if(rollId!=0)
            {
                var i=new RollReceiptViewer();
                i._Edit(rollId);
            }
        }

        /// <summary>
        /// Даилог печати
        /// 1=просмотр, 2=печать
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="rollId">id рулона (rolls_buffer.idp_roll)</param>
        /// <param name="faultReason">причина забраковки</param>
        private bool PrintReceipt(int mode=0, int rollId=0, string faultReason="")
        {
            bool result=false;

            if(rollId==0)
            {
                rollId = SelectedBufferItem.CheckGet("IDP_ROLL").ToInt();
            }           

            if(rollId!=0)
            {
                var receiptViewer=new RollReceiptViewer();
                receiptViewer.RollId=rollId;
                receiptViewer.RollFaultReason=faultReason;
                result=receiptViewer.Init();

                switch(mode)
                {
                    //просмотр
                    default:
                    case 1:
                        receiptViewer.Show();
                        break;

                    //печать
                    case 2:
                        receiptViewer.Print(true);
                        break;
                }
            }

            return result;
        }

        private void EditRoll()
        {
            var rollId = SelectedBufferItem.CheckGet("IDP_ROLL").ToInt();
            if(rollId!=0)
            {
                var i=new RollCorrectWeight();
                i.ReceiverName = "ReelControlKsh";
                i.Edit(rollId);
            }
        }

        private void MoveRoll()
        {
            var rollId = SelectedBufferItem.CheckGet("IDP_ROLL").ToInt();
            var rollMass = SelectedBufferItem.CheckGet("QTY").ToInt();
            if (
                rollId != 0
                && rollMass != 0
            )
            {
                var i = new RollRemove();
                i.ReceiverName = "ReelControlKsh";
                i.MachineId = CurrentMachineId;
                i.RollId = rollId;
                i.RollMass = rollMass;
                if (SelectedBufferItem.ContainsKey("ROLLBACK_IS"))
                {
                    i.RollbackExpenseAllowed = SelectedBufferItem.CheckGet("ROLLBACK_IS").ToBool();
                }

                i.Edit();
            }
        }

        private void TestShowError()
        {
            {
                var t="Корректировка рулона невозможна";
                var m="Не хватает информации в системе по данному рулону";
                
                var i=new ErrorTouch();
                i.Show(t,m);
            }
        }

        /// <summary>
        /// получает последний рулон в буфере и показывает для него диалог печати ярлыка,
        /// для настройки принтера       
        /// </summary>
        private async void GetLastRoll()
        {
            bool resume=true;
            
            if(resume)
            {
                var p=new Dictionary<string,string>();
                {
                }
                
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Roll");
                q.Request.SetParam("Action", "GetBufferLast");

                q.Request.SetParams(p);

                q.Request.Timeout = RequestTimeoutAction;
                q.Request.Attempts= RequestAttemptsAction;
                
                UpdateTime(true);
                await Task.Run(() =>
                {
                    q.DoQuery();
                });
                UpdateTime(false);

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds=ListDataSet.Create(result,"ITEMS");
                        var rollId=ds.GetFirstItemValueByKey("ID").ToInt();
                        if(rollId!=0)
                        {
                            PrintReceipt(1,rollId); 
                        }
                        else
                        {
                            {
                                var t="Нет рулонов в буфере.";
                                var m="Невозможно отобразить диалог печати ярлыка.";
                
                                var i=new ErrorTouch();
                                i.Show(t,m);
                            }
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
        /// поиск рулона по заданному id и печать ярлыка на него
        /// </summary>
        private async void SearchRoll()
        {
            bool resume=true;
            
            if(resume)
            {
                var p=new Dictionary<string,string>();
                {
                }
                
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Roll");
                q.Request.SetParam("Action", "GetBufferLast");

                q.Request.SetParams(p);

                q.Request.Timeout = RequestTimeoutAction;
                q.Request.Attempts= RequestAttemptsAction;

                UpdateTime(true);
                await Task.Run(() =>
                {
                    q.DoQuery();
                });
                UpdateTime(false);

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds=ListDataSet.Create(result,"ITEMS");
                        var rollId=ds.GetFirstItemValueByKey("ID").ToInt();
                        if(rollId!=0)
                        {
                            PrintReceipt(1,rollId); 
                        }
                        else
                        {
                            {
                                var t="Нет рулонов в буфере.";
                                var m="Невозможно отобразить диалог печати ярлыка.";
                
                                var i=new ErrorTouch();
                                i.Show(t,m);
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        public async void ShowRollInfo()
        {
            var resume=true;
            var row=new Dictionary<string,string>();

            var rollId=RollBufferGrid.SelectedItem.CheckGet("IDP_ROLL").ToInt();
            if(rollId == 0)
            {
                resume=false;
            }
            
            if(resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID", rollId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Production");
                q.Request.SetParam("Object","Roll");
                q.Request.SetParam("Action","GetDataReceipt");

                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestTimeoutMin;
                q.Request.Attempts=1;

                UpdateTime(true);
                await Task.Run(() =>
                {
                    q.DoQuery();
                });
                UpdateTime(false);

                if(q.Answer.Status == 0)                
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if(result!=null)
                    {
                        var ds=ListDataSet.Create(result, "ITEMS");
                        row=ds.GetFirstItem();
                    }
                }
            }

            if(resume)
            {
                if(row.Count > 0)
                {
                    row.Remove("SIDE");
                    row.Remove("WET_LIST");
                    row.Remove("FAULT_NOTE");
                    row.Remove("_");
                    row.Remove("_SELECTED");
                    row.Remove("_ROWNUMBER");
                }

                var t="Информация о рулоне";
                var m="";
                {
                    m=m.Append($"{row.GetDumpString()}");
                }
                var i=new ErrorTouch();
                i.Show(t,m);
            }
        }

        public void RestoreRoll()
        {
            var restoreFrame = new RollRestore();
            restoreFrame.MachineId = CurrentMachineId;
            restoreFrame.ReelNum = CurrentReelId;
            restoreFrame.Edit();
        }

        public void PrintLabel()
        {
            var rollPrintLabelFrame = new RollPrintLabel();
            rollPrintLabelFrame.Show();
        }

        /// <summary>
        /// Получение данных о расходном материале и открытие окна для списания
        /// </summary>
        /// <param name="barcode">Значение, зашифрованное в штрих-коде</param>
        private void GetMaterial(string barcode)
        {
            if (!barcode.IsNullOrEmpty())
            {
                if(barcode.Length==13)
                {
                    var k=Cryptor.MakeRandom().ToString();
                    {
                        var v=new Dictionary<string,string>();
                        v.CheckAdd("INSTANCE_ID",k.ToString());
                        if(!Central.SessionValues.ContainsKey(MaterialWriteOffInstanceKey))
                        {
                            Central.SessionValues.Add(MaterialWriteOffInstanceKey,v);
                        }
                        Central.SessionValues[MaterialWriteOffInstanceKey]=v;
                    }

                    var writeOffFrame = new RollMaterialWriteOff();
                    writeOffFrame.MachineId = CurrentMachineId;
                    writeOffFrame.Barcode=barcode;
                    writeOffFrame.InstanceId=k;
                    writeOffFrame.Edit();    
                }
            }
        }


        

        private void ShowLog()
        {
            var h=new LogTouch();
            h.Show();
        }

        private void ShowInfo()
        {
            var t="Отладочная информация";
            var m=Central.MakeInfoString();
            var i=new ErrorTouch();
            i.Show(t,m);
        }

        private void ShowTest()
        {
            var t = "Отладочная информация";
            var m = DebugString;
            var i = new ErrorTouch();
            i.Show(t, m);
        }

        private void ShowSettings()
        {
            var i=new Settings();
            i.ReceiverName = "ReelControlKsh";
            i.Edit();
        }

        private void HelpButton_Click_1(object sender,System.Windows.RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void CorrugatedMachineButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetDefaults();
            ChangeMachineId();
            ReturnTimerReset();
            LoadItems(0);
            SetPrceId(CurrentMachineId, CurrentReelId);
        }

        private void Reel1Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetDefaults();
            CheckReel(1);
            CheckMachine();
            ReturnTimerReset();
            LoadItems(0);
            SetPrceId(CurrentMachineId, 1);
        }

        private void Reel2Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetDefaults();
            CheckReel(2);
            CheckMachine();
            ReturnTimerReset();
            LoadItems(0);
            SetPrceId(CurrentMachineId, 2);
        }

        private void Reel3Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetDefaults();
            CheckReel(3);
            CheckMachine();
            ReturnTimerReset();
            LoadItems(0);
            SetPrceId(CurrentMachineId, 3);
        }

        private void Reel4Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetDefaults();
            CheckReel(4);
            CheckMachine();
            ReturnTimerReset();
            LoadItems(0);
            SetPrceId(CurrentMachineId, 4);
        }

        private void Reel5Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SetDefaults();
            CheckReel(5);
            CheckMachine();
            ReturnTimerReset();
            LoadItems(0);
            SetPrceId(CurrentMachineId, 5);
        }

        private void LeftPutRollButton_Click(object sender, RoutedEventArgs e)
        {
            PutRoll(0);
            ReturnTimerReset();
        }

        private void RightPutRollButton_Click(object sender, RoutedEventArgs e)
        {
            PutRoll(1);
            ReturnTimerReset();
        }

        private void LeftRemoveRollButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveRoll(LeftReelRollId,1);
            ReturnTimerReset();
        }

        private void RightRemoveRollButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveRoll(RightReelRollId,2);
            ReturnTimerReset();
        }

        private void LeftFosberButton_Click(object sender, RoutedEventArgs e)
        {
            FosberLeftButton.IsEnabled = false;
            FosberRightButton.IsEnabled = true;
            SetRunningRoll(0);
            ReturnTimerReset();
        }

        private void RightFosberButton_Click(object sender, RoutedEventArgs e)
        {
            FosberLeftButton.IsEnabled = true;
            FosberRightButton.IsEnabled = false;
            SetRunningRoll(1);
            ReturnTimerReset();
        }

        private void MoveRollButton_Click(object sender, RoutedEventArgs e)
        {
            MoveRoll();
            ReturnTimerReset();
        }

        private void TestButton_Click(object sender,RoutedEventArgs e)
        {
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "RollControl",
                SenderName = "RollControl",
                Action = "SetDisplayMode",
                Message = "Single",
            });
        }

        private void DefaultButton_Click(object sender,RoutedEventArgs e)
        {
            SetDefaults();
        }

        private void UpdateButton_Click(object sender,RoutedEventArgs e)
        {

        }

        private void LeftIdleButton_Click(object sender,RoutedEventArgs e)
        {
            SetReelState(1,0,"ЛПК Б0 100/1000 мак кр",11111);
        }

        private void LeftActiveButton_Click(object sender,RoutedEventArgs e)
        {
            SetReelState(1,1,"ЛПК Б0 100/1000 мак кр",22222);
        }

        private void LeftEmptyButton_Click(object sender,RoutedEventArgs e)
        {
            SetReelState(1,2);
        }

        private void RightIdleButton_Click(object sender,RoutedEventArgs e)
        {
            SetReelState(2,0,"РПК Б0 100/1000 мак кр",11111);
        }

        private void RightActiveButton_Click(object sender,RoutedEventArgs e)
        {
            SetReelState(2,1,"РПК Б0 100/1000 мак кр",222222);
        }

        private void RightEmptyButton_Click(object sender,RoutedEventArgs e)
        {
            SetReelState(2,2);
        }

        private void RunUpdateButton_Click(object sender,RoutedEventArgs e)
        {
            LoadItemsRun();
        }

        private void StopUpdateButton_Click(object sender,RoutedEventArgs e)
        {
            LoadItemsStop();
        }

        private void PrintingButton_Click(object sender, RoutedEventArgs e)
        {
            string cliche = "";
            int i = 0;
            var prValues = GetClichePrintingParams();
            int clicheQty = 1;

            if (prValues.Count == 1)
            {
                cliche = $"Клише: {prValues[0].CheckGet("NKLISHE1")}";
                if (prValues[0].CheckGet("NUMBER_OF_OUTS").ToInt() == 2)
                {
                    cliche = $"{cliche} и парное";
                    clicheQty = 2;
                }
            }

            if (prValues.Count == 2)
            {
                cliche = $"Левый: {prValues[0].CheckGet("NKLISHE1")}\nПравый: {prValues[1].CheckGet("NKLISHE1")}";
                i = 1;
                clicheQty = 2;
            }
            string paint = $"краска: {prValues[i].CheckGet("COL1")}";
            string offset = $"смещение относительно вала: {prValues[i].CheckGet("CLICHE_POSITION").ToInt()}";

            if (clicheQty == 2)
            {
                var clicheGap = 0;
                if (prValues.Count == 2)
                {
                    clicheGap = prValues[0].CheckGet("OFFSET_PRINTING").ToInt() + prValues[1].CheckGet("OFFSET_PRINTING_ON_LEFT").ToInt() - 70;
                }
                else
                {
                    clicheGap = prValues[0].CheckGet("OFFSET_PRINTING").ToInt() + prValues[0].CheckGet("OFFSET_PRINTING_ON_LEFT").ToInt() - 70;
                }

                if (clicheGap < 0)
                    clicheGap = 0;
                offset += $",\nмежду фартуками: {clicheGap}";
            }

            var dw = new DialogWindow($"{cliche},\n{paint},\n{offset}", "Параметры печати");
            dw.ShowDialog();
        }

        private void RenderButton_Click(object sender,RoutedEventArgs e)
        {
            Render();
        }

        private void Resize1Button_Click(object sender,RoutedEventArgs e)
        {
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "All",
                SenderName = "Navigator",
                Action = "Resize",
                Message = "800x600",
            });
        }

        private void Resize2Button_Click(object sender,RoutedEventArgs e)
        {
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "All",
                SenderName = "Navigator",
                Action = "Resize",
                Message = "1280x800",
            });
        }

        private void PrintButton_Click(object sender,RoutedEventArgs e)
        {
            PrintReceipt(1);
        }

        private void CorrectWeighButton_Click(object sender,RoutedEventArgs e)
        {
            EditRoll();
        }

        private void ErrorButton_Click(object sender,RoutedEventArgs e)
        {
            TestShowError();
        }

        private void RestartButton_Click(object sender,RoutedEventArgs e)
        {
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Main",
                ReceiverName = "MainWindow",
                SenderName = "Navigator",
                Action = "Restart",
                Message = "",
            }); 
        }

        private void PrintNowButton_Click(object sender,RoutedEventArgs e)
        {
            PrintReceipt(2);
        }

        private void ExitButton_Click(object sender,RoutedEventArgs e)
        {
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Main",
                ReceiverName = "MainWindow",
                SenderName = "Navigator",
                Action = "Exit",
                Message = "",
            }); 
        }

        private void F11Button_Click(object sender,RoutedEventArgs e)
        {
            //переход в полноэкранный режим
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Main",
                ReceiverName = "MainWindow",
                SenderName = "Navigator",
                Action = "SetScreenMode",
                Message = "fullscreentoggle",
            });
        }

        private void TerminateButton_Click(object sender,RoutedEventArgs e)
        {
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Main",
                ReceiverName = "MainWindow",
                SenderName = "Navigator",
                Action = "Exit",
                Message = "",
            }); 
        }

        private void SettingsButton_Click(object sender,RoutedEventArgs e)
        {
            GetLastRoll();
        }

        private void TasksButton_Click(object sender, RoutedEventArgs e)
        {
            RestoreRoll();
        }

        private void BurgerButton_Click(object sender,RoutedEventArgs e)
        {
            BurgerMenu.IsOpen=true;
        }

        private void BurgerSettings_Click(object sender,RoutedEventArgs e)
        {
            GetLastRoll();
        }

        private void BurgerExit_Click(object sender,RoutedEventArgs e)
        {
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Main",
                ReceiverName = "MainWindow",
                SenderName = "Navigator",
                Action = "Exit",
                Message = "",
            }); 
        }

        

        private void RolliInfo_Click(object sender,RoutedEventArgs e)
        {
            ShowRollInfo();
        }

        private void BurgerRestore_Click(object sender,RoutedEventArgs e)
        {
            RestoreRoll();
        }

        private void HelpButton_Click(object sender,RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void BurgerDebug_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LogButton_Click(object sender, RoutedEventArgs e)
        {
            ShowLog();
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            ShowInfo();
        }

        private void BurgerPrintLabel_Click(object sender, RoutedEventArgs e)
        {
            PrintLabel();
        }

        private void SettingsButton_Click_1(object sender, RoutedEventArgs e)
        {
            ShowSettings();
        }

        private void InfoButton2_Click(object sender, RoutedEventArgs e)
        {
            ShowTest();
        }
    }
}
