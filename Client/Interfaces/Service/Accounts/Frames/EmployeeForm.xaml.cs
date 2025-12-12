using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Accounts
{
    /// <summary>
    /// карточка сотрудника
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2022-06-02</released>
    /// <changed>2022-06-02</changed>
    /// <changed>zelenskiy_sv</changed>
    public partial class EmployeeForm : UserControl
    {
        public EmployeeForm()
        {
            Id = 0;
            FrameName = "User";

            InitializeComponent();
            Blocked = new CheckBox();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();
        }


        private CheckBox Blocked { get; set; }

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
        /// оригинальное значение поля LOCKED (до изменения)
        /// </summary>
        public bool? originalChecked { get; set; }

        /// <summary>
        /// должности (данные)
        /// </summary>
        public ListDataSet PositionDS { get; set; }

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
                    Path="ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ACCO_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DEPARTMENT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Department,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="POSITION_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Position,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SURNAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Surname,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Name,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="MIDDLE_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=MiddleName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="EMAIL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Email,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.LatinOnly, null },  
                    },
                },
                new FormHelperField()
                {
                    Path="INNER_PHONE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=InnerPhone,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null }
                    },
                },
                new FormHelperField()
                {
                    Path="MOBILE_PHONE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=MobilePhone,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null }
                    },
                },
                new FormHelperField()
                {
                    Path="LOCKED",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=Blocked,
                    ControlType="CheckBox",
                    Enabled = false,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;

            //после установки значений
            Form.AfterSet = (Dictionary<string, string> v) =>
              {
                  //фокус на поле ввода логина
                  Surname.Focus();
                  Surname.SelectAll();
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
                ReceiverGroup = "Accounts",
                ReceiverName = "",
                SenderName = "User",
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
        /// редактирвоание записи
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

            var shortName = Tools.FioToShortName(Surname.Text, Name.Text, MiddleName.Text);

            if (Id == 0)
            {
                Central.WM.Show(frameName, "Новый сотрудник", true, "add", this);
            }
            else
            {
                Central.WM.Show(frameName, $"Сотрудник {shortName}", true, "add", this);
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
                    p.CheckAdd("ID", Id.ToString());                    
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Accounts");
                q.Request.SetParam("Object", "User");
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
                        {
                            var ds=ListDataSet.Create(result,"DEPARTMENTS");
                            var row = new Dictionary<string, string>()
                            {
                                {"ID", "0" },
                                {"NAME", "" },
                            };
                            ds.ItemsPrepend(row);
                            Department.SetItems(ds, "ID", "NAME");                            
                        }
                        
                        {
                            var ds = ListDataSet.Create(result, "POSITIONS");
                            var row = new Dictionary<string, string>()
                            {
                                {"ID", "0" },
                                {"NAME", "" },
                            };
                            ds.ItemsPrepend(row);
                            var list = new Dictionary<string, string>();
                            Position.SetItems(ds, "ID", "NAME");
                        }

                        {
                            var ds=ListDataSet.Create(result,"ITEMS");
                            Form.SetValues(ds);
                        }

                        originalChecked = Blocked.IsChecked;

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
        public void Save()
        {
            bool resume = true;
            string error = "";

            //стандартная валидация данных средствами формы
            if (resume)
            {
                var validationResult = Form.Validate();
                if (!validationResult)
                {
                    resume = false;
                }
            }

            var v = Form.GetValues();

            //отправка данных
            if (resume)
            {
                // добавление параметров для создания аккаунта
                var p = GetAccount(v);

                v.AddRange(p);

                SaveData(v);
            }
            else
            {
                Form.SetStatus(error, 1);
            }
        }

        /// <summary>
        /// отпаравка данных на сервер
        /// </summary>
        public async void SaveData(Dictionary<string,string> p)
        {
            DisableControls();
            
            var q = new LPackClientQuery();
            q.Request.SetParam("Module","Accounts");
            q.Request.SetParam("Object","User");
            q.Request.SetParam("Action","Save");

            q.Request.SetParams(p);

            bool insert = p.CheckGet("ID").ToInt() == 0;
            
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
                        var id = ds.GetFirstItemValueByKey("ID").ToInt();
                        
                        if (id != 0)
                        {
                            if (insert)
                            {
                                // если сотрудник новый то необходимо его добавить вгруппу all code = 1
                                //FIXME: pq, ps, pd, pf, wtf?
                                var qg = new LPackClientQuery();
                                qg.Request.SetParam("Module", "Accounts");
                                qg.Request.SetParam("Object", "User");
                                qg.Request.SetParam("Action", "AddGroup");

                                var pg = new Dictionary<string, string>();

                                pg.CheckAdd("EMPL_ID", id.ToString());
                                pg.CheckAdd("WOGR_ID", "1");

                                qg.Request.SetParams(pg);

                                await Task.Run(() =>
                                {
                                    qg.DoQuery();


                                });

                                if (qg.Answer.Status == 0)
                                {
                                }
                            }

                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverName = "EmployeeTab",
                                Action = "Refresh",
                                Message= $"{id}",
                            });

                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverName = "AccountTab",
                                Action = "Refresh",
                                Message= $"{id}",
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

            EnableControls();
        }

        /// <summary>
        /// возвращает набор параметров для создания аккаунта
        /// </summary>
        private Dictionary<string, string> GetAccount(Dictionary<string, string> p)
        {
            var v = new Dictionary<string, string>();

            var shortName = Tools.FioToShortName(p["SURNAME"], p["NAME"], p["MIDDLE_NAME"]);
            var login = Tools.Translit(shortName.Replace(" ", "_").Replace(".", "").ToLower());
            var pwd = "1234";

            v.Add("LOGIN", login);
            v.Add("PASSWORD", pwd);
            v.Add("LOGIN_NAME", shortName);
            v.Add("NEED_ACCOUNT", "0");

            if (Id == 0)
            {
                var msg = $"Создать аккаунт для сотруднка {shortName} ?";

                var d = new DialogWindow($"{msg}", "Создание аккаунта", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() == true)
                {
                    v["NEED_ACCOUNT"] = "1";
                }
            }

            return v;
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
        }

        private void CancelButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Save();
        }

        private void Blocked_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            // запрет разблокировки, если изменение записи и была заблокирована
            if ((Id > 0) && (originalChecked == true))
                Blocked.IsChecked = true;
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Surname.Focus();
        }
    }
}
