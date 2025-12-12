using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Взвешивание
    /// </summary>
    /// <author>ledovskikh_dv</author>
    public partial class UnitWeight : UserControl
    {
        public UnitWeight(int factId)
        {
            InitializeComponent();

            if (factId == 1)
            {
                RoleName = "[erp]industrial_waste";
                _factId = factId;
            }
            else if (factId == 2)
            {
                RoleName = "[erp]industrial_waste_ksh";
                _factId = factId;
            }

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            GridInit();
            SetTimer();
            SetDefaults();
            ProcessPermissions();

            FireStatus.Visibility = Visibility.Hidden;
            FireAlarmImage.Visibility = Visibility.Hidden;
        }

        public string RoleName = "[erp]industrial_waste";

        private int _factId { get; set; } = 1;

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// данные из выбранной в гриде строки
        /// </summary>
        Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// статус пожара на объекте
        /// </summary>
        public string FireInPlase { get; set; }

        /// <summary>
        /// место работы программы
        /// </summary>
        private const string PLACE = "Пресс-переработка";

        /// <summary>
        /// таймер проверки пожара
        /// </summary>
        System.Windows.Threading.DispatcherTimer Timer;

        /// <summary>
        /// регулярное выражение для проверки ввода
        /// </summary>
        public static Regex onlyNumbers = new Regex("[^0-9,]+");

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

            if (Grid != null && Grid.Menu != null && Grid.Menu.Count > 0)
            {
                foreach (var manuItem in Grid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// установка таймера
        /// </summary>
        public void SetTimer()
        {
            Timer = new System.Windows.Threading.DispatcherTimer();
            Timer.Tick += new EventHandler(FireAlarmCheck);
            Timer.Interval = new TimeSpan(0, 0, 10);
            Timer.Start();

            {
                var row = new Dictionary<string, string>();
                row.CheckAdd("TIMEOUT", "10");
                row.CheckAdd("DESCRIPTION", "");
                Central.Stat.TimerAdd("UnitWeight_RunComplectationWarningTimer", row);
            }
        }

        /// <summary>
        // инициализация компонентов таблицы
        /// </summary>
        public void GridInit()
        {
            //инициализация грида
            {
                Form = new FormHelper();

                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Path="INWA_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Visible = false,
                    },
                    new DataGridHelperColumn
                    {
                        Path="INWS_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Visible = false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Источник",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=160,
                        MaxWidth=510,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Брутто, кг",
                        Path="GROSS_WEIGHT",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth=73,
                        MaxWidth=100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Тара, кг",
                        Path="TARE_WEIGHT",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth=65,
                        MaxWidth=85,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Нетто, кг",
                        Path="NET_WEIGHT",
                        ColumnType=ColumnTypeRef.Double,
                        MinWidth=65,
                        MaxWidth=93,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Дата, время",
                        Path="CREATED_DTTM",
                        ColumnType=ColumnTypeRef.DateTime,
                        MinWidth=120,
                        MaxWidth=177,
                    },
                    new DataGridHelperColumn
                    {
                       Header = " ",
                       Path = "_",
                       ColumnType = DataGridHelperColumn.ColumnTypeRef.String,
                       MinWidth = 5,
                       MaxWidth = 2000,
                    },
                };
                Grid.SetColumns(columns);

                Grid.SetMode(1);

                Grid.UseRowHeader = true;
                Grid.Init();

                Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    { "set", new DataGridContextMenuItem(){
                        Header="Добавить",
                        Tag = "access_mode_full_access",
                        Action=()=>
                        {
                            SetWeight();
                        }
                    }},
                    { "edit", new DataGridContextMenuItem(){
                        Header="Изменить",
                        Tag = "access_mode_full_access",
                        Action=()=>
                        {
                            Edit();
                        }
                    }},
                };
                Grid.Menu.Add("Debug", new DataGridContextMenuItem()
                {
                    Visible = false,
                });

                Grid.PrimaryKey = "INWS_ID";

                //при выборе строки в гриде, обновляются актуальные действия для записи
                Grid.OnSelectItem = selectedItem =>
                {
                    SelectedItem = selectedItem;
                    var outVal = new DataGridContextMenuItem();
                    if (Grid.Menu.TryGetValue("Item1", out outVal))
                        outVal.Enabled = !SelectedItem.CheckGet("LOCKED").ToBool();

                    ProcessPermissions();
                };

                //двойной клик на строке откроет форму редактирования
                Grid.OnDblClick = selectedItem =>
                {
                    if (Central.Navigator.GetRoleLevel(this.RoleName) > Role.AccessMode.ReadOnly)
                    {
                        SetWeight();
                    }
                };

                //данные грида
                Grid.OnLoadItems = GridLoadItems;
                Grid.Run();
            }

            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>();

                Form.SetFields(fields);
            }
        }

        /// <summary>
        /// получение данных весов
        /// </summary>
        public async void GridLoadItems()
        {
            GridDisableControls();

            bool resume = true;

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "IndustrialWaste");
                q.Request.SetParam("Action", "ListIndustrialWaste");
                q.Request.SetParam("FACT_ID", $"{_factId}");

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
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            Grid.UpdateItems(ds);
                        }
                    }
                }
            }
            GridEnableControls();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку добавления записи
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetButton_Click(object sender, RoutedEventArgs e)
        {
            SetWeight();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку редактирования записи
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            Edit();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку пожара
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FireAlarmButton_Click(object sender, RoutedEventArgs e)
        {
            FireAlarm();
        }

        /// <summary>
        /// Сообщение о пожаре
        /// </summary>
        private void FireAlarm()
        {
            if (FireInPlase == null)
            {
                var d = new DialogWindow($"Объявить пожарную тревогу на объекте?", "Пожарная тревога", "", DialogWindowButtons.YesNo);
                d.ShowDialog();
                if(d.DialogResult == true)
                {
                    Timer.Stop();

                    GridDisableControls();

                    FireStatus.Visibility = Visibility.Visible;
                    FireStatus.Text = $"Пожар! {PLACE}";
                    FireAlarmButton.Style = (Style)FireAlarmButton.TryFindResource("RollReelButtonActive");
                    FireInPlase = PLACE;

                    FireAlarmImage.Visibility = Visibility.Visible;

                    UpdateFireStatus(PLACE);

                    GridEnableControls();
                }
            }
            else if (FireInPlase == PLACE)
            {
                GridDisableControls();

                FireInPlase = null;
                FireStatus.Visibility = Visibility.Hidden;
                FireAlarmButton.Style = (Style)FireAlarmButton.TryFindResource("RollReelButton");

                FireAlarmImage.Visibility = Visibility.Hidden;

                UpdateFireStatus();

                GridEnableControls();

                Timer.Start();
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

                if (_factId == 2)
                {
                    p.Add("FIRE_NAME", "FIRE_KSH");
                }

                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();
            }
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void GridDisableControls()
        {
            GridToolbar.IsEnabled = false;
            Grid.ShowSplash();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void GridEnableControls()
        {
            GridToolbar.IsEnabled = true;
            Grid.HideSplash();
        }

        /// <summary>
        /// добавление записи
        /// </summary>
        public void SetWeight()
        {
            if (SelectedItem.CheckGet("INWS_ID").ToInt() != 0)
            {
                var inws_id = SelectedItem.CheckGet("INWS_ID").ToInt();
                var source = SelectedItem.CheckGet("NAME").ToString();
                var gross_weight = Weight.Text.ToDouble();
                
                var i = new SetWeight();
                i.Set(inws_id, source, gross_weight);
            }
        }

        /// <summary>
        /// редактирование записи
        /// </summary>
        public void Edit()
        {
            var inwa_id = SelectedItem.CheckGet("INWA_ID").ToInt();
            if (inwa_id != 0)
            {
                var source = SelectedItem.CheckGet("NAME").ToString();
                var gross_weight = SelectedItem.CheckGet("GROSS_WEIGHT").ToDouble();
                var tare_weight = SelectedItem.CheckGet("TARE_WEIGHT").ToDouble();

                var i = new UpdateWeight();
                i.Edit(inwa_id, source, gross_weight, tare_weight);
            }
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = "UnitWeight",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            Grid.Destruct();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
            //Group ProductionTask
            if (m.ReceiverGroup.IndexOf("Production") > -1 && m.ReceiverName.IndexOf("UnitWeight") > -1)
            {
                switch (m.Action)
                {
                    case "Refresh":
                        Grid.LoadItems();

                        Grid.SetSelectedItemId(Grid.Items[0]["INWA_ID"].ToInt());

                        break;
                }
            }
        }

        /// <summary>
        /// обработка ввода с клавиатуры (роли)
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F1:
                    ShowHelp();
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

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        /// <summary>
        /// проверка пожара
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void FireAlarmCheck(object sender, EventArgs e)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "IndustrialWaste");
            q.Request.SetParam("Action", "ListFire");

            if (_factId == 2)
            {
                q.Request.SetParam("FIRE_NAME", "FIRE_KSH");
            }

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
                    FireInPlase = ds.Items[0]["PARAM_VALUE"];

                    if(FireInPlase != null && FireInPlase != "null")
                    {
                        FireStatus.Text = "Пожар! " + FireInPlase;
                        FireStatus.Visibility = Visibility.Visible;
                        FireAlarmImage.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        FireStatus.Visibility = Visibility.Hidden;
                        FireAlarmImage.Visibility = Visibility.Hidden;
                    }
                }
            }
        }

        /// <summary>
        /// проверка ввода
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = onlyNumbers.IsMatch(e.Text);
        }

        /// <summary>
        /// обработчик нажатия на кнопку документации
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        /// <summary>
        /// документация к интерфейсу
        /// </summary>
        public void ShowHelp()
        {
            var h = new UnitWeightHelp();
            h.Init();
        }

        /// <summary>
        /// Отладочная информация
        /// </summary>
        private void ShowInfo()
        {
            var t = "Отладочная информация";
            var m = Central.MakeInfoString();
            m += $"\n FACT_ID={_factId}";
            m += $"\n ROLE={RoleName}";
            var i = new ErrorTouch();
            i.Show(t, m);
        }

        /// <summary>
        /// Обработчик нажатия на кнопку Обновить
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            Restart();
        }

        /// <summary>
        /// рестарт программы
        /// </summary>
        private void Restart()
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

        /// <summary>
        /// Обработчик нажатия на кнопку Выход
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BurgerExit_Click(object sender, RoutedEventArgs e)
        {
            Exit();
        }

        /// <summary>
        /// закрытие программы
        /// </summary>
        private void Exit()
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

        /// <summary>
        /// Отображает меню бургера по нажатию
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BurgerButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        /// <summary>
        /// обработчик нажатия на кнопку Информация
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InfoButto_Click(object sender, RoutedEventArgs e)
        {
            ShowInfo();
        }
    }
}
