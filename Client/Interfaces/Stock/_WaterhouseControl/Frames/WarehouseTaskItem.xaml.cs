using Client.Common;
using Client.Interfaces.Production;
using Client.Interfaces.Stock._WaterhouseControl;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// Interaction logic for WarehouseTaskItem.xaml
    /// Интерфейс для работы с задачами склада
    /// <autor>eletskikh_ya</autor>
    /// </summary>
    public partial class WarehouseTaskItem : UserControl
    {
        public WarehouseTaskItem()
        {
            FrameName = "WarehouseTaskItem";
            InitializeComponent();

            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();
        }

        private bool DataReady = true;

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }


        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// идентификатор ячейки, с которой работает форма
        /// (primary key записи таблицы)
        /// </summary>
        private int Id { get; set; }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }

        /// <summary>
        /// ингициалищация компонентов формы
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();
            //FormHelper.ComboBoxInitHelper(ForkliftDriver, "Warehouse", "ForkliftDriver", "List", "EMPL_ID", "NAME", new Dictionary<string, string>() { { "ID", "1" } });
            //FormHelper.ComboBoxInitHelper(ForkliftDriver, "Warehouse", "ForkliftDriver", "List", "EMPL_ID", "NAME", new Dictionary<string, string>() { { "ID", "2" } });

            FormHelper.ComboBoxInitHelper(ForkliftDriver, "Warehouse", "ForkliftDriver", "ListAll", "ACCO_ID", "NAME");


            FormHelper.ComboBoxInitHelper(TaskType, "Warehouse", "Task", "ListType", "WMTT_ID", "TASK");


            //Warehouse.Items.Add("-1", "Все");
            //FormHelper.ComboBoxInitHelper(Warehouse, "Warehouse", "Warehouse", "List", "WMWA_ID", "WAREHOUSE", null, true);
            //FormHelper.ComboBoxInitHelper(Warehouse, "Warehouse", "Warehouse", "List", "WMWA_ID", "WAREHOUSE");


            FormHelper.ComboBoxInitHelper(FromCell, "Warehouse", "Storage", "List", "WMST_ID", "NUM", null, true);
            FormHelper.ComboBoxInitHelper(ToCell, "Warehouse", "Storage", "List", "WMST_ID", "NUM", null, true);

        
            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="WMTT_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TaskType,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="TASK_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TaskName,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="DESCRIPTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TaskDescription,
                    ControlType="TextBox",
                },
                new FormHelperField()
                {
                    Path="ACCO_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ForkliftDriver,
                    ControlType="SelectBox",
                },
                new FormHelperField()
                {
                    Path="OUTER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=OuterID,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        
                    },
                },
                new FormHelperField()
                {
                    Path="FROM_WMST_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=FromCell,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        
                    },
                },
                new FormHelperField()
                {
                    Path="TO_WMST_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ToCell,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        
                    },
                },
                //new FormHelperField()
                //{
                //    Path="WMWA_ID",
                //    FieldType=FormHelperField.FieldTypeRef.Integer,
                //    Control=Warehouse,
                //    ControlType="SelectBox",
                //    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                //        { FormHelperField.FieldFilterRef.Required, null },
                //    },
                //},
                new FormHelperField()
                {
                    Path="WMZO_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Zone,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;
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
                    p.CheckAdd("WMTA_ID", Id.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Task");
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
                        DataReady = false;

                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            Form.SetValues(ds);


                            string completedDate = ds.Items[0].CheckGet("COMPLETED_DTTM");
                            string aceptedDate = ds.Items[0].CheckGet("ACCEPTED_DTTM");

                            if (aceptedDate == string.Empty && completedDate == string.Empty)
                            {
                                

                            }
                            else
                            {
                                // Переводим все контролы в режим только чтения и запрещаем кнопку
                                // сохранить и кнопки выбора ячеек

                                foreach (var item in FormHelper.FindLogicalChildren<TextBox>(this))
                                {
                                    item.IsEnabled = false;
                                }

                                SaveButton.IsEnabled = false;
                                FromCellButton.IsEnabled = false;
                                ToCellButton.IsEnabled= false;
                            }
                        }

                        Show();

                        DataReady = true;
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
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;

            FrameName = GetFrameName();

            if (Id == 0)
            {
                Central.WM.Show(FrameName, "Новая Задача", true, "add", this);
            }
            else
            {
                Central.WM.Show(FrameName, "Редактирование задачи " + Id, true, "edit", this);
                Central.WM.SetActive(FrameName);
            }
        }


        /// <summary>
        /// формирует уникальный идентификатор фрейма
        /// </summary>
        /// <returns></returns>
        public string GetFrameName()
        {
            string result = "";

            result = $"Задача_{Id}";

            return result;
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close(FrameName);
            Central.WM.SetActive("WarehouseTask");

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
        /// Сохранение изменений
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


            {
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
        }

        /// <summary>
        /// отпаравка данных на сервер
        /// </summary>
        public void SaveData(Dictionary<string, string> p)
        {
            DisableControls();

            bool resume = true;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Task");
            q.Request.SetParam("Action", "Save");

            p.Add("WMTA_ID", Id.ToString());

            if (resume)
            {
                q.Request.SetParams(p);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            var id = ds.GetFirstItemValueByKey("WMTA_ID").ToInt();
                            if (id != 0)
                            {
                                //отправляем сообщение гриду о необходимости обновить данные
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "WarehouseControl",
                                    ReceiverName = "WarehouseTask",
                                    SenderName = "WMSTask",
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
            }

            EnableControls();
        }



        public void Edit(int id=0)
        {
            Id = id;
            FormHelper.ComboBoxInitHelper(Zone, "Warehouse", "Zone", "List", "WMZO_ID", "ZONE_FULL_NAME", null, true);

            GetData();
        }

        private void FromCellSelectButton_Click(object sender, RoutedEventArgs e)
        {
            var selectCell = new SelectCell();
            //selectCell.Warehouse.SelectedItem = Warehouse.SelectedItem;
            selectCell.OnSelectedCell += SelectCell_OnFromSelectedCell;
            selectCell.Show();
        }

        private void SelectCell_OnFromSelectedCell(Dictionary<string, string> storageGridSelectedItem)
        {
            FromCell.SelectedItem = new KeyValuePair<string, string>(storageGridSelectedItem.CheckGet("WMST_ID").Replace(".0", ""), storageGridSelectedItem.CheckGet("NUM"));
        }

        private void SelectCell_OnToSelectedCell(Dictionary<string, string> storageGridSelectedItem)
        {
            ToCell.SelectedItem = new KeyValuePair<string, string>(storageGridSelectedItem.CheckGet("WMST_ID").Replace(".0", ""), storageGridSelectedItem.CheckGet("NUM"));
        }

        private void ToCellSelectButton_Click(object sender, RoutedEventArgs e)
        {
            var selectCell = new SelectCell();
            //selectCell.Warehouse.SelectedItem = Warehouse.SelectedItem;
            selectCell.OnSelectedCell += SelectCell_OnToSelectedCell;
            selectCell.Show();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void Warehouse_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (DataReady)
            {
                //Warehouse.SelectedItem = new KeyValuePair<string, string>("0", "");
                FromCell.SelectedItem = new KeyValuePair<string, string>("0", "");
                ToCell.SelectedItem = new KeyValuePair<string, string>("0", "");
                ForkliftDriver.SelectedItem = new KeyValuePair<string, string>("0", "");

                //int warehouseId = Warehouse.SelectedItem.Key.ToInt();

                //if (warehouseId != 0)
                {
                    ForkliftDriver.Items.Clear();// = new Dictionary<string, string>();
                    Zone.Items.Clear();
                    FormHelper.ComboBoxInitHelper(ForkliftDriver, "Warehouse", "ForkliftDriver", "List", "ACCO_ID", "NAME");
                    //FormHelper.ComboBoxInitHelper(Zone, "Warehouse", "Zone", "ListByWarehouse", "WMZO_ID", "ZONE", new Dictionary<string, string>() { { "WMWA_ID", warehouseId.ToString() } }, true);
                    FormHelper.ComboBoxInitHelper(Zone, "Warehouse", "Zone", "List", "WMZO_ID", "ZONE_FULL_NAME", null, true);
                }
            }
        }

        private void Zone_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ForkliftDriver.Items.Clear();// = new Dictionary<string, string>();
            FormHelper.ComboBoxInitHelper(ForkliftDriver, "Warehouse", "ForkliftDriver", "List", "ACCO_ID", "NAME", new Dictionary<string, string>() { { "WMZO_ID", Zone.SelectedItem.Key } });
        }
    }
}
