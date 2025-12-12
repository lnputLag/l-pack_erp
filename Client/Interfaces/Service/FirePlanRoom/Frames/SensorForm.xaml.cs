using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Service
{
    /// <summary>
    /// карточка датчика
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    /// <released>2024-04-20</released>
    /// <changed>2024-05-02</changed>
    public partial class SensorForm : UserControl
    {
        public SensorForm()
        {
            FiasId = 0;
            FrameName = "Sensor";
            ReceiverName = "RoomList";
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();
            //            DataList = new List<Dictionary<string, string>>();

        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Имя вкладки, которая становится активной после закрытия фрейма
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// идентификатор записи, с которой работает форма
        /// (primary key записи таблицы FIAS_ID)
        /// </summary>
        public int FiasId { get; set; }

        /// <summary>
        /// идентификатор записи, из списка локаций
        /// (primary key записи таблицы FIAL_ID)
        /// </summary>
        public int FialId { get; set; }

        /// <summary>
        /// Описание датчиков (данные)
        /// </summary>
        public ListDataSet NoteDS { get; set; }

        /// <summary>
        /// Датасет, связывающий примечание для датчика с надписью
        /// </summary>
        public ListDataSet DataSetNamePlaneToNote { get; set; }

        /// <summary>
        /// полученные значение от запросов
        /// </summary>
        public List<Dictionary<string, string>> DataList { get; set; }

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
                    Path="FIAS_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FIAL_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FIAE_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Note,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange=(FormHelperField f, string v)=>
                    {
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "Service",
                        Object = "FireAlarm",
                        Action = "GetSensorRoom",
                        AnswerSectionKey="ITEMS",
                        OnComplete = (FormHelperField f,ListDataSet ds) =>
                        {
                          if (ds != null && ds.Items != null && ds.Items.Count > 0)
                            {
                                //Note.DropDownListBox.Items.Clear();
                                //Note.Items.Clear();
                                //Note.DropDownListBox.SelectedItem = null;
                                //Note.ValueTextBox.Text = "";
                                //Form.SetValueByPath("FIAE_ID", "");
                                //Note.IsEnabled = true;
                                Note.SetItems(ds, "FIAE_ID", "NOTE");
                                DataSetNamePlaneToNote = ds;
                            }
                        },
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
                    Path="X_COORDINATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=XCoordinate,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="Y_COORDINATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=YCoordinate,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="NAME_PLANE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control= NamePlane,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }, //не обязательно для заполнения
                },
                new FormHelperField()
                {
                    Path="ACTIVE_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=ActiveFlagCheckBox,
                    ControlType="CheckBox",
                    Enabled = true,
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
                  //фокус на поле ввода названия датчика
                  Name.Focus();
                  // Name.SelectAll();
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
                ReceiverGroup = "Room",
                ReceiverName = "RoomList",
                SenderName = "Sensor",
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
        public void Create(Dictionary<string, string> p)
        {
            FiasId = 0;
            FialId = p.CheckGet("FIAL_ID").ToInt();
            GetData();
        }

        /// <summary>
        /// редактирование записи
        /// </summary>
        /// <param name="p"></param>
        public void Edit(Dictionary<string, string> p)
        {
            FiasId = p.CheckGet("FIAS_ID").ToInt();
            FialId = p.CheckGet("FIAL_ID").ToInt();
            if (FiasId == 0)
            {
                XCoordinate.Text = p.CheckGet("X").ToString();
                YCoordinate.Text = p.CheckGet("Y").ToString();
            }
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
            Central.WM.FrameMode = 0;

            var frameName = GetFrameName();

            if (FiasId == 0)
            {
                Central.WM.Show(frameName, "Новый датчик", true, "add", this);
            }
            else
            {
                Central.WM.Show(frameName, $"Датчик {Name.Text}", true, "add", this);
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
            result = $"{FrameName}_{FiasId}";
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
                    p.CheckAdd("FIAS_ID", FiasId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Service");
                q.Request.SetParam("Object", "FireAlarm");
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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        Form.SetValues(ds);
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
            v.CheckAdd("FIAS_ID", FiasId.ToString());
            v.CheckAdd("FIAL_ID", FialId.ToString());
            if (Note.SelectedItem.Key.ToInt() > 0)
                v.CheckAdd("FIAE_ID", Note.SelectedItem.Key.ToInt().ToString());

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
        /// отправка данных на сервер
        /// </summary>
        public async void SaveData(Dictionary<string, string> p)
        {

            DisableControls();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Service");
            q.Request.SetParam("Object", "FireAlarm");
            q.Request.SetParam("Action", "Save");

            q.Request.SetParams(p);

            bool insert = p.CheckGet("FIAS_ID").ToInt() == 0;

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
                        var id = ds.GetFirstItemValueByKey("FIAS_ID").ToInt();

                        if (id != 0)
                        {
                            // Отправляем сообщение о необходимости обновить грид списка датчиков
                            {
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "Service",
                                    ReceiverName = "RoomList",
                                    SenderName = "SensorForm",
                                    Action = "Refresh",
                                    Message = id.ToString(),
                                }
                                );
                            }

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

        /// <summary>
        /// выбрали примечание к датчику
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void Note_SelectedItemChanged(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            //var msg = $"Выбрана запись {Note.ValueTextBox.Text}";
            //var q = new DialogWindow($"{msg}", "Отладка", "", DialogWindowButtons.NoYes);
            //if (q.ShowDialog() == true)
            //{
            //    NamePlane.Text = DataSetNamePlaneToNote.Items.FirstOrDefault(x => x.CheckGet("FIAE_ID") == Note.SelectedItem.Key).CheckGet("NAME_PLANE");
            //}

            NamePlane.Text = DataSetNamePlaneToNote.Items.FirstOrDefault(x => x.CheckGet("FIAE_ID") == Note.SelectedItem.Key).CheckGet("NAME_PLANE");
        }






    }
}
