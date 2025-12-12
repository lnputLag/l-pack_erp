using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Управление отгрузками, форма "Карточка водителя"    
    /// </summary>
    /// <author>balchugov_dv</author>   
    public partial class Driver:UserControl
    {
        public Driver()
        {
            InitializeComponent();
            
            Id=0;
            DriverLogId=0;
            ReturnTabName="";

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown += ProcessKeyboard;

            InitForm();
        }

        /// <summary>
        /// Идентификатор водителя.
        /// driver.id_d
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Идентификатор прибывшего водителя.
        /// driver_lod.id_dl
        /// </summary>
        public int DriverLogId { get; set; }

        /// <summary>
        /// Форма редактирования водителя
        /// </summary>
        public FormHelper DriverForm { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ReturnTabName { get; set; }

        public delegate void SaveDelegate(Dictionary<string, string> driverData);

        public SaveDelegate OnSave;

        /// <summary>
        /// Деструктор. Завершает вспомогательные процессы
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="ShipmentControl",
                ReceiverName = "",
                SenderName = "DriverView",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            GoBack();
        }

        /// <summary>
        /// Возврат на фрейм, откуда был вызван данный фрейм
        /// </summary>
        public void GoBack()
        {
            if (!string.IsNullOrEmpty(ReturnTabName))
            {
                Central.WM.SetActive(ReturnTabName, true);
                ReturnTabName = "";
            }
        }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        public void InitForm()
        {
            DriverForm=new FormHelper();
            //список колонок формы
            var fields=new List<FormHelperField>()
            {                
                new FormHelperField()
                { 
                    Path="DRIVERNAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DriverName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.Required, null },                    
                    },
                },
                new FormHelperField()
                { 
                    Path="CARNUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CarNumber,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.ToUpperCase, null },
                        { FormHelperField.FieldFilterRef.CarNumber, null },
                    },
                },
                new FormHelperField()
                { 
                    Path="TRAILERNUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TrailerNumber,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.ToUpperCase, null },
                        { FormHelperField.FieldFilterRef.CarNumber, null },
                    },
                },                
                new FormHelperField()
                { 
                    Path="CARMARK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CarMark,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        //{ FormHelperField.FieldFilterRef.Required, null },                    
                    },
                },                
                new FormHelperField()
                { 
                    Path="DRIVERPHONE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DriverPhone,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                { 
                    Path="PASSPORTSERIES",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PassportSeries,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },                    
                    },
                },
                new FormHelperField()
                { 
                    Path="PASSPORTNUMBER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PassportNumber,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.DigitOnly, 6 },     
                        { FormHelperField.FieldFilterRef.MaxLen, 6 },                    
                    },
                },
                new FormHelperField()
                { 
                    Path="PASSPORTISSUEDATE",
                    FieldType=FormHelperField.FieldTypeRef.String,                    
                    Control=PassportIssueDate,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                { 
                    Path="PASSPORTISSUER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PassportIssuer,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 250 },                    
                    },
                },
                new FormHelperField()
                { 
                    Path="CARLENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CarLength,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxValue, 25000 },
                    },
                },
                new FormHelperField()
                { 
                    Path="CARWIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CarWidth,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.Required, null },                    
                        { FormHelperField.FieldFilterRef.MaxValue, 6000 },
                    },
                },
                new FormHelperField()
                { 
                    Path="CARHEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CarHeight,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.Required, null },          
                        { FormHelperField.FieldFilterRef.MaxValue, 6000 },
                    },
                },
                new FormHelperField()
                { 
                    Path="TRAILER_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=Trailer,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                    OnChange=(FormHelperField f, string v)=>{ 
                        var val=v.ToBool();
                        if(val)
                        {
                            TrailerLength.IsEnabled=true;
                            TrailerWidth.IsEnabled=true;
                            TrailerHeight.IsEnabled=true;
                        }
                        else
                        {
                            TrailerLength.IsEnabled=false;
                            TrailerWidth.IsEnabled=false;
                            TrailerHeight.IsEnabled=false;
                            
                            TrailerLength.Text="";
                            TrailerWidth.Text="";
                            TrailerHeight.Text="";
                        }

                    },
                },
                new FormHelperField()
                { 
                    Path="TRAILERLENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TrailerLength,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.MaxValue, 25000 },
                    },
                },
                new FormHelperField()
                { 
                    Path="TRAILERWIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TrailerWidth,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.MaxValue, 6000 },
                    },
                },
                new FormHelperField()
                { 
                    Path="TRAILERHEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TrailerHeight,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.MaxValue, 6000 },
                    },
                },
                new FormHelperField()
                { 
                    Path="CUSTOMER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Customer,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                {
                    Path="DRIVERNOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DriverNote,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },


            };

            DriverForm.SetFields(fields);
            DriverForm.OnValidate=(bool valid, string message) =>
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


            DriverForm.SetDefaults();
        }


        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m">Сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {
        }

        /// <summary>
        /// Обработчик нажатий клавиатуры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessKeyboard(object sender,System.Windows.Input.KeyEventArgs e)
        {
            Central.Dbg($"TestUserView.OnKeyDown KEY:{e.Key.ToString()}");
            switch (e.Key)
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
        /// Создание пустой формы для добавления водителя
        /// </summary>
        public void Create()
        {
            DriverForm.SetDefaults();
            GetData(0);
        }

        /// <summary>
        /// Создание формы для редактирования водителя
        /// </summary>
        /// <param name="id"></param>
        public void Edit( int id )
        {
            GetData(id);
        }

        /// <summary>
        /// Проверка и подготовка данных водителя для записи в БД
        /// </summary>
        public Dictionary<string, string> PrepareData()
        {
            var p = new Dictionary<string, string>();
            if (DriverForm.Validate())
            {
                p = DriverForm.GetValues();

                p["CARMARK"]=CarMark.SelectedItem.Key;
                p.Add("ID",Id.ToString());
                p.Add("DRIVERLOGID",DriverLogId.ToString());

                var c=(bool)UsePassport.IsChecked;

                if(c)
                {
                    
                }
                else
                {
                    p.CheckAdd("PASSPORTSERIES","");
                    p.CheckAdd("PASSPORTNUMBER","");
                    p.CheckAdd("PASSPORTISSUEDATE","");
                    p.CheckAdd("PASSPORTISSUER","");
                }
            }

            return p;
        }

        public void ProcessPassport(bool c)
        {
            if(c)
            {
                PassportData.IsEnabled=true;
                PassportData.Opacity=1.0;
            }
            else
            {
                PassportData.IsEnabled=false;
                PassportData.Opacity=0.7;
            }
        }

        public void CheckPassport()
        {
            var v = DriverForm.GetValues();
            var c=(bool)UsePassport.IsChecked;
            ProcessPassport(c);

            InitForm();
            if(!c)
            {
                DriverForm.RemoveFilter("PASSPORTSERIES", FormHelperField.FieldFilterRef.Required);
                DriverForm.RemoveFilter("PASSPORTNUMBER", FormHelperField.FieldFilterRef.Required);
                DriverForm.RemoveFilter("PASSPORTISSUEDATE", FormHelperField.FieldFilterRef.Required);
                DriverForm.RemoveFilter("PASSPORTISSUER", FormHelperField.FieldFilterRef.Required);
            }

            DriverForm.SetValues(v);
        }
    
        /// <summary>
        /// Поучение данных водителя
        /// </summary>
        /// <param name="id"></param>
        public async void GetData(int id)
        {
            Id=id;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module","Shipments");
            q.Request.SetParam("Object","TransportDriver");
            q.Request.SetParam("Action","Get");
            
            q.Request.SetParam("Id",Id.ToString());
            q.Request.SetParam("DriverId",DriverLogId.ToString());

            await Task.Run(() =>
            {
               q.DoQuery();
            });

            if(q.Answer.Status == 0)                
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                if(result!=null)
                {
                    // Данные для выпадающего списка марок автомобилей
                    if (result.ContainsKey("CarMarks"))
                    {
                        var CarMarksDS = result["CarMarks"];
                        CarMarksDS?.Init(); 
                        CarMark.Items = CarMarksDS.GetItemsList("ID", "NAME");
                    }

                    var carMarkId="-1";
                    if(result.ContainsKey("Drivers"))
                    {
                        var DriverDS = result["Drivers"];
                        DriverDS?.Init();
                        if(Id!=0)
                        {
                            var first=DriverDS.Items.First();
                            if(first!=null)
                            {
                                DriverForm.SetValues(first);

                                //паспорт: все или ничего
                                if(
                                    !first.CheckGet("PASSPORTSERIES").ToString().IsNullOrEmpty()
                                    && !first.CheckGet("PASSPORTNUMBER").ToString().IsNullOrEmpty()
                                    && !first.CheckGet("PASSPORTISSUEDATE").ToString().IsNullOrEmpty()
                                    && !first.CheckGet("PASSPORTISSUER").ToString().IsNullOrEmpty()
                                )
                                {
                                    UsePassport.IsChecked=true;
                                }
                                else
                                {
                                    UsePassport.IsChecked=false;
                                }
                                CheckPassport();
                                

                                Id=DriverDS.GetFirstItemValueByKey("ID").ToInt();
                                carMarkId=DriverDS.GetFirstItemValueByKey("CARMARK");

                                if( !string.IsNullOrEmpty(DriverDS.GetFirstItemValueByKey("PASSPORTISSUEDATE"))  )
                                {
                                    var d=DriverDS.GetFirstItemValueByKey("PASSPORTISSUEDATE");
                                    DriverForm.SetValueByPath("PASSPORTISSUEDATE", d);
                                }

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
            
        }

        /// <summary>
        /// Сохранение данных водителя
        /// </summary>
        /// <param name="p"></param>
        public async void Save()
        {
            var p = PrepareData();
            Toolbar.IsEnabled = false;

            if (p.Count > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "TransportDriver");
                q.Request.SetParam("Action", "Save");
                q.Request.SetParams(p);

                await Task.Run(() => {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var driverId=ds.GetFirstItemValueByKey("ID").ToInt();

                        var v=new Dictionary<string,string>();
                        { 
                            var first=ds.GetFirstItem();
                            if(first!=null)
                            {
                                /*
                                    { "ID", driverId.ToString() },
                                    { "DRIVERLOGID", driverlogId.ToString() },
                                 */
                                v=first;
                            }
                        }

                        if(driverId!=0)
                        {
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "ShipmentControl",
                                ReceiverName = "DriverList,DriverListAll",
                                SenderName = "Driver",
                                Action = "Refresh",
                                Message = driverId.ToString(),
                                ContextObject=v,
                            });

                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "ShipmentControl",
                                ReceiverName = "DriverListExpected",
                                SenderName = "Driver",
                                Action = "Refresh",
                                Message = "",
                                ContextObject = null,
                            });

                            //отправляем сообщение гриду о необходимости обновить данные
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverGroup = "MoldedContainerShipment",
                                ReceiverName = "MoldedContainerShipmentList",
                                SenderName = "Driver",
                                Action = "refresh",
                            });

                            //отправляем сообщение гриду о необходимости обновить данные
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverGroup = "ShipmentKsh",
                                ReceiverName = "ShipmentKshList",
                                SenderName = "Driver",
                                Action = "refresh",
                            });

                            OnSave?.Invoke(v);

                            Close();
                        }
                    }

                }
                else
                {
                    q.ProcessError();
                }
            }

            Toolbar.IsEnabled=true;
        }

        /// <summary>
        /// Отображение вкладки с формой редактирования данных водителя
        /// </summary>
        public void Show()
        {
            string title=$"Водитель {Id}";
            if(Id == 0)
            {
                title="Новый водитель";
            }
            Central.WM.AddTab($"driverview_{Id}",title,true,"add",this);

        }

        /// <summary>
        /// Закрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab($"driverview_{Id}");
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

        /// <summary>
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/shipments/control/listing#block3");
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
        /// Обработчик нажатия на кнопку стандартных габаритов автомобиля
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StandartCarButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            CarLength.Text="13600";
            CarWidth.Text="2450";
            CarHeight.Text="2400";
        }

        private void UsePassport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CheckPassport();
        }

        /// <summary>
        /// Обработка ввода номера телефона.
        /// Автоматом меняем 8 на 7, если номер начинается с 9, добавляем код 7 перед 9
        /// Если номер начинается с 3, считаем его белорусским и разрешаем вводить 12 цифр
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DriverPhone_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(DriverPhone.Text.Length == 1)
            {
                if (DriverPhone.Text == "8")
                {
                    DriverPhone.Text = "7";
                    DriverPhone.SelectionStart = 1;
                    DriverPhone.MaxLength = 11;
                }
                else if (DriverPhone.Text == "9")
                {
                    DriverPhone.Text = "79";
                    DriverPhone.SelectionStart = 2;
                    DriverPhone.MaxLength = 11;
                }
                else if (DriverPhone.Text == "3")
                {
                    //Белорусский номер
                    DriverPhone.MaxLength = 12;
                }
            }
        }
    }
}
