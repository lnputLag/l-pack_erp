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
    /// коррекция рулона
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-06-29</released>
    /// <changed>2022-06-29</changed>    
    public partial class RollCorrectWeight : UserControl
    {
        public RollCorrectWeight()
        {
            InitializeComponent();

            Loaded+=RollCorrectWeight_Loaded;

            InitForm();
            SetDefaults();
        }

        private void RollCorrectWeight_Loaded(object sender,RoutedEventArgs e)
        {
            Residue.Focus();
            //Residue.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            var l=Residue.Text.Length;
            Residue.CaretIndex=l;
        }

        public Window Window { get; set; }
        
        /// <summary>
        /// id рулона, приходит снаружи
        /// </summary>
        public int RollId { get; set; }

        /// <summary>
        /// флаг блокировки рулона
        /// </summary>
        int RollBlockedFlag { get; set; }

        /// <summary>
        /// длина материала в рулоне, м
        /// </summary>
        int RollLenght { get; set; }
        
        /// <summary>
        /// плотность материала в рулоне, г/кв.м.
        /// </summary>
        int RollDensity { get; set; }

        /// <summary>
        /// ширина рулона, мм
        /// </summary>
        int RollWidth { get; set; }

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
                    Path="RESIDUE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Residue,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{ 
                    },
                },
                new FormHelperField()
                { 
                    Path="DIMENSION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="RadioBox",
                    Control=Dimension,
                    Default="м",
                },
               
                new FormHelperField()
                { 
                    Path="ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                    Control=null,
                },
                new FormHelperField()
                { 
                    Path="BLOCKED",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                    Control=null,
                    AfterSet=(FormHelperField f, string v)=>{ 
                        var blocked=v.ToBool();
                        if(blocked)
                        {
                            BlockButton.Visibility=Visibility.Collapsed;
                            UnblockButton.Visibility=Visibility.Visible;
                        }
                        else
                        {
                            BlockButton.Visibility=Visibility.Visible;
                            UnblockButton.Visibility=Visibility.Collapsed;
                        }
                    }
                },
                new FormHelperField()
                { 
                    Path="DENSITY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                    Control=null,
                    
                },
                new FormHelperField()
                { 
                    Path="WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                    Control=null,
                },
                new FormHelperField()
                { 
                    Path="LENGTH",
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
            };

            Form.SetFields(fields);
            Form.StatusControl=FormStatus;

            //подставим в поле остаток длину рулона
            Form.BeforeSet=(Dictionary<string,string> v) =>
            {
                int i=v.CheckGet("LENGTH").ToInt();
                v.CheckAdd("RESIDUE",i.ToString());
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
                SenderName = "RollCorrectWeight",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
           
        }

        private void SetDefaults()
        {
            Form.SetDefaults();

            //RestLength.Text = "";
            RollBlockedFlag = 0;
            RollId = 0;
            RollLenght = 0;
            RollDensity = 0;
            RollWidth = 0;
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

        public void Edit(int rollId)
        {
            RollId = rollId;
            GetData();
        }

        /// <summary>
        /// получение данных
        /// </summary>
        private async void GetData()
        {
            var p=new Dictionary<string,string>();
            {
                p.CheckAdd("ID",RollId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Roll");
            q.Request.SetParam("Action", "GetDataCorrection");
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
                    var row=ds.GetFirstItem();
                    if(row!=null)
                    {
                        if(
                            row.CheckGet("DENSITY").ToInt() >0 
                            && row.CheckGet("WIDTH").ToInt() >0 
                        )
                        {
                            complete=true;
                        }
                    }

                    if(complete)
                    {
                        Form.SetValues(ds);
                        Show();                       
                    }
                    else
                    {
                        var t="Корректировка рулона невозможна";
                        var m="Не хватает информации в системе по данному рулону";
                
                        var i=new ErrorTouch();
                        i.Show(t,m);
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// изменение данных при смене единиц измерения,
        /// при переключении ед. измер. в поле "остаток" будет подставлено
        /// значение в зависимости от выбранных ед. измер.        
        /// mode -- режим, 1=длина, 2=масса
        /// </summary>
        /// <param name="mode"></param>
        public void CheckResidue(int mode=0)
        {
            var v=Form.GetValues();
            var residue=0;

            switch(mode)
            {
                //длина
                default:
                case 1:
                    residue=v.CheckGet("LENGTH").ToInt();
                    break;

                //масса
                case 2:
                    residue=GetRollMass(v);
                    break;
            }

            v.CheckAdd("RESIDUE",residue.ToString());
            Form.SetValues(v);
        }

        public int GetRollMass(Dictionary<string,string> v, bool useResidue=false)
        {
            var mass=0;
            
            int residue=0;
            if(useResidue)
            {
                residue=v.CheckGet("RESIDUE").ToInt();
            }
            else
            {
                residue=v.CheckGet("LENGTH").ToInt();
            }
                
            int density=v.CheckGet("DENSITY").ToInt();
            int width=v.CheckGet("WIDTH").ToInt();
            
            var m=((double) ((double)residue*(double)density*(double)width) / (double)1000000);
            mass = (int)Math.Round( m );
            return mass;
        }

        /// <summary>
        /// блокировка рулонв
        /// </summary>
        /// <param name="flag">0=разблокировка,1=блокировка</param>
        private async void UpdateRollFlag(int flag=0)
        {
            var p=new Dictionary<string,string>();
            {
                p.CheckAdd("ID",RollId.ToString());
                p.CheckAdd("BLOCKED",flag.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Roll");
            q.Request.SetParam("Action", "UpdateBlockedFlag");
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
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
                                ReceiverGroup = "Production",
                                ReceiverName = ReceiverName,
                                SenderName = "RollCorrectWeight",
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
                int residue=v.CheckGet("RESIDUE").ToInt();
                if(residue==0)
                {
                    error=$"Введите остаток";
                    resume=false;
                }
            }

            int mass=0;

            //дополнительные проверки
            if(resume)
            {
                var dimension=v.CheckGet("DIMENSION").ToString();
                dimension=dimension.ToLower();

                switch(dimension)
                {
                    case "кг":
                        mass=v.CheckGet("RESIDUE").ToInt();
                        if(mass<=0)
                        {
                            error=$"Ошибка вычисления массы";
                            resume=false;
                        }
                        break;

                    case "м":
                    default:
                        /*  Могут автоматически списываться рулоны с заниженной длинной, вплоть до 0,
                         *  хотя в реальности рулон может практически не измениться.
                         *  Требуется возможность указывать любое значение для приведения к факту.
                        if(resume)
                        {
                            int length=v.CheckGet("LENGTH").ToInt();
                            int residue = v.CheckGet("RESIDUE").ToInt();       

                            if(residue>length)
                            {
                                error=$"Слишком большая длина";
                                resume=false;
                            }
                        }
                        */
                        mass=GetRollMass(v,true);

                        /*   Аналогично, могут автоматически списываться рулоны с заниженной массой,
                         *   хотя в реальности рулон может практически не измениться
                         *  Требуется возможность указывать любое значение для приведения к факту.
                        if(resume)
                        {
                            var massOld=v.CheckGet("MASS").ToInt();  
                            if(mass>=massOld)
                            {
                                error=$"Указанный остаток ({mass} кг) больше предыдущего ({massOld} кг)";
                                resume=false;
                            }
                        }
                        */
                        
                        if(resume)
                        {
                            if(mass<=0)
                            {
                                error=$"Ошибка вычисления массы";
                                resume=false;
                            }
                        }

                        break;
                }
            }


            var p=new Dictionary<string,string>();
            if(resume)
            {
                p.CheckAdd("ID",RollId.ToString());
                p.CheckAdd("QUANTITY",mass.ToString());
            }

            //все данные собраны, отправляем
            if(resume)
            {
                SaveData(p);
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
            q.Request.SetParam("Module","Production");
            q.Request.SetParam("Object","Roll");
            q.Request.SetParam("Action","UpdateResidue");

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
                                ReceiverGroup = "Production",
                                ReceiverName = ReceiverName,
                                SenderName = "RollCorrectWeight",
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
            Central.WM.Show($"RollCorrect","Корректировка рулона",true,"add",this);
        }

        public void Close()
        {
            Central.WM.Close($"RollCorrect");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void ChangeValue(string symbol)
        {
            if(!string.IsNullOrEmpty(symbol))
            {
                var s=Form.GetValueByPath("RESIDUE");

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

                Form.SetValueByPath("RESIDUE",s);
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

        private void BlockButton_Click(object sender,RoutedEventArgs e)
        {
            UpdateRollFlag(1);
        }

        private void UnblockButton_Click(object sender,RoutedEventArgs e)
        {
            UpdateRollFlag(0);
        }

        private void PrintButton_Click(object sender,RoutedEventArgs e)
        {
            PrintReceipt();
        }

        private void DimensionLen_Click(object sender,RoutedEventArgs e)
        {
            CheckResidue(1);
        }

        private void DimensionMass_Click(object sender,RoutedEventArgs e)
        {
            CheckResidue(2);
        }

      
    }
}
