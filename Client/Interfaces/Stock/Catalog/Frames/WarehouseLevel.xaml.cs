using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Interaction logic for WMSLevel.xaml
    /// Интерфейс для создания и редактирования Ярусов
    /// </summary>
    /// <author>eletskikh_ya</author>
    public partial class WarehouseLevel : UserControl
    {
        public WarehouseLevel()
        {
            Id = 0;
            FrameName = "WMSLevel";

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();
        }

        /// <summary>
        /// Данные по складу
        /// </summary>
        private Dictionary<string, string> warehouseItem;

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
        /// идентификатор ячейки, с которой работает форма
        /// (primary key записи таблицы)
        /// </summary>
        private int Id { get; set; }


        public void SetDefaults()
        {
            Form.SetDefaults();
        }

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
                    Path="LEVEL_NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TextName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 8 },
                    },
                },
                new FormHelperField()
                {
                    Path="WMWA_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Warehouse,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="ORDER_NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TextOrder,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },

                },
                new FormHelperField()
                {
                    Path="PRIORITY",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TextPriority,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;

            //после установки значений
            Form.AfterSet = (Dictionary<string, string> v) =>
            {
                //фокус на поле ввода
                TextName.Focus();
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
                Central.WM.Show(frameName, "Новый ярус", true, "add", this);
            }
            else
            {
                TextName.IsReadOnly = true;
                Central.WM.Show(frameName, $"Ярус {Id}", true, "add", this);
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
        /// Редактировать ряд
        /// </summary>
        /// <param name="levelId"></param>
        public void Edit(int levelId, Dictionary<string, string> warehouse)
        {
            Id = levelId;
            warehouseItem = warehouse;
            FormHelper.ComboBoxInitHelper(Warehouse, "Warehouse", "Warehouse", "List", "WMWA_ID", "WAREHOUSE", null, true, true);
            Warehouse.SelectedItem = Warehouse.Items.FirstOrDefault(x => x.Key == warehouseItem.CheckGet("WMWA_ID").ToInt().ToString());
            
            GetData();
        }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            DisableControls();

            if (Id != 0)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("WMLE_ID", Id.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Level");
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
            else
            {
                Show();
            }

            EnableControls();
        }

        /// <summary>
        /// создание нового уровня
        /// </summary>
        public void Create(Dictionary<string,string>warehouse)
        {
            warehouseItem = warehouse;
            FormHelper.ComboBoxInitHelper(Warehouse, "Warehouse", "Warehouse", "List", "WMWA_ID", "WAREHOUSE", null, true, true);
            Warehouse.SelectedItem = Warehouse.Items.FirstOrDefault(x => x.Key == warehouseItem.CheckGet("WMWA_ID").ToInt().ToString());

            Id = 0;
            GetData();
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

        /// <summary>
        /// Сохранение данных
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
        public void SaveData(Dictionary<string, string> p)
        {
            DisableControls();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Level");
            q.Request.SetParam("Action", "Save");

            p.Add("WMLE_ID", Id.ToString());

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
                                SenderName = "WMSLevel",
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
            TextName.Focus();
        }
    }
}
