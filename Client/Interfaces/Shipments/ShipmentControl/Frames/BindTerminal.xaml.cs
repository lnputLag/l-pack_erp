using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Привязка отгрузки к терминалу    
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>2</version>
    /// <released>2021-12-07</released>          
    public partial class BindTerminal:UserControl
    {
        public BindTerminal()
        {
            InitializeComponent();
            
            ShipmentId=0;
            TerminalId=0;
            ShipmentType = -1;
            DefaultTerminalId =0;

            CargoTypeLabelBorder.Visibility = Visibility.Collapsed;
            CargoTypeSelectBoxBorder.Visibility = Visibility.Collapsed;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown+=ProcessKeyboard;

            InitForm();
        }

        /// <summary>
        /// id отгрузки (transport.idts)
        /// </summary>
        public int ShipmentId { get;set;}

        /// <summary>
        /// id терминала (terminal.id_ter)
        /// </summary>
        public int TerminalId { get;set; }

        /// <summary>
        /// 2 -- бумага,
        /// 8 -- ТМЦ,
        /// остальное гофра
        /// </summary>
        public int ShipmentType { get;set; }

        /// <summary>
        /// Форма редактирования
        /// </summary>
        public FormHelper Form { get;set;}

        /// <summary>
        /// рекомендуемый терминал
        /// при вызове интерфейса вычисляется рекомендуемый терминал
        /// вычисляется специальной процедурой в базе данных
        /// get_default_terminal_f(:idts)
        /// </summary>
        public int DefaultTerminalId { get;set;}
        public string DefaultTerminalName { get;set;}

        /// <summary>
        /// Флаг неизвестного водителя из таблицы t.Driver (unknown_driver)
        /// </summary>
        public bool UnknownDriverFlag { get; set; }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="ShipmentControl",
                ReceiverName = "",
                SenderName = "BindTerminal",
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
            //список полей формы
            var fields=new List<FormHelperField>()
            {        
                new FormHelperField()
                { 
                    Path="SHIPMENTID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ShipmentIdField,                    
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },
                new FormHelperField()
                { 
                    Path="APPLICATIONID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ApplicationId,                    
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },
                new FormHelperField()
                { 
                    Path="BUYERNAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=BuyerName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },
                new FormHelperField()
                { 
                    Path="SHIPMENTDATETIME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    //dd.mm.yyyy hh24:mi:ss
                    Control=ShipmentDate,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },
                new FormHelperField()
                { 
                    Path="DRIVERNAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DriverName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },
                new FormHelperField()
                { 
                    Path="TERMINALID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TerminalIdField,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.Required, null },          
                    },
                },
                new FormHelperField()
                { 
                    Path="DEFAULTTERMINAL",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=DefaultTerminal,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },
                new FormHelperField()
                { 
                    Path="FORKLIFTDRIVERID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ForkliftDriverId,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.Required, null },          
                    },
                },
                new FormHelperField()
                {
                    Path="CARGO_TYPE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CargoTypeSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                { 
                    Path="SIDE_LOADING",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                { 
                    Path="TERMINAL_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                { 
                    Path="TERMINAL_NAME",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },

                new FormHelperField()
                {
                    Path="PRODUCTIONTYPE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
            };

            Form.SetFields(fields);
            Form.OnValidate=(bool valid, string message) =>
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

            // Поля выпадающего списка водителей погрузчиков
            {
                var columns = new List<DataGridHelperColumn>()
                {
                    new DataGridHelperColumn()
                    {
                        Header="#",
                        Path="ID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=50,
                        //Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Водитель",
                        Path="NAME",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=120,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Заданий",
                        Path="SHIPMENTSCOUNT",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=60,
                    },   
                    new DataGridHelperColumn()
                    {
                        Header="Отгрузок",
                        Path="LOADEDCNT",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=60,
                    },                    
                    new DataGridHelperColumn()
                    {
                        Header="Кв. м.",
                        Path="LOADEDSQUARE",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=60,
                    },                    
                    new DataGridHelperColumn()
                    {
                        Header="Обед",
                        Path="DINNER",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        MinWidth=45,
                    },      
                    new DataGridHelperColumn()
                    {
                        Header="Роль",
                        Path="STOCKNAME",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=65,
                    },   
                   
                   
                };
                ForkliftDriverId.GridColumns=columns;
                ForkliftDriverId.GridSelectedItemFormat="NAME";
                ForkliftDriverId.OnSelectItem=(Dictionary<string,string> selectedItem) =>
                {
                    var result=true;
                    return result;
                };
            }
            
            // Поля выпадающего списка терминалов
            {
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Терминал",
                        Path="TERMINAL_NAME",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width=60,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Покупатель",
                        Path="BUYER_NAME",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=100,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Перевозчик",
                        Path="DRIVER_NAME",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Автомобиль",
                        Path="CAR",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Постановка",
                        Path="BIND_DATE",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                        Format="dd.MM HH:mm",
                        MinWidth=120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Погрузчик",
                        Path="FORKLIFTDRIVER_NAME",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ терминала",
                        Path="ID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="",
                        Path="FORKLIFTDRIVER_ID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        Width=0,
                        Hidden=true,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="",
                        Path="TERMINAL_STATUS",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=0,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="№ терминала",
                        Path="TERMINAL_ID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Hidden=true,
                    },
                };
                TerminalIdField.GridColumns=columns;
                TerminalIdField.GridSelectedItemFormat="TERMINAL_NAME";
                TerminalIdField.OnSelectItem=(Dictionary<string,string> selectedItem) =>
                {
                    var result=true;
                    if(string.IsNullOrEmpty(selectedItem.CheckGet("BUYER_NAME")))
                    {
                        //свободный терминал
                        result=true;
                    }
                    else
                    {
                        result=false;
                    }

                    //terminal data -> FROM fields
                    var p=new Dictionary<string,string>();
                    p.CheckAdd("TERMINAL_ID", selectedItem.CheckGet("TERMINAL_ID"));
                    p.CheckAdd("TERMINAL_NAME", selectedItem.CheckGet("TERMINAL_NAME"));
                    Form.SetValues(p);

                    return result;
                };
            }

        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage m)
        {
            //Group 
            if(m.ReceiverGroup.IndexOf("ShipmentControl") > -1)
            {
                switch(m.Action)
                {
                    case "Refresh":
                        break;
                }
            }
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        private void ProcessKeyboard(object sender,System.Windows.Input.KeyEventArgs e)
        {
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
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/shipments/control/common/bind_terminal");
        }

        /// <summary>
        /// Редактировыание полей формы
        /// </summary>
        public void Edit( )
        {
            GetData();            
        }
        
        /// <summary>
        /// Получение данных для полей формы
        /// </summary>
        public async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module","Shipments");
            q.Request.SetParam("Object","Shipment");
            q.Request.SetParam("Action","GetTerminal");
            
            q.Request.SetParam("SHIPMENT_ID", ShipmentId.ToString());

            await Task.Run(() =>
            {
               q.DoQuery();
            });

            if(q.Answer.Status == 0)                
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                if(result!=null)
                {
                    var shipment=new Dictionary<string,string>();

                    {
                        var ds=ListDataSet.Create(result, "SHIPMENT");
                        if (ds.Items.Count > 0)
                        {
                            DefaultTerminalId=ds.GetFirstItemValueByKey("DEFAULT_TERMINAL_ID").ToInt();
                            DefaultTerminalName=ds.GetFirstItemValueByKey("DEFAULT_TERMINAL_NAME").ToString();
                            UnknownDriverFlag = ds.GetFirstItemValueByKey("UNKNOWN_DRIVER_FLAG").ToBool();

                            shipment=ds.GetFirstItem();
                        }
                    }

                    {
                        var ds=ListDataSet.Create(result, "FORKLIFTDRIVERS");

                        if(ds.Items.Count>0 && ShipmentType != -1)
                        {
                            var items2=new List<Dictionary<string, string>>();
                            foreach(Dictionary<string, string> row in ds.Items)
                            {
                                var stockRollFlag = row.CheckGet("STOCK_ROLL_FLAG").ToBool();
                                var stockProductFlag = row.CheckGet("STOCK_PRODUCT_FLAG").ToBool();

                                if ((ShipmentType == 2 && stockRollFlag) || (ShipmentType != 2 && ShipmentType != 8 && stockProductFlag) || (ShipmentType == 8))
                                {
                                    items2.Add(row);
                                }
                            }
                            ds.Items=items2;
                        }

                        ForkliftDriverId.SetItems(ds);
                    }
                    
                    {
                        var ds=ListDataSet.Create(result, "TERMINALS");

                        ListDataSet ds2 = new ListDataSet();

                        foreach (var item in ds.Items)
                        {
                            //оставим только
                            //  не заблокированные
                            if(
                                item.CheckGet("BLOCKED_FLAG") == "1"                               
                            )
                            {

                            }
                            else
                            {
                                item.Add("ID", item.CheckGet("TERMINAL_ID"));
                                ds2.Items.Add(item);
                            }
                        }

                        TerminalIdField.SetItems(ds2);
                    }

                    {
                        var ds = ListDataSet.Create(result, "CARGO_TYPE");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            CargoTypeSelectBox.SetItems(ds, "ID", "NAME");
                        }
                    }

                    {
                        var ds=ListDataSet.Create(result, "SHIPMENT");
                        if (ds.Items.Count > 0)
                        {
                            if(ds.Items[0]!=null)
                            {
                                var first=ds.Items[0];

                                if(TerminalId!=0)
                                {

                                    first.CheckAdd("TERMINALID",TerminalId.ToString());
                                }

                                if(DefaultTerminalId!=0)
                                {
                                    first.CheckAdd("TERMINALID",DefaultTerminalId.ToString());
                                    first.CheckAdd("DEFAULTTERMINAL",DefaultTerminalName.ToString());
                                }

                                if(ShipmentId!=0)
                                {
                                    first.CheckAdd("SHIPMENTID",ShipmentId.ToString());
                                }
                            }    
                        }
                        Form.SetValues(ds);  
                    }

                    {
                        if (ShipmentType == 8)
                        {
                            Form.RemoveFilter("FORKLIFTDRIVERID", FormHelperField.FieldFilterRef.Required);

                            CargoTypeLabelBorder.Visibility = Visibility.Visible;
                            CargoTypeSelectBoxBorder.Visibility = Visibility.Visible;
                        }
                    }

                    if (!UnknownDriverFlag)
                    {
                        Show();
                    }
                    else
                    {
                        string msg = "Невозможно поставить отгрузку на терминал, у которой не определен водитель.";
                        var d = new DialogWindow($"{msg}", "Привязка к терминалу", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                                  
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Сохранение
        /// </summary>
        public void Save()
        {
            if(Form.Validate())
            {
                var p=Form.GetValues();    
                
                bool resume=true;

                if(resume)
                {
                    if(p.CheckGet("SIDE_LOADING").ToBool())
                    {
                        if(p.CheckGet("TERMINAL_ID").ToInt() != 27)
                        {
                            string msg = "";
                            msg=$"{msg}\nУ автомобиля установлено свойство [Боковая загрузка].";
                            msg=$"{msg}\nТакие отгрузки следует ставить на терминал №23.";
                            msg=$"{msg}\nСейчас выбран терминал №{p.CheckGet("TERMINAL_NAME")}";
                            msg=$"{msg}\n";
                            msg=$"{msg}\nВы действительно хотите привязать отгрузку к терминалу?";
                            
                            
                            var d = new DialogWindow($"{msg}", "Привязка к терминалу", "", DialogWindowButtons.YesNo);
                            var dialogResult=(bool)d.ShowDialog();        
                            if(!dialogResult)
                            {
                                resume=false;
                            }
                        }
                    }
                }

                if (resume)
                {
                    if (p.CheckGet("PRODUCTIONTYPE").ToInt() == 8)
                    {
                        if (string.IsNullOrEmpty(p.CheckGet("CARGO_TYPE_ID")))
                        {
                            string msg = "";
                            msg = $"{msg}\nТип этой отгрузки - поставка ТМЦ.";
                            msg = $"{msg}\nНе выбран тип поставляемого груза!";
                            msg = $"{msg}\n";
                            msg = $"{msg}\nВы действительно хотите привязать отгрузку к терминалу?";


                            var d = new DialogWindow($"{msg}", "Привязка к терминалу", "", DialogWindowButtons.NoYes);
                            var dialogResult = (bool)d.ShowDialog();
                            if (!dialogResult)
                            {
                                resume = false;
                            }
                        }
                    }
                }

                if(resume)
                {
                    SaveData(p);
                }                
            }
        }

        /// <summary>
        /// Сохранение значений полей в БД
        /// </summary>
        public async void SaveData(Dictionary<string,string> p)
        {
            Toolbar.IsEnabled = false;
            Mouse.OverrideCursor=Cursors.Wait;
            
            var q = new LPackClientQuery();
            q.Request.SetParam("Module","Shipments");
            q.Request.SetParam("Object","Shipment");
            q.Request.SetParam("Action","BindTerminal");

            if (p.CheckGet("PRODUCTIONTYPE").ToInt() == 8)
            {
                p.CheckAdd("APPLICATIONID", "0");
            }

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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        var id=ds.GetFirstItemValueByKey("ID").ToInt();
                        
                        if(id!=0)
                        {
                            //отправляем сообщение гриду о необходимости обновить данные
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup="ShipmentControl",
                                ReceiverName = "TerminalList,DriverList,ShipmentList,ShipmentKshList",
                                SenderName = "BindTerminal",
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
            Mouse.OverrideCursor=null;
        }

        /// <summary>
        /// Окно с формой редактирования
        /// </summary>
        public Window Window { get;set;}

        /// <summary>
        /// Отображение окна с формой редактирования
        /// </summary>
        public void Show()
        {
            string title=$"Привязка к терминалу";
            
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

                       
            if( Window != null )
            {
                //Window.Topmost=true;
                Window.ShowDialog();
            }

            Window.Closed+=Window_Closed;

            TerminalIdField.Focus();
        }

        /// <summary>
        /// отвязка отгрузки от терминала
        /// </summary>
        public async void Unbind(Dictionary<string,string> values)
        {
            /*
                TERMINAL_NUMBER
                BUYER_NAME
                DRIVER_NAME
                TERMINAL_ID
             */

            var resume = true;
            var terminalId = 0;

            if (resume)
            {
                terminalId = values.CheckGet("TERMINAL_ID").ToInt();
                if (terminalId == 0)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                var msg = "";
                msg = $"{msg}Отвязать отгрузку от терминала?\n";

                msg = $"{msg}Терминал: {values.CheckGet("TERMINAL_NUMBER")}\n";
                if (!string.IsNullOrEmpty(values.CheckGet("BUYER_NAME")))
                {
                    msg = $"{msg}Покупатель: {values.CheckGet("BUYER_NAME")}\n";
                }
                else
                {
                    msg = $"{msg}Покупатель: {values.CheckGet("BAYER_NAME")}\n";
                }
                msg = $"{msg}Водитель: {values.CheckGet("DRIVER_NAME")}\n";

                var d = new DialogWindow($"{msg}", "Отвязка от терминала", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() != true)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                string underloadMessage = "";

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("SHIPMENT_ID", values.CheckGet("TRANSPORT_ID"));
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Position");
                q.Request.SetParam("Action", "ListQuantityDeviation");

                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            foreach (var item in ds.Items)
                            {
                                if (item.CheckGet("QUANTITY_BY_CONSUMPTION").ToInt() < item.CheckGet("QUANTITY_BY_ORDER").ToInt())
                                {
                                    underloadMessage = $"{underloadMessage}{Environment.NewLine}" +
                                        $"Позиция: {item.CheckGet("PRODUCT_NAME")}{Environment.NewLine}" +
                                        $"По заявке: {item.CheckGet("QUANTITY_BY_ORDER").ToInt()} Погружено: {item.CheckGet("QUANTITY_BY_CONSUMPTION").ToInt()}{Environment.NewLine}" +
                                        $"Отклонение: {item.CheckGet("QUANTITY_BY_ORDER").ToInt() - item.CheckGet("QUANTITY_BY_CONSUMPTION").ToInt()}шт. ({item.CheckGet("QUANTITY_PERCENTAGE_DEVIATION").ToDouble()}%){Environment.NewLine}";
                                }
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(underloadMessage))
                {
                    underloadMessage = $"Внимание, остались недогруженные позиции! Вы хотите продолжить?{Environment.NewLine}" +
                        $"{underloadMessage}";
                    var d = new DialogWindow($"{underloadMessage}", "Отвязка от терминала", "", DialogWindowButtons.NoYes);
                    if (d.ShowDialog() != true)
                    {
                        resume = false;
                    }
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID", terminalId.ToString());
                    p.CheckAdd("IDTS", values.CheckGet("TRANSPORT_ID"));
                }
                
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Shipments");
                q.Request.SetParam("Object", "Shipment");
                q.Request.SetParam("Action", "UnbindTerminal");
                
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
                            var ds = ListDataSet.Create(result, "ITEMS");
                            var id=ds.GetFirstItemValueByKey("ID").ToInt();
                        
                            if(id!=0)
                            {
                                //отправляем сообщение гриду о необходимости обновить данные
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup="ShipmentControl",
                                    ReceiverName = "TerminalList,DriverList,ShipmentList",
                                    SenderName = "BindTerminal",
                                    Action = "Refresh",
                                });

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
        /// Дополнительный обработчик закрытия окна
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender,System.EventArgs e)
        {
            Destroy();
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Close()
        {
            var window=this.Window;
            if( window != null )
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

        /// <summary>
        /// Обработчик нажатия на кнопку сохранения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Save();
        }
        
        private void HelpButton_OnClick(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
    }
}