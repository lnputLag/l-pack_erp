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
    /// печать ярлыка по ROLL_IDP
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-09-09</released>
    /// <changed>2022-09-09</changed>
    public partial class RollPrintLabel : UserControl
    {
        public RollPrintLabel()
        {
            RollId=0;

            InitializeComponent();

            Loaded+=RollCorrectWeight_Loaded;

            InitForm();
            SetDefaults();

            if(!Central.DebugMode)
            {
                //PrintButton.Visibility=Visibility.Collapsed;
            }
        }

        public int RollId{get;set;}

        private void RollCorrectWeight_Loaded(object sender,RoutedEventArgs e)
        {
            RollIdp.Focus();
            //Residue.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            var l=RollIdp.Text.Length;
            RollIdp.CaretIndex=l;
        }

        public Window Window { get; set; }
        
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
                    Path="ROLL_IDP",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=RollIdp,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },
            };

            Form.SetFields(fields);
            Form.StatusControl=FormStatus;
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
                SenderName = "RollPrintLabel",
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

        /// <summary>
        /// Сохранение данных: подготовка данных
        /// </summary>
        public async void GetRoll()
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
                int id=v.CheckGet("ROLL_IDP").ToInt();
                if(id==0)
                {
                    error=$"Введите номер рулона";
                    resume=false;
                }
            }
            
            //все данные собраны, отправляем
            if(resume)
            {
                DoGetRoll(v);
            }
            else
            {
                Form.SetStatus(error,1);
            }
        }

        /// <summary>
        /// Сохранение данных: отпарвка данных
        /// </summary>
        public async void DoGetRoll(Dictionary<string,string> p)
        {
            bool complete=false;

            DisableControls();
            
            var q = new LPackClientQuery();
            q.Request.SetParam("Module","Production");
            q.Request.SetParam("Object","Roll");
            q.Request.SetParam("Action","Get");

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
                            Form.SetStatus("Подождите, идет печать");
                            PrintReceipt(2,id);
                            complete=true;
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }


            if(complete)
            {
                Form.SetStatus("");
            }
            else
            {
                Form.SetStatus("Рулон не найден");
            }

            EnableControls();
        }


        private void DisableControls()
        {
            SaveButton.IsEnabled=false;
        }

        private void EnableControls()
        {
            SaveButton.IsEnabled=true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode">1=просмотр,2=печать</param>
        /// <param name="rollId"></param>
        /// <returns></returns>
        private bool PrintReceipt(int mode=0, int rollId=0)
        {
            bool result=false;

            if(rollId==0)
            {
                rollId = RollId;
            }           

            if(rollId!=0)
            {
                var receiptViewer=new RollReceiptViewer();
                receiptViewer.RollId=rollId;
                result=receiptViewer.Init();

                switch(mode)
                {
                    //просмотр
                    default:
                    case 1:
                        receiptViewer.Show();
                        break;

                    //печать
                    case 2:
                        receiptViewer.Print(true);
                        break;
                }
            }

            return result;
        }

        


        public void Show()
        {
            //Central.WM.FrameMode=2;
            Central.WM.Show($"RollPrintLabel","Печать ярлыка",true,"add",this);
        }

        public void Close()
        {
            Central.WM.Close($"RollPrintLabel");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            GetRoll();
        }

        private void ChangeValue(string symbol)
        {
            if(!string.IsNullOrEmpty(symbol))
            {
                var s=Form.GetValueByPath("ROLL_IDP");

                switch(symbol)
                {
                    case "<":
                        if(!string.IsNullOrEmpty(s))
                        {
                            s=s.Substring(0,(s.Length-1));
                        }
                        break;

                    default:
                        s=s+symbol;
                        break;
                }

                Form.SetValueByPath("ROLL_IDP",s);
            }
        }

        private void KeyboardButton1_Click(object sender,RoutedEventArgs e)
        {
            ChangeValue("1");
        }

        private void KeyboardButton2_Click(object sender,RoutedEventArgs e)
        {
            ChangeValue("2");
        }

        private void KeyboardButton3_Click(object sender,RoutedEventArgs e)
        {
            ChangeValue("3");
        }

        private void KeyboardButton4_Click(object sender,RoutedEventArgs e)
        {
            ChangeValue("4");
        }

        private void KeyboardButton5_Click(object sender,RoutedEventArgs e)
        {
            ChangeValue("5");
        }

        private void KeyboardButton6_Click(object sender,RoutedEventArgs e)
        {
            ChangeValue("6");
        }

        private void KeyboardButton7_Click(object sender,RoutedEventArgs e)
        {
            ChangeValue("7");
        }

        private void KeyboardButton8_Click(object sender,RoutedEventArgs e)
        {
            ChangeValue("8");
        }

        private void KeyboardButton9_Click(object sender,RoutedEventArgs e)
        {
            ChangeValue("9");
        }

        private void KeyboardButtonBackspace_Click(object sender,RoutedEventArgs e)
        {
            ChangeValue("<");
        }

        private void KeyboardButton0_Click(object sender,RoutedEventArgs e)
        {
            ChangeValue("0");
        }

        private void KeyboardButtonNext_Click(object sender,RoutedEventArgs e)
        {
        }

        private void PrintButton_Click(object sender,RoutedEventArgs e)
        {
            PrintReceipt();
        }

    }
}
