using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Interaction logic for Warehouse.xaml
    /// Эта форма предназначенна для создания склада
    /// </summary>
    /// <author>eletskikh_ya</author>
    public partial class Warehouse : UserControl
    {
        public Warehouse()
        {
            FrameName = "Warehouse";

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();

            Id = 0;
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
        private int Id { get; set; }
       
        /// <summary>
        /// Установка 
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();

            var factorySelectBoxItems = new Dictionary<string, string>();
            factorySelectBoxItems.Add("1", "ЛИПЕЦК");
            factorySelectBoxItems.Add("2", "КАШИРА");
            FactorySelectBox.SetItems(factorySelectBoxItems);

            TextName.Focus();
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="WAREHOUSE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TextName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.ToUpperCase, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 32 },
                    },
                },
                new FormHelperField()
                {
                    Path = "FACTORY_ID",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = FactorySelectBox,
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;

            Form.BeforeSet = (Dictionary<string, string> values) =>
            {
                if (!string.IsNullOrEmpty(values.CheckGet("FACTORY_ID")))
                {
                    values.CheckAdd("FACTORY_ID", values.CheckGet("FACTORY_ID").ToInt().ToString());
                }
            };
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
                Central.WM.Show(frameName, "Новый склад", true, "add", this);
            }
            else
            {
                Central.WM.Show(frameName, $"Склад", true, "add", this);
            }

            if (Id != 0)
            {
                TextName.IsReadOnly = true;
                FactorySelectBox.IsEnabled = false;
            }
        }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            DisableControls();

            bool resume = Id != 0;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("WMWA_ID", Id.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Warehouse");
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
                            var ds = ListDataSet.Create(result, "ITEMS");
                            Form.SetValues(ds);
                        }

                        Show();

                        SaveButton.IsEnabled = false;
                    }
                }
                else
                {
                    q.ProcessError();
                }

            }
            else
            {
                Show();
            }

            EnableControls();
        }

       /// <summary>
       /// Редактирование склада
       /// </summary>
       /// <param name="id">код склада, если 0 то будет создан новый склад</param>
        public void Edit(int id = 0)
        {
            Id = id;
            GetData();
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
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "wms",
                ReceiverName = "",
                SenderName = GetFrameName(),
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
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
        /// отпаравка данных на сервер
        /// </summary>
        public void SaveData(Dictionary<string, string> p)
        {
            DisableControls();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Warehouse");
            q.Request.SetParam("Action", "Save");

            p.Add("WMWA_ID", Id.ToString());

            q.Request.SetParams(p);

            q.DoQuery();

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
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "WMS",
                                ReceiverName = "WMS_list",
                                SenderName = "Warehouse",
                                Action = "Refresh",
                                Message = $"{id}",
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
        /// Функция сохранения - обновления
        /// </summary>
        private void Save()
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
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(FocusManager.GetFocusedElement(this));
        }
    }
}
