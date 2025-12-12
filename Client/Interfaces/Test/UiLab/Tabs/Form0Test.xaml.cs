using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Test
{
    /// <summary>
    /// </summary>
    public partial class Form0Test : UserControl
    {
        public Form0Test()
        {
            InitializeComponent();
            
            Id=0;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown+=OnKeyDown;
            
            FormInit();
            SetDefaults();
        }

        public FormHelper Form { get;set;}

        public void FormInit()
        {
            {

                Form=new FormHelper();
                //список колонок формы
                var fields=new List<FormHelperField>()
                {
                    new FormHelperField()
                    { 
                        Path="SENDER",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=Sender,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                            { FormHelperField.FieldFilterRef.Required, null },                    
                        },
                    },
                    new FormHelperField()
                    { 
                        Path="RECIPIENT",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=Recipient,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                            { FormHelperField.FieldFilterRef.Required, null },                    
                        },
                    },
                    new FormHelperField()
                    { 
                        Path="RECIPIENTCOPY",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=RecipientCopy,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        },
                    },
                    new FormHelperField()
                    { 
                        Path="SUBJECT",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=Subject,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                            { FormHelperField.FieldFilterRef.Required, null },                    
                        },
                    },
                    new FormHelperField()
                    { 
                        Path="SENDDATE",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=SendDate,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                            { FormHelperField.FieldFilterRef.Required, null },                    
                        },
                    },
                    new FormHelperField()
                    { 
                        Path="MESSAGE",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=Message,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                            { FormHelperField.FieldFilterRef.Required, null },                    
                        },
                    },
                     new FormHelperField()
                    { 
                        Path="CODE",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=ErrorCode,
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                        },
                    },
                    new FormHelperField()
                    { 
                        Path="APPLICATION_ID",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=Application,
                        ControlType="SelectBox",
                        Default="0",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                        },
                    },
                    new FormHelperField()
                    {
                        Path="DATE_TIME_PICKER",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=DateTimePicker,
                        ControlType="dateedit",
                        Format="dd.MM.yyyy HH:mm:ss",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="DATE_PICKER",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=DatePicker,
                        ControlType="dateedit",
                        Format="dd.MM.yyyy",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                };

                Form.SetFields(fields);
                Form.OnValidate=(bool valid, string message) =>
                {
                    if(valid)
                    {
                        SaveButton.IsEnabled=true;
                        FormStatus.Text="";
                    }
                    else
                    {
                        SaveButton.IsEnabled=false;
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

            {
                var columns = new List<DataGridHelperColumn>()
                {
                    new DataGridHelperColumn()
                    {
                        Header="ИД заявки",
                        Path="ID",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        Width=60,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Товар",
                        Path="GOODS_NAME",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                        MinWidth=190,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="В заявке",
                        Doc="Количество изделий в заявке, шт",
                        Path="IN_APPLICATION_QUANTITY",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=60,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="В ПЗ",
                        Doc="Количество заготовок в ПЗ, всего, шт",
                        Path="BLANK_QUANTITY",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                        MinWidth=60,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="Отгрузка",
                        Path="SHIPMENT_DATE",
                        Format="dd.MM HH:mm",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                        MinWidth=70,
                    },
                    new DataGridHelperColumn()
                    {
                        Header="ПЗ",
                        Path="PRODUCTION_DATE",
                        Format="dd.MM HH:mm",
                        ColumnType=DataGridHelperColumn.ColumnTypeRef.DateTime,
                        MinWidth=70,
                    },
                };
                Application.GridColumns=columns;
                Application.SelectedItemValue="GOODS_NAME SHIPMENT_DATE";
            }
        }

        public void SetDefaults()
        {
            //SendDate.Text=DateTime.Now.AddMinutes(5).ToString("dd.MM.yyyy HH:mm:ss");
            //Sender.Text="Информационная рассылка Л-ПАК <info@l-pak.ru>";
            Form.SetDefaults();
        }

        #region Common

        public void Destroy()
        {
            Messenger.Default.Unregister<ItemMessage>(this);
        }


        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }
        
        private void OnKeyDown(object sender,System.Windows.Input.KeyEventArgs e)
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

        #endregion

        public void Create()
        {
            SetDefaults();
            Show();
        }

        public void Edit( int id )
        {
           GetData(id);
        }

        public void Show()
        {
            string title=$"Сообщение {Id}";
            if(Id == 0)
            {
                title="Новое сообщение";
                SaveButton.Visibility=System.Windows.Visibility.Visible;
            }
            else
            {
                SaveButton.Visibility=System.Windows.Visibility.Collapsed;
            }
            
            var key=$"Messages_Email_{Id}";
            Central.WM.AddTab(key, title, true, "add", this);

            Recipient.Focus();
        }

        public void Hide()
        {
            var key=$"Messages_Email_{Id}";
            Central.WM.RemoveTab(key);
        }

        public RowDataSet UserData { get; set; }
        public int Id { get;set;}

        public async void GetData(int id)
        {
            var p=new Dictionary<string,string> 
            {
                { "ID",id.ToString()},
            };

            await Task.Run(()=>{ 
                UserData=_LPackClientDataProvider.DoQueryDeserialize<RowDataSet>("Messages","Email","Get","Items",p);                                
            });

            if(UserData != null)
            {
                UserData.Init();

                if(UserData.Values.Count>0)
                {
                    Id=UserData.getValue("ID").ToInt();
                    Form.SetValues(UserData);
                    Show();
                }
            }
            
        }

        public async void SaveData(Dictionary<string,string> p)
        {
            var result="";
            var resultData=new Dictionary<string,string>();
            
            await Task.Run(()=>{ 
                result=_LPackClientDataProvider.DoQueryRawResult("Messages","Email","Save","Items",p);                                
            });

            if(!string.IsNullOrEmpty(result))
            {
                resultData=JsonConvert.DeserializeObject<Dictionary<string,string>>(result);
                if(resultData.Count > 0)
                {
                    if(resultData.ContainsKey("ID"))
                    {
                        if(resultData["ID"].ToInt() != 0)
                        {
                            
                            //отправляем сообщение о необходимости обновить данные
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "Messages",
                                SenderName = "EmailView",
                                Action = "Refresh",
                            });

                            //закрываем фрейм
                            Hide();
                        }
                    }

                }
            }
        }

        public void Save()
        {
            if(Form.Validate())
            {
                var p=Form.GetValues();
                p.Add("ID",Id.ToString());
                SaveData(p);
            }
        }


        public async void ApplicationLoadItems()
        {
            var position=new Dictionary<string, string>();
            position.CheckAdd("GOODS_ID","333894");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Position");
            q.Request.SetParam("Action", "ListByGoodId");
            q.Request.SetParam("GOODS_ID", position.CheckGet("GOODS_ID"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "APPLICATION");
                    Application.SetItems(ds);
                    ApplicationSetSelectedItem();
                    //Form.SetValues(p);
                }
            }
        }
        
        public async void ApplicationClearItems()
        {
              //var v=new  Dictionary<string, string>();
              //  v.CheckAdd("APPLICATION_ID","");
              //  Form.SetValues(v);  
        
            var emptyDS = new ListDataSet();
            emptyDS.Init();
            Application.SetItems(emptyDS);
            Application.SetSelectedItem(new Dictionary<string,string>());
        }

        public async void ApplicationSetSelectedItem()
        {
            var v=new  Dictionary<string, string>();
            v.CheckAdd("APPLICATION_ID","1391071");
            Form.SetValues(v);

            //var selectedItem=new Dictionary<string, string>();
            //selectedItem.CheckAdd("ID","1391071");
            //Application.SetSelectedItem(selectedItem);
        }

        public void Test()
        {
            Create();
            ApplicationLoadItems();
            Application.Show();
        }

        private void CancelButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Hide();
        }

        private void SaveButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Save();
        }

        private void TestButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            var today=DateTime.Now.AddMinutes(5).ToString("dd.MM.yyyy HH:mm:ss");
            SendDate.Text=today;
            Sender.Text="Информационная рассылка Л-ПАК <info@l-pak.ru>";
            Recipient.Text="<balchugov_dv@l-pak.ru>";
            Subject.Text=$"test message {today}";
            Message.Text=$"test message {today}";
        }

        private void ApplicationLoadButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ApplicationLoadItems();
        }

        private void ApplicationClearButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ApplicationClearItems();
        }

        private void ApplicationSetButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ApplicationSetSelectedItem();
        }

        private void ApplicationShowButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Application.Show();
        }
    }
}
