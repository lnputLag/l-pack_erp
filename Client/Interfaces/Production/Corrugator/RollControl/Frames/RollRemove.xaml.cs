using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// перемещение рулона
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-04-29</released>
    /// <changed>2022-06-29</changed>
    public partial class RollRemove : UserControl
    {
        public RollRemove()
        {
            RollId=0;
            RollMass=0;
            Operation="";
            RollbackExpenseAllowed = true;

            InitializeComponent();

            InitForm();
            SetDefaults();           
        }
        
        /// <summary>
        /// id рулона, приходит снаружи
        /// </summary>
        public int RollId { get; set; }
        /// <summary>
        /// масса материала на рулоне, кг
        /// </summary>
        public int RollMass { get; set; }
        /// <summary>
        /// код назначения при перемещении рулона
        /// </summary>
        public string Operation { get;set; }
        /// <summary>
        /// код количества копий при печати ярлыка
        /// </summary>
        public string Operation2 { get;set; }

        public bool RollbackExpenseAllowed;

        /// <summary>
        /// Идентификатор гофроагрегата
        /// </summary>
        public int MachineId = 2;

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
                    Path="OPERATION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="RadioBox",
                    Control=OperationCode,
                    Default="RBS",
                    //OnChange = OperationChange,
                },
                new FormHelperField()
                { 
                    Path="OPERATION2",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    ControlType="RadioBox",
                    Control=OperationCode2,
                    Default="P1",
                },
            };

            Form.SetFields(fields);
            Form.StatusControl=FormStatus;
        }

        private void OperationChange(FormHelperField f, string v)
        {
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
                SenderName = "RollRemove",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
           
        }

        private void SetDefaults()
        {
            Form.SetDefaults();
            
            RollbackExpense.IsEnabled = RollbackExpenseAllowed;

            // Настраиваем видимость в зависимости от ГА
            if (MachineId == 21)
            {
                //У BHS-2 только 3 раската
                DestinationRB1.Visibility = Visibility.Collapsed;
                DestinationRB2.Visibility = Visibility.Collapsed;
            }
            if (MachineId == 23)
            {
                DestinationZ0.Visibility = Visibility.Collapsed;
            }
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
            SetDefaults();
            Show();
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
            
            Operation=v.CheckGet("OPERATION");
            Operation2=v.CheckGet("OPERATION2");

            var labelCount=1;
            if(Operation2 == "P1")
            {
                labelCount=1;
            }
            else
            {
                labelCount=2;
            }
            
           
            if(resume)
            {
                if(string.IsNullOrEmpty(Operation))
                {
                    error=$"Выберите назначение";
                    resume=false;
                }
            }

            //дополнительные проверки
            if(resume)
            {
                Operation = Operation.Substring(2, 1);

                // при забраковке открываем окно выбора типа брака
                if (Operation == "Z")
                {
                    var i = new RollSetDefect();
                    i.ReceiverName = ReceiverName;
                    i.Id=RollId;
                    i.Mass=RollMass;
                    i.Operation=Operation;
                    i.LabelCount = labelCount;
                    i.Edit();
                    resume=false;
                    Close();
                }
            }

            if(resume)
            {
                var o=new Dictionary<string,string>()
                {
                    { "ID", RollId.ToString() },
                    { "MASS", RollMass.ToString() },
                    { "OPERATION", Operation },
                    { "LABEL_COUNT", labelCount.ToString() },
                };
                
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "Production",
                    ReceiverName = ReceiverName,
                    SenderName = "RollMove",
                    Action = "MoveRoll",
                    Message = Operation,
                    ContextObject=o,
                });

                Close();
            }
            else
            {
                Form.SetStatus(error,1);
            }
        }

        public void Show()
        {
            //Central.WM.FrameMode=2;
            Central.WM.Show($"RollCorrect","Корректировка рулона",true,"add",this);
        }

        public void Close()
        {
            Central.WM.Close($"RollCorrect");
        }

        private void UpdateButtons(object sender)
        {
            RadioButton pressed = (RadioButton)sender;

            if (pressed.Tag.ToString() == "RBS"
                || pressed.Tag.ToString() == "RBZ")
            {
                OperationCode2.Visibility = Visibility.Visible;
            }
            else
            {
                OperationCode2.Visibility = Visibility.Hidden;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void DestinationButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateButtons(sender);
        }
    }
}
