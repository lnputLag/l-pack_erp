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
    /// Назначение водителя погрузчика
    /// (привязка водителя погрузчика к заданию на отгрузку, стоящем на терминале) 
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>2</version>
    /// <released>2021-12-07</released>
    public partial class AssignForkliftdriver:UserControl
    {
        public AssignForkliftdriver()
        {
            InitializeComponent();
            
            ShipmentId=0;

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
        /// форма
        /// </summary>
        public FormHelper Form { get;set;}
        
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
                SenderName = "AssignForkliftdriver",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// инициализация формы
        /// </summary>
        public void InitForm()
        {
            Form=new FormHelper();
            //список колонок формы
            var fields=new List<FormHelperField>()
            {        
                new FormHelperField()
                { 
                    Path="ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ShipmentIdField,                    
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },
                new FormHelperField()
                { 
                    Path="TERMINAL_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TerminalIdField,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        { FormHelperField.FieldFilterRef.Required, null },          
                    },
                },
                new FormHelperField()
                { 
                    Path="FORKLIFTDRIVER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ForkliftIdField,
                    ControlType="SelectBox",
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
                    //SaveButton.IsEnabled=true;
                    FormStatus.Text="";
                }
                else
                {
                    //SaveButton.IsEnabled=false;
                    FormStatus.Text="Не все поля заполнены верно";
                }
            };

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
                        Header="Водитель",
                        Path="NAME",
                        Doc="",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=120,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Заданий",
                        Path="SHIPMENTS_COUNT",
                        Doc="Количество имеющихся на данный момент заданий у водителя",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=60,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Отгрузок",
                        Path="LOADED_CNT",
                        Doc="Общее количество выполенных отгрузок с начала текущей смены, шт.",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=60,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Кв. м.",
                        Path="LOADED_SQUARE",
                        Doc="Общая площадь отгруженного с начала текущей смены, кв.м.",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=60,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Обед",
                        Path="DINNER",
                        Doc="Признак, что водитель находится на обеде",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Boolean,
                        MinWidth=45,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Тип",
                        Path="STOCK_NAME",
                        Doc="Тип терминала",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=65,
                    },

                };
                ForkliftIdField.GridColumns=columns;
                ForkliftIdField.GridSelectedItemFormat="NAME";
                ForkliftIdField.OnSelectItem=(Dictionary<string,string> selectedItem) =>
                {
                    var result=true;
                    //if(selectedItem.CheckGet("DINNER").ToInt()==1)
                    //{
                    //    result=false;
                    //}
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
            Central.ShowHelp("/doc/l-pack-erp/shipments/control/common/assign_forkliftdriver");
        }
        
        public void Edit( )
        {
            GetData();            
        }

        public void Save()
        {
            if(Form.Validate())
            {
                var p=Form.GetValues();
                SaveData(p);
            }
        }
    
        public async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module","Shipments");
            q.Request.SetParam("Object","Shipment");
            q.Request.SetParam("Action","GetForkliftdriver");
            
            q.Request.SetParam("SHIPMENT_ID",ShipmentId.ToString());
            q.Request.SetParam("SHOW_ALL_TERMINALS","1");

            await Task.Run(() =>
            {
               q.DoQuery();
            });

            if(q.Answer.Status == 0)                
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                if(result!=null)
                {
                    {
                        var ds=ListDataSet.Create(result, "FORKLIFTDRIVERS");
                        ForkliftIdField.SetItems(ds);
                    }
                    
                    {
                        var ds=ListDataSet.Create(result, "TERMINALS");
                        TerminalIdField.SetItems(ds);
                    }
                    
                    {
                        var ds=ListDataSet.Create(result, "SHIPMENTS");
                        Form.SetValues(ds);
                    }

                    Show();                  
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public async void SaveData(Dictionary<string,string> p)
        {
            Toolbar.IsEnabled=false;

             var q = new LPackClientQuery();
            q.Request.SetParam("Module","Shipments");
            q.Request.SetParam("Object","Shipment");
            q.Request.SetParam("Action","AssignForkliftdriver");
            
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
                    //отправляем сообщение гриду о необходимости обновить данные
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup="ShipmentControl",
                        ReceiverName = "TerminalList,DriverList,ShipmentList",
                        SenderName = "ReassignForkliftView",
                        Action = "Refresh",
                    });

                    Close();
                }
            }
            else
            {
                q.ProcessError();
            }

            Toolbar.IsEnabled=true;
        }

        public Window Window { get;set;}
        public void Show()
        {
            string title=$"Назначение водителя погрузчика";
            
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
                Window.Topmost=true;
                Window.ShowDialog();
            }

            Window.Closed+=Window_Closed;

            TerminalIdField.Focus();
        }

        private void Window_Closed(object sender,System.EventArgs e)
        {
            Destroy();
        }

        public void Close()
        {
            var window=this.Window;
            if( window != null )
            {
                window.Close();
            }            

            Destroy();
        }

        private void CancelButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Close();
        }

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