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
using System.ComponentModel;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// список зарегистрированных водителей
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    public partial class DriverList : WizardFrame
    {
        public DriverList()
        {
            InitializeComponent();
            
            if(Central.InDesignMode()){
                return;
            }

            LastItemId = 0;
            
            InitForm();
            InitGrid();
            SetDefaults();
            Central.Msg.Register(ProcessMessage);

            DateTimeout=new Timeout(
                10,
                ()=>{
                    var today=DateTime.Now.ToString("dd.MM HH:mm");
                    Date.Text=$"{today}";
                },
                true,
                true
            );
            DateTimeout.Run();

            if(Central.DebugMode)
            {
                DebugMenu.Visibility=Visibility.Visible;
            }else{
                DebugMenu.Visibility=Visibility.Collapsed;
            }
        }

        private Timeout DateTimeout {get;set;}
        /// <summary>
        /// последняя зарегистрированная машина
        /// </summary>
        private int LastItemId { get; set; }

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
                    Path="MACHINE_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },          
                    First=true,
                },
                new FormHelperField()
                {
                    Path="MACHINE_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },          
                    First=true,
                },
                new FormHelperField()
                {
                    Path="CARGO_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    First=true,
                },
                new FormHelperField()
                {
                    Path="CARGO_TYPE_DESCRIPTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    First=true,
                },
            };

            Form.SetFields(fields);
        }

        public void InitGrid()
        {
             //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Машина",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width=200,
                        MinWidth=120,
                        MaxWidth=300,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Телефон",
                        Path="PHONE_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width=170,
                        MinWidth=120,
                        MaxWidth=250,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Время",
                        Path="TM",
                        ColumnType=ColumnTypeRef.String,
                        Width=80,
                        MinWidth=60,
                        MaxWidth=90,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="STATUS",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=300,
                        MaxWidth=550,
                    },
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="ID_SCRAP",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=60,
                        MaxWidth=60,
                        Hidden = true,
                    },
                    
                };
                Grid.SetColumns(columns);
            };

            Grid.PrimaryKey = "ID_SCRAP";
            Grid.UseSorting = false;
            Grid.AutoUpdateInterval = 60;
            Grid.UseRowHeader = false;
            Grid.SelectItemMode = 0;
            Grid.SetMode(1);
            Grid.Init();


            Grid.DisableControls=()=>
            {
                GridToolbar.IsEnabled = false;
                Grid.ShowSplash();
            };
                
            Grid.EnableControls=()=>
            {
                GridToolbar.IsEnabled = true;
                Grid.HideSplash();
            };

            Grid.OnLoadItems = async ()=>
            {
                Grid.DisableControls();

                var today=DateTime.Now;
                bool resume = true;

                if (resume)
                {
                    var p = new Dictionary<string, string>();
                    {
                        p.CheckAdd("ID_ST", Form.GetValueByPath("MACHINE_ID").ToString());
                    }

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "PaperProduction");
                    q.Request.SetParam("Object", "TransportDriver");
                    q.Request.SetParam("Action", "ListRegistered");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if(q.Answer.Status == 0)                
                    {
                        Grid.UpdateItemsAnswer(q.Answer,"ITEMS");
                    }
                    else
                    {
                       // q.ProcessError();
                    }
                }

                Grid.EnableControls();
            };

            Grid.OnSelectItem = (row) =>
            {
                if(row!=null)
                {
                }
            };

            //Grid.Run();

            {
                ScaleTransform scale = new ScaleTransform(1.2, 1.2);
                Grid.LayoutTransform = scale;
            }
            
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            //установка значений по умолчанию
            Form.SetDefaults();
            NextButtonSet(true);
        }

        /// <summary>
        /// обработка сообщений
        /// </summary>
        /// <param name="message"></param>
        public void ProcessMessage(ItemMessage message)
        {
            if(message!=null)
            {
                if(message.ReceiverName==ControlName)
                {
                    switch (message.Action)
                    {
                        //фрейм загружен 
                        case "Showed":
                            Grid.UpdateItems();
                            SelectLast();
                            DriverRegistrationInterface.ResetValues(Wizard);
                            SetDefaults();
                            LoadValues();
                            SetTitle();
                            Grid.Run();
                            NextButtonSet(true);
                            break;

                        //ввод с экранной клавиатуры
                        case "KeyPressed":
                            ChangeValue(message.Message);                        
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// ввод в поле из виртуальной клавиатуры
        /// </summary>
        /// <param name="symbol"></param>
        private void ChangeValue(string symbol)
        {
            if (IsActive() && !string.IsNullOrEmpty(symbol))
            {
                var s = Form.GetValueByPath("_SEARCH_CAR_MODEL");
                switch (symbol)
                {
                    case "BACK_SPACE":
                        if (s.Length > 0)
                        {
                            s = s.Substring(0, (s.Length - 1));
                        }
                        break;

                    default:
                        if (s.Length < 10)
                        {
                            s = s + symbol;
                        }
                        break;
                }
                Form.SetValueByPath("_SEARCH_CAR_MODEL", s);
                Grid.SearchItems(s);
            }
            
            //Validate();
        }

        /// <summary>
        /// проверка, можно ли активировать кнопку "далее"
        /// </summary>
        private void Validate()
        {
             NextButtonSet(true);
        }        

        /// <summary>
        /// активация/деактивация кнопки "далее"
        /// </summary>
        /// <param name="mode"></param>
        private void NextButtonSet(bool mode=true)
        {
            if(NextButton!=null)
            {
                if(mode)
                {
                    NextButton.IsEnabled=true;
                    NextButton.Opacity=1.0;
                    NextButton.Style=(Style)NextButton.TryFindResource("TouchFormButtonPrimaryBig");
                }
                else
                {
                    NextButton.IsEnabled=false;
                    NextButton.Opacity=0.5;
                    NextButton.Style=(Style)NextButton.TryFindResource("TouchFormButtonBig");
                }
            }
        }

        /// <summary>
        /// установка тайла окна
        /// </summary>
        private void SetTitle()
        {
            var machineNumber=Form.GetValueByPath("MACHINE_NUMBER").ToInt();
            Title.Text=$"БДМ-{machineNumber}";
            if (machineNumber == 1)
            {
                BookingButton.Visibility = Visibility.Collapsed;
            }
        }

        private void SelectLast()
        {
            var v = Wizard.Values;
            LastItemId = v.CheckGet("ITEM_ID").ToInt();
            if (LastItemId!=0)
            {    
                Grid.SelectRowByKey(LastItemId,"ID_SCRAP");
                Grid.SelectedItem=new Dictionary<string, string>()
                {
                    {"ID_SCRAP",LastItemId.ToString()},
                };
            }

        }
        
        private void ShowInfo()
        {
            var t="Отладочная информация";
            var m=Central.MakeInfoString();
            var i=new ErrorTouch();
            i.Show(t,m);
        }

        private void TestShowError()
        {
            {
                var t="Не удалось отправить СМС";
                var m="Повторите отправку позже";
                
                var i=new ErrorTouch();
                i.Show(t,m);
            }
        }

        /// <summary>
        /// отображение статьи в справочной системе
        /// </summary>
        public void ShowHelp()
        {            
            //Central.ShowHelp($"/doc/l-pack-erp/production/roll_control");
            //var h=new RollControlHelp();
            //h.Init();
        }


        /// <summary>
        /// нажали кнопку "Домой"
        /// </summary>
        private void HomeButtonClick(object sender, RoutedEventArgs e)
        {
            Wizard.Navigate(0);
        }

        /// <summary>
        /// нажали кнопку "Предыдущий"
        /// </summary>
        private void PriorButtonClick(object sender, RoutedEventArgs e)
        {
            Wizard.Navigate(-1);
        }

        /// <summary>
        /// нажали кнопку "Следующий"
        /// </summary>
        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            Wizard.Navigate(1);
        }

        private void SettingsMenu_Click(object sender, RoutedEventArgs e)
        {
            var itemId=Grid.SelectedItem.CheckGet("ID_SCRAP").ToInt();
            DriverRegistrationInterface.ProcessLabel(1,itemId);
        }

        private void InfoMenu_Click(object sender, RoutedEventArgs e)
        {
            ShowInfo();
        }

        private void RestartMenu_Click(object sender, RoutedEventArgs e)
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

        private void ExitMenu_Click(object sender, RoutedEventArgs e)
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

        private void F11Menu_Click(object sender, RoutedEventArgs e)
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

        private void PrintDialogMenu_Click(object sender, RoutedEventArgs e)
        {
            var itemId=Grid.SelectedItem.CheckGet("ID_SCRAP").ToInt();
            DriverRegistrationInterface.ProcessLabel(1,itemId);
        }

        private void PrintNowMenu_Click(object sender, RoutedEventArgs e)
        {
            var itemId=Grid.SelectedItem.CheckGet("ID_SCRAP").ToInt();
            DriverRegistrationInterface.ProcessLabel(2,itemId);
        }

        private void ErrorMenu_Click(object sender, RoutedEventArgs e)
        {
            TestShowError();
        }

        private void RenderMenu_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Resize1Menu_Click(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "All",
                SenderName = "Navigator",
                Action = "Resize",
                Message = "800x600",
            });
        }

        private void Resize2Menu_Click(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "All",
                SenderName = "Navigator",
                Action = "Resize",
                Message = "1280x800",
            });
        }

        private void BurgerMenu_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BurgerMenuButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen=true;
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        /// <summary>
        ///  нажали кнопку "Я знаю код брони"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BookingButton_Click(object sender, RoutedEventArgs e)
        {
            Form.SetValueByPath("CARGO_TYPE", "6");
            Form.SetValueByPath("CARGO_TYPE_DESCRIPTION", "Я знаю код брони");
            SaveValues();
            Wizard.Navigate("BookingCode");
        }
    }
}
