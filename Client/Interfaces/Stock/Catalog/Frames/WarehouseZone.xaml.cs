using AutoUpdaterDotNET;
using Client.Common;
using Client.Interfaces.Shipments;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
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
    /// Interaction logic for WarehouseZone.xaml
    /// </summary>
    public partial class WarehouseZone : UserControl
    {
        public WarehouseZone()
        {
            InitializeComponent();

            Id = 0;
            FrameName = "WMSZone";

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
        /// идентификатор ячейки, с которой работает форма
        /// (primary key записи таблицы)
        /// </summary>
        private int Id { get; set; }


        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        private void PrepareDriverGroups()
        {
            //FormHelper.ComboBoxInitHelper(ForkliftDriverGroup, "Accounts", "Group", "List", "ID", "NAME", null, true);

            List<string> groups = new List<string>()
            {
                "driver_bdm_1",
                "driver_bdm_2",
                "driver_stock_stacker",
                "driver_container_scrap",
                "driver_container_stock"
            };
                
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Accounts");
            q.Request.SetParam("Object", "Group");
            q.Request.SetParam("Action", "List");

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var items = ListDataSet.Create(result, "ITEMS");
                    foreach (var item in items.Items)
                    {
                        if(groups.Contains(item.CheckGet("CODE")))
                        {
                            ForkliftDriverGroup.Items.Add(item.CheckGet("ID").ToInt().ToString(), item.CheckGet("NAME"));
                            ForkliftDriverGroup.UpdateListItems(ForkliftDriverGroup.Items);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            ForkliftDriverGroup.Items.Add("0", "Нет");
            FormHelper.ComboBoxInitHelper(Warehouse, "Warehouse", "Warehouse", "List", "WMWA_ID", "WAREHOUSE", null, true);
            PrepareDriverGroups();

            ForkliftDriverGroup.SelectedItem = new KeyValuePair<string, string>("0", "Нет");
            

            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ZONE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TextName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        //{ FormHelperField.FieldFilterRef.AlphaDigitOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 32 },
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
                    Path="WOGR_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ForkliftDriverGroup,
                    ControlType="SelectBox",
                },
                new FormHelperField()
                {
                    Path="BARCODE_STORAGE_USAGE_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=CheckBarcodeStorage,
                    ControlType="CheckBox",
                },
                new FormHelperField()
                {
                    Path="BARCODE_ITEM_USAGE_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=CheckBarcodeItem,
                    ControlType="CheckBox",
                },
                new FormHelperField()
                {
                    Path="CODE_ITEM_VERIFY_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=CheckVerifyCode,
                    ControlType="CheckBox",
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;
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
        /// Редактировать зону
        /// </summary>
        /// <param name="RowId"></param>
        public void Edit(int RowId)
        {
            Id = RowId;
            GetData();
        }

        internal void Create(Dictionary<string,string> warehouse)
        {
            if (warehouse != null)
            {
                Warehouse.SelectedItem = new KeyValuePair<string, string>(warehouse.CheckGet("WMWA_ID").ToInt().ToString(), warehouse.CheckGet("WAREHOUSE"));
            }

            Id = 0;
            GetData();
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
                    p.CheckAdd("WMZO_ID", Id.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Zone");
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

                            //SaveButton.IsEnabled = false;
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
                Central.WM.Show(frameName, "Новая зона", true, "add", this);
            }
            else
            {
                Central.WM.Show(frameName, $"Зона {Id}", true, "add", this);
            }

            if (Id != 0)
            {
                //TextName.IsReadOnly = true;
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
            q.Request.SetParam("Object", "Zone");
            q.Request.SetParam("Action", "Save");

            p.Add("WMZO_ID", Id.ToString());

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
                                SenderName = "WMSZone",
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
    }
}
