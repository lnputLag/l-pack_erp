using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Messages;
using Client.Interfaces.Production.Corrugator._CorrugatorMachineOperator.Elements;
using Client.Interfaces.Production.Corrugator.CorrugatorMachineOperator.Frames;
using Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh;
using Client.Interfaces.Sales;
using Client.Interfaces.Stock;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.Strapper
{
    /// <summary>
    /// Упаковщик Кашира
    /// </summary>
    public partial class StrapperKsh : ControlBase
    {
        public StrapperKsh()
        {
            ControlTitle = "Упаковщик КШ";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]strapper_ksh";
            InitializeComponent();

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (System.Windows.Input.KeyEventArgs e) =>
            {
                ScannerInput.InputProcess(Input, e);

                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                InitForm();
                SetDefaults();
                OutgoingGridInit();
                IdleGridInit();

                RunAutoUpdateTimer();
                RunCallMessageTimer();
                GetData();
                IdleGridLoadItems();
                SetTimerFire(10);
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                OutgoingGrid.Destruct();

                IdleGrid.Destruct();

                if (AutoUpdateTimer != null)
                {
                    AutoUpdateTimer.Stop();
                }

                if (CallMessageTimer != null)
                {
                    CallMessageTimer.Stop();
                }

                TimerFire?.Stop();

                ScannerInput.Dispose();
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
                ScannerInput.Init(this);
                ScannerInput.SetScanningStatus(ScannerInputControl.ScannerInputStatusRef.Enabled);
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
                ScannerInput.SetScanningStatus(ScannerInputControl.ScannerInputStatusRef.Disabled);
            };

            {
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "main",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonControl = RefreshButton,
                    ButtonName = "RefreshButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        GetData();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "help",
                    Group = "main",
                    Enabled = true,
                    Title = "Справка",
                    Description = "Показать справочную информацию",
                    ButtonUse = true,
                    ButtonName = "HelpButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "fire_button",
                    Group = "main",
                    Enabled = true,
                    Title = "Пожар",
                    Description = "Сообщить о пожаре",
                    ButtonUse = true,
                    ButtonControl = FireAlarmButton,
                    ButtonName = "FireAlarmButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        FireAlarm();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "close_call_message",
                    Group = "call_message",
                    Enabled = true,
                    Title = "Закрыть сообщение",
                    Description = "Обработать вызов и закрыть сообщение",
                    ButtonUse = true,
                    ButtonControl = CloseCallMessageButton,
                    ButtonName = "CloseCallMessageButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        CloseCallMessage();
                    },
                });
            }

            Commander.SetCurrentGridName("IdleGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "idle_refresh",
                    Group = "idle_main",
                    Enabled = true,
                    Title = "Обновить",
                    Description = "Обновить данные",
                    ButtonUse = true,
                    ButtonControl = IdleRefreshButton,
                    ButtonName = "IdleRefreshButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        IdleGridLoadItems();
                    },
                });
                //Commander.Add(new CommandItem()
                //{
                //    Name = "idle_split",
                //    Group = "idle_manager",
                //    Enabled = true,
                //    Title = "Разбить",
                //    Description = "Разбить простой",
                //    ButtonUse = Central.DebugMode,
                //    MenuUse = Central.DebugMode,
                //    ButtonControl = IdleSplitButton,
                //    ButtonName = "IdleSplitButton",
                //    AccessLevel = Role.AccessMode.FullAccess,
                //    Action = () =>
                //    {
                //        var s = IdleGrid.SelectedItem;

                //        IdleSplit(s);
                //    },
                //});
                Commander.Add(new CommandItem()
                {
                    Name = "idle_edit",
                    Group = "idle_manager",
                    Enabled = true,
                    Title = "Изменить",
                    Description = "Изменить простой",
                    ButtonUse = true,
                    MenuUse = true,
                    ButtonControl = IdleEditButton,
                    ButtonName = "IdleEditButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    HotKey= "DoubleCLick",
                    Action = () =>
                    {
                        var s = IdleGrid.SelectedItem;

                        if (s != null && s.CheckGet("IDIDLES").ToInt() > 0)
                        {
                            var idleEdit = new IdleEdit();
                            idleEdit.Id = s.CheckGet("IDIDLES").ToInt();
                            idleEdit.IdleReasonText.Text = s.CheckGet("REASON").ToString();
                            idleEdit.Idle = IdleEdit.TypeIdle.StrapperKsh;
                            idleEdit.SelectedIdleItem = s;
                            idleEdit.OnClose = IdleGridLoadItems;
                            idleEdit.Show();
                        }
                    },
                });
            }

            Commander.Init(this);
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Датасет с данными грида позиций входящих исходящих
        /// </summary>
        private ListDataSet OutgoingGridDataSet { get; set; }

        /// <summary>
        /// Датасет с данными грида простоев
        /// </summary>
        private ListDataSet IdleGridDataSet { get; set; }

        /// <summary>
        /// Таймер получения данных
        /// </summary>
        private DispatcherTimer AutoUpdateTimer { get; set; }

        /// <summary>
        /// интервал обновления таймера получения данных (сек)
        /// </summary>
        private int AutoUpdateTimerInterval { get; set; }

        /// <summary>
        /// Таймер проверки сообщенй о вызове
        /// </summary>
        private DispatcherTimer CallMessageTimer { get; set; }

        /// <summary>
        /// Статус пожара на объекте
        /// </summary>
        public string FireInPlace { get; set; }
        /// <summary>
        /// Таймер проверки пожара
        /// </summary>
        private DispatcherTimer TimerFire;

        /// <summary>
        /// Место работы программы
        /// </summary>
        public const string CurrentPlaceName = "Упаковка 1";

        /// <summary>
        /// интервал обновления таймера проверки сообщенй о вызове (сек)
        /// </summary>
        private int CallMessageTimerInterval { get; set; }

        /// <summary>
        /// Флаг того, что запрос при сканировании ярлыка ещё работает
        /// </summary>
        private bool QueryInProgress { get; set; }

        private bool CallMessageExistFlag = false;

        private int FactoryId = 2;

        private string PlaceName = "УПК";

        private int PlaceNumber = 1;

        private int MachineId = 3809;

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            OutgoingGridDataSet = new ListDataSet();
            IdleGridDataSet = new ListDataSet();

            AutoUpdateTimerInterval = 60 * 5;
            CallMessageTimerInterval = 10;

            ScannerInput.OnBarcodeProcess = ScannerInputProcess;
        }

        private void ScannerInputProcess(string code)
        {
            // Если в данный момент не выполняется запрос, то можем обрабатывать новый введённый штрихкод
            if (!QueryInProgress)
            {
                if (!string.IsNullOrEmpty(code) && code.Length >= 13)
                {
                    MovePallet(code);
                    QueryInProgress = false;
                }
            }
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void InitForm()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=SearchText,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="IDLE_SEARCH",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=IdleSearchText,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                };

                Form.SetFields(fields);
            }
        }

        private void OutgoingGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид поддона",
                        Description = "Идентификатор поддона",
                        Path="ID_PODDON",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер поддона",
                        Description = "Номер поддона",
                        Path="PZ_NUM",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество, шт.",
                        Description = "Количество на поддоне",
                        Path="KOL",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=12,
                        Format="N0",
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продукция",
                        Description = "Наименование продукции",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=40,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Description = "Артикул продукции",
                        Path="ARTIKUL",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Заявка",
                        Description = "Данные по заявке",
                        Path="ORDER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=40,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Ид отгрузки",
                        Description = "Идентификатор отгрузки",
                        Path="ID_TS",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид заявки",
                        Description = "Идентификатор заявки",
                        Path="ID_ORDER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                };
                OutgoingGrid.SetColumns(columns);
                OutgoingGrid.SetPrimaryKey("ID_PODDON");
                OutgoingGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                OutgoingGrid.Toolbar = OutgoingGridToolbar;
                OutgoingGrid.SearchText = SearchText;
                OutgoingGrid.AutoUpdateInterval = 0;
                OutgoingGrid.Commands = Commander;
                OutgoingGrid.UseProgressSplashAuto = false;
                OutgoingGrid.ItemsAutoUpdate = false;
                OutgoingGrid.Init();
                OutgoingGrid.Run();
            }
        }

        /// <summary>
        /// инициализация грида IdleGrid
        /// </summary>
        public void IdleGridInit()
        {
            {
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="IDIDLES",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Начало простоя",
                        Path="FROMDT",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="dd.MM.yyyy HH:mm:ss",
                        Width2=14,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продолжительность",
                        Path="DT",
                        ColumnType=ColumnTypeRef.DateTime,
                        Format="HH:mm:ss",
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Причина",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=14,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Описание",
                        Path="DETAIL",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16
                    },
                    new DataGridHelperColumn
                    {
                        Header="REASON",
                        Path="REASON",
                        ColumnType=ColumnTypeRef.String,
                        Width2=16,
                        Visible=false
                    },
                };
                IdleGrid.SetColumns(columns);
                IdleGrid.SetPrimaryKey("IDIDLES");
                IdleGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                IdleGrid.Toolbar = IdleGridToolbar;
                IdleGrid.SearchText = IdleSearchText;
                IdleGrid.AutoUpdateInterval = 0;
                IdleGrid.Commands = Commander;
                IdleGrid.UseProgressSplashAuto = false;
                IdleGrid.ItemsAutoUpdate = false;
                IdleGrid.Init();
                IdleGrid.Run();
            }
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void IdleGridLoadItems()
        {
            IdleGridToolbar.IsEnabled = false;

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", "3809");
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Idle");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            IdleGridDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    IdleGridDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            else
            {
                q.SilentErrorProcess = true;
                q.ProcessError();
            }

            if (IdleGridDataSet != null && IdleGridDataSet.Items != null && IdleGridDataSet.Items.Count > 0)
            {
                foreach (var i in IdleGridDataSet.Items)
                {
                    if (!i.CheckGet("DESCRIPTION").IsNullOrEmpty())
                    {
                        i.CheckAdd("DETAIL", $"{i.CheckGet("DESCRIPTION")}. {i.CheckGet("REASON")}");
                    }
                }

                IdleGrid.UpdateItems(IdleGridDataSet);
            }
            else
            {
                IdleGrid.ClearItems();
            }

            IdleGridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Получение данных
        /// </summary>
        private async void GetData()
        {
            OutgoingGridToolbar.IsEnabled = false;

            var p = new Dictionary<string, string>();
            p.Add("STOCK_ROW", PlaceName);
            p.Add("STOCK_NUMBER", $"{PlaceNumber}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "Condition");
            q.Request.SetParam("Action", "ListPallet");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            OutgoingGridDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null && result.Count > 0)
                {
                    OutgoingGridDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            else
            {
                q.SilentErrorProcess = true;
                q.ProcessError();
            }

            if (OutgoingGridDataSet != null && OutgoingGridDataSet.Items != null && OutgoingGridDataSet.Items.Count > 0)
            {
                OutgoingGrid.UpdateItems(OutgoingGridDataSet);
            }
            else
            {
                OutgoingGrid.ClearItems();
            }

            OutgoingGridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Проверка нализия сообщений о вызове
        /// </summary>
        private async void GetCallMessage()
        {
            bool showMessageWindow = false;

            var p = new Dictionary<string, string>();
            p.Add("MACHINE_ID", $"{MachineId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production/StrapperKsh");
            q.Request.SetParam("Object", "Machine");
            q.Request.SetParam("Action", "GetCallMessage");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null && result.Count > 0)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if(!CallMessageExistFlag && ds.Items[0].CheckGet("CALL_FORKLIFT_FLAG").ToBool())
                        {
                            showMessageWindow = true;
                            CallMessageExistFlag = true;
                        }
                    }
                }
            }
            else
            {
                q.SilentErrorProcess = true;
                q.ProcessError();
            }

            if (showMessageWindow)
            {
                ShowCallMessage(true);
            }
        }

        private void RunAutoUpdateTimer()
        {
            if (AutoUpdateTimerInterval != 0)
            {
                if (AutoUpdateTimer == null)
                {
                    AutoUpdateTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, AutoUpdateTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", AutoUpdateTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("StrapperKsh_RunAutoUpdateTimer", row);
                    }

                    AutoUpdateTimer.Tick += (s, e) =>
                    {
                        GetData();
                        IdleGridLoadItems();
                    };
                }

                if (AutoUpdateTimer.IsEnabled)
                {
                    AutoUpdateTimer.Stop();
                }

                AutoUpdateTimer.Start();
            }
        }

        /// <summary>
        /// установка таймера проверки пожара
        /// </summary>
        public void SetTimerFire(int autoUpdateFireStatusInterval)
        {
            TimerFire = new DispatcherTimer();
            TimerFire.Tick += CheckFireStatus;
            TimerFire.Interval = new TimeSpan(0, 0, autoUpdateFireStatusInterval);
            TimerFire.Start();

            {
                var row = new Dictionary<string, string>();
                row.CheckAdd("TIMEOUT", autoUpdateFireStatusInterval.ToString());
                row.CheckAdd("DESCRIPTION", "");
                Central.Stat.TimerAdd("CorrugatorMachineOperator_SetTimerFire", row);
            }
        }

        /// <summary>
        /// Проверка статуса об пожаре
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CheckFireStatus(object sender, EventArgs e)
        {
            var q = await LPackClientQuery.DoQueryAsync("Production", "IndustrialWaste", "ListFire", "ITEMS", new Dictionary<string, string>
                {
                    { "FIRE_NAME", "FIRE_KSH" }
                });

            if (q.Answer.Status == 0)
            {
                if (q.Answer.QueryResult != null)
                {
                    var ds = q.Answer.QueryResult;

                    if (ds.Items != null && ds.Items.Count > 0)
                    {
                        FireInPlace = ds.Items.First().CheckGet("PARAM_VALUE");

                        if (FireInPlace != null && FireInPlace != "null" && !FireInPlace.IsNullOrEmpty())
                        {
                            FireStatus.Text = "Пожар! " + FireInPlace;
                            FireStatus.Visibility = Visibility.Visible;

                            foreach (Window window in Application.Current.Windows)
                            {
                                if (window.Content is FireInformatio)
                                {
                                    window.Close();
                                }
                            }

                            var fire = new FireInformatio();
                            fire.ShowWindow(FireInPlace);
                        }
                        else
                        {
                            FireStatus.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
        }

        private void RunCallMessageTimer()
        {
            if (CallMessageTimerInterval != 0)
            {
                if (CallMessageTimer == null)
                {
                    CallMessageTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, CallMessageTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", CallMessageTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("StrapperKsh_RunCallMessageTimer", row);
                    }

                    CallMessageTimer.Tick += (s, e) =>
                    {
                        GetCallMessage();
                    };
                }

                if (CallMessageTimer.IsEnabled)
                {
                    CallMessageTimer.Stop();
                }

                CallMessageTimer.Start();
            }
        }

        private void SetSplash(bool inProgressFlag, string msg = "Загрузка")
        {
            QueryInProgress = inProgressFlag;
            SplashControl.Visible = inProgressFlag;

            SplashControl.Message = msg;
        }

        private void ShowCallMessage(bool showFlag)
        {
            if (showFlag)
            {
                CallMessageRowDefinition.Height = new GridLength(1, GridUnitType.Star);
                CallMessageBorder.Visibility = Visibility.Visible;
            }
            else
            {
                CallMessageBorder.Visibility = Visibility.Collapsed;
                CallMessageRowDefinition.Height = new GridLength(1, GridUnitType.Auto);
            }
        }

        private async void CloseCallMessage()
        {
            string msg = $"Обработать вызов и закрыть сообщение?";
            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.YesNo);
            if (d.ShowDialog() == true)
            {
                var p = new Dictionary<string, string>();
                p.Add("MACHINE_ID", $"{MachineId}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production/StrapperKsh");
                q.Request.SetParam("Object", "Machine");
                q.Request.SetParam("Action", "ProcessCallMessage");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    bool succesfullFlag = false;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null && result.Count > 0)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(ds.Items[0].CheckGet("MACHINE_ID")))
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (succesfullFlag)
                    {
                        ShowCallMessage(false);
                        CallMessageExistFlag = false;
                    }
                    else
                    {
                        msg = $"При обработке вызова произошла ошибка. Пожалуйста, сообщите о проблеме.";
                        d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }


        /// <summary>
        /// Разбивка простоя
        /// </summary>
        /// <param name="idle"></param>
        /// <exception cref="NotImplementedException"></exception>
        private async void IdleSplit(Dictionary<string, string> idle)
        {
            if (idle == null)
                return;

            // Разрешаем разбивку только для завершенного простоя
            var toDt = idle.CheckGet("DT");
            if (string.IsNullOrEmpty(toDt))
            {
                var d = new DialogWindow("Дождитесь пока простой закончится\nЗатем его можно будет разбить.", "Простой не завершен!");
                d.ShowDialog();
                return;
            }

            int id = idle.CheckGet("IDIDLES").ToInt();
            var ctrl = new IdleSplit();

            // Попытка инициализации из выбранной строки
            DateTime? fromTime = TryParseDateTime(idle.CheckGet("FROMDT"));
            DateTime? toTime = TryParseDateTime(idle.CheckGet("TODT"));

            // Если данных нет, запрашиваем подробности
            if (!fromTime.HasValue || !toTime.HasValue)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Idle");
                q.Request.SetParam("Action", "Get");
                q.Request.SetParam("IDIDLES", id.ToString());

                q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() => q.DoQuery());

                if (q.Answer.Status == 0)
                {
                    try
                    {
                        var resp = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        var data = ListDataSet.Create(resp, "ITEMS");

                        string fromStr = data.Items.First().CheckGet("FROMDT");
                        string toStr = data.Items.First().CheckGet("TODT");

                        fromTime = fromTime ?? TryParseDateTime(fromStr);
                        toTime = toTime ?? TryParseDateTime(toStr);
                    }
                    catch
                    {

                    }
                }
            }

            if (!fromTime.HasValue || !toTime.HasValue || fromTime.Value >= toTime.Value)
            {
                var d = new DialogWindow("Не удалось получить корректные границы простоя.", "Ошибка данных");
                d.ShowDialog();
                return;
            }

            ctrl.FromTime = fromTime.Value;
            ctrl.ToTime = toTime.Value;
            ctrl.SplitTime = ctrl.FromTime.AddTicks((ctrl.ToTime - ctrl.FromTime).Ticks / 2);

            var host = new Window
            {
                Title = $"Разбивка простоя {id}",
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                Owner = Application.Current?.MainWindow,
                Content = ctrl,
                ResizeMode = ResizeMode.NoResize
            };

            ctrl.SplitConfirmed += async (s, e) =>
            {
                const string fmt = "dd.MM.yyyy HH:mm:ss";

                var qSplit = new LPackClientQuery();
                qSplit.Request.SetParam("Module", "Production");
                qSplit.Request.SetParam("Object", "Idle");
                qSplit.Request.SetParam("Action", "Split");
                qSplit.Request.SetParam("IDIDLES", id.ToString());
                qSplit.Request.SetParam("FROMDT", e.FromTime.ToString(fmt, CultureInfo.InvariantCulture));
                qSplit.Request.SetParam("TODT", e.ToTime.ToString(fmt, CultureInfo.InvariantCulture));
                qSplit.Request.SetParam("SPLIT_DTTM", e.SplitTime.ToString(fmt, CultureInfo.InvariantCulture));

                qSplit.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
                qSplit.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() => qSplit.DoQuery());

                if (qSplit.Answer.Status == 0)
                {
                    host.DialogResult = true;
                    host.Close();
                    IdleGrid.LoadItems();
                }
            };

            ctrl.Cancelled += (s, e) =>
            {
                host.DialogResult = false;
                host.Close();
            };

            host.ShowDialog();

            static DateTime? TryParseDateTime(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return null;
                if (DateTime.TryParseExact(s,
                        new[] { "dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy HH:mm", "dd.MM.yyyy",
                                "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.fff" },
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeLocal, out var dt))
                {
                    return dt;
                }
                if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dt))
                {
                    return dt;
                }
                return null;
            }
        }

        /// <summary>
        /// Запрос на перемещение поддона
        /// Вызывается в моемент сканирования ярлыка
        /// </summary>
        /// <param name="code"></param>
        private void MovePallet(string str)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SetSplash(true, "Обработка поддона");
            });

            str = str.Trim();
            if (str.Length == 13 && int.TryParse(str.Substring(3, 9), out var palletId))
            {
                {
                    var p = new Dictionary<string, string>();
                    p.Add("PALLET_ID", $"{palletId}");
                    p.Add("PLACE_NAME", PlaceName);
                    p.Add("PLACE_NUMBER", $"{PlaceNumber}");
                    p.Add("FACTORY_ID", $"{FactoryId}");

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Production/StrapperKsh");
                    q.Request.SetParam("Object", "Pallet");
                    q.Request.SetParam("Action", "Move");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            if (ds != null && ds.Items != null && ds.Items.Count > 0)
                            {
                                if (ds.Items.First().CheckGet("PALLET_ID").ToInt() > 0)
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        bool firstScanFlag = true;
                                        try
                                        {
                                            firstScanFlag = ds.Items[0].CheckGet("FIRST_SCAN_IN_STRAPPER").ToInt() > 0;
                                        }
                                        catch (Exception ex)
                                        {
                                            firstScanFlag = true;
                                        }

                                        // Если впервые сканируем ярлык на сигноде
                                        if (firstScanFlag)
                                        {
                                            string msg = "Ярлык успешно отсканирован";
                                            int status = 2;
                                            var d = new StackerScanedLableInfo(msg, status);
                                            d.WindowMaxSizeFlag = true;
                                            d.ShowAndAutoClose(1);
                                        }
                                        else
                                        {
                                            string msg = "Ярлык успешно повторно отсканирован";
                                            int status = 0;
                                            var d = new StackerScanedLableInfo(msg, status);
                                            d.WindowMaxSizeFlag = true;
                                            d.ShowAndAutoClose(1);
                                        }

                                        GetData();
                                    });
                                }
                            }
                        }
                    }
                    else if (q.Answer.Status == 145)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            string msg = q.Answer.Error.Message;
                            int status = 1;
                            var d = new StackerScanedLableInfo(msg, status);
                            d.WindowMaxSizeFlag = true;
                            d.ShowAndAutoClose(1);
                        });

                        q.SilentErrorProcess = true;
                        q.ProcessError();
                    }
                    else
                    {
                        q.SilentErrorProcess = true;
                        q.ProcessError();
                    }
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                SetSplash(false);
            });
        }


        /// <summary>
        /// Сообщение о пожаре
        /// </summary>
        private void FireAlarm()
        {
            if (FireInPlace == string.Empty)
            {
                FireStatus.Visibility = Visibility.Visible;
                FireStatus.Text = $"Пожар! {CurrentPlaceName}";
                FireAlarmButton.Style = (Style)FireAlarmButton.TryFindResource("ButtonFireActive");
                FireInPlace = CurrentPlaceName;
                FireAlarmImage.Visibility = Visibility.Visible;
                UpdateFireStatus(CurrentPlaceName);
            }
            else if (FireInPlace == CurrentPlaceName)
            {
                FireInPlace = string.Empty;
                FireStatus.Visibility = Visibility.Hidden;
                FireAlarmButton.Style = (Style)FireAlarmButton.TryFindResource("ButtonFire");

                FireAlarmImage.Visibility = Visibility.Hidden;

                UpdateFireStatus();
            }
        }

        /// <summary>
        /// запрос на изменение места пожара
        /// </summary>
        /// <param name="place"> место пожара, если 'null' значит пожара нет</param>
        public void UpdateFireStatus(string place = "")
        {
            bool resume = true;

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "IndustrialWaste");
                q.Request.SetParam("Action", "UpdateFire");

                var p = new Dictionary<string, string>();

                p.Add("PLACE", place);
                p.Add("FIRE_NAME", "FIRE_KSH");

                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();
            }
        }
    }
}
