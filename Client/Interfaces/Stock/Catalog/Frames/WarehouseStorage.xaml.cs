using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
using Client.Interfaces.Shipments;
using Client.Interfaces.Stock._WaterhouseControl;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using static Client.Common.FormHelperField;


namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Интерфейс редактирования и создания хранилища
    /// <author>eletskikh_ya</author>
    /// </summary>
    public partial class WarehouseStorage : ControlBase
    {
        public WarehouseStorage()
        {
            ControlTitle = "Редактирование хранилища";
            DocumentationUrl = "/doc/l-pack-erp/";
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                Init();
                SetDefaults();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
            };
        }

        /// <summary>
        /// Разграничитель для наименования хранилища
        /// </summary>
        private string Delimiter { get; set; }

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
        /// (primary key записи таблицы wms_storage)
        /// </summary>
        public int StorageId { get; set; }


        public bool FromLevel { get; internal set; }

        /// <summary>
        /// Датасет с данными по всем складам WMS
        /// </summary>
        public ListDataSet WarehouseDataSet { get; set; }

        public int _WarehouseId { get; set; }
        public int _ZoneId { get; set; }
        public int _RowId { get; set; }
        public int _CellId { get; set; }
        public int _LevelId { get; set; }

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
                    Path="NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=StorageNameTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 16 },
                    },
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
                    Path="WMRO_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=RowSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="WMCE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CellSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="WMLE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=LevelSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="WMSY_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=StorageTypeSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="WMSA_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=StorageAreaSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRIORITY",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=StoragePriorityTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRIORITY_FIXED_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=StorageFixedPriorityCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="WMSS_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                    Control=null,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="WMST_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                    Control=null,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="RESERVED_WMIT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                    Control=null,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;
        }

        public void SetDefaults()
        {
            WarehouseDataSet = new ListDataSet();

            BarcodeBorder.Visibility = Visibility.Collapsed;
            PrintButton.IsEnabled = false;

            Form.SetDefaults();
            GetWarehouseList();

            if (StorageId > 0)
            {
                GetData();

                WarehouseSelectBox.IsReadOnly = true;
                ZoneSelectBox.IsReadOnly = true;
                RowSelectBox.IsReadOnly = true;
                CellSelectBox.IsReadOnly = true;
                LevelSelectBox.IsReadOnly = true;
            }
            else
            {
                if (_WarehouseId > 0)
                {
                    Form.SetValueByPath("WMWA_ID", $"{_WarehouseId}");
                }

                if (_ZoneId > 0)
                {
                    Form.SetValueByPath("WMZO_ID", $"{_ZoneId}");
                }

                if (_RowId > 0)
                {
                    Form.SetValueByPath("WMRO_ID", $"{_RowId}");
                }

                if (_CellId > 0)
                {
                    Form.SetValueByPath("WMCE_ID", $"{_CellId}");
                }

                if (_LevelId > 0)
                {
                    Form.SetValueByPath("WMLE_ID", $"{_LevelId}");
                }
            }
        }

        /// <summary>
        /// Получаем список всех складов WMS для заполнения выпадающего списка складов
        /// </summary>
        public void GetWarehouseList()
        {
            var p = new Dictionary<string, string>();
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Warehouse");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    WarehouseDataSet = ListDataSet.Create(result, "ITEMS");
                    WarehouseSelectBox.SetItems(WarehouseDataSet, FieldTypeRef.Integer, "WMWA_ID", "WAREHOUSE");
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            DisableControls();

            bool resume = StorageId != 0;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("WMST_ID", StorageId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Storage");
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

                        ShowLabel(Form.GetValueByPath("NUM"), Form.GetValueByPath("WMST_ID"));
                        PrintButton.IsEnabled = true;
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
        /// Отображение этикетки существующего хранилища
        /// </summary>
        /// <param name="storageNumber"></param>
        /// <param name="storageId"></param>
        private void ShowLabel(string storageNumber, string storageId)
        {
            if (Document != null)
            {
                BarcodeBorder.Visibility = Visibility.Visible;
                BarcodeGenerator generator = new BarcodeGenerator();
                generator.AddStorage(storageNumber, storageId);
                ScrollViewer.Document = generator.GenerateDocument();
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
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

            this.FrameName = $"{FrameName}_{StorageId}";
            if (StorageId == 0)
            {
                Central.WM.Show(FrameName, "Новая ячейка", true, "add", this);
            }
            else
            {
                Central.WM.Show(FrameName, $"Ячейка {StorageId}", true, "add", this);
            }
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close(FrameName);

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
                SenderName = "WarehouseStorage",
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

        /// <summary>
        /// Формирование наименования хранилища
        /// </summary>
        private void GenerateName()
        {
            var storageNum = "";
            if (!string.IsNullOrEmpty(RowSelectBox.SelectedItem.Value))
            {
                storageNum = RowSelectBox.SelectedItem.Value;
                if (!string.IsNullOrEmpty(CellSelectBox.SelectedItem.Value))
                {
                    storageNum += Delimiter + CellSelectBox.SelectedItem.Value;
                    if (!string.IsNullOrEmpty(LevelSelectBox.SelectedItem.Value))
                    {
                        storageNum += Delimiter + LevelSelectBox.SelectedItem.Value;
                    }
                }
            }

            StorageNameTextBox.Text = storageNum;
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
            ClearSelectBox(StorageTypeSelectBox);
            ClearSelectBox(StorageAreaSelectBox);
            ClearSelectBox(RowSelectBox);
            ClearSelectBox(CellSelectBox);
            ClearSelectBox(LevelSelectBox);

            Delimiter = WarehouseDataSet.Items.FirstOrDefault(x => x.CheckGet("WMWA_ID").ToInt() == WarehouseSelectBox.SelectedItem.Key.ToInt()).CheckGet("DELIMITER");

            FormHelper.ComboBoxInitHelper(ZoneSelectBox, "Warehouse", "Zone", "ListByWarehouse", "WMZO_ID", "ZONE", new Dictionary<string, string>() { { "WMWA_ID", WarehouseSelectBox.SelectedItem.Key } }, true, true);
            FormHelper.ComboBoxInitHelper(StorageTypeSelectBox, "Warehouse", "StorageType", "ListByWarehouse", "WMSY_ID", "STORAGE_TYPE", new Dictionary<string, string>() { { "WMWA_ID", WarehouseSelectBox.SelectedItem.Key } }, true, true);
            FormHelper.ComboBoxInitHelper(StorageAreaSelectBox, "Warehouse", "StorageArea", "ListByWarehouse", "WMSA_ID", "AREA", new Dictionary<string, string>() { { "WMWA_ID", WarehouseSelectBox.SelectedItem.Key } }, true, true);
            FormHelper.ComboBoxInitHelper(RowSelectBox, "Warehouse", "Row", "ListByWarehouse", "WMRO_ID", "ROW_NUM", new Dictionary<string, string>() { { "WMWA_ID", WarehouseSelectBox.SelectedItem.Key } }, true, true);
            FormHelper.ComboBoxInitHelper(CellSelectBox, "Warehouse", "Cell", "ListByWarehouse", "WMCE_ID", "CELL_NUM", new Dictionary<string, string>() { { "WMWA_ID", WarehouseSelectBox.SelectedItem.Key } }, true, true);
            FormHelper.ComboBoxInitHelper(LevelSelectBox, "Warehouse", "Level", "ListByWarehouse", "WMLE_ID", "LEVEL_NUM", new Dictionary<string, string>() { { "WMWA_ID", WarehouseSelectBox.SelectedItem.Key } }, true, true);
        }

        /// <summary>
        /// Сохраняем данные по хранилищу
        /// </summary>
        private void Save()
        {
            bool resume = true;
            string error = "";
            Dictionary<string, string> formData = Form.GetValues();

            //стандартная валидация данных средствами формы
            if (resume)
            {
                var validationResult = Form.Validate();
                if (!validationResult)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                formData = Form.GetValues();

                if (StorageId > 0)
                {
                    //отправка данных
                    SaveData(formData);
                }
                else
                {
                    resume = CheckStorage(formData);

                    if (resume)
                    {
                        //отправка данных
                        SaveData(formData);
                    }
                    else
                    {
                        Form.SetStatus("Ячейка с данными параметрами существует", 1);
                    }
                }
            }
            else
            {
                Form.SetStatus(error, 1);
            }
        }

        /// <summary>
        /// Проверяем существование такого хранилища
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private bool CheckStorage(Dictionary<string, string> param)
        {
            bool result = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Storage");
            q.Request.SetParam("Action", "Check");

            q.Request.SetParams(param);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var resultData = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (resultData != null)
                {

                    var ds = ListDataSet.Create(resultData, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        if (ds.Items.First().CheckGet("CNT").ToInt() == 0)
                        {
                            result = true;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// отпаравка данных на сервер
        /// </summary>
        public void SaveData(Dictionary<string, string> p)
        {
            DisableControls();

            if (StorageId == 0)
            {
                p.CheckAdd("WMSS_ID", "2");
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Storage");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
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
                            SenderName = "WarehouseStorage",
                            Action = "Refresh",
                            Message = $"{id}",
                            ContextObject = FromLevel
                        });

                        Close();
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
        /// Печать этикетки хранилища
        /// </summary>
        private void PrintLabel(string storageNumber, string storageId)
        {
            BarcodeGenerator generator = new BarcodeGenerator();
            generator.AddStorage(storageNumber, storageId);
            var doc = generator.GenerateDocument();
            var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;

            PrintDocument(paginator);
        }

        public void PrintDocument(DocumentPaginator documentPaginator)
        {
            var printHelper = new PrintHelper();
            printHelper.PrintingProfile = PrintingSettings.RawLabelPrinter.ProfileName;
            printHelper.PrintingDuplex = System.Drawing.Printing.Duplex.Simplex;
            printHelper.PrintingLandscape = true;
            printHelper.Init();
            var printingResult = printHelper.StartPrinting(documentPaginator);
            printHelper.Dispose();
        }

        /// <summary>
        /// Настройка принтера
        /// </summary>
        public void SetPrintSettings()
        {
            var i = new PrintingInterface();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Warehouse_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SelectWarehouse();
        }

        private void GenerateNameFromSelectBox(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GenerateName();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            PrintLabel(Form.GetValueByPath("NUM"), Form.GetValueByPath("WMST_ID"));
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void BurgerPrintSettings_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
