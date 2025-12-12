using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// указание причины брака
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-04-29</released>
    /// <changed>2022-06-29</changed>
    public partial class RollSetDefect : UserControl
    {
        public RollSetDefect()
        {
            Id=0;
            Mass=0;
            Operation="";

            InitializeComponent();

            InitForm();
            SetDefaults();

            if(!Central.DebugMode)
            {
                //PrintButton.Visibility=Visibility.Collapsed;
            }
        }

        /// <summary>
        /// id рулона, приходит снаружи
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// масса материала на рулоне, кг
        /// </summary>
        public int Mass { get; set; }
        /// <summary>
        /// код назначения при перемещении рулона
        /// </summary>
        public string Operation { get;set; }
        /// <summary>
        /// количество ярлыков для печати
        /// </summary>
        public int LabelCount { get;set; }
        
        public string ReceiverName = "RollControl";

        /// <summary>
        /// Форма
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Инициализация формы
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
                    ControlType="void",
                    Control=null,
                },
                new FormHelperField()
                { 
                    Path="MASS",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                    Control=null,
                },
                new FormHelperField()
                { 
                    Path="OPERATION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="void",
                    Control=null,
                },
                new FormHelperField()
                { 
                    Path="REASON",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="void",
                    Control=null,
                },
                new FormHelperField()
                { 
                    Path="REASON_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="RadioBox",
                    Control=Reason,                    
                },
                new FormHelperField()
                {
                    Path="LABEL_COUNT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                    Control=null,
                },

            };

            Form.SetFields(fields);
            Form.StatusControl=FormStatus;

            //
            Form.BeforeSet=(Dictionary<string,string> v) =>
            {
            };

            //
            Form.AfterSet=(Dictionary<string,string> v) =>
            {
            };
        }

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="Production",
                ReceiverName = "",
                SenderName = "RollSetDefect",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
           
        }

        private void SetDefaults()
        {
            Form.SetDefaults();
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch(e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
            }
        }

        public void Edit()
        {
            GetData();
        }

        /// <summary>
        /// получение данных
        /// </summary>
        private async void GetData()
        {
            var p=new Dictionary<string,string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Roll");
            q.Request.SetParam("Action", "FaultTypeRef");
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var complete=false;
                    var ds=ListDataSet.Create(result,"ITEMS");
                    var items = ds.GetItemsList("ID", "NAME");

                    if(items.Count>0)
                    {
                        {
                            Container1.Children.Clear();
                            Container2.Children.Clear();
                            Container3.Children.Clear();
                        }
                        
                        
                        var start=0;
                        var blocks=3;
                        var max=(int)items.Count/blocks;

                        for(int j=1; j<blocks; j++)
                        {
                            var i=0;
                            foreach(KeyValuePair<string,string> item in items)
                            {
                                var finish=start+max;
                                if(i>=start && i<=finish)
                                {
                                    var el=new RadioButton();
                                    el.Content=item.Value;
                                    el.GroupName="Reason";
                                    el.Style=(Style)SaveButton.TryFindResource("FormFieldRadioSmall");
                                    el.MinWidth=200;
                                    el.MaxWidth=300;
                                    el.Tag=item.Key;
                                    el.Click+=El_Click;

                                    var container=Container1;
                                    switch(j)
                                    {
                                        case 1:
                                            container=Container1;
                                            break;

                                        case 2:
                                            container=Container2;
                                            break;

                                        case 3:
                                            container=Container3;
                                            break;
                                    }
                                    container.Children.Add(el);
                                }
                                i++;
                            }
                            start=start+max;
                        }

                        complete=true;
                       
                    }

                    if(complete)
                    {
                        InitForm();

                        var v=new Dictionary<string,string>();
                        v.CheckAdd("ID",Id.ToString());
                        v.CheckAdd("MASS",Mass.ToString());
                        v.CheckAdd("OPERATION",Operation.ToString());
                        v.CheckAdd("LABEL_COUNT", LabelCount.ToString());
                        Form.SetValues(v);

                        Show();                       
                    }                   
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        private void El_Click(object sender,RoutedEventArgs e)
        {
            var el=(RadioButton)sender;
            var id=el.Tag.ToString();
            var txt=el.Content.ToString();

            var v=new Dictionary<string,string>();
            v.CheckAdd("REASON",txt);
            v.CheckAdd("REASON_ID",id);
            Form.SetValues(v);

        }


        /// <summary>
        /// Сохранение данных: подготовка данных
        /// </summary>
        public async void Save()
        {
            bool resume = true;
            string error="";

            //стандартная валидация данных средствами формы
            if(resume)
            {
                var validationResult=Form.Validate();
                if(!validationResult)
                {
                    resume = false;
                }
            }

            var v=Form.GetValues();     
            
            if(resume)
            {
                string reason=v.CheckGet("REASON").ToString();
                if(string.IsNullOrEmpty(reason))
                {
                    error=$"Укажите причину забраковки";
                    resume=false;
                }
            }

            //все данные собраны, отправляем
            if(resume)
            {
                SaveData(v);                
            }
            else
            {
                Form.SetStatus(error,1);
            }
        }

        /// <summary>
        /// Сохранение данных: отправка данных
        /// </summary>
        public async void SaveData(Dictionary<string,string> p)
        {
            Form.EnableControls();
            
            var q = new LPackClientQuery();
            q.Request.SetParam("Module","Production");
            q.Request.SetParam("Object","Roll");
            q.Request.SetParam("Action","Fault");

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
                            var o=Form.GetValues();
                
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "Production",
                                ReceiverName = ReceiverName,
                                SenderName = "RollSetDefect",
                                Action = "MoveRoll",
                                Message = Operation,
                                ContextObject=o,
                            });

                            Close();
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            Form.EnableControls();
        }

        public void Show()
        {
            //Central.WM.FrameMode=2;
            Central.WM.Show($"RollSetDefect","Причина брака",true,"add",this);
        }

        public void Close()
        {
            Central.WM.Close($"RollSetDefect");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

      
    }
}
