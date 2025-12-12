using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Service
{
    /// <summary>
    /// редактирование машины
    /// </summary>
    /// <author>eletskikh_ya</author>
    /// <changed>Грешных Н.И.</changed> 
    public partial class TemporaryCarPass : UserControl
    {
        public TemporaryCarPass()
        {
            Id = 0;
            FrameName = "TemporaryCarPass";
            ReceiverName = "CarList";

            InitializeComponent();
            
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// идентификатор записи, с которой работает форма
        /// (primary key записи таблицы)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Имя вкладки, которая становится активной после закрытия фрейма
        /// </summary>
        public string ReceiverName;

        /// <summary>
        // инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Num,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.ToUpperCase, null },
                    },
                },
                new FormHelperField()
                {
                    Path="DESCRIPTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Description,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="FROM_DT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=FromDate,
                    ControlType="TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        //{ FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 10 },
                    }

                },
                new FormHelperField()
                {
                    Path="TO_DT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ToDate,
                    ControlType="TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        //{ FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 10 },
                    }

                },
            };

            Form.SetFields(fields);
            //Form.ToolbarControl = FormToolbar;
            Form.StatusControl = FormStatus;

            //после установки значений
            Form.AfterSet = (Dictionary<string, string> v) =>
              {
                  //фокус на поле ввода номера машины
                  Num.Focus();
              };
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
        }
        
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "StockGate",
                ReceiverName = "",
                SenderName = "CarPerment",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
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

                case Key.Enter:
                    Save();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// создание новой записи
        /// </summary>
        public void Create()
        {
            Id = 0;
            GetData();
        }

        /// <summary>
        /// редактирование записи
        /// </summary>
        /// <param name="id"></param>
        public void Edit( int id )
        {
            Id=id;
            GetData();
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;

            var frameName=GetFrameName();

            var shortNum = Num.Text;

            if (Id == 0)
            {
                Central.WM.Show(frameName, "Новая машина", true, "add", this);
            }
            else
            {
                Central.WM.Show(frameName, $"Машина {shortNum}", true, "add", this);
            }

        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName=GetFrameName();
            Central.WM.Close(frameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        /// <summary>
        /// формирует уникальный идентификатор фрейма
        /// </summary>
        /// <returns></returns>
        public string GetFrameName()
        {
            string result = "";
            result = $"{FrameName}_{Id}";
            return result;
        }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            DisableControls();

            bool resume = true;

            if (resume)
            {
                var p=new Dictionary<string,string>();
                {
                    p.CheckAdd("CHCA_ID", Id.ToString());                    
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Transport");
                q.Request.SetParam("Object", "Access");
                q.Request.SetParam("Action", "GetCar");
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
                        {
                            var ds=ListDataSet.Create(result,"ITEMS");
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

            EnableControls();
        }

        /// <summary>
        /// подготовка данных
        /// </summary>
        public async void Save()
        {
            bool resume = true;
            string error = "";

            //стандартная валидация данных средствами формы
            if (resume)
            {
                FromDate.Text = FromDate.Text.Replace('/', '.');
                FromDate.Text = FromDate.Text.Replace(' ', '.');

                var validationResult = Form.Validate();

                if (!validationResult)
                {
                    resume = false;
                }

                if (FromDate.Text == string.Empty)
                {

                }
                else
                {
                    DateTime result = DateTime.Now;

                    if (DateTime.TryParseExact(FromDate.Text, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                    {

                    }
                    else
                    {
                        Form.SetStatus(false, "Формат даты должен быть dd.MM.yyyy");
                        resume = false;

                        FromDate.BorderBrush = HColor.Red.ToBrush();
                    }

                }
            }



            //отправка данных
            if (resume)
            {
                DisableControls();
                var p = Form.GetValues();
                p.Add("CHCA_ID", Id.ToString());
                p.Add("ACCO_ID", Central.User.AccountId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Transport");
                q.Request.SetParam("Object", "Access");
                q.Request.SetParam("Action", "SaveCar");
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
                        //Если ответ не пустой, отправляем сообщение Гриду о необходимости обновить данные
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "Service",
                            ReceiverName = ReceiverName,
                            SenderName = "TemporaryCarPassRecord",
                            Action = "Refresh",
                            Message = $"{Id}",
                        });
                    }
                }
                else
                {
                    q.ProcessError();
                }

                Close();
                EnableControls();
            }
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
            if (Id == 0)
            {
                Num.IsEnabled = true;
                FromDate.IsEnabled = true;
                ToDate.IsEnabled = true;
                Description.IsEnabled = true;
            }
            else
            {
                Num.IsEnabled = false;
                FromDate.IsEnabled = false;
                ToDate.IsEnabled = true;
                Description.IsEnabled = false;
            }
        }

        private void CancelButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Save();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Num.Focus();
        }
    }
}
