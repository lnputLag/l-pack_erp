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
    /// примечание к отгрузке
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>2</version>
    /// <released>2021-12-07</released>       
    public partial class ShipmentComment:UserControl
    {
        public ShipmentComment()
        {
            InitializeComponent();
            
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
                SenderName = "Comment",
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
                    Path="COMMENTS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Comments,                    
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
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
                    FormStatus.Text="Не все поля заполнены верно";
                }
            };
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
            Central.ShowHelp("/doc/l-pack-erp/shipments/control/common/comment");
        }
        
        public void Edit(int id=0)
        {
            ShipmentId=id;
            var p = new Dictionary<string, string>();
            p.CheckAdd("ID",ShipmentId.ToString());
            GetData(p);            
        }

        public async void GetData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module","Shipments");
            q.Request.SetParam("Object","Shipment");
            q.Request.SetParam("Action","GetComment");

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
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
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
        
        public void Save()
        {
            if(Form.Validate())
            {
                var p=Form.GetValues();
                p.CheckAdd("ID",ShipmentId.ToString());
                SaveData(p);
            }
        }

        public async void SaveData(Dictionary<string,string> p)
        {
            Toolbar.IsEnabled=false;
            
            var q = new LPackClientQuery();
            q.Request.SetParam("Module","Shipments");
            q.Request.SetParam("Object","Shipment");
            q.Request.SetParam("Action","SetComment");

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
                                ReceiverName = "ShipmentList",
                                SenderName = "Comment",
                                Action = "Refresh",
                            });

                            //отправляем сообщение гриду о необходимости обновить данные
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverGroup = "MoldedContainerShipment",
                                ReceiverName = "MoldedContainerShipmentList",
                                SenderName = "Comment",
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

        public Window Window { get;set;}
        public void Show()
        {
            string title=$"Примечания к отгрузке";
            
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

            Comments.Focus();
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

        private void Window_Closed(object sender,System.EventArgs e)
        {
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
