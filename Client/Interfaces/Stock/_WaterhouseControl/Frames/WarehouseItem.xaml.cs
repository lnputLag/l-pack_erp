using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Service.Printing;
using Client.Interfaces.Stock._WaterhouseControl;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Office.Interop.Excel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Interaction logic for WMSItem.xaml
    /// Интерфейс работы с ТМЦ
    /// </summary>
    /// <author>eletskikh_ya</author>
    public partial class WarehouseItem : UserControl
    {
        public WarehouseItem()
        {
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
        /// идентификатор ячейки, с которой работает форма
        /// (primary key записи таблицы)
        /// </summary>
        private int Id { get; set; }

        /// <summary>
        /// 1- State на поступление
        /// </summary>
        private int ItemState = 1;
        private int ItemStorageId = 0;

        private ListDataSet InventoryItemDataSet { get; set; }

        /// <summary>
        /// ингициалищация компонентов формы
        /// </summary>
        public void SetDefaults()
        {
            InventoryItemDataSet = new ListDataSet();

            Form.SetDefaults();

            ListInventoryItem();
            ItemNameSelectBox.SetItems(InventoryItemDataSet, "WMII_ID", "NAME");

            //FormHelper.ComboBoxInitHelper(ItemNameSelectBox, "Warehouse", "Inventory", "List", "WMII_ID", "NAME");
            FormHelper.ComboBoxInitHelper(WarehouseSelectBox, "Warehouse", "Warehouse", "List", "WMWA_ID", "WAREHOUSE", null, true, true);
            FormHelper.ComboBoxInitHelper(ItemGroup, "Warehouse", "Item", "ListGroup", "WMIG_ID", "GROUP_NAME");
            FormHelper.ComboBoxInitHelper(ItemUnit, "Warehouse", "Item", "ListUnit", "UNIT_ID", "SHORT_NAME");
        }

        public void ListInventoryItem()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Inventory");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    InventoryItemDataSet = ds;
                }
            }
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            // при добавлении всегда ставим тип На поступление, типы находятся в таблице WMS_ITEM_STATE_REF
            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="WMII_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ItemNameSelectBox,
                    ControlType="SelectBox",
                    Validate = (f,v)=>
                    {
                        if (OldPositionRadioButton.IsChecked == true)
                        {
                            if (ItemNameSelectBox.SelectedItem.Key.ToInt() == 0)
                            {
                                f.ValidateResult = false;
                                f.ValidateProcessed = true;

                                f.ValidateMessage = "Необходимо задать номенклатуру";
                            }
                        }
                    }
                },
                new FormHelperField()
                {
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ItemNameTextBox,
                    ControlType="TextBox",
                    Validate = (f,v)=>
                    {
                        if (!string.IsNullOrEmpty(ItemNameTextBox.Text))
                        {
                            if (ItemNameTextBox.Text.Length < 2 || ItemNameTextBox.Text.Length > 128)
                            {
                                f.ValidateResult = false;
                                f.ValidateProcessed = true;

                                f.ValidateMessage = "Наименование ТМЦ должно быть длинной от 2 до 128 символов";
                            }
                        }
                        else
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;

                            f.ValidateMessage = "Необходимо задать наименование";
                        }
                    }
                },
                new FormHelperField()
                {
                    Path="WMIG_ID",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=ItemGroup,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
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
                    Path="BATCHQTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TextBatchQty,
                    ControlType="TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 6 }
                    }
                },
                new FormHelperField()
                {
                    Path="QTY",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=TextQty,
                    ControlType="TextBox",
                    Format = "0.##",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 6 }
                    },

                    Validate = (f,v)=>
                    {
                        if(!string.IsNullOrEmpty(TextQty.Text))
                        {

                        }
                        else
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Коичество не может быть не задано";
                        }
                    }
                },
                new FormHelperField()
                {
                    Path="UNIT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=ItemUnit,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TextLen,
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
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TextNote,
                    ControlType="TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        { FormHelperField.FieldFilterRef.MaxLen, 256 }
                    }
                },
                new FormHelperField()
                {
                    Path="OUTER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TextOuterId,
                    ControlType="TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 3 },
                    }
                },
                new FormHelperField()
                {
                    Path="PRODUCED_DT",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Control=ToDate,
                    ControlType="TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        { FormHelperField.FieldFilterRef.MaxLen, 10 },
                    }
                    
                },
                new FormHelperField()
                {
                    Path="OUTER_NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TextOuterNum,
                    ControlType="TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        { FormHelperField.FieldFilterRef.MaxLen, 32 }
                    }
                },
                new FormHelperField()
                {
                    Path="OUTER_GROUP_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TextOuterGroupId,
                    ControlType="TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        { FormHelperField.FieldFilterRef.MaxLen, 3 },
                    }
                },
                
                new FormHelperField()
                {
                    Path="VERIFICATION_CODE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TextVerificationCode,
                    ControlType="TextBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 3 },
                    },
                    Validate = (f,v)=>
                    {
                        if(!string.IsNullOrEmpty(TextVerificationCode.Text))
                        {
                            if(TextVerificationCode.Text.Length!=3)
                            {
                                f.ValidateResult = false;
                                f.ValidateProcessed = true;

                                f.ValidateMessage = "Длина кода должна быть 3 символа или пустой";
                            }
                        }
                    } 
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;

            Form.OnValidate = (bool valid, string message) =>
            {
                if (valid)
                {
                    Status.Text = "";
                }
                else
                {
                    Status.Text = "Не все поля заполнены верно";
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

            if (Id == 0)
            {
                if (Central.DebugMode)
                {
                    TextOuterId.IsReadOnly = false;
                    TextOuterNum.IsReadOnly = false;
                    TextOuterGroupId.IsReadOnly = false;
                }
                else
                {
                    TextOuterIdBorder.Visibility = Visibility.Collapsed;
                    TextOuterIdLabelBorder.Visibility = Visibility.Collapsed;
                    TextOuterNumBorder.Visibility = Visibility.Collapsed;
                    TextOuterNumLabelBorder.Visibility = Visibility.Collapsed;
                    TextOuterGroupIdBorder.Visibility = Visibility.Collapsed;
                    TextOuterGroupIdLabelBorder.Visibility= Visibility.Collapsed;
                }

                ScrollViewer.Visibility = Visibility.Collapsed;
            }

            var frameName = GetFrameName();
            if (Id == 0)
            {
                Central.WM.Show(frameName, "Новая ТМЦ", true, "add", this);
            }
            else
            {
                Central.WM.Show(frameName, GetActionName(), true, "add", this);
            }
        }

        /// <summary>
        /// Функция редактирования TMЦ
        /// </summary>
        /// <param name="IDItem"> id тмц, если 0 то это создание новой тмц</param>
        public void Edit(int IDItem=0)
        {
            Id = IDItem;

            if (Id != 0)
            {
                PrintButton.IsEnabled = true;

                LabelBatch1.Visibility = Visibility.Collapsed;
                LabelBatch2.Visibility = Visibility.Collapsed;
                SplitToBatch.Visibility = Visibility.Collapsed;
                TextBatchQty.Visibility = Visibility.Collapsed;

                BorderBatch1.Visibility = Visibility.Collapsed;
                BorderBatch2.Visibility = Visibility.Collapsed;
                BorderBatch3.Visibility = Visibility.Collapsed;
                BorderBatch4.Visibility = Visibility.Collapsed;
            }
            
            GetData();
        }

        private void PrepareData()
        {
            if (Document != null)
            {
                BarcodeGenerator generator = new BarcodeGenerator();

                ScrollViewer.Document = generator.GenerateItemDocument(Id.ToString(), TextQty.Text + " " + ItemUnit.SelectedItem.Value, ItemNameTextBox.Text, ToDate.Text);
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
                    p.CheckAdd("WMIT_ID", Id.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Item");
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
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            ItemStorageId = ds.Items[0].CheckGet("WMST_ID").ToInt();
                            ItemState = ds.Items[0].CheckGet("WMIS_ID").ToInt();
                            if (ds.Items[0].CheckGet("PRODUCED_DT").Length > 0)
                            {
                                ToDate.EditValue = DateTime.Parse(ds.Items[0].CheckGet("PRODUCED_DT"));
                            }

                            if (ds.Items[0].CheckGet("WMII_ID").ToInt() > 0)
                            {
                                OldPositionRadioButton.IsChecked = true;
                                OldPositionRadioButtonClick();
                                ItemNameSelectBox.IsReadOnly = true;
                            }
                            else
                            {
                                NewPositionRadioButton.IsChecked = true;
                                NewPositionRadioButtonClick();
                            }

                            NewPositionRadioButton.IsEnabled = false;
                            OldPositionRadioButton.IsEnabled = false;

                            if (ItemState > 1)
                            {
                                WarehouseSelectBox.IsReadOnly = true;
                            }
                        }

                        Form.SetValues(ds);

                        PrepareData();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
            else
            {
                NewPositionRadioButton.IsChecked = true;
                NewPositionRadioButtonClick();
            }
            
            EnableControls();

            Show();
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
        /// Возвращает название режима работы с тмц
        /// </summary>
        /// <returns></returns>
        private string GetActionName()
        {
            string result = "Редактирование ТМЦ";

            return result;
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
        /// Сохранение изменений
        /// </summary>
        public void Save()
        {
            bool resume = true;
            string error = "";

            ToDate.BorderBrush = null;
            TextBatchQty.BorderBrush = null;
            TextQty.BorderBrush = null;

            //стандартная валидация данных средствами формы
            if (resume)
            {
                ToDate.Text = ToDate.Text.Replace('/', '.');
                ToDate.Text = ToDate.Text.Replace(' ', '.');

                var validationResult = Form.Validate();

                if (!validationResult)
                {
                    resume = false;
                }

                if (ToDate.Text == string.Empty)
                {

                }
                else
                {
                    DateTime result = DateTime.Now;

                    if (DateTime.TryParseExact(ToDate.Text, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                    {
                        
                    }
                    else
                    {
                        Form.SetStatus(false, "Формат даты должен быть dd.MM.yyyy");
                        resume = false;

                        ToDate.BorderBrush = HColor.Red.ToBrush();
                    }

                }

                if(resume)
                {
                    int count = TextQty.Text.ToInt();

                    if(count<0)
                    {
                        Form.SetStatus(false, "Количество должно быть больше 0");
                        resume = false;

                        TextQty.BorderBrush = HColor.Red.ToBrush();

                    }
                    else
                    if (SplitToBatch.IsChecked== true)
                    {
                        int batch = TextBatchQty.Text.ToInt();

                        if(batch>count)
                        {
                            Form.SetStatus(false, "Количество единиц в партии должно быть больше количества ТМЦ");
                            resume = false;

                            TextBatchQty.BorderBrush = HColor.Red.ToBrush();

                        }
                    }
                }
            }

            if(resume)
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
            q.Request.SetParam("Object", "Item");

            if (SplitToBatch.IsChecked == true)
            {
                q.Request.SetParam("Action", "SaveBatch");
                p.CheckAdd("BATCHQTY", TextBatchQty.Text.ToString());
            }
            else
            {
                p.Remove("BATCHQTY");
                q.Request.SetParam("Action", "Save");
            }

            p.Add("WMIT_ID", Id.ToString());
            // мемто на складе
            p["WMST_ID"] = ItemStorageId.ToString();  
            // 1- State на поступление
            p.CheckAdd("WMIS_ID", ItemState.ToString());

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
                            var id = ds.GetFirstItemValueByKey("ID").ToInt();
                            if (id >= 0)
                            {
                                //отправляем сообщение гриду о необходимости обновить данные
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "WMS",
                                    ReceiverName = "WMS_list",
                                    SenderName = "WMSItem",
                                    Action = "Refresh",
                                    Message = $"{id}",
                                });

                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverGroup = "WMS",
                                    ReceiverName = "WarehouseItemAccounting",
                                    SenderName = "WMSItem",
                                    Action = "refresh",
                                });

                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverGroup = "WMS",
                                    ReceiverName = "WarehouseListArrival",
                                    SenderName = "WMSItem",
                                    Action = "refresh",
                                });

                                Close();
                            }
                            else
                            {
                                Form.SetStatus("Во время сохранения возникла ошибка, проверьте данные и попробуйте еще раз", 1);
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            BarcodeGenerator generator = new BarcodeGenerator();
            var doc = generator.GenerateItemDocument(Id.ToString(), TextQty.Text + " " + ItemUnit.SelectedItem.Value, ItemNameTextBox.Text, ToDate.Text);
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

        public void SetPrintSettings()
        {
            var i = new PrintingInterface();
        }

        public void NewPositionRadioButtonClick()
        {
            ItemNameTextBox.IsEnabled = true;

            OldPositionRadioButton.IsChecked = false;
            ItemNameSelectBox.IsEnabled = false;
            ItemNameSelectBox.Clear();
        }

        public void OldPositionRadioButtonClick()
        {
            ItemNameSelectBox.IsEnabled = true;
            ItemNameSelectBox.SetItems(InventoryItemDataSet, "WMII_ID", "NAME");

            NewPositionRadioButton.IsChecked = false;
            ItemNameTextBox.IsEnabled = false;
            ItemNameTextBox.Text = "";
        }

        private void SplitToBatch_Checked(object sender, RoutedEventArgs e)
        {
            TextBatchQty.IsEnabled = true;
        }

        private void SplitToBatch_Unchecked(object sender, RoutedEventArgs e)
        {
            TextBatchQty.IsEnabled = false;
            TextBatchQty.Text = "1";
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

        private void NewPositionRadioButton_Click(object sender, RoutedEventArgs e)
        {
            NewPositionRadioButtonClick();
        }

        private void OldPositionRadioButton_Click(object sender, RoutedEventArgs e)
        {
            OldPositionRadioButtonClick();
        }

        private void ItemNameSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (ItemNameSelectBox.SelectedItem.Key.ToInt() > 0)
            {
                ItemNameTextBox.Text = ItemNameSelectBox.SelectedItem.Value;
            }
            else
            {
                ItemNameTextBox.Text = "";
            }
        }
    }
}
