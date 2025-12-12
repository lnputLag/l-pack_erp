using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Interaction logic for WarehouseCellTypeItem.xaml
    /// Интерфейс для редактирования и создания типов хранилища
    /// </summary>
    /// <author>eletskikh_ya</author>
    public partial class WarehouseCellTypeItem : UserControl
    {
        public WarehouseCellTypeItem()
        {
            Id = 0;
            FrameName = "WMSCellType";

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
        /// Обработчик сообщений
        /// </summary>
        /// <param name="obj"></param>
        private void ProcessMessages(ItemMessage obj)
        {

        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {


            Form = new FormHelper();
            
            FormHelper.ComboBoxInitHelper(Warehouse, "Warehouse", "Warehouse", "List", "WMWA_ID", "WAREHOUSE", null, true);

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="STORAGE_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TextName,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 32 },
                    },
                },
                new FormHelperField()
                {
                    Path="RACK_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=CheckPack,
                    ControlType = "CheckBox",
                },
                new FormHelperField()
                {
                    Path="DEPTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TextDepth,
                    ControlType="TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 6 }
                    }
                },
                new FormHelperField()
                {
                    Path="WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TextWidth,
                    ControlType="TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 6 }
                    }
                },

                 new FormHelperField()
                {
                    Path="WIDTH_MAX",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TextMaxWidth,
                    ControlType="TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 6 }
                    }
                },
                new FormHelperField()
                {
                    Path="HEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TextHeight,
                    ControlType="TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 6 }
                    }
                },
                new FormHelperField()
                {
                    Path="WEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=TextWeight,
                    ControlType="TextBox",
                    Format = ".###",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 12 },
                        { FormHelperField.FieldFilterRef.MaxValue, 999999 },

                    }
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
                    Path="BARCODE_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=CheckBarcode,
                    ControlType="CheckBox",
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;

            //после установки значений
            Form.AfterSet = (Dictionary<string, string> v) =>
            {
                //фокус на поле ввода логина
                TextName.Focus();
            };
        }

        /// <summary>
        /// Установки по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
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
                Central.WM.Show(frameName, "Новый тип ячейки", true, "add", this);
            }
            else
            {
                Central.WM.Show(frameName, $"Тип ячейки {Id}", true, "add", this);
            }

            if (Id != 0)
            {
                TextName.IsReadOnly = true;
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

            //ставим фокус на вкладку по умолчанию
            Central.WM.SetActive("CatalogCellType");
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
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;

            TextName.IsEnabled = true;
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;

            TextName.IsEnabled = false;
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
                    p.CheckAdd("WMSY_ID", Id.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "StorageType");
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
        /// Создаем новый тип хранилища
        /// </summary>
        public void Create()
        {
            Id = 0;
            GetData();
        }

        /// <summary>
        /// Редактировать тип хранилища
        /// </summary>
        public void Edit(int id)
        {
            Id = id;
            GetData();
        }

        /// <summary>
        /// отправка данных на сервер
        /// </summary>
        public void SaveData(Dictionary<string, string> p)
        {
            DisableControls();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "StorageType");
            q.Request.SetParam("Action", "Save");

            p.Add("WMSY_ID", Id.ToString());

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
                                ReceiverName = "WMS_cell_type",
                                SenderName = "WMSCellType",
                                Action = "Refresh",
                                Message = $"{id}",
                            });


                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverGroup = "WMS",
                                ReceiverName = "WarehouseCellType",
                                SenderName = "WMSCellType",
                                Action = "refresh",
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
