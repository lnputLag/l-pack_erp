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
using System.Windows.Media;
using Client.Interfaces.Main;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Выбор поставщика/машины
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class SelectPostavshic : UserControl
    {
        public SelectPostavshic(Dictionary<string, string> v)
        {
            InitializeComponent();
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            IdSt = v.CheckGet("IdSt").ToInt();
            ReceiverName = v.CheckGet("ReceiverName").ToString();
            Mode = v.CheckGet("Mode").ToInt();
            PhoneDriver = v.CheckGet("PhoneDriver").ToString();
            IdPost = v.CheckGet("IdPost").ToInt();
            PostavshicName = v.CheckGet("PostavshicName").ToString();
            MarkaCar = v.CheckGet("MarkaCar").ToString();
            NumberCar = v.CheckGet("NumberCar").ToString();
            TypeGrid = v.CheckGet("TypeGrid").ToInt();
        }

        /// <summary>
        /// Форма
        /// </summary>
        public FormHelper Form { get; set; }

        private DriverList parentForm { get; set; }

        /// <summary>
        /// интервал автозакрытия формы, сек
        /// </summary>
        private int AutoCloseFormInterval { get; set; }

        /// <summary>
        /// таймер автозакрытия формы
        /// </summary>
        private DispatcherTimer AutoCloseFormTimer { get; set; }

        private DateTime LastClick { get; set; }

        /// <summary>
        /// данные из выбранной в гриде списка строки
        /// </summary>
        private Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Имя вкладки, куда происходит возврат при закрытии этой вкладки
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Имя этой вкладки
        /// </summary>
        public string TabName;

        /// <summary>
        /// 0 - выбор поставщика, 1 - выбор марки машины
        /// </summary>
        public int TypeGrid { get; set; }

        /// <summary>
        /// Id поставщика
        /// </summary>
        public int IdPost { get; set; }

        /// <summary>
        /// Название поставщика
        /// </summary>
        public string PostavshicName { get; set; }

        /// <summary>
        /// текущая плащадка 716- БДМ1, 1716-БДМ2
        /// </summary>
        public int IdSt { get; set; }

        /// <summary>
        /// марка машины
        /// </summary>
        public string MarkaCar { get; set; }

        /// <summary>
        /// номер машины (только три цифры)
        /// </summary>
        public string NumberCar { get; set; }

        /// <summary>
        /// сотовый телефон водителя (только цифры)
        /// </summary>
        public string PhoneDriver { get; set; }

        /// <summary>
        /// 0 - привез макулатуру, 1 - приехал за ПЭС
        /// </summary>
        public int Mode { get; set; }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        public void InitForm()
        {
            Form = new FormHelper();
            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="FilterAlpha",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=FilterAlpha,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);

            LastClick = DateTime.Now;
            AutoCloseFormInterval = 5 * 60;
            ListGridInit();
            ReturnInterface(true);
            FormCloseTimerRun();
            //SaveButton.IsEnabled = false;

            //фокус на первое поле
            Form.AfterSet = (Dictionary<string, string> v) =>
            {
            };
        }

        public void Show()
        {
            string title;
            if (TypeGrid == 0)
            {
                title = "Выбор поставщика";
                TabName = "SelectPostavshic";
            }
            else
            {
                title = "Выбор машины";
                TabName = "SelectCar";
            }

            Central.WM.AddTab(TabName, title, true, "add", this);
            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage obj)
        {
            //
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            /*   
            //отправляем сообщение о закрытии окна
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "DriverRegistration",
                    ReceiverName = "",
                    SenderName = "SelectPostavshic",
                    Action = "Closed",
                });
    */
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //останавливаем таймеры грида
            ListGrid.Destruct();

            FormCloseTimerStop();

            //устанавливаем активное окно
            if (TypeGrid == 0)
            {
                Central.WM.SetActive("EditPhone", true);
                
            }
            else
            {
                Central.WM.SetActive("SelectPostavshic", true);
            }
        }

        /// <summary>
        /// запус таймера автозакрытия формы
        /// </summary>
        private void FormCloseTimerRun()
        {

            if (AutoCloseFormInterval != 0)
            {
                if (AutoCloseFormTimer == null)
                {
                    AutoCloseFormTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, AutoCloseFormInterval)
                    };

                    AutoCloseFormTimer.Tick += (s, e) =>
                    {
                        ReturnInterface();
                    };
                }

                if (AutoCloseFormTimer.IsEnabled)
                {
                    AutoCloseFormTimer.Stop();
                }
                AutoCloseFormTimer.Start();
            }

        }

        //перезапуск таймера автозакрытия формы
        public void FormCloseTimerReset()
        {
            LastClick = DateTime.Now;
        }

        //останов таймера автозакрытия формы
        private void FormCloseTimerStop()
        {

            if (AutoCloseFormTimer != null)
            {
                if (AutoCloseFormTimer.IsEnabled)
                {
                    AutoCloseFormTimer.Stop();
                }
            }

        }

        /// <summary>
        /// при переключении интерфейса в другое сосотояение оно отображается
        /// определенное время, затем интерфейс возвращается в исходное состояние
        /// любой клик на элементе сбрабывает таймер возврата:
        /// </summary>
        private void ReturnInterface(bool force = false)
        {

            var today = DateTime.Now;
            var dt = ((TimeSpan)(today - LastClick)).TotalSeconds;

            if (
                dt > AutoCloseFormInterval
            //    || force
            )
            {
                Close();
            }
        }

        private void SetDefaults()
        {
            Form.SetDefaults();
        }


        private void KeyboardButton1_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("А");
        }

        private void KeyboardButton2_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Б");
        }

        private void KeyboardButton3_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("В");
        }

        private void KeyboardButton4_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Г");
        }

        private void KeyboardButton5_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Д");
        }

        private void KeyboardButton6_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Е");
        }

        private void KeyboardButton7_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Ж");
        }

        private void KeyboardButton8_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("З");
        }

        private void KeyboardButton9_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("И");
        }

        private void KeyboardButton10_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("К");
        }

        private void KeyboardButton11_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Л");
        }

        private void KeyboardButton12_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("М");
        }

        private void KeyboardButton13_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Н");
        }

        private void KeyboardButton14_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("О");
        }

        private void KeyboardButton15_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("П");
        }

        private void KeyboardButton16_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Р");
        }

        private void KeyboardButton17_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("С");
        }

        private void KeyboardButton18_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Т");
        }

        private void KeyboardButton19_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("У");
        }

        private void KeyboardButton20_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Ф");
        }

        private void KeyboardButton21_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Х");
        }

        private void KeyboardButton22_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Ц");
        }

        private void KeyboardButton23_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Ч");
        }

        private void KeyboardButton24_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Ш");
        }

        private void KeyboardButton25_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Щ");
        }

        private void KeyboardButton26_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Ы");
        }

        private void KeyboardButton27_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Ь");
        }

        private void KeyboardButton28_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Э");
        }

        private void KeyboardButton29_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Ю");
        }

        private void KeyboardButton30_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("Я");
        }

        private void KeyboardButton31_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender);
            ChangeValue("<");
        }

        private void ChangeValue(string symbol)
        {
            if (!string.IsNullOrEmpty(symbol))
            {
                var s = Form.GetValueByPath("FilterAlpha");

                switch (symbol)
                {
                    case "<":
                        if (!string.IsNullOrEmpty(s))
                        {
                            s = s.Substring(0, (s.Length - 1));
                        }
                        break;

                    default:
                        s = s + symbol;
                        break;
                }

                Form.SetValueByPath("FilterAlpha", s);
            }

            ListGrid.UpdateItems();
            FormCloseTimerReset();
        }

        /// <summary>
        /// инициализация списка поставщиков/машин
        /// </summary>
        public void ListGridInit()
        {

            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Название",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=700,
                        MaxWidth=1000,
                        MinWidth =100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Hidden = true,
                    },
                };
                ListGrid.SetColumns(columns);
            };

            // Запрет на изменение сортировки в таблице
            ListGrid.UseSorting = false;
            // автообновление нет
            ListGrid.AutoUpdateInterval = 60;
            ListGrid.UseRowHeader = false;

            ListGrid.SearchText = FilterAlpha;

            ListGrid.Init();
            // предотвращение двойного вызова OnSelectItem
            ListGrid.SelectItemMode = 0;

            //данные грида
            if (TypeGrid == 0)
            {
                ListGrid.OnLoadItems = ListPostavshicGridLoadItems;
                LabelSelectPostavshic.Content = "Выберите поставщика макулатуры";
            }
            else
            {
                ListGrid.OnLoadItems = ListAutomobilGridLoadItems;
                LabelSelectPostavshic.Content = "Выберите автомобиль";
            }

            ListGrid.Run();

            ScaleTransform scale = new ScaleTransform(2, 2);
            ListGrid.LayoutTransform = scale;

        }

        /// <summary>
        /// получение записей списка поставщиков
        /// </summary>
        private async void ListPostavshicGridLoadItems()
        {
            DisableControls();

            bool resume = true;

            if (resume)
            {

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "DriverRegistration");
                q.Request.SetParam("Action", "DriverRegistrationPostavshiclList");

                q.Request.Timeout = Central.Parameters.RequestTimeoutGrid;
                q.Request.Attempts = Central.Parameters.RequestAttemptsGrid;

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
                            ListGrid.UpdateItems(ds);
                        }

                    }
                }
            }

            EnableControls();
        }

        /// <summary>
        /// получение записей списка машин
        /// </summary>
        private async void ListAutomobilGridLoadItems()
        {
            DisableControls();

            bool resume = true;

            if (resume)
            {

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPm");
                q.Request.SetParam("Object", "DriverRegistration");
                q.Request.SetParam("Action", "DriverRegistrationAutomobilList");

                q.Request.Timeout = Central.Parameters.RequestTimeoutGrid;
                q.Request.Attempts = Central.Parameters.RequestAttemptsGrid;

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
                            ListGrid.UpdateItems(ds);
                        }

                    }
                }
            }

            EnableControls();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            ListGrid.ShowSplash();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            ListGrid.HideSplash();
        }

        /// <summary>
        /// нажали кнопку "Домой"
        /// </summary>
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender, Home);
            //Home();
        }

        /// <summary>
        /// нажали кнопку "Предыдующий"
        /// </summary>
        private void PriorButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender, Close);
        }

        /// <summary>
        /// нажали кнопку "Следующий"
        /// </summary>
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            Helper.ButtonClickAnimation(sender, Save);
        }

        private void Home()
        {
            ReceiverName = "DriverList";
            Central.WM.Close($"EditPhone");
            Central.WM.Close($"SettingsMode");

            Close();
        }

        private void Close()
        {
            if (TypeGrid == 0)
            {
                Central.WM.RemoveTab($"SelectPostavshic");
            }
            else
            {
                Central.WM.RemoveTab($"SelectCar");
         //       TypeGrid = 0;
            }

            Destroy();
        }

        private void Save()
        {
            //привез макулатуру         
            if (Mode == 0)
            {
                if (TypeGrid == 0)
                {
                    //следующее окно выбора машины    
                    IdPost = ListGrid.SelectedItem.CheckGet("ID").ToInt(); ;
                    PostavshicName = ListGrid.SelectedItem.CheckGet("NAME").ToString();

                    var p = new Dictionary<string, string>();
                    p.CheckAdd("IdSt", IdSt.ToString());
                    p.CheckAdd("ReceiverName", TabName.ToString());
                    p.CheckAdd("Mode", Mode.ToString());
                    p.CheckAdd("PhoneDriver", PhoneDriver.ToString());
                    p.CheckAdd("IdPost", IdPost.ToString());
                    p.CheckAdd("PostavshicName", PostavshicName.ToString());
                    p.CheckAdd("MarkaCar", MarkaCar.ToString());
                    p.CheckAdd("TypeGrid", "1");

                    var i = new SelectPostavshic(p);
                    i.Show();
                }
                else
                {
                    //следующее окно ввод номера машины        
                    MarkaCar = ListGrid.SelectedItem.CheckGet("NAME").ToString();

                    var p = new Dictionary<string, string>();
                    p.CheckAdd("IdSt", IdSt.ToString());
                    p.CheckAdd("ReceiverName", TabName.ToString());
                    p.CheckAdd("Mode", Mode.ToString());
                    p.CheckAdd("PhoneDriver", PhoneDriver.ToString());
                    p.CheckAdd("IdPost", IdPost.ToString());
                    p.CheckAdd("PostavshicName", PostavshicName.ToString());
                    p.CheckAdd("MarkaCar", MarkaCar.ToString());

                    var i = new EditNumCar(p);
                    i.Show();
                }
            }
            else if (Mode == 1)
            {
                // приехал за ПЭС
                //следующее окно ввод номера машины        
                MarkaCar = ListGrid.SelectedItem.CheckGet("NAME").ToString();

                var p = new Dictionary<string, string>();
                p.CheckAdd("IdSt", IdSt.ToString());
                p.CheckAdd("ReceiverName", TabName.ToString());
                p.CheckAdd("Mode", Mode.ToString());
                p.CheckAdd("PhoneDriver", PhoneDriver.ToString());
                p.CheckAdd("IdPost", IdPost.ToString());
                p.CheckAdd("PostavshicName", PostavshicName.ToString());
                p.CheckAdd("MarkaCar", MarkaCar.ToString());

                var i = new EditNumCar(p);
                i.Show();
            }
        }

    }
}
