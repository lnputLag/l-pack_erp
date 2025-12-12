using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// параметры
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-12-27</released>
    /// <changed>2022-12-27</changed>
    public partial class Settings : UserControl
    {
        public Settings()
        {
            InitializeComponent();

            InitForm();
            SetDefaults();
        }

        public string ReceiverName = "RollControl";

        public Window Window { get; set; }
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
                    Path="FOSBER_REEL_MANUAL_MODE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="RadioBox",
                    Control=Mode,
                    Default="0",
                },
            };

            Form.SetFields(fields);
            Form.StatusControl=FormStatus;

            //подставим в поле остаток длину рулона
            Form.BeforeSet=(Dictionary<string,string> v) =>
            {
            };

            //фокус на первое поле
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
                SenderName = "Settings",
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
            {
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Settings");
            q.Request.SetParam("Object", "Fosber");
            q.Request.SetParam("Action", "Get");
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
                    
                    var ds=ListDataSet.Create(result,"ITEMS");
                    if(ds.Items.Count > 0)
                    {
                        var values= new Dictionary<string, string>();
                        
                        /*
                            NAME
                            VALUE
                            DESCRIPTION
                        */
                        foreach(Dictionary<string, string> row in ds.Items)
                        {
                            var k=row.CheckGet("NAME").ToString();
                            var v=row.CheckGet("VALUE").ToString();

                            if(k == "FOSBER_REEL_MANUAL_MODE")
                            {
                                values.CheckAdd(k,v);                  
                            }                            
                        }

                        Form.SetValues(values);
                        Show();  
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
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

            var values=Form.GetValues();     
            
            //все данные собраны, отправляем
            if(resume)
            {
                SaveData(values);
            }
            else
            {
                Form.SetStatus(error,1);
            }
        }

        /// <summary>
        /// Сохранение данных: отпарвка данных
        /// </summary>
        public async void SaveData(Dictionary<string,string> p)
        {
            Form.EnableControls();
            
            var q = new LPackClientQuery();
            q.Request.SetParam("Module","Settings");
            q.Request.SetParam("Object","Fosber");
            q.Request.SetParam("Action","Save");

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
                        var saveResult=ds.GetFirstItemValueByKey("RESULT").ToInt();
                        
                        if(saveResult!=0)
                        {
                            //отправляем сообщение гриду о необходимости обновить данные
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "Production",
                                ReceiverName = ReceiverName,
                                SenderName = "Settings",
                                Action = "Refresh",
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

        /// <summary>
        /// mode--режим: 1=ручной, 0=автоматический
        /// </summary>
        /// <param name="mode"></param>
        public void CheckMode(int mode=0)
        {
            var v=Form.GetValues();
            v.CheckAdd("FOSBER_REEL_MANUAL_MODE",mode.ToString());
            Form.SetValues(v);
        }

       

        public void Show()
        {
            Central.WM.FrameMode=1;
            Central.WM.Show($"Settings","Настройка",true,"add",this);
        }

        public void Close()
        {
            Central.WM.Close($"Settings");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void ModeAuto_Click(object sender,RoutedEventArgs e)
        {
            CheckMode(0);
        }

        private void ModeManual_Click(object sender,RoutedEventArgs e)
        {
            CheckMode(1);
        }
      
    }
}
