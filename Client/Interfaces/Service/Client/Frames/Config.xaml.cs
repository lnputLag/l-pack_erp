using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Service
{
    /// <summary>
    /// форма редактирования конфигурации
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-02-21</released>
    /// <changed>2023-02-21</changed>
    public partial class Config : UserControl
    {
        public Config()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
            Loaded += OnLoad;
            
            Init();
            SetDefaults();

            HostUserId = "";
            InstallationPlace = "";
            Mode = 0;
            FrameName = "Config";
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
        public string HostUserId { get; set; }
        /// <summary>
        /// место установки
        /// </summary>
        public string InstallationPlace { get; set; }
        /// <summary>
        /// 1=InstallationPlace,2=HostUserId
        /// </summary>
        public int Mode { get; set; }

        /// <summary>
        // инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>();

            if(Mode == 2) 
            {
                fields.Add(
                    new FormHelperField()
                    {
                        Path = "HOST_USER_ID",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = Hostname,
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    }
                );
            }

            if(Mode == 1)
            {
                fields.Add(
                    new FormHelperField()
                    {
                        Path = "INSTALLATION_PLACE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = InstallationPLace,
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    }
                );
            }

            {
                fields.Add(
                    new FormHelperField()
                    {
                        Path = "CONTENT",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = Content,
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        },
                    }
                );
            }   

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;

            //после установки значений
            Form.AfterSet = (Dictionary<string, string> v) =>
            {
                //фокус на кнопку обновления
                Hostname.Focus();
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
                ReceiverGroup = "Service",
                ReceiverName = "",
                SenderName = "Config",
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

        ///// <summary>
        ///// создание новой записи
        ///// </summary>
        //public void Create(string hostname="")
        //{
        //    HostUserId = "";
        //    SetDefaults();
        //    var v=new Dictionary<string,string>();
        //    v.CheckAdd("HOST_USER_ID",hostname);
        //    Form.SetValues(v);
        //    Show();
        //}

        /// <summary>
        /// редактирвоание записи
        /// </summary>
        /// <param name="hostname"></param>
        public void Edit()
        {
            //HostUserId = hostname;
            GetData();
        }
        
        /// <summary>
        /// удаление записи
        /// </summary>
        /// <param name="hostname"></param>
        public void Delete(string hostname)
        {
            HostUserId = hostname;

            var t="Удаление записи";
            var m = "";
            m=m.Append("Удалить конфигурацию?",true);
            m=m.Append($"Hostname=[{HostUserId}]",true);
            var d = "";
                
            var dialog = new DialogWindow(m, t, d, DialogWindowButtons.OKCancel);
            var dialogResult=(bool)dialog.ShowDialog();
            if(dialogResult)
            {
                DeleteData();
            }
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

            var frameTitle = "";
            if(Mode == 2)
            {
                frameTitle = $"{HostUserId}";
            }

            if(Mode == 1)
            {
                frameTitle = $"{InstallationPlace}";
            }

            Central.WM.Show(frameName, $"Конфиг {frameTitle}", true, "add", this);
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            var frameName=GetFrameName(); 
            Central.WM.SetActive(frameName);
        }

        /// <summary>
        /// формирует уникальный идентификатор фрейма
        /// </summary>
        /// <returns></returns>
        public string GetFrameName()
        {
            string result = "";
            if(Mode == 2)
            {
                result = $"{FrameName}_{HostUserId}";
            }

            if(Mode == 1)
            {
                result = $"{FrameName}_{InstallationPlace}";
            }

            result =result.MakeSafeName();
            return result;
        }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            DisableControls();

            bool resume = true;
            var v = new Dictionary<string, string>();

            
            

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    switch(Mode)
                    {
                        case 1:
                            {
                                v.CheckAdd("INSTALLATION_PLACE", InstallationPlace.ToString());
                                p.CheckAdd("INSTALLATION_PLACE", InstallationPlace.ToString());
                            }
                            break;

                        case 2:
                            {
                                v.CheckAdd("HOST_USER_ID", HostUserId.ToString());
                                p.CheckAdd("HOST_USER_ID", HostUserId.ToString());
                            }
                            break;
                    }
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Service");
                q.Request.SetParam("Object", "Control");
                q.Request.SetParam("Action", "GetConfig");
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
                            if(ds.Items.Count > 0)
                            {
                                foreach(Dictionary<string,string> row in ds.Items)
                                {
                                    if(Mode == 1)
                                    {
                                        if(row.CheckGet("INSTALLATION_PLACE") == InstallationPlace)
                                        {
                                            v = row;
                                            break;
                                        }
                                    }

                                    if(Mode == 2)
                                    {
                                        if(row.CheckGet("HOST_USER_ID") == HostUserId)
                                        {
                                            v = row;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            Form.SetValues(v);
            Show();

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
            q.Request.SetParam("Module", "Service");
            q.Request.SetParam("Object", "Control");
            q.Request.SetParam("Action", "SaveConfig");

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
                        var hostname = ds.GetFirstItemValueByKey("HOST_USER_ID").ToString();
                        var installationPlace = ds.GetFirstItemValueByKey("INSTALLATION_PLACE").ToString();
                        var complete = false;

                        if(!complete)
                        {
                            if(Mode == 2)
                            {
                                if(!hostname.IsNullOrEmpty())
                                {
                                    complete = true;
                                }
                            }
                        }

                        if(!complete)
                        {
                            if(Mode == 1)
                            {
                                if(!installationPlace.IsNullOrEmpty())
                                {
                                    complete = true;
                                }
                            }
                        }

                        if (complete)
                        {
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "ConfigList",
                                SenderName = "Config",
                                Action = "Refresh",
                                Message = $"{hostname}",
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
        /// удаление строки
        /// </summary>
        public async void DeleteData()
        {
            DisableControls();

            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("HOST_USER_ID", HostUserId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Service");
                q.Request.SetParam("Object", "Control");
                q.Request.SetParam("Action", "DeleteConfig");

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
                            var hostname = ds.GetFirstItemValueByKey("HOST_USER_ID").ToString();

                            if (!hostname.IsNullOrEmpty())
                            {
                                //отправляем сообщение гриду о необходимости обновить данные
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "ConfigList",
                                    SenderName = "Config",
                                    Action = "Refresh",
                                    Message = $"{hostname}",
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

    }
}
