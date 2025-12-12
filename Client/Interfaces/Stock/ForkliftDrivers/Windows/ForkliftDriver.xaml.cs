using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Stock.ForkliftDrivers.Windows
{
    /// <summary>
    /// Форма редактирования водителя погрузчика
    /// </summary>
    /// <author>Михеев И.С.</author>
    public partial class ForkliftDriver : UserControl
    {
        public ForkliftDriver()
        {
            InitializeComponent();

            Id = 0;
            ReturnTabName = "";

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            //регистрация обработчика клавиатуры
            PreviewKeyDown += ProcessKeyboard;

            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// Идентификатор водителя
        /// </summary>
        private int Id { get; set; }

        /// <summary>
        /// Форма редактирования водителя
        /// </summary>
        private FormHelper DriverForm { get; set; }

        /// <summary>
        /// Имя таба родителя.
        /// При закрытии текущей формы будет попытка установить активность таба родителя по этому имени.
        /// Сообщения через шину сообщений будут отправляться с этим именем объекта-получателя
        /// </summary>
        public string ReturnTabName { get; set; }

        /// <summary>
        /// Идентификатор плоащдки по умолчанию.
        /// При создании новой записи, если это значение заполнено, то будет выбрана соответсвтующая площадка в выпадающем списке FactorySelectBox
        /// </summary>
        public int DefaultFactoryId { get; set; }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            DriverForm.SetDefaults();
            FormStatus.Text = "";

            ForemanTypeListBox.Items = new Dictionary<string, string> { { "0", "нет" }, { "1", "на СГП" }, { "2", "в буфере заготовок" } }; ;
            ForemanTypeListBox.SelectedItem = ForemanTypeListBox.Items.FirstOrDefault(x => x.Key == "0");

            var factorySelectBoxItems = new Dictionary<string, string>();
            factorySelectBoxItems.Add("1", "Л-ПАК ЛИПЕЦК");
            factorySelectBoxItems.Add("2", "Л-ПАК КАШИРА");
            FactorySelectBox.SetItems(factorySelectBoxItems);
        }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        private void InitForm()
        {
            DriverForm = new FormHelper();
            //список колонок формы
            var fields = new List<FormHelperField>
            {
                new FormHelperField
                {
                    Path = "NAME",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = DriverName,
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 20 },
                    },
                },
                new FormHelperField
                {
                    Path = "PHONE",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = DriverPhone,
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField
                {
                    Path = "FOREMAN_FLAG",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = ForemanTypeListBox,
                    ControlType = "SelectBox",
                },
                new FormHelperField
                {
                    Path = "STOCK_PRODUCT_FLAG",
                    FieldType = FormHelperField.FieldTypeRef.Boolean,
                    Control = StockProductCheckBox,
                    ControlType = "CheckBox",
                },
                new FormHelperField
                {
                    Path = "STOCK_ROLL_FLAG",
                    FieldType = FormHelperField.FieldTypeRef.Boolean,
                    Control = StockRollCheckBox,
                    ControlType = "CheckBox",
                },
                new FormHelperField
                {
                    Path = "STOCK_WASTEPAPER_FLAG",
                    FieldType = FormHelperField.FieldTypeRef.Boolean,
                    Control = StockWastepaperCheckBox,
                    ControlType = "CheckBox",
                },
                new FormHelperField
                {
                    Path = "ARCHIVE_FLAG",
                    FieldType = FormHelperField.FieldTypeRef.Boolean,
                    Control = ArchiveCheckBox,
                    ControlType = "CheckBox",
                },
                new FormHelperField
                {
                    Path = "PASSWORD",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = Password,
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    }
                },
                new FormHelperField
                {
                    Path = "FACTORY_ID",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = FactorySelectBox,
                    ControlType = "SelectBox",
                },
            };

            DriverForm.SetFields(fields);
            DriverForm.OnValidate = (valid, message) =>
            {
                FormStatus.Text = valid ? "" : "Не все поля заполнены верно";
            };
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        /// <summary>
        /// Проверка и подготовка данных водителя для записи в БД
        /// </summary>
        private Dictionary<string, string> PrepareData()
        {
            var p = new Dictionary<string, string>();
            if (DriverForm.Validate())
            {
                p = DriverForm.GetValues();

                p.Add("ID", Id.ToString());
            }

            return p;
        }

        /// <summary>
        /// Сохранение данных водителя
        /// </summary>
        private async void Save()
        {
            var p = PrepareData();
            Toolbar.IsEnabled = false;
            
            if (p.Count > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Stock");
                q.Request.SetParam("Object", "ForkliftDriver");
                q.Request.SetParam("Action", "Save");

                q.Request.SetParams(p);

                await Task.Run(() => { q.DoQuery(); });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "List");
                        var id = ds.GetFirstItemValueByKey("ID").ToInt();

                        if (id != 0)
                        {
                            Messenger.Default.Send(new ItemMessage
                            {
                                ReceiverName = ReturnTabName,
                                SenderName = "ForkliftDriver",
                                Action = "Refresh",
                                Message = id.ToString(),
                            });

                            Close();
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            Toolbar.IsEnabled = true;
        }

        /// <summary>
        /// Создание формы для редактирования водителя
        /// </summary>
        /// <param name="id"></param>
        public void Edit(int id)
        {
            if (id != 0)
            {
                GetData(id);
            }
            else
            {
                if (DefaultFactoryId > 0)
                {
                    FactorySelectBox.SetSelectedItemByKey($"{DefaultFactoryId}");
                }
            }

            Show();
        }      

        /// <summary>
        /// Получение данных водителя
        /// </summary>
        /// <param name="id"></param>
        private async void GetData(int id)
        {
            Id = id;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "ForkliftDriver");
            q.Request.SetParam("Action", "Get");

            q.Request.SetParam("Id", Id.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (result.ContainsKey("ForkLiftDriver"))
                    {
                        var ds = result["ForkLiftDriver"];
                        ds?.Init();

                        if (id != 0)
                        {
                            DriverForm.SetValues(ds);

                            StockProductCheckBox_OnClick(this, null);
                        }
                    }

                    Show();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Отображение вкладки с формой редактирования данных водителя
        /// </summary>
        private void Show()
        {
            var title = $"Водитель {Id}";
            if (Id == 0)
            {
                title = "Новый водитель";
            }
            Central.WM.AddTab($"driverview_{Id}", title, true, "add", this);
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        private void Close()
        {
            Central.WM.RemoveTab($"driverview_{Id}");
            Destroy();
        }

        /// <summary>
        /// Деструктор компонентов. Завершает вспомогательные процессы
        /// </summary>
        private void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage
            {
                ReceiverGroup = "ShipmentControl",
                ReceiverName = "",
                SenderName = "ForkliftDriver",
                Action = "Closed",
            });

            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage
            {
                ReceiverName = ReturnTabName,
                SenderName = "ForkliftDriver",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            GoBack();
        }

        /// <summary>
        /// Возврат на фрейм, откуда был вызван данный фрейм
        /// </summary>
        private void GoBack()
        {
            if (!string.IsNullOrEmpty(ReturnTabName))
            {
                Central.WM.SetActive(ReturnTabName, true);
                ReturnTabName = "";
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m">Сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {
        }

        /// <summary>
        /// Обработчик нажатий клавиатуры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessKeyboard(object sender, KeyEventArgs e)
        {
            Central.Dbg($"TestUserView.OnKeyDown KEY:{e.Key}");
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;

                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Вызов справки
        /// </summary>
        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/shipments/control/listing#block3");
        }

        /// <summary>
        /// Если водитель архивный, не давать редактировать поля
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ArchiveCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            var status = !(ArchiveCheckBox.IsChecked != null && ArchiveCheckBox.IsChecked.Value);
            DriverName.IsEnabled = status;
            DriverPhone.IsEnabled = status;
            Password.IsEnabled = status;
            StockProductCheckBox.IsEnabled = status;
            StockRollCheckBox.IsEnabled = status;
            StockWastepaperCheckBox.IsEnabled = status;
            ForemanTypeListBox.IsEnabled = status && StockProductCheckBox.IsChecked == true;
        }

        private void StockProductCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            var status = !(ArchiveCheckBox.IsChecked != null && ArchiveCheckBox.IsChecked.Value);

            ForemanTypeListBox.IsEnabled = StockProductCheckBox.IsChecked == true && status;

            if (StockProductCheckBox.IsChecked == false)
            {
                ForemanTypeListBox.SelectedItem = ForemanTypeListBox.Items.FirstOrDefault(x => x.Key == "0");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
