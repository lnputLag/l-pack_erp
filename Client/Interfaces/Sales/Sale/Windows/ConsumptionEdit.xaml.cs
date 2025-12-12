using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using static Client.Interfaces.Main.DataGridHelperColumn;
using static DevExpress.Data.Filtering.Helpers.SubExprHelper.ThreadHoppingFiltering;

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Форма редактирования позиции расхода в накладной
    /// </summary>
    public partial class ConsumptionEdit : ControlBase
    {
        /// <summary>
        /// Обязательные к заполнению переменные:
        /// InvoiceId;
        /// CompletedFlag;
        /// CustomerId.
        /// Не обязательные к заполнению переменные:
        /// ParentFrame.
        /// </summary>
        public ConsumptionEdit()
        {
            ControlTitle = "Редактирование позиции расхода";
            FrameName = "ConsumptionEdit";
            ConsumptionData = new Dictionary<string, string>();
            InitializeComponent();

            OnLoad = () =>
            {
                Init();
                SetDefaults();

                if (ConsumptionId > 0)
                {
                    PriceForAllBorder.Visibility = Visibility.Visible;
                    
                    AddressSelectBox.Visibility = Visibility.Collapsed;
                    AddressSelectBoxBorder.Visibility = Visibility.Collapsed;
                    AddressLabelBorder.Visibility = Visibility.Collapsed;
                    SearchText.Visibility = Visibility.Collapsed;
                    SearchLabel.Visibility = Visibility.Collapsed;
                    SearchBorder.Visibility = Visibility.Collapsed;

                    PositionGridBorder.Visibility = Visibility.Collapsed;
                    LoadItems();
                }
                else
                {
                    PriceForAllBorder.Visibility = Visibility.Collapsed;

                    AddressSelectBox.Visibility = Visibility.Visible;
                    AddressSelectBoxBorder.Visibility = Visibility.Visible;
                    AddressLabelBorder.Visibility = Visibility.Visible;
                    SearchText.Visibility = Visibility.Visible;
                    SearchLabel.Visibility = Visibility.Visible;
                    SearchBorder.Visibility = Visibility.Visible;

                    PositionGridBorder.Visibility = Visibility.Visible;
                    ProductGridInit();
                    ProductGridLoadItems();
                }
                

                Messenger.Default.Register<ItemMessage>(this, _ProcessMessages);
                Central.Msg.Register(ProcessMessages);
            };

            OnUnload = () =>
            {
                Messenger.Default.Unregister<ItemMessage>(this);
                Central.Msg.UnRegister(ProcessMessages);
            };

            OnFocusGot = () =>
            {
            };

            OnFocusLost = () =>
            {
            };
        }

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        public string ParentFrame { get; set; }

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
        /// Ид накладной расхода.
        /// naklrashod.nsthet
        /// </summary>
        public int InvoiceId { get; set; }

        /// <summary>
        /// Ид позиции расхода.
        /// rashod.idr
        /// </summary>
        public int ConsumptionId { get; set; }

        /// <summary>
        /// Флаг того, что выбранная позиция расхода уже проведена.
        /// rashod.provedeno.
        /// </summary>
        public bool CompletedFlag { get; set; }

        /// <summary>
        /// идентификатор покупателя
        /// pokupatel.id_pok
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Данные по выбранной позиции расхода
        /// </summary>
        public Dictionary<string, string> ConsumptionData { get; set; }

        /// <summary>
        /// Датасет с данными для грида продукции на остатках
        /// </summary>
        public ListDataSet ProductGridDataSet { get; set; }

        public bool VirtualFlag { get; set; }

        /// <summary>
        /// инициализация компонентов формы
        /// </summary>
        public void Init()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SearchText,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },

                new FormHelperField()
                {
                    Path="PRODUCT_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductNameTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_CODE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductCodeTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_KOD",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductKodTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QuantityTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.IsNotZero, null },
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRICE",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=PriceTextBox,
                    ControlType="TextBox",
                    Format="N10",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRICE_FOR_ALL",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=PriceForAllCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path = "ORDER_POSITION_ID",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = AddressSelectBox,
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                    },
                },
            };
            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
        }

        /// <summary>
        /// Загрузка данных формы
        /// </summary>
        public void LoadItems()
        {
            Form.SetValues(ConsumptionData);
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();

            if (CompletedFlag)
            {
                QuantityTextBox.IsReadOnly = true;
            }

            ProductGridDataSet = new ListDataSet();
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
            Central.WM.FrameMode = 2;

            // Если редактируем позицию
            if (ConsumptionId > 0)
            {
                FrameName = $"{FrameName}_{ConsumptionId}";
                Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
                windowParametrs.Add("no_resize", "1");
                windowParametrs.Add("center_screen", "1");
                this.MinHeight = 200;
                this.MinWidth = 750;
                Central.WM.Show(FrameName, "Редактирование позиции", true, "main", this, "top", windowParametrs);
            }
            // Если добавляем новую позицию
            else
            {
                FrameName = $"{FrameName}";
                Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
                windowParametrs.Add("no_resize", "1");
                windowParametrs.Add("center_screen", "1");
                this.MinHeight = 650;
                this.MinWidth = 750;

                string virtualTitle = "";
                if (VirtualFlag)
                {
                    virtualTitle = "виртуальной ";
                }

                Central.WM.Show(FrameName, $"Добавление {virtualTitle}позиции", true, "main", this, "top", windowParametrs);
            }
        }

        /// <summary>
        /// Инициализация грида продукции на остатках
        /// </summary>
        public void ProductGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Ид",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="PRODUCT_CODE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=44,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Код",
                        Path="PRODUCT_KOD",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цена",
                        Path="CENAPRODRR",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N2",
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Doc="Количество продукции в отделе",
                        Path="QUANTITY_IN_DEPARTMENT",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Отдел",
                        Path="DEPARTMENT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="PRODUCT_IDK1",
                        Path="PRODUCT_IDK1",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="DEPARTMENT_ID",
                        Path="DEPARTMENT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=5,
                        Hidden=true,
                    },
                };
                ProductGrid.SetColumns(columns);
                ProductGrid.SetPrimaryKey("PRODUCT_ID");
                ProductGrid.SetSorting("PRODUCT_CODE", ListSortDirection.Ascending);
                ProductGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

                ProductGrid.SearchText = SearchText;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                ProductGrid.OnSelectItem = selectedItem =>
                {
                    Form.SetValues(selectedItem);
                    AddressSelectBoxLoadItems(selectedItem.CheckGet("PRODUCT_ID").ToInt());

                    if ((selectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 2
                        || selectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 3
                        || selectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 4
                        || selectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 16
                        || selectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 26)
                        || (!VirtualFlag
                            && (selectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 5
                            || selectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 6)
                        ))
                    {
                        SaveButton.IsEnabled = false;
                        MainGrid.IsEnabled = false;
                    }
                    else
                    {
                        SaveButton.IsEnabled = true;
                        MainGrid.IsEnabled = true;
                    }
                };

                ProductGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // Продукция, производимая нами
                            if ((row.CheckGet("PRODUCT_IDK1").ToInt() == 2
                                || row.CheckGet("PRODUCT_IDK1").ToInt() == 3
                                || row.CheckGet("PRODUCT_IDK1").ToInt() == 4
                                || row.CheckGet("PRODUCT_IDK1").ToInt() == 16
                                || row.CheckGet("PRODUCT_IDK1").ToInt() == 26)
                                || (!VirtualFlag
                                    && (row.CheckGet("PRODUCT_IDK1").ToInt() == 5
                                    || row.CheckGet("PRODUCT_IDK1").ToInt() == 6)
                                ))
                            {
                                color = HColor.Yellow;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                ProductGrid.Init();
                ProductGrid.Run();
            }
        }

        private void AddressSelectBoxLoadItems(int productId)
        {
            AddressSelectBox.Items.Clear();

            FormHelper.ComboBoxInitHelper(AddressSelectBox, "Sales", "Sale", "ListAddress", "ORDER_POSITION_ID", "ADDRESS_NAME",
                new Dictionary<string, string>() { { "INVOICE_ID", $"{InvoiceId}" }, { "PRODUCT_ID", $"{productId}" } }, true);

            if (AddressSelectBox.Items != null && AddressSelectBox.Items.Count == 1)
            {
                AddressSelectBox.SetSelectedItemFirst();
            }
        }

        /// <summary>
        /// Получение данных для грида продукций на остатках
        /// </summary>
        public void ProductGridLoadItems()
        {
            DisableControls();

            if (VirtualFlag)
            {
                var p = new Dictionary<string, string>();
                p.Add("INVOICE_ID", $"{InvoiceId}");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Sale");
                q.Request.SetParam("Action", "ListProductByInvoice");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    bool succesfullFlag = false;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        ProductGridDataSet = ListDataSet.Create(result, "ITEMS");
                        ProductGrid.UpdateItems(ProductGridDataSet);
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
            else
            {
                var p = new Dictionary<string, string>();
                p.Add("DEPARTMENT_ID", "1");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Sale");
                q.Request.SetParam("Action", "ListProductInDepartment");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    bool succesfullFlag = false;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        ProductGridDataSet = ListDataSet.Create(result, "ITEMS");
                        ProductGrid.UpdateItems(ProductGridDataSet);
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
        /// Сохранение данных по позиции расходу
        /// </summary>
        public void Save()
        {
            if (Form.Validate())
            {
                DisableControls();

                int consumptionId = 0;

                // Если редактируем позицию расхода
                if (ConsumptionId > 0)
                {
                    var p = new Dictionary<string, string>();
                    p.Add("CONSUMPTION_ID", ConsumptionId.ToString());
                    p.Add("INVOICE_ID", InvoiceId.ToString());
                    p.Add("PRICE", Form.GetValueByPath("PRICE"));
                    p.Add("COMPLETED_FLAG", CompletedFlag.ToInt().ToString());
                    p.Add("CUSTOMER_ID", CustomerId.ToString());
                    p.Add("QUANTITY", Form.GetValueByPath("QUANTITY"));
                    p.Add("PRICE_FOR_ALL", Form.GetValueByPath("PRICE_FOR_ALL").ToInt().ToString());

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "Sale");
                    q.Request.SetParam("Action", "UpdateConsumption");
                    q.Request.SetParams(p);
                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
                    q.DoQuery();

                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            var dataSet = ListDataSet.Create(result, "ITEMS");
                            if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                            {
                                consumptionId = dataSet.Items.First().CheckGet("CONSUMPTION_ID").ToInt();
                            }
                        }

                        if (consumptionId > 0)
                        {
                            string msg = $"Успешное редактирование позиции расхода.";
                            var d = new DialogWindow($"{msg}", "Редактирование позиций накладной", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        else
                        {
                            string msg = $"При редактировании позиции произошла ошибка. Пожалуйста, сообщите о проблеме.";
                            var d = new DialogWindow($"{msg}", "Редактирование позиций накладной", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
                // Если создаём новую позицию
                else
                {
                    if ((ProductGrid.SelectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 2
                        || ProductGrid.SelectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 3
                        || ProductGrid.SelectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 4
                        || ProductGrid.SelectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 16
                        || ProductGrid.SelectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 26)
                        || (!VirtualFlag
                            && (ProductGrid.SelectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 5
                            || ProductGrid.SelectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 6)
                        ))
                    {
                        string msg = $"Добавление этого вида продукции запрещено.";
                        var d = new DialogWindow($"{msg}", "Редактирование позиций накладной", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        if (VirtualFlag
                            && (ProductGrid.SelectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 5
                            || ProductGrid.SelectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 6)
                            && string.IsNullOrEmpty(Form.GetValueByPath("ORDER_POSITION_ID")))
                        {
                            string msg = $"Добавление вритуального поддона без указания адреса доставки запрещено.";
                            var d = new DialogWindow($"{msg}", "Редактирование виртуальной позиций накладной", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        else
                        {
                            var p = new Dictionary<string, string>();
                            p.Add("INVOICE_ID", InvoiceId.ToString());
                            p.Add("PRODUCT_IDK1", ProductGrid.SelectedItem.CheckGet("PRODUCT_IDK1"));
                            p.Add("DEPARTMENT_ID", ProductGrid.SelectedItem.CheckGet("DEPARTMENT_ID"));
                            p.Add("PRODUCT_ID", ProductGrid.SelectedItem.CheckGet("PRODUCT_ID"));
                            p.Add("QUANTITY", Form.GetValueByPath("QUANTITY"));
                            p.Add("PRICE", Form.GetValueByPath("PRICE"));
                            p.Add("ID_INV", "-1");
                            p.Add("CUSTOMER_ID", CustomerId.ToString());
                            p.Add("VIRTUAL_FLAG", $"{VirtualFlag.ToInt()}");
                            p.Add("ORDER_POSITION_ID", Form.GetValueByPath("ORDER_POSITION_ID"));

                            var q = new LPackClientQuery();
                            q.Request.SetParam("Module", "Sales");
                            q.Request.SetParam("Object", "Sale");
                            q.Request.SetParam("Action", "SaveConsumption");
                            q.Request.SetParams(p);
                            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
                            q.DoQuery();

                            if (q.Answer.Status == 0)
                            {
                                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                if (result != null)
                                {
                                    var dataSet = ListDataSet.Create(result, "ITEMS");
                                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                                    {
                                        consumptionId = dataSet.Items.First().CheckGet("CONSUMPTION_ID").ToInt();
                                    }
                                }

                                string virtualTitle = "";
                                if (VirtualFlag)
                                {
                                    virtualTitle = "виртуальной ";
                                }

                                if (consumptionId > 0)
                                {
                                    string msg = $"Успешное добавление {virtualTitle}позиции расхода.";
                                    var d = new DialogWindow($"{msg}", $"Редактирование {virtualTitle}позиций накладной", "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                }
                                else
                                {
                                    string msg = $"При добавлении {virtualTitle}позиции произошла ошибка. Пожалуйста, сообщите о проблеме.";
                                    var d = new DialogWindow($"{msg}", $"Редактирование {virtualTitle}позиций накладной", "", DialogWindowButtons.OK);
                                    d.ShowDialog();
                                }
                            }
                            else
                            {
                                q.ProcessError();
                            }
                        }
                    }
                }

                if (consumptionId > 0)
                {
                    // Отправляем сообщение вкладке "Позиции накладной расхода" обновиться
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "Preproduction",
                        ReceiverName = "ConsumptionList",
                        SenderName = "ConsumptionEdit",
                        Action = "Refresh",
                        Message = "",
                    }
                    );

                    // Отправляем сообщение вкладке "Список продаж" обновиться
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "Preproduction",
                        ReceiverName = "SaleList",
                        SenderName = "ConsumptionEdit",
                        Action = "Refresh",
                        Message = "",
                    }
                    );

                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "Preproduction",
                        ReceiverName = "ConsumptionList",
                        SenderName = "ConsumptionEdit",
                        Action = "UploadWebDocumentList",
                        Message = "",
                    }
                    );

                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverName = "ShipmentDocumentList",
                        SenderName = "ConsumptionEdit",
                        Action = "Refresh",
                        Message = "",
                    }
                    );

                    Close();
                }

                EnableControls();
            }
        }

        /// <summary>
        /// Обновляем данные
        /// </summary>
        public void Refresh()
        {
            if (ConsumptionId > 0) 
            {
                LoadItems();
            }
            else
            {
                ProductGridLoadItems();
            }
        }

        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }

        /// <summary>
        /// обработка сообщений
        /// </summary>
        /// <param name="message"></param>
        public void ProcessMessages(ItemMessage message)
        {
            if (message != null)
            {
                if (message.SenderName == "WindowManager")
                {
                    switch (message.Action)
                    {
                        case "FocusGot":
                            break;

                        case "FocusLost":
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void _ProcessMessages(ItemMessage m)
        {

        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            if (!string.IsNullOrEmpty(ParentFrame))
            {
                Central.WM.SetActive(ParentFrame, true);
            }

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
                ReceiverGroup = "Sales",
                ReceiverName = "",
                SenderName = "ConsumptionEdit",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/sales/sale_list/consumption_list/consumption_edit");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }
    }
}
