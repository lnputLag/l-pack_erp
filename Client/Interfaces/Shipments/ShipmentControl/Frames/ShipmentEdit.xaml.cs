using Client.Common;
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
    /// редактирование данных отгрузки
    /// водитель, примечание, причина опоздания
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>2</version>
    /// <released>2021-12-07</released>          
    public partial class ShipmentEdit:UserControl
    {
        public ShipmentEdit()
        {
            InitializeComponent();
            
            ReturnTabName="";
            TabName="shipmentedit";

            Id=0;            
            SelfShipmentId=1095;
            DriverListAllInterface=null;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this,ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown+=ProcessKeyboard;

            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// ID отгрузки (transport.idts)
        /// </summary>
        public int Id { get; set; }
        public int SelfShipmentId { get;set;}

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
                SenderName = "ShipmentEdit",
                Action = "Closed",
            });
            
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            if(ReturnTabName=="add")
            {
                Central.WM.SetLayer("add");
                ReturnTabName="";
            }
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
                    Control=ShipmentId,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
               
                new FormHelperField()
                {
                    Path="DATETIME",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Format="dd.MM.yyyy",
                    Control=Datetime,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="_SHIPMENT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ShipmentString,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="_DRIVER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DriverString,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COMMENTS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Comment,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LATENESS_REASON_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=LatenessReasonId,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LATENESS_REASON_COMMENT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=LatenessReasonComment,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SHIPMENT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ShipmentId,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DRIVER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=DriverId,
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
           
        }

        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage m)
        {
            //Group 
            if (m.ReceiverGroup.IndexOf("ShipmentControl") > -1)
            {
                if(m.SenderName=="DriverListAll")
                {
                    if(m.Action=="SelectItem")
                    {
                        var p=new Dictionary<string,string>();
                        if(m.ContextObject!=null)
                        {
                            p=(Dictionary<string,string>)m.ContextObject;
                        }
                        SelectDriverComplete(p);
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик клавиатуры
        /// </summary>
        private void ProcessKeyboard(object sender,System.Windows.Input.KeyEventArgs e)
        {
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
        public void Edit(int id)
        {
            Id=id;
            GetData();
        }
        
        /// <summary>
        /// Заполнение данными полей формы
        /// </summary>
        public async void GetData()
        {
            DisableControls();

            var p=new Dictionary<string,string>();
            {
                p.CheckAdd("ID",Id.ToString());
            }
            
            var q = new LPackClientQuery();
            q.Request.SetParam("Module","Shipments");
            q.Request.SetParam("Object","Shipment");
            q.Request.SetParam("Action","GetData");
            
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
               q.DoQuery();
            });

            if(q.Answer.Status == 0)                
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                if(result!=null)
                {
                    //причины опоздания
                    {
                        var ds=ListDataSet.Create(result,"REASONS");     
                        ds.ItemsPrepend(new Dictionary<string, string>(){
                            { "KEY", "0" },
                            { "VALUE", "" },
                        });
                        LatenessReasonId.SetItems(ds);
                    }
                    

                    {
                        var ds=ListDataSet.Create(result,"ITEMS");

                        {
                            //_SHIPMENT=SHIPMENT_ID BUYER СONSIGNEE ADDRESS
                            var v=ds.GetFirstItem();
                            if(v!=null)
                            {
                                {
                                    var s="";
                                    s=$"{v.CheckGet("SHIPMENT_ID")}";
                                    s=$"{s} {v.CheckGet("BUYER")}";
                                    s=$"{s} {v.CheckGet("СONSIGNEE")}";
                                    s=$"{s} {v.CheckGet("ADDRESS")}";
                                    v.CheckAdd("_SHIPMENT",s);
                                }

                                if(v.CheckGet("DRIVER_ID").ToInt()==SelfShipmentId)
                                {
                                    v.CheckAdd("_DRIVER","самовывоз");
                                }
                                else
                                {
                                    var s="";
                                    s=$"{v.CheckGet("DRIVER_ID")}";
                                    s=$"{s} {v.CheckGet("DRIVER_NAME")}";
                                    s=$"{s} {v.CheckGet("CAR_MARK")}";
                                    s=$"{s} {v.CheckGet("CAR_NUMBER")}";
                                    v.CheckAdd("_DRIVER",s);
                                }

                                Form.SetValues(v);
                            }
                        }
                    }

                    Show();                  
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

      

        /// <summary>
        /// Сохранение
        /// </summary>
        public void Save()
        {
            if(Form.Validate())
            {
                var v=Form.GetValues();
                SaveData(v);
            }
        }

        /// <summary>
        /// Сохранение значений в БД
        /// </summary>
        public async void SaveData(Dictionary<string, string> p)
        {
            Toolbar.IsEnabled=false;
            
            var q = new LPackClientQuery();
            q.Request.SetParam("Module","Shipments");
            q.Request.SetParam("Object","Shipment");
            q.Request.SetParam("Action","SaveData");

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
                        var id=ds.GetFirstItemValueByKey("SHIPMENT_ID").ToInt();
                        
                        if(id!=0)
                        {
                            //отправляем сообщение гриду о необходимости обновить данные
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup="ShipmentControl",
                                ReceiverName = "ShipmentList",
                                SenderName = "ShipmentEdit",
                                Action = "Refresh",
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

        
        public DriverListAll DriverListAllInterface { get;set;}

        /// <summary>
        /// выбора водителя 
        /// с помощью интерфейса "Все водители"
        /// </summary>
        public void SelectDriver()
        {
            DriverListAllInterface = new DriverListAll();
            //DriverListAllInterface.ReturnTabName=TabId;
            DriverListAllInterface.Init();            
            DriverListAllInterface.Show();
        }

        /// <summary>
        /// водитель выбран
        /// </summary>
        public async void SelectDriverComplete(Dictionary<string,string> item)
        {
            var resume=true;

            var driverId=0;
            if (resume)
            {
                driverId=item.CheckGet("ID").ToInt();
                if (driverId == 0)
                {
                    resume = false;
                }
            }

            if(resume)
            {
                var s="";
                s=$"{driverId}";
                s=$"{s} {item.CheckGet("DRIVERNAME")}";
                s=$"{s} {item.CheckGet("CARMARK")}";
                s=$"{s} {item.CheckGet("CARNUMBER")}";
                
                var v=new Dictionary<string,string>();
                v.CheckAdd("_DRIVER",s);
                v.CheckAdd("DRIVER_ID",driverId.ToString());
                Form.SetValues(v);

                DriverListAllInterface.Close();
                Central.WM.SetLayer(TabId);
            }

        }

        public void SetSelfShipment()
        {
            var v=new Dictionary<string,string>();
            v.CheckAdd("_DRIVER","самовывоз");
            v.CheckAdd("DRIVER_ID",SelfShipmentId.ToString());
            Form.SetValues(v);
        }


        /// <summary>
        /// разблокировка контролов 
        /// </summary>
        public void EnableControls()
        {
            SaveButton.IsEnabled=true;
        }

        /// <summary>
        /// блокировка контролов на время получения данных
        /// </summary>
        public void DisableControls()
        {
            SaveButton.IsEnabled=false;
        }


        /// <summary>
        /// Таб для возврата
        /// Если определен, фокус будет возвращен этому табу
        /// </summary>
        public string ReturnTabName { get; set; }

        /// <summary>
        /// Имя фрейма
        /// Техническое имя для идентификации в системе WM
        /// </summary>
        public string TabName { get; set; }

        /// <summary>
        /// идентификатор таба
        /// </summary>
        public string TabId { get; set; }

        /// <summary>
        /// Окно с формой редактирования привязки
        /// </summary>
        public Window Window { get; set; }

        /// <summary>
        /// Отображение окна с формой редактирования
        /// </summary>
        public void Show()
        {
            string title = $"Отгрузка {Id}";
            TabId=$"{TabName}_{Id}";
            Central.WM.AddTab(TabId,title,true,"add",this);
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
        /// Сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab(TabId);            

            if(Window!=null)
            {
                Window.Close();
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

        private void TransportDriver_MouseDoubleClick(object sender,MouseButtonEventArgs e)
        {

        }

        private void SelectDriver_Click(object sender,RoutedEventArgs e)
        {
            SelectDriver();
        }

        private void SelfShipmentButton_Click(object sender,RoutedEventArgs e)
        {
            SetSelfShipment();
        }
    }
}