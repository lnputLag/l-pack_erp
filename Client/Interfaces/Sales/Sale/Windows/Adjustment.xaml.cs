using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Production;
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
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Sales
{
    /// <summary>
    /// Окно создания корректировки по продаже
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class Adjustment : ControlBase
    {
        /// <summary>
        /// Обязательные к заполнению переменные:
        /// InvoiceId.
        /// </summary>
        public Adjustment()
        {
            ControlTitle = "Корректировка по накладной";
            RoleName = "[erp]sales_manager";
            InitializeComponent();

            if (Central.DebugMode)
            {
                PrintReceiptButton.Visibility = Visibility.Visible;
            }
            else
            {
                PrintReceiptButton.Visibility = Visibility.Collapsed;
            }

            OnMessage = (ItemMessage message) => {
                DebugLog($"message=[{message.Message}]");
            };

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                Messenger.Default.Register<ItemMessage>(this, _ProcessMessages);

                FormInit();
                SetDefaults();
                DetailGridInit();

                // Если корректировка по этой накладной уже создана
                if (AdjusmentId > 0)
                {
                    AdjustmenRadioButton.IsChecked = false;
                    AdjusmentToolBar.IsEnabled = false;
                    LoadAdjusment();
                    LoadAdjusmentDetail();
                }
                else
                {
                    GetAdjusmentNumber();
                }

                UpdateActions();
                ProcessPermissions();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                Messenger.Default.Unregister<ItemMessage>(this);
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
        /// Идентификатор накладной расхода
        /// naklrashod.nsthet
        /// </summary>
        public int InvoiceId { get; set; }

        /// <summary>
        /// Идентификатор площадки по накладной
        /// </summary>
        public int FactoryId { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Идентификатор созданной корректировки
        /// </summary>
        public int AdjusmentId { get; set; }

        /// <summary>
        /// Данные для грида детализации созданной корректировки
        /// </summary>
        public ListDataSet DetailGridDataSet { get; set; }

        /// <summary>
        /// Дата, с которой разрешено редактирование движения товаров, продукции и денег
        /// </summary>
        public string ReportingPeriod { get; set; }

        /// <summary>
        /// Идентификатор сотрудника - подписанта
        /// </summary>
        public int SignotoryEmployeeId { get; set; }

        /// <summary>
        /// Полное имя сотрудника - подписанта
        /// </summary>
        public string SignotoryEmployeeName { get; set; }

        public void FormInit()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path = "ADJUSTMENT_DATE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Format = "dd.MM.yyyy",
                        Control = AdjustmentDateTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "ADJUSTMENT_NUMBER",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = AdjustmentNumberTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                             { FormHelperField.FieldFilterRef.Required, null },
                        },
                    },
                };

                Form.SetFields(fields);
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            AdjustmenRadioButton.IsChecked = true;
            ErrorRadioButton.IsChecked = false;

            string dateTimeNow = DateTime.Now.ToString("dd.MM.yyyy");
            Form.SetValueByPath("ADJUSTMENT_DATE", dateTimeNow);

            DetailGridDataSet = new ListDataSet();
            AdjusmentDetailToolBar.IsEnabled = false;
        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }

            if (DetailGrid != null && DetailGrid.Menu != null && DetailGrid.Menu.Count > 0)
            {
                foreach (var manuItem in DetailGrid.Menu)
                {
                    var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                    var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                    if (accessMode > userAccessMode)
                    {
                        manuItem.Value.Enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Инициализация грида детализации по корректировке
        /// </summary>
        public void DetailGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="*",
                        Path="EDITED",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид корректировки",
                        Path="DETAIL_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер",
                        Path="INCOMING_BASE_NUMBER",
                        Doc="Номер поддона/рулона",
                        ColumnType=ColumnTypeRef.String,
                        Width2=9,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продукция",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=37,
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
                        Header="Количество",
                        Path="QUANTITY_OLD",
                        Doc="Старое количество",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="*Количество",
                        Path="QUANTITY_NEW",
                        Doc="Новое количество",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        OnClickAction = (row, el) =>
                        {
                            if (Central.Navigator.GetRoleLevel("[erp]sales_manager") > Role.AccessMode.ReadOnly)
                            {
                                EditQuantityCell();
                            }
    
                            return null;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цена",
                        Path="PRICE_OLD",
                        Doc="Старая цена",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="*Цена",
                        Path="PRICE_NEW",
                        Doc="Новая цена",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                        OnClickAction = (row, el) =>
                        {
                            if (Central.Navigator.GetRoleLevel("[erp]sales_manager") > Role.AccessMode.ReadOnly)
                            {
                                EditPriceCell();
                            }
                            
                            return null;
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"Ид расхода",
                        Path="CONSUMPTION_BASE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=8,
                        TooltipDoc="Идентификатор расхода, который редактируем",
                        Doc="Идентификатор расхода, который редактируем",
                        Description="Идентификатор расхода, который редактируем",
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"ТТН прихода",
                        Path="INCOMING_NAME_STH",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                        TooltipDoc="Номер ТТН накладной расхода по рождённому приходу",
                        Doc="Номер ТТН накладной расхода по рождённому приходу",
                        Description="Номер ТТН накладной расхода по рождённому приходу",
                    },
                    new DataGridHelperColumn
                    {
                        Header=$"ТТН расхода",
                        Path="CONSUMPTION_NAME_STH",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                        TooltipDoc="Номер ТТН накладной расхода по рождённому расходу",
                        Doc="Номер ТТН накладной расхода по рождённому расходу",
                        Description="Номер ТТН накладной расхода по рождённому расходу",
                    },
                    new DataGridHelperColumn
                    {
                        Header="INCOMING_ID",
                        Path="INCOMING_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="INCOMING_DATAOPRSTH",
                        Path="INCOMING_DATAOPRSTH",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=7,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="CONSUMPTION_ID",
                        Path="CONSUMPTION_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="CONSUMPTION_DATAOPRSTH",
                        Path="CONSUMPTION_DATAOPRSTH",
                        ColumnType=ColumnTypeRef.DateTime,
                        Width2=7,
                        Hidden=true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="INCOMING_BASE_ID",
                        Path="INCOMING_BASE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=7,
                        Hidden=true,
                    },
                };
                DetailGrid.SetColumns(columns);
                DetailGrid.SetPrimaryKey("DETAIL_ID");
                DetailGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;

                //при выборе строки в гриде, обновляются актуальные действия для записи
                DetailGrid.OnSelectItem = selectedItem =>
                {
                    SetActionEnabled(selectedItem);
                };

                DetailGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // Есть Рожденный расход или Рожденный приход
                            if (!string.IsNullOrEmpty(row.CheckGet("CONSUMPTION_ID")) || !string.IsNullOrEmpty(row.CheckGet("INCOMING_ID")))
                            {
                                color = HColor.Green;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                // контекстное меню
                DetailGrid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "EditQuantityCell",
                        new DataGridContextMenuItem()
                        {
                            Header="Изменить количество",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                EditQuantityCell();
                            }
                        }
                    },
                    {
                        "EditPriceCell",
                        new DataGridContextMenuItem()
                        {
                            Header="Изменить цену",
                            Tag = "access_mode_full_access",
                            Action=()=>
                            {
                                EditPriceCell();
                            }
                        }
                    },
                };

                DetailGrid.Init();
                DetailGrid.Run();
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
            var frameName = $"{ControlName}_{InvoiceId}";
            this.MinHeight = 768;
            this.MinWidth = 1024;

            Central.WM.Show(frameName, $"Корректировка {AdjusmentId} по накладной {InvoiceId}", true, "main", this);
        }

        public void SetActionEnabled(Dictionary<string, string> selectedItem)
        {
            DetailGrid.Menu["EditQuantityCell"].Enabled = false;
            DetailGrid.Menu["EditPriceCell"].Enabled = false;
            FindInvoiceByAdjusmentSaleDetailButton.IsEnabled = false;

            if (DetailGrid != null && selectedItem != null && selectedItem.Count > 0)
            {
                if (SaveAdjusmentDetailButton.IsEnabled)
                {
                    DetailGrid.Menu["EditQuantityCell"].Enabled = true;
                    DetailGrid.Menu["EditPriceCell"].Enabled = true;
                }

                if ((!string.IsNullOrEmpty(selectedItem.CheckGet("INCOMING_NAME_STH")) && !string.IsNullOrEmpty(selectedItem.CheckGet("INCOMING_DATAOPRSTH")))
                    || (!string.IsNullOrEmpty(selectedItem.CheckGet("CONSUMPTION_NAME_STH")) && !string.IsNullOrEmpty(selectedItem.CheckGet("CONSUMPTION_DATAOPRSTH"))))
                {
                    FindInvoiceByAdjusmentSaleDetailButton.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Изменение значения в выбранной ячейке Новая цена
        /// </summary>
        public void EditPriceCell()
        {
            //FIXME Переделать на даблклик

            if (SaveAdjusmentDetailButton.IsEnabled)
            {
                bool resume = false;

                double newPrice = 0;
                double oldPrice = DetailGrid.SelectedItem.CheckGet("PRICE_OLD").ToDouble();
                var i = new ComplectationCMQuantity(oldPrice, false);
                i.Show("Новая цена");
                if (i.OkFlag)
                {
                    newPrice = i.QtyDouble;
                    resume = true;
                }

                if (resume)
                {
                    DetailGrid.SelectedItem.CheckAdd("PRICE_NEW", newPrice.ToString());
                    DetailGrid.SelectedItem.CheckAdd("EDITED", "1");
                }

                DetailGrid.UpdateItems();
                UpdateActions();
            }
        }

        /// <summary>
        /// Изменение значения в выбранной ячейке Новое количество
        /// </summary>
        public void EditQuantityCell()
        {
            //FIXME Переделать на даблклик

            if (SaveAdjusmentDetailButton.IsEnabled)
            {
                bool resume = false;

                int newQuantity = 0;
                int oldQuantity = DetailGrid.SelectedItem.CheckGet("QUANTITY_OLD").ToInt();
                var i = new ComplectationCMQuantity(oldQuantity, false);
                i.Show("Новое количество");
                if (i.OkFlag)
                {
                    newQuantity = i.QtyInt;
                    resume = true;
                }

                if (resume)
                {
                    DetailGrid.SelectedItem.CheckAdd("QUANTITY_NEW", newQuantity.ToString());
                    DetailGrid.SelectedItem.CheckAdd("EDITED", "1");
                }

                DetailGrid.UpdateItems();
                UpdateActions();
            }
        }

        public void FindInvoiceByAdjusmentSaleDetail()
        {
            bool resume = false;
            string invoiceNumber = "";
            string invoiceDate = "";
            if (!string.IsNullOrEmpty(DetailGrid.SelectedItem.CheckGet("INCOMING_NAME_STH")) && !string.IsNullOrEmpty(DetailGrid.SelectedItem.CheckGet("INCOMING_DATAOPRSTH")))
            {
                invoiceNumber = DetailGrid.SelectedItem.CheckGet("INCOMING_NAME_STH");
                invoiceDate = DetailGrid.SelectedItem.CheckGet("INCOMING_DATAOPRSTH");
                resume = true;
            }
            else if (!string.IsNullOrEmpty(DetailGrid.SelectedItem.CheckGet("CONSUMPTION_NAME_STH")) && !string.IsNullOrEmpty(DetailGrid.SelectedItem.CheckGet("CONSUMPTION_DATAOPRSTH")))
            {
                invoiceNumber = DetailGrid.SelectedItem.CheckGet("CONSUMPTION_NAME_STH");
                invoiceDate = DetailGrid.SelectedItem.CheckGet("CONSUMPTION_DATAOPRSTH");
                resume = true;
            }

            if (resume)
            {
                Dictionary<string, string> contextObject = new Dictionary<string, string>();
                contextObject.Add("INVOICE_ID", invoiceNumber);
                contextObject.Add("INVOICE_DATE", invoiceDate);

                // Отправляем сообщение вкладке "Список продаж" обновиться
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "Preproduction",
                    ReceiverName = "SaleList",
                    SenderName = "Adjustment",
                    Action = "Find",
                    Message = "",
                    ContextObject = contextObject,
                }
                );

                Central.WM.SetActive("SaleList");
                Close();
            }
        }

        /// <summary>
        /// Создание новой корректировки
        /// </summary>
        public void SaveAdjusment()
        {
            if (Form.Validate())
            {
                if (InReportingPeriod())
                {
                    AdjusmentToolBar.IsEnabled = false;

                    var p = new Dictionary<string, string>();
                    p.Add("INVOICE_ID", InvoiceId.ToString());
                    p.AddRange(Form.GetValues());

                    if ((bool)AdjustmenRadioButton.IsChecked)
                    {
                        p.Add("ADJUSTMENT_TYPE", "0");
                    }
                    else if ((bool)ErrorRadioButton.IsChecked)
                    {
                        p.Add("ADJUSTMENT_TYPE", "1");
                    }

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Sales");
                    q.Request.SetParam("Object", "Sale");
                    q.Request.SetParam("Action", "SaveAdjusment");
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
                                AdjusmentId = dataSet.Items.First().CheckGet("ADJUSMENT_ID").ToInt();

                                // Заполняем данные грида детализации по созданной корректировке
                                DetailGridDataSet = ListDataSet.Create(result, "DETAILS");
                                DetailGrid.UpdateItems(DetailGridDataSet);
                                if (DetailGridDataSet != null && DetailGridDataSet.Items != null && DetailGridDataSet.Items.Count > 0)
                                {
                                    AdjusmentDetailToolBar.IsEnabled = true;
                                    UpdateActions();
                                }
                            }
                        }

                        if (AdjusmentId > 0)
                        {
                            // Отправляем сообщение вкладке "Позиции накладной расхода" с новый Ид корректировки по этой накладной
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "Preproduction",
                                ReceiverName = "ConsumptionList",
                                SenderName = "Adjustment",
                                Action = "SetAdjustmentId",
                                Message = $"{AdjusmentId}",
                                ContextObject = Form.GetValues(),
                            }
                            );

                            // Отправляем сообщение вкладке "Список продаж" обновиться
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "Preproduction",
                                ReceiverName = "SaleList",
                                SenderName = "Adjustment",
                                Action = "Refresh",
                                Message = "",
                            }
                            );
                        }
                        else
                        {
                            string msg = $"При создании новой корректировки произошла ошибка. Пожалуйста, сообщите о проблеме.";
                            var d = new DialogWindow($"{msg}", "Корректировка по накладной", "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        q.ProcessError();

                        // Активируем тулбар только если не смогли создать корректировку
                        AdjusmentToolBar.IsEnabled = true;
                    }
                }
                else
                {
                    string msg = $"Дата не в отчётном периоде";
                    var d = new DialogWindow($"{msg}", "Корректировка по накладной", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }

        /// <summary>
        /// Получаем данные по существующей корректировке
        /// </summary>
        public void LoadAdjusment()
        {
            var p = new Dictionary<string, string>();
            p.Add("ADJUSMENT_ID", AdjusmentId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "Sale");
            q.Request.SetParam("Action", "GetAdjusment");
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
                    Form.SetValues(dataSet);
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        if (dataSet.Items.First().CheckGet("ADJUSTMENT_TYPE").ToInt() == 1)
                        {
                            ErrorRadioButton.IsChecked = true;
                        }
                        else
                        {
                            AdjustmenRadioButton.IsChecked = true;
                        }

                        if (dataSet.Items.First().CheckGet("ADJUSTMENT_VIRTUAL_FLAG").ToInt() > 0)
                        {
                            VirtualIncomingCheckBox.IsChecked = true;
                        }
                        else
                        {
                            VirtualIncomingCheckBox.IsChecked = false;
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Получаем данные по позициям корректировки
        /// </summary>
        public void LoadAdjusmentDetail()
        {
            var p = new Dictionary<string, string>();
            p.Add("ADJUSMENT_ID", AdjusmentId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "Sale");
            q.Request.SetParam("Action", "GetAdjusmentDetail");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // Заполняем данные грида детализации по созданной корректировке
                    DetailGridDataSet = ListDataSet.Create(result, "DETAILS");
                    DetailGrid.UpdateItems(DetailGridDataSet);
                    if (DetailGridDataSet != null && DetailGridDataSet.Items != null && DetailGridDataSet.Items.Count > 0)
                    {
                        AdjusmentDetailToolBar.IsEnabled = true;
                        UpdateActions();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Получаем следующий номер для корректировки
        /// </summary>
        private void GetAdjusmentNumber()
        {
            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"{this.FactoryId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "Adjustment");
            q.Request.SetParam("Action", "GetNextNumber");
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
                        string adjustmentNumber = dataSet.Items[0].CheckGet("NEXT_NUMBER");
                        Form.SetValueByPath("ADJUSTMENT_NUMBER", adjustmentNumber);
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Сохраняем данные по изменённым позициям в корректировке
        /// </summary>
        public void SaveAdjusmentDetail()
        {
            if (InReportingPeriod())
            {
                DetailGrid.ShowSplash();
                AdjusmentDetailToolBar.IsEnabled = false;

                var p = new Dictionary<string, string>();
                p.Add("ADJUSMENT_ID", AdjusmentId.ToString());
                p.Add("VIRTUAL_INCOMING", ((bool)VirtualIncomingCheckBox.IsChecked).ToInt().ToString());
                var editedRows = DetailGrid.GridItems.Where(x => x.CheckGet("EDITED").ToInt() == 1).ToList();
                p.Add("EDITED_ROWS", JsonConvert.SerializeObject(editedRows));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Sale");
                q.Request.SetParam("Action", "UpdateAdjusmentDetail");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    int adjusmentId = 0;
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");
                        if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                        {
                            adjusmentId = dataSet.Items.First().CheckGet("ADJUSMENT_ID").ToInt();
                        }
                    }

                    if (adjusmentId > 0)
                    {
                        foreach (var item in DetailGrid.GridItems)
                        {
                            if (item.CheckGet("EDITED").ToInt() == 1)
                            {
                                item.CheckAdd("EDITED", "0");
                            }
                        }
                        DetailGrid.UpdateItems();
                        UpdateActions();

                        // Отправляем сообщение
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "Sales",
                            ReceiverName = "AdjustmentList",
                            SenderName = "ChoiseSignotory",
                            Action = "Refresh",
                            Message = "",
                        }
                        );
                    }
                    else
                    {
                        string msg = $"При обновлении корректировки произошла ошибка. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Корректировка по накладной", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }

                DetailGrid.HideSplash();
                AdjusmentDetailToolBar.IsEnabled = true;
            }
            else
            {
                string msg = $"Дата не в отчётном периоде";
                var d = new DialogWindow($"{msg}", "Корректировка по накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Проводим корректировку
        /// </summary>
        public void ConfirmAdjusment()
        {
            if (InReportingPeriod())
            {
                DetailGrid.ShowSplash();
                AdjusmentDetailToolBar.IsEnabled = false;

                var p = new Dictionary<string, string>();
                p.Add("ADJUSMENT_ID", AdjusmentId.ToString());
                p.Add("VIRTUAL_INCOMING", ((bool)VirtualIncomingCheckBox.IsChecked).ToInt().ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Sale");
                q.Request.SetParam("Action", "ConfirmAdjusment");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    int adjusmentId = 0;
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");
                        if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                        {
                            adjusmentId = dataSet.Items.First().CheckGet("ADJUSMENT_ID").ToInt();
                        }
                    }

                    if (adjusmentId > 0)
                    {
                        LoadAdjusmentDetail();

                        // Отправляем сообщение
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "Sales",
                            ReceiverName = "AdjustmentList",
                            SenderName = "ChoiseSignotory",
                            Action = "Refresh",
                            Message = "",
                        }
                        );
                    }
                    else
                    {
                        string msg = $"При проведении корректировки произошла ошибка. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Корректировка по накладной", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }

                DetailGrid.HideSplash();
                AdjusmentDetailToolBar.IsEnabled = true;
            }
            else
            {
                string msg = $"Дата не в отчётном периоде";
                var d = new DialogWindow($"{msg}", "Корректировка по накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Отменяем проведение корректировки
        /// </summary>
        public void UnconfirmAdjusment()
        {
            if (InReportingPeriod())
            {
                DetailGrid.ShowSplash();
                AdjusmentDetailToolBar.IsEnabled = false;

                var p = new Dictionary<string, string>();
                p.Add("ADJUSMENT_ID", AdjusmentId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sales");
                q.Request.SetParam("Object", "Sale");
                q.Request.SetParam("Action", "UnconfirmAdjusment");
                q.Request.SetParams(p);
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    int adjusmentId = 0;
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");
                        if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                        {
                            adjusmentId = dataSet.Items.First().CheckGet("ADJUSMENT_ID").ToInt();
                        }
                    }

                    if (adjusmentId > 0)
                    {
                        LoadAdjusmentDetail();

                        // Отправляем сообщение
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "Sales",
                            ReceiverName = "AdjustmentList",
                            SenderName = "ChoiseSignotory",
                            Action = "Refresh",
                            Message = "",
                        }
                        );
                    }
                    else
                    {
                        string msg = $"При отмене проведения корректировки произошла ошибка. Пожалуйста, сообщите о проблеме.";
                        var d = new DialogWindow($"{msg}", "Корректировка по накладной", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }

                DetailGrid.HideSplash();
                AdjusmentDetailToolBar.IsEnabled = true;
            }
            else
            {
                string msg = $"Дата не в отчётном периоде";
                var d = new DialogWindow($"{msg}", "Корректировка по накладной", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "help":
                    {
                        Central.ShowHelp("/doc/l-pack-erp/sales/sale_list/consumption_list/adjustment");
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Проверяем, что находимся в отчётном периоде
        /// </summary>
        /// <returns></returns>
        public bool InReportingPeriod()
        {
            bool result = false;

            if (Form != null && !string.IsNullOrEmpty(ReportingPeriod))
            {
                if (Form.GetValueByPath("ADJUSTMENT_DATE").ToDateTime() >= ReportingPeriod.ToDateTime())
                {
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Обновляем доступные действия
        /// </summary>
        public void UpdateActions()
        {
            if (Form != null)
            {
                CreateAdjusmentButton.IsEnabled = false;

                ConfirmButton.IsEnabled = false;
                UnconfirmButton.IsEnabled = false;
                SaveAdjusmentDetailButton.IsEnabled = false;

                if (InReportingPeriod())
                {
                    if (AdjusmentId == 0)
                    {
                        CreateAdjusmentButton.IsEnabled = true;
                    }

                    if (DetailGrid != null && DetailGrid.GridItems != null && DetailGrid.GridItems.Count > 0)
                    {
                        // Если есть порожденный приход или расход, значит корректировка уже проведена
                        if (DetailGrid.GridItems.Count(x => x.CheckGet("CONSUMPTION_ID").ToInt() > 0) > 0
                            || DetailGrid.GridItems.Count(x => x.CheckGet("INCOMING_ID").ToInt() > 0) > 0)
                        {
                            ConfirmButton.IsEnabled = false;
                            UnconfirmButton.IsEnabled = true;

                            SaveAdjusmentDetailButton.IsEnabled = false;
                        }
                        else
                        {
                            ConfirmButton.IsEnabled = true;
                            UnconfirmButton.IsEnabled = false;

                            SaveAdjusmentDetailButton.IsEnabled = true;
                            if (DetailGrid.GridItems.Count(x => x.CheckGet("EDITED").ToInt() > 0) > 0)
                            {
                                SaveAdjusmentDetailButton.Style = (Style)SaveAdjusmentDetailButton.TryFindResource("FButtonPrimary");

                                ConfirmButton.IsEnabled = false;
                                UnconfirmButton.IsEnabled = false;
                            }
                            else
                            {
                                SaveAdjusmentDetailButton.Style = (Style)SaveAdjusmentDetailButton.TryFindResource("Button");
                            }
                        }
                    }
                }

                ProcessPermissions();
            }
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName = $"{ControlName}_{InvoiceId}";
            Central.WM.Close(frameName);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void _ProcessMessages(ItemMessage m)
        {

        }

        public void PrintUKD()
        {
            DocumentPrintManager documentPrintManager = new DocumentPrintManager();
            documentPrintManager.RoleName = this.RoleName;
            documentPrintManager.SetSignotory(SignotoryEmployeeId, SignotoryEmployeeName);
            documentPrintManager.InvoiceId = InvoiceId;
            documentPrintManager.AdjustmentId = AdjusmentId;
            documentPrintManager.FormInit();
            documentPrintManager.SetDefaults();
            documentPrintManager.LoadInvoiceData();
            Dictionary<string, string> documentCountList = new Dictionary<string, string>();
            documentCountList.Add("UNIVERSAL_ADJUSTMENT_DOC", "1");
            documentPrintManager.Form.SetValues(documentCountList);
            documentPrintManager.PrintDocument();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessCommand("help");
        }

        private void AdjustmenRadioButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorRadioButton.IsChecked = false;
        }

        private void ErrorRadioButton_Click(object sender, RoutedEventArgs e)
        {
            AdjustmenRadioButton.IsChecked = false;
        }

        private void CreateAdjusmentButton_Click(object sender, RoutedEventArgs e)
        {
            SaveAdjusment();
        }

        private void SaveAdjusmentDetailButton_Click(object sender, RoutedEventArgs e)
        {
            SaveAdjusmentDetail();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            ConfirmAdjusment();
        }

        private void UnconfirmButton_Click(object sender, RoutedEventArgs e)
        {
            UnconfirmAdjusment();
        }

        private void PrintUKDButton_Click(object sender, RoutedEventArgs e)
        {
            PrintUKD();
        }

        private void PrintReceiptButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void FindInvoiceByAdjusmentSaleDetailButton_Click(object sender, RoutedEventArgs e)
        {
            FindInvoiceByAdjusmentSaleDetail();
        }
    }
}
