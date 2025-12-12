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
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Управление отгрузками, форма "Установка даты отгрузки"    
    /// </summary>
    /// <author>balchugov_dv</author>   
    public partial class ShipmentDateTime:UserControl
    {
        /*
            интерфейс нужне для выбора даты и времени отгрузки
            на входе интерфейс получает:
                ShipmentType
                ShipmentId
                ReceiverName -- имя интерфейса, которому будет отправлено сообщение
            интерфейс отображает список с временем и датой отгрузки
            пользователь выбирает подходящее ему время и нажимает OK
            по готовности отправляется сообщение указанному получателю:
                ReceiverGroup   ="ShipmentControl",
                SenderName      ="ShipmentDateTime",
                ReceiverName    =ReceiverName,
                Action          ="Save",


         */

        public ShipmentDateTime()
        {
            InitializeComponent();
            
            ShipmentId=0;
            ShipmentType=0;
            SelectedShipmentTime="";
            SelectedShipmentDate="";
            ReceiverName="";
            MessageActionName = "Save";
            Shipment =new Dictionary<string, string>();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown+=ProcessKeyboard;

            InitForm();
            InitGrid();
            SetDefaults();
        }


        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            ShipmentDate.Text=DateTime.Now.ToString("dd.MM.yyyy");            
        }

        /// <summary>
        /// Форма ввода времени отгрузки
        /// </summary>
        public FormHelper Form { get;set;}

        public Dictionary<string,string> Shipment { get;set;}

        /// <summary>
        /// Деструктор, завершает все вспомогательные процессы 
        /// </summary>
        public void Destroy()
        {
            Messenger.Default.Unregister<ItemMessage>(this);
            Grid.Destruct();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }

        /// <summary>
        /// Обработчик клавиатуры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessKeyboard(object sender,System.Windows.Input.KeyEventArgs e)
        {
            Central.Dbg($"TestUserView.OnKeyDown KEY:{e.Key.ToString()}");
            switch (e.Key)
            {
                case Key.Escape:
                    Hide();
                    e.Handled=true;
                    break;
                      
                case Key.Enter:
                    if(Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        Save();
                        e.Handled=true;
                    }
                    break;

                case Key.F1:
                    Central.ShowHelp("/doc/l-pack-erp/production/production_tasks/list");
                    e.Handled=true;
                    break;
                
            }
        }

        /// <summary>
        /// Редактирование полей формы
        /// </summary>
        public void Edit()
        {
            GetData();
            Show();
        }

        /// <summary>
        /// Окно с формой редактирования времени отгрузки
        /// </summary>
        public Window Window { get;set;}

        /// <summary>
        /// Отображение окна с формой редактирования времени отгрузки
        /// </summary>
        public void Show()
        {
            string title=$"Дата и время отгрузки";
            
            int w=315;
            //int h=250;
            int h = 575;

            Window = new Window
            {
                Title = title,
                Width = w + 17,
                Height = h + 40,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode=ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,

            };

            Window.Content = new Frame
            {
                Content = this,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

                       
            if( Window != null )
            {
                Window.Topmost=true;
                Window.ShowDialog();
            }

            ShipmentDate.Focus();
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Hide()
        {
            var window=this.Window;
            if( window != null )
            {
                window.Close();
            }  
        }

        /// <summary>
        /// Набор данных для формы
        /// </summary>
        public ListDataSet ItemsDS { get; set; }

        /// <summary>
        /// Идентификатор отгрузки
        /// </summary>
        public int ShipmentId { get;set;}

        /// <summary>
        /// Тип отгрузки
        /// </summary>
        public int ShipmentType { get; set; }

        /// <summary>
        /// Выбранное время отгрузки
        /// </summary>
        public string SelectedShipmentTime { get; set; }

        /// <summary>
        /// Выбранная дата отгрузки
        /// </summary>
        public string SelectedShipmentDate { get; set; }

        /// <summary>
        /// Имя интерфейса, откуда вызывалось окно
        /// (при вызове окна оно будет установлено, при окончании работы
        /// окна, будет создано сообщение этому интерфейсу)
        /// </summary>
        public string ReceiverName { get; set; }
        /// <summary>
        /// Имя Action, которое будет указано в Messenger
        /// </summary>
        public string MessageActionName { get; set; }

        /// <summary>
        /// Инициализация таблицы с данными
        /// </summary>
        public void InitGrid()
        {
            //список колонок грида
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Дата",
                    Path="DT",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=65,
                    MaxWidth = 65,
                },
                new DataGridHelperColumn
                {
                    Header="Время",
                    Path="TIME",
                    ColumnType=ColumnTypeRef.String,
                    MinWidth=52,
                    MaxWidth = 52,
                },
                new DataGridHelperColumn
                {
                    Header="Количество отгрузок",
                    Path="COUNT",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=51,
                    MaxWidth=51,
                },
                new DataGridHelperColumn
                {
                    Header="Максимальное количество отгрузок",
                    Path="LIMIT",
                    ColumnType=ColumnTypeRef.Integer,
                    MinWidth=45,
                    MaxWidth=45,
                },
            };
            Grid.SetColumns(columns);
            Grid.UseRowHeader = false;

            Grid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            Grid.Init();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            Grid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem.Count > 0)
                    {
                        if(selectedItem.ContainsKey("TIME"))
                        {
                            if(!string.IsNullOrEmpty(selectedItem["TIME"]))
                            {
                                SelectedShipmentTime=selectedItem["TIME"];
                            }                              
                        }

                        if(selectedItem.ContainsKey("DT"))
                        {
                            if(!string.IsNullOrEmpty(selectedItem["DT"]))
                            {
                                SelectedShipmentDate=selectedItem["DT"];
                            }                              
                        }

                          
                    }
                };

            //данные грида
            Grid.OnLoadItems = LoadItems;   
            Grid.AutoUpdateInterval=0;
            //Grid.Run();
        }

        /// <summary>
        /// Инициализация формы с полем даты
        /// </summary>
        public void InitForm()
        {
            Form=new FormHelper();
            //список колонок формы
            var fields=new List<FormHelperField>()
            {
                new FormHelperField()
                { 
                    Path="SHIPMENTDATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ShipmentDate,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.Required, null },                    
                    },
                },
            };

            Form.SetFields(fields);
            Form.OnValidate=(bool valid, string message) =>
            {
                if(valid)
                {
                    FormStatus.Text="";
                }
                else
                {
                    if(!string.IsNullOrEmpty(message))
                    {
                        FormStatus.Text=message;
                    }
                    else
                    {
                        FormStatus.Text="Не все поля заполнены верно";
                    }                    
                }
            };
        }

        /// <summary>
        /// Получение данных для таблицы
        /// </summary>
        public void GetData(int shipmentId=0, int shipmentType=0)
        {
            if(shipmentId!=0)
            {
                ShipmentId=shipmentId;
            }
            if(shipmentType!=0)
            {
                ShipmentType=shipmentType;
            }
            LoadItems();
        }

        public async void LoadItems()
        {
            Grid.ShowSplash();
            ShipmentDateGroup.IsEnabled=false;
            bool resume = true;

            var shipmentDate=ShipmentDate.Text;
            if(resume)
            {
                if(string.IsNullOrEmpty(shipmentDate))
                {
                     resume = false;
                }
            }            

            if(resume)
            {
                if(ShipmentId==0)
                {
                     resume = false;
                }
            }   

            if(resume)
            {
                var p = new Dictionary<string, string>()
                {
                    { "SHIPMENTID",ShipmentId.ToString() },
                    { "SHIPMENTTYPE",ShipmentType.ToString() },
                    { "SHIPMENTDATE",shipmentDate.ToString() }
                };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Shipment");
                q.Request.SetParam("Action", "ListTimes");

                q.Request.SetParams(p);

                q.Request.Timeout = 30000;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                Mouse.OverrideCursor = Cursors.Wait;
                await Task.Run(() =>
                {
                    q.DoQuery();
                });
                Mouse.OverrideCursor = null;

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var itemsDS = ListDataSet.Create(result, "Items");
                        Grid.UpdateItems(itemsDS);
                    }
                }
            }
            else
            {
                Grid.ClearItems();
            }

            Grid.HideSplash();
            ShipmentDateGroup.IsEnabled=true;
        }

        

        /// <summary>
        /// Сохранение 
        /// </summary>
        public void Save()
        {
            if(Form.Validate())
            {
                var shipmentDate=SelectedShipmentDate;
                var shipmentTime=SelectedShipmentTime;

                var resume=true;

                if(resume)
                {
                    if(string.IsNullOrEmpty(shipmentDate))
                    {
                        resume=false;
                        Form.SetStatus(true, "Укажите дату отгрузки");
                    }
                }

                if(resume)
                {
                    if(string.IsNullOrEmpty(shipmentTime))
                    {
                        resume=false;
                        Form.SetStatus(true, "Выберите время отгрузки");
                    }
                }

                if (Grid != null && Grid.SelectedItem != null && Grid.SelectedItem.Count > 0)
                {
                    if (Grid.SelectedItem.CheckGet("COUNT").ToInt() == Grid.SelectedItem.CheckGet("LIMIT").ToInt())
                    {
                        resume = false;
                        Form.SetStatus(false, "Максимальное количество ");
                    }
                }

                if(resume)
                {
                    var p = new Dictionary<string, string>()
                    {
                        { "SHIPMENT_ID",ShipmentId.ToString() },
                        { "SHIPMENT_DATE",shipmentDate.ToString() },
                        { "SHIPMENT_TIME",shipmentTime.ToString() },
                    };

                    if(!string.IsNullOrEmpty(ReceiverName))
                    {
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup   ="ShipmentControl",
                            SenderName      ="ShipmentDateTime",
                            ReceiverName    =ReceiverName,
                            Action          =MessageActionName,
                            Message         ="",
                            ContextObject   =p,
                        });
                    }
                    Hide();
                }                
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку отмены
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Hide();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку сохранения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Save();
        }

        /// <summary>
        /// Обработчик изменения выбранной даты
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShipmentDate_TextChanged(object sender,TextChangedEventArgs e)
        {
            Grid.LoadItems();
        }
    }
}
