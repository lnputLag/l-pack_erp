using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Production;
using Client.Interfaces.Stock._WaterhouseControl;
using Client.Interfaces.Stock.ForkliftDrivers.Windows;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.ServiceModel.Configuration;
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
    /// Интерфейс для работы с Операциями
    /// <author>eletskikh_ya</author>
    /// </summary>
    public partial class WarehouseOperationItem : UserControl
    {
        public WarehouseOperationItem()
        {
            FrameName = "WarehouseOperationItem";
            InitializeComponent();

            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();
        }

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
        public int OperationId { get; set; }

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

            ItemSelectBox.ShowDropdownButton.Visibility = Visibility.Collapsed;
            FromCellSelectBox.ShowDropdownButton.Visibility = Visibility.Collapsed;
            ToCellSelectBox.ShowDropdownButton.Visibility = Visibility.Collapsed;

            FormHelper.ComboBoxInitHelper(WarehouseSelectBox, "Warehouse", "Warehouse", "List", "WMWA_ID", "WAREHOUSE", null, true);
            FormHelper.ComboBoxInitHelper(TaskOperation, "Warehouse", "Operation", "ListOfType", "WMOT_ID", "OPERATION", null, true);
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
                    Path="WMOT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TaskOperation,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="WMIT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ItemSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="OPERATION_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=OperationName,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="DESCRIPTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=OperationDescription,
                    ControlType="TextBox",
                },
                new FormHelperField()
                {
                    Path="ACCO_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ForkliftDriverSelectBox,
                    ControlType="SelectBox",
                },
                new FormHelperField()
                {
                    Path="FROM_WMST_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=FromCellSelectBox,
                    ControlType="SelectBox",
                },
                new FormHelperField()
                {
                    Path="TO_WMST_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ToCellSelectBox,
                    ControlType="SelectBox",
                },
                new FormHelperField()
                {
                    Path="WMWA_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=WarehouseSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="WMZO_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ZoneSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="WMTA_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TaskSelectItem,
                    ControlType="SelectBox",
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
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;

            this.FrameName = $"{FrameName}_{OperationId}";
            if (OperationId == 0)
            {
                Central.WM.Show(FrameName, "Новая операция", true, "add", this);
            }
            else
            {
                GetData();
                Central.WM.Show(FrameName, "Редактирование операции " + OperationId, true, "add", this);
            }
        }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            DisableControls();

            bool resume = OperationId != 0;
            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("WMOP_ID", OperationId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Operation");
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
                            if (ds != null && ds.Items != null && ds.Items.Count > 0)
                            {
                                var dsFirstItem = ds.Items[0];
                                Form.SetValueByPath("WMOT_ID", dsFirstItem.CheckGet("WMOT_ID"));
                                Form.SetValueByPath("WMWA_ID", dsFirstItem.CheckGet("WMWA_ID"));
                                Form.SetValueByPath("WMZO_ID", dsFirstItem.CheckGet("WMZO_ID"));
                                ItemSelectBoxSelectItem(new KeyValuePair<string, string> (dsFirstItem.CheckGet("WMIT_ID") , dsFirstItem.CheckGet("NAME")));
                                Form.SetValueByPath("OPERATION_NAME", dsFirstItem.CheckGet("OPERATION_NAME"));
                                Form.SetValueByPath("DESCRIPTION", dsFirstItem.CheckGet("DESCRIPTION"));
                                Form.SetValueByPath("ACCO_ID", dsFirstItem.CheckGet("ACCO_ID").ToInt().ToString());
                                Form.SetValueByPath("FROM_WMST_ID", dsFirstItem.CheckGet("FROM_WMST_ID"));
                                Form.SetValueByPath("TO_WMST_ID", dsFirstItem.CheckGet("TO_WMST_ID"));
                                Form.SetValueByPath("WMTA_ID", dsFirstItem.CheckGet("WMTA_ID"));
                            }

                            if (ds.Items[0].CheckGet("ACCEPTED_DTTM").Length != 0)
                            {
                                // Переводим все контролы в режим только чтения и запрещаем кнопку
                                // сохранить и кнопки выбора ячеек
                                foreach (var item in FormHelper.FindLogicalChildren<TextBox>(this))
                                {
                                    item.IsEnabled = false;
                                }

                                SaveButton.IsEnabled = false;
                                FromCellButton.IsEnabled = false;
                                ToCellButton.IsEnabled = false;
                                ItemButton.IsEnabled = false;
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
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close(FrameName);
            Central.WM.SetActive("WarehouseOperation");

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
                SenderName = FrameName,
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// Очищаем наполнение селектбокса
        /// </summary>
        /// <param name="selectBox"></param>
        private void ClearSelectBox(SelectBox selectBox)
        {
            selectBox.DropDownListBox.Items.Clear();
            selectBox.DropDownListBox.SelectedItem = null;
            selectBox.ValueTextBox.Text = "";
            selectBox.Items = new Dictionary<string, string>();
            selectBox.SelectedItem = new KeyValuePair<string, string>();
        }

        /// <summary>
        /// Обновление данных при выборе склада
        /// </summary>
        public void SelectWarehouse()
        {
            ClearSelectBox(ZoneSelectBox);

            FormHelper.ComboBoxInitHelper(ZoneSelectBox, "Warehouse", "Zone", "ListByWarehouse", "WMZO_ID", "ZONE", new Dictionary<string, string>() { { "WMWA_ID", WarehouseSelectBox.SelectedItem.Key } }, true, true);
        }

        public void SelectZone()
        {
            ClearSelectBox(FromCellSelectBox);
            ClearSelectBox(ToCellSelectBox);
            ClearSelectBox(ForkliftDriverSelectBox);
            ClearSelectBox(TaskSelectItem);

            FormHelper.ComboBoxInitHelper(FromCellSelectBox, "Warehouse", "Storage", "ListByZone", "WMST_ID", "NUM", new Dictionary<string, string>() { { "WMZO_ID", ZoneSelectBox.SelectedItem.Key } }, true, true);
            FormHelper.ComboBoxInitHelper(ToCellSelectBox, "Warehouse", "Storage", "ListByZone", "WMST_ID", "NUM", new Dictionary<string, string>() { { "WMZO_ID", ZoneSelectBox.SelectedItem.Key } }, true, true);
            FormHelper.ComboBoxInitHelper(ForkliftDriverSelectBox, "Warehouse", "ForkliftDriver", "List", "ACCO_ID", "NAME", new Dictionary<string, string>() { { "WMZO_ID", ZoneSelectBox.SelectedItem.Key } }, true, true);
            FormHelper.ComboBoxInitHelper(TaskSelectItem, "Warehouse", "Task", "ListByZone", "WMTA_ID", "TASK_NAME", new Dictionary<string, string>() { { "WMZO_ID", ZoneSelectBox.SelectedItem.Key } }, true, true);
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
            q.Request.SetParam("Object", "Operation");
            q.Request.SetParam("Action", "Save");

            p.Add("WMOP_ID", OperationId.ToString());

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
                            var id = ds.GetFirstItemValueByKey("WMOP_ID").ToInt();
                            if (id != 0)
                            {
                                //отправляем сообщение гриду о необходимости обновить данные
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "WarehouseControl",
                                    ReceiverName = "WarehouseOperation",
                                    SenderName = "WMSOperation",
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

        private void WarehouseSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SelectWarehouse();
        }

        private void ZoneSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SelectZone();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }
























        private void ChangeOperationType()
        {
            var item = TaskOperation.SelectedItem;

            FromCellButton.IsEnabled = true;
            ToCellButton.IsEnabled = true;

            switch (item.Key)
            {
                // приход
                case "1":
                    FromCellButton.IsEnabled = FromCellSelectBox.IsEnabled = false;
                    ToCellButton.IsEnabled = ToCellSelectBox.IsEnabled = true;
                    break;
                // перемещение 
                case "2":
                    FromCellSelectBox.IsEnabled = true;
                    ToCellSelectBox.IsEnabled = true;
                    break;
                // списание
                case "3":
                    FromCellButton.IsEnabled = FromCellSelectBox.IsEnabled = false;
                    ToCellButton.IsEnabled = ToCellSelectBox.IsEnabled = true;
                    break;
            }

            if (!FromCellSelectBox.IsEnabled)
            {
                FromCellSelectBox.SelectedItem = new KeyValuePair<string, string>("0.0", "");
            }

            if (!ToCellSelectBox.IsEnabled)
            {
                ToCellSelectBox.SelectedItem = new KeyValuePair<string, string>("0.0", "");
            }

            ItemSelectBoxSelectItem(new KeyValuePair<string, string>("0", ""));
        }

        private void TaskOperation_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ChangeOperationType();
        }

        private void FromCellSelectButton_Click(object sender, RoutedEventArgs e)
        {
            var selectCell = new SelectCell();
            selectCell.WarehouseSelectBox.SelectedItem = WarehouseSelectBox.SelectedItem;
            selectCell.OnSelectedCell += SelectCell_OnFromSelectedCell;
            selectCell.Show();
        }

        private void SelectCell_OnFromSelectedCell(Dictionary<string, string> storageGridSelectedItem)
        {
            FromCellSelectBox.SelectedItem = new KeyValuePair<string, string>( storageGridSelectedItem.CheckGet("WMST_ID").Replace(".0",""), storageGridSelectedItem.CheckGet("NUM"));
        }

        private void SelectCell_OnToSelectedCell(Dictionary<string, string> storageGridSelectedItem)
        {
            ToCellSelectBox.SelectedItem = new KeyValuePair<string, string>(storageGridSelectedItem.CheckGet("WMST_ID").Replace(".0", ""), storageGridSelectedItem.CheckGet("NUM"));
        }

        private void ToCellSelectButton_Click(object sender, RoutedEventArgs e)
        {
            var selectCell = new SelectCell();
            selectCell.WarehouseSelectBox.SelectedItem = WarehouseSelectBox.SelectedItem;
            selectCell.OnSelectedCell += SelectCell_OnToSelectedCell;
            selectCell.Show();
        }

        private void SelectItemButton_Click(object sender, RoutedEventArgs e)
        {
            var selectItem = new SelectItem();
            selectItem.OnSelectItem += SelectItem_OnSelectItem;
            selectItem.Show();
        }

        private void ItemSelectBoxSelectItem(KeyValuePair<string, string> item)
        {
            if (ItemSelectBox.Items != null && ItemSelectBox.Items.Count > 0)
            {
                if (ItemSelectBox.Items.ContainsKey(item.Key))
                {
                    ItemSelectBox.SetSelectedItemByKey(item.Key);
                }
                else
                {
                    ItemSelectBox.Items.Add(item.Key, item.Value);
                    ItemSelectBox.SetSelectedItemByKey(item.Key);
                }
            }
            else
            {
                if (ItemSelectBox.Items == null)
                {
                    ItemSelectBox.Items = new Dictionary<string, string>();
                }

                ItemSelectBox.Items.Add(item.Key, item.Value);
                ItemSelectBox.SetSelectedItemByKey(item.Key);
            }
        }

        private void SelectItem_OnSelectItem(Dictionary<string, string> item)
        {
            ChangeOperationType();

            int operatioId = TaskOperation.SelectedItem.Key.ToInt();
            int storageId = item.CheckGet("WMST_ID").ToInt();

            if (storageId != 0)
            {
                FromCellSelectBox.SetSelectedItemByKey(storageId.ToString());
                FromCellSelectBox.IsEnabled = false;
            }

            ItemSelectBoxSelectItem(new KeyValuePair<string, string>(item.CheckGet("WMIT_ID").Replace(".0", ""), item.CheckGet("NAME")));
        }
    }
}
