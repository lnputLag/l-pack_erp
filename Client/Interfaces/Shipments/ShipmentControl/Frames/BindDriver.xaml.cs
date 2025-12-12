using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Привязка водителя к отгрузке    
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>2</version>
    /// <released>2021-12-07</released>          
    public partial class BindDriver:UserControl
    {
        public BindDriver()
        {
            InitializeComponent();

            Id=0;
            DriverLogId=0;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this,ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown+=ProcessKeyboard;

            InitForm();
        }

        /// <summary>
        /// ID отгрузки (transport.idts)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ID прибывшего водителя (driver_log_id)
        /// </summary>
        public int DriverLogId { get; set; }

        /// <summary>
        /// ID водителя (driver.id_d)
        /// </summary>
        public int DriverId { get; set; }

        /// <summary>
        /// Набор данных по отгрузкам
        /// </summary>
        public ListDataSet ShipmentsDS { get; set; }

        /// <summary>
        /// Набор данных по водителям
        /// </summary>
        public ListDataSet DriversDS { get; set; }

        /// <summary>
        /// Форма редактирования
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Деструктор. Завершает все вспомогательные процессы и отправляет сигнал о закрытии окна
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="ShipmentControl",
                ReceiverName = "",
                SenderName = "BindDriver",
                Action = "Closed",
            });
            
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        public void InitForm()
        {
            Form=new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="SHIPMENT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Shipment,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="DRIVER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Driver,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="SHIPMENT_DATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Date,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SHIPMENT_TIME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Time,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SET_DATETIME",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=SetDatetime,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

            };

            Form.SetFields(fields);
            Form.OnValidate=(bool valid,string message) =>
            {
                if(valid)
                {
                    //SaveButton.IsEnabled=true;
                    FormStatus.Text="";
                }
                else
                {
                    //SaveButton.IsEnabled=false;
                    FormStatus.Text="Не все поля заполнены верно";
                }
            };

            // Таблица в выпадающем списке отгрузок
            {
                var columns = new List<DataGridHelperColumn>()
                {
                    new DataGridHelperColumn()
                    {
                        Header="#",
                        Path="ID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=50,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Покупатель",
                        Path="BUYER",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=120,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Грузополучатель",
                        Path="СONSIGNEE",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=120,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Адрес доставки",
                        Path="SHIPMENTADDRESS",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=200,
                    },
                };
                Shipment.GridColumns=columns;
            }

            // Таблица в выпадающем списке водителей
            {
                var columns = new List<DataGridHelperColumn>()
                {
                    new DataGridHelperColumn()
                    {
                        Header="#",
                        Path="ID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=50,
                        Doc="Ид прибывшего водителя",
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Водитель",
                        Path="DRIVERNAME",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=120,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Авто",
                        Path="CARMARK",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=120,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Приезд",
                        Path="ARRIVEDATE",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width=120,
                    },
                };
                Driver.GridColumns=columns;
            }
        }


        /// <summary>
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage m)
        {
            //Group 
            if (m.ReceiverGroup.IndexOf("ShipmentControl") > -1)
            {
                if(m.ReceiverName=="BindDriver")
                {
                    switch (m.Action)
                    {
                        case "Save":
                        {
                            var p=new Dictionary<string,string>();
                            if(m.ContextObject!=null)
                            {
                                p=(Dictionary<string,string>)m.ContextObject;
                            }
                            FormSetDatetime(p);
                        }
                        break;
                    }
                }
                
            }
        }

        /// <summary>
        /// Обработчик клавиатуры
        /// </summary>
        private void ProcessKeyboard(object sender,System.Windows.Input.KeyEventArgs e)
        {
            Central.Dbg($"TestUserView.OnKeyDown KEY:{e.Key.ToString()}");
            switch(e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled=true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled=true;
                    break;
            }
        }

        /// <summary>
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/shipments/control/common/bind_driver");
        }
        
        /// <summary>
        /// Редактирование формы
        /// </summary>
        public void Edit()
        {
            GetData();
        }
        
        /// <summary>
        /// Заполнение данными полей формы
        /// </summary>
        public void GetData()
        {
            if(ShipmentsDS != null)
            {
                var shipments = new List<Dictionary<string, string>>();

                if (ShipmentsDS.Items.Count > 0)
                {
                    foreach (Dictionary<string, string> row in ShipmentsDS.Items)
                    {
                        if (string.IsNullOrEmpty(row.CheckGet("DLDTTMENTRY")))
                        {
                            shipments.Add(row);
                        }
                    }
                }

                if (shipments.Count > 0)
                {
                    ShipmentsDS.Items = shipments;
                }

                Shipment.GridDataSet = ShipmentsDS;
                Shipment.SetValue(Id.ToString());
            }

            if(DriversDS!=null)
            {
                var drivers = new List<Dictionary<string,string>>();
                if(DriversDS.Items.Count>0)
                {
                    foreach(Dictionary<string,string> row in DriversDS.Items)
                    {
                        if(row.ContainsKey("ENTRYDATE"))
                        {
                            if(string.IsNullOrEmpty(row["ENTRYDATE"]))
                            {
                                drivers.Add(row);
                            }
                        }
                    }
                }

                if(drivers.Count>0)
                {
                    DriversDS.Items=drivers;
                }

                Driver.GridDataSet=DriversDS;
                Driver.SetValue(DriverLogId.ToString());
            }

            Show();
        }

        /// <summary>
        /// установка даты и времени в поля формы
        /// </summary>
        /// <param name="p"></param>
        public void FormSetDatetime(Dictionary<string,string> p)
        {
            var v=Form.GetValues();
            if(v.CheckGet("SHIPMENT_ID").ToInt()==p.CheckGet("SHIPMENT_ID").ToInt())
            {
                v.CheckAdd("SHIPMENT_DATE",p.CheckGet("SHIPMENT_DATE"));
                v.CheckAdd("SHIPMENT_TIME",p.CheckGet("SHIPMENT_TIME"));
                v.CheckAdd("SET_DATETIME","1");
                Form.SetValues(v);
                Save();
            }            
        }

        /// <summary>
        /// Сохранение
        /// </summary>
        public void Save()
        {
            if(Form.Validate())
            {
                var v=Form.GetValues();
                
                var resume = true;
                var checkDateTime = false;
                var SetDateTime = false;
                int ShipmentType = 0;
                int ShipmentId = 0;

                if (resume)
                {
                    
                    /*
                    для опоздавших или перенесенных отгрузок
                    дадим промежуточный этап: выбор времени и даты отгрузки
                    */

                    ShipmentId=v.CheckGet("SHIPMENT_ID").ToInt();
                    if(ShipmentId!=0)
                    {
                        foreach(Dictionary<string,string> item in ShipmentsDS.Items)
                        {
                            if(item.CheckGet("ID").ToInt()==ShipmentId)
                            {
                                if(
                                    item.CheckGet("UNSHIPPED").ToInt()==1
                                    || item.CheckGet("LATE").ToInt()==1
                                )
                                {
                                    ShipmentType=item.CheckGet("SHIPMENTTYPE").ToInt();

                                    // Если отгрузка не в рулонах
                                    if (item.CheckGet("SHIPMENTTYPE").ToInt() != 2)
                                    {
                                        checkDateTime = true;
                                    }
                                }
                            }
                        }
                    }                   
                }

                if(resume)
                {
                    if(checkDateTime)
                    {
                        if(
                            string.IsNullOrEmpty(v.CheckGet("SHIPMENT_DATE"))
                            || string.IsNullOrEmpty(v.CheckGet("SHIPMENT_TIME"))
                        )
                        {
                             /*
                                уточняем дату и время отгрузки
                                придет сообщение
                                    ReceiverGroup   ="ShipmentControl",
                                    SenderName      ="ShipmentDateTime",
                                    ReceiverName    ="BindDriverView",
                                    Action          ="Save",

                                SHIPMENT_ID
                                SHIPMENT_DATE
                                SHIPMENT_TIME
                             */

                            var i = new ShipmentDateTime();
                            i.ShipmentType=ShipmentType;
                            i.ShipmentId=ShipmentId;
                            i.ReceiverName="BindDriver";
                            i.Edit();
                            resume=false; 
                        }
                    }
                }

                if(resume)
                {
                    /*
                        SHIPMENT_ID
                        DRIVER_ID
                        SHIPMENT_DATE
                        SHIPMENT_TIME
                        SET_DATETIME
                     */
                    SaveData(v);
                }

            }
        }

        /// <summary>
        /// отправка запроса
        /// </summary>
        public async void SaveData(Dictionary<string, string> p)
        {
            Toolbar.IsEnabled=false;
            
            var q = new LPackClientQuery();
            q.Request.SetParam("Module","Shipments");
            q.Request.SetParam("Object","Shipment");
            q.Request.SetParam("Action","BindDriver");

            q.Request.SetParams(p);
            
            await Task.Run(() =>
            {
                q.DoQuery();
            });
            
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if(result != null)
                {
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var id=ds.GetFirstItemValueByKey("ID").ToInt();
                        
                        if(id!=0)
                        {
                            //отправляем сообщение гриду о необходимости обновить данные
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup="ShipmentControl",
                                ReceiverName = "DriverList,ShipmentList",
                                SenderName = "BindDriver",
                                Action = "Refresh",
                            });

                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverGroup = "ShipmentKshControl",
                                ReceiverName = "ShipmentKshList",
                                SenderName = "BindDriver",
                                Action = "refresh",
                            });

                            //закрываем фрейм
                            Close();
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();                
            }

            Toolbar.IsEnabled=true;
        }

        /// <summary>
        /// Окно с формой редактирования привязки
        /// </summary>
        public Window Window { get; set; }

        /// <summary>
        /// Отображение окна с формой редактирования
        /// </summary>
        public void Show()
        {
            string title = $"Привязка водителя к отгрузке";

            int w=(int)Width;
            int h=(int)Height;

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


            if(Window != null)
            {
                Window.Topmost=true;
                Window.ShowDialog();
            }

            Window.Closed+=Window_Closed;
        }

        /// <summary>
        /// Дополнительный обработчик закрытия окна
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender,System.EventArgs e)
        {
            Destroy();
        }

        /// <summary>
        /// Закрывает окно
        /// </summary>
        public void Close()
        {
            var window = this.Window;
            if(window != null)
            {
                window.Close();
            }
            Destroy();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку отмены
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void HelpButton_OnClick(object sender, RoutedEventArgs e)
        {
            ShowHelp();
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
    }
}