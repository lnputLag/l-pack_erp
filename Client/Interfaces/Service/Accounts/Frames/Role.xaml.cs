using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Accounts
{
    /// <summary>
    /// редактирвоание роли пользователя
    /// </summary>
    /// <author>zelenskiy_sv</author>
    public partial class Role : UserControl
    {
        public Role()
        {
            Id = 0;
            FrameName = "Role";

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
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Name,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="ROGR_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=RoleGroup,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="CODE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Code,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.LatinOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="DESCRIPTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Description,
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
                //фокус на кнопку обновления
                Name.Focus();
                Name.SelectAll();
            };

        }

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
                SenderName = "Role",
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
            switch (e.Key)
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
        public void Edit(int id)
        {
            Id = id;
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

            var frameName = GetFrameName();

            if (Id == 0)
            {
                Central.WM.Show(frameName, "Новая роль", true, "add", this);
            }
            else
            {
                Central.WM.Show(frameName, $"Роль {Code.Text}", true, "add", this);
            }
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName = GetFrameName();
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
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID", Id.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Accounts");
                q.Request.SetParam("Object", "Role");
                q.Request.SetParam("Action", "Get");
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
                            var ds = ListDataSet.Create(result, "ROLE_GROUPS");
                            RoleGroup.SetItems(ds, "ID", "NAME");
                        }

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
        public async void SaveData(Dictionary<string, string> p)
        {
            DisableControls();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Accounts");
            q.Request.SetParam("Object", "Role");
            q.Request.SetParam("Action", "Save");

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
                        var id = ds.GetFirstItemValueByKey("ID").ToInt();

                        if (id != 0)
                        {
                            //отправляем сообщение гриду о необходимости обновить данные
                            //Messenger.Default.Send(new ItemMessage()
                            //{
                            //    ReceiverGroup = "Role",
                            //    SenderName = "RoleView",
                            //    Action = "Refresh",
                            //    Message = $"{id}",
                            //});
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverName = "RoleTab",
                                SenderName = "RoleView",
                                Action = "Refresh",
                                Message = $"{id}",
                            });

                            //отправляем сообщение гриду о необходимости обновить данные
                            //Messenger.Default.Send(new ItemMessage()
                            //{
                            //    ReceiverGroup = "Account",
                            //    SenderName = "RoleView",
                            //    Action = "Refresh",
                            //});
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverName = "AccountTab",
                                SenderName = "RoleView",
                                Action = "Refresh",
                            });

                            //отправляем сообщение главному окну о необходимости обновить меню
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "Main",
                                SenderName = "RoleView",
                                Action = "UpdateMenu",
                            });
                            //Central.Msg.SendMessage(new ItemMessage()
                            //{
                            //    ReceiverName = "Main",
                            //    SenderName = "RoleView",
                            //    Action = "UpdateMenu",
                            //});

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

        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Save();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Name.Focus();
        }
    }
}
