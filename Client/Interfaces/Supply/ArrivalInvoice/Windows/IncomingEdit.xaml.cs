using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Utils.About;
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
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Supply
{
    /// <summary>
    /// Форма редактирования позиции прихода в накладной
    /// </summary>
    public partial class IncomingEdit : ControlBase
    {
        public IncomingEdit()
        {
            ControlTitle = "Редактирование позиции прихода";
            RoleName = "[erp]arrival_invoice";
            DocumentationUrl = "/doc/l-pack-erp/";
            InitializeComponent();

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
                FormInit();
                SetDefaults();

                if (IncomingId > 0)
                {
                    PositionGridBorder.Visibility = Visibility.Collapsed;
                    LoadItems();
                }
                else
                {
                    PositionGridBorder.Visibility = Visibility.Visible;

                    ProductGridInit();
                }
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                ProductGrid?.Destruct();
            };

            OnFocusGot = () =>
            {
            };

            OnFocusLost = () =>
            {
            };

            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Group = "main",
                    Enabled = true,
                    Title = "",
                    Description = "Сохранить",
                    ButtonUse = true,
                    ButtonControl = SaveButton,
                    ButtonName = "SaveButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        Save();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "cancel",
                    Group = "main",
                    Enabled = true,
                    Title = "",
                    Description = "Отмена",
                    ButtonUse = true,
                    ButtonControl = CancelButton,
                    ButtonName = "CancelButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Close();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "refresh",
                    Group = "main",
                    Enabled = true,
                    Title = "Показать",
                    Description = "Показать данные на основе введённых данных",
                    ButtonUse = true,
                    ButtonControl = RefreshButton,
                    ButtonName = "RefreshButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    Action = () =>
                    {
                        Refresh();
                    },
                    CheckEnabled = () =>
                    {
                        bool result = false;

                        if (!(IncomingId > 0))
                        {
                            result = true;
                        }

                        return result;
                    },
                });
            }

            Commander.Init(this);
        }

        /// <summary>
        /// Ид позиции прихода.
        /// prihod.idp
        /// </summary>
        public int IncomingId { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Данные по выбранной позиции прихода
        /// </summary>
        public Dictionary<string, string> IncomingData { get; set; }

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        public string ParentFrame { get; set; }

        /// <summary>
        /// Ид накладной прихода.
        /// naklprih.nsthet
        /// </summary>
        public int InvoiceId { get; set; }

        /// <summary>
        /// идентификатор покупателя
        /// pokupatel.id_pok
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Датасет с данными для грида продукции
        /// </summary>
        public ListDataSet ProductGridDataSet { get; set; }

        /// <summary>
        /// инициализация компонентов формы
        /// </summary>
        public void FormInit()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
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
                    Path="QUANTITY2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Quantity2TextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
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
                    Path="ROLL_NAME_INNER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NumberTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SUMMARY_PRICE",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=SummaryPriceTextBox,
                    ControlType="TextBox",
                    Format="N10",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "DEPARTMENT_ID",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = DepartmentSelectBox,
                    ControlType = "SelectBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="COMPLETED_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_IDK1",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
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
            Form.SetValues(IncomingData);
        }

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
                        Description = "Идентификатор продукции",
                        Path="PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Description = "Наименование продукции",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=45,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Description = "Артикул продукции",
                        Path="PRODUCT_CODE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Код",
                        Description = "Код продукции",
                        Path="PRODUCT_KOD",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Производитель",
                        Description = "Производитель продукции",
                        Path="MANUFACTURER_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=13,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид категории продукции",
                        Description = "Ид категории продукции",
                        Path="PRODUCT_IDK1",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        Hidden=true,
                    },
                };
                ProductGrid.SetColumns(columns);
                ProductGrid.SearchText = SearchText;
                ProductGrid.SetPrimaryKey("PRODUCT_ID");
                ProductGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ProductGrid.AutoUpdateInterval = 0;
                ProductGrid.Toolbar = ProductGridToolbar;
                ProductGrid.OnLoadItems = ProductGridLoadItems;

                ProductGrid.OnSelectItem = selectedItem =>
                {
                    if (selectedItem != null && selectedItem.Count > 0)
                    {
                        if (selectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 2 
                            || selectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 3
                            || selectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 14
                            || selectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 15)
                        {
                            selectedItem.CheckAdd("DEPARTMENT_ID", "0");
                        }
                        else if (selectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 16
                            || selectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 26)
                        {
                            selectedItem.CheckAdd("DEPARTMENT_ID", "5");
                        }
                        else if (selectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 4 
                            || selectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 5 
                            || selectedItem.CheckGet("PRODUCT_IDK1").ToInt() == 6)
                        {
                            selectedItem.CheckAdd("DEPARTMENT_ID", "1");
                        }
                        else
                        {
                            selectedItem.CheckAdd("DEPARTMENT_ID", "1");
                        }
                    }

                    Form.SetValues(selectedItem);
                };

                ProductGrid.UseProgressSplashAuto = false;
                ProductGrid.Init();
            }
        }

        public async void ProductGridLoadItems()
        {
            if (!(IncomingId > 0))
            {
                if (SearchText != null && !string.IsNullOrEmpty(SearchText.Text))
                {
                    var p = new Dictionary<string, string>();
                    p.Add("SEARCH_TEXT", SearchText.Text);

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Supply");
                    q.Request.SetParam("Object", "ArrivalInvoice");
                    q.Request.SetParam("Action", "ListProductBySearchText");
                    q.Request.SetParams(p);
                    q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    ProductGridDataSet = new ListDataSet();
                    if (q.Answer.Status == 0)
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                        if (result != null)
                        {
                            ProductGridDataSet = ListDataSet.Create(result, "ITEMS");
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                    ProductGrid.UpdateItems(ProductGridDataSet);
                }
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();

            if (IncomingData?.CheckGet("COMPLETED_FLAG").ToInt() > 0)
            {
                QuantityTextBox.IsReadOnly = true;
                Quantity2TextBox.IsReadOnly = true;
            }

            ProductGridDataSet = new ListDataSet();

            LoadDepartmentList();
        }

        public void LoadDepartmentList()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Supply");
            q.Request.SetParam("Object", "ArrivalInvoice");
            q.Request.SetParam("Action", "ListDepartment");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        DepartmentSelectBox.SetItems(ds, "ID", "NAME");
                    }
                }
            }
            else
            {
                q.ProcessError();
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
            Central.WM.FrameMode = 2;

            //FIXME
            //size

            // Если редактируем позицию
            if (IncomingId > 0)
            {
                FrameName = $"{FrameName}_{IncomingId}";
                Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
                windowParametrs.Add("no_resize", "1");
                windowParametrs.Add("center_screen", "1");
                this.MinHeight = 200;
                this.MinWidth = 750;
                Central.WM.Show(FrameName, $"Редактирование позиции № {IncomingId}", true, "main", this, "top", windowParametrs);
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
                Central.WM.Show(FrameName, "Добавление позиции", true, "main", this, "top", windowParametrs);
            }
        }

        /// <summary>
        /// Обновляем данные
        /// </summary>
        public void Refresh()
        {
            ProductGrid.LoadItems();
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

        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }

        public void Save()
        {
            if (Form.Validate())
            {
                DisableControls();

                int incomingId = 0;

                // Если редактируем позицию
                if (IncomingId > 0)
                {
                    var p = new Dictionary<string, string>();
                    p.AddRange(Form.GetValues());
                    p.CheckAdd("INCOMING_ID", $"{IncomingId}");

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Supply");
                    q.Request.SetParam("Object", "ArrivalInvoice");
                    q.Request.SetParam("Action", "UpdateIncoming");
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
                                incomingId = dataSet.Items.First().CheckGet("INCOMING_ID").ToInt();
                            }
                        }

                        if (incomingId > 0)
                        {
                            var msg = "Успешное редактирование позиции прихода.";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        else
                        {
                            var msg = "Ошибка редактирования позиции прихода. Пожалуйста, сообщите о проблеме.";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }
                else if (ProductGrid != null && ProductGrid.SelectedItem != null && ProductGrid.SelectedItem.Count > 0)
                {
                    var p = new Dictionary<string, string>();
                    p.AddRange(Form.GetValues());
                    p.CheckAdd("INVOICE_ID", $"{InvoiceId}");

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Supply");
                    q.Request.SetParam("Object", "ArrivalInvoice");
                    q.Request.SetParam("Action", "SaveIncoming");
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
                                incomingId = dataSet.Items.First().CheckGet("INCOMING_ID").ToInt();
                            }
                        }

                        if (incomingId > 0)
                        {
                            var msg = "Успешное создание позиции прихода.";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                        else
                        {
                            var msg = "Ошибка создания позиции прихода. Пожалуйста, сообщите о проблеме.";
                            var d = new DialogWindow($"{msg}", this.ControlTitle, "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        q.ProcessError();
                    }
                }

                if (incomingId > 0)
                {
                    // Отправляем сообщение вкладке "Приходная накладная" обновиться
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Supply",
                        ReceiverName = "ArrivalInvoice",
                        SenderName = this.ControlName,
                        Action = "Refresh",
                        Message = $"{InvoiceId}",
                    });

                    // Отправляем сообщение вкладке "Список приходных накладных" обновиться
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Supply",
                        ReceiverName = "ArrivalInvoiceList",
                        SenderName = this.ControlName,
                        Action = "Refresh",
                        Message = $"{InvoiceId}",
                    });

                    if (IncomingId > 0)
                    {
                        Close();
                    }
                    else
                    {
                        Form.SetValueByPath("QUANTITY", "");
                        Form.SetValueByPath("QUANTITY2", "");
                    }
                }

                EnableControls();
            }
        }
    }
}
