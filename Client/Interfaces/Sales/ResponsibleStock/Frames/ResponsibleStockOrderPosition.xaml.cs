using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
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
    /// Окно редактирования позиций заявки
    /// </summary>
    public partial class ResponsibleStockOrderPosition : UserControl
    {
        public ResponsibleStockOrderPosition()
        {
            FrameName = "ResponsibleStockOrderPosition";

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();

            GridInit();

            if (Central.DebugMode)
            {
                PriceBySpecificationWithoNdsTextBox.IsReadOnly = false;
                SpecificationTextBox.IsReadOnly = false;
                ActualSpecificationTextBox.IsReadOnly = false;
            }
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
        /// Выбранная запись в гриде
        /// </summary>
        public Dictionary<string, string> GridSelectedItem { get; set; }

        public ListDataSet GridDataSet { get; set; }

        /// <summary>
        /// nsthet
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// iddog
        /// </summary>
        public int ContractId { get; set; }

        /// <summary>
        /// idorderdates
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// idadres
        /// </summary>
        public int AddressId { get; set; }

        /// <summary>
        /// iddir
        /// </summary>
        public int DirId { get; set; }

        /// <summary>
        /// dt
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// id_ts
        /// </summary>
        public int TransportId { get; set; }

        /// <summary>
        /// finish
        /// </summary>
        public int Finish { get; set; }

        /// <summary>
        /// id2
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// id_dsd
        /// </summary>
        public int SpecificationId { get; set; }

        /// <summary>
        /// Список уже добавленных в заявку позиций
        /// </summary>
        public List<string> SelectedCodeList { get; set; }

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
                    Path="QUANTITY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QuantityTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="ACTUAL_SPECIFICATION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ActualSpecificationTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SPECIFICATION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SpecificationTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COUNT_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CountPalletTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ACTUAL_PRICE_WITHOUT_NDS",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=ActualPriceWithoutNdsTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                         { FormHelperField.FieldFilterRef.Required, null },
                         { FormHelperField.FieldFilterRef.IsNotZero, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRICE_WITHOUT_NDS",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=PriceBySpecificationWithoNdsTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE_STOCKMAN",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NoteStockmanTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE_LOADER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NoteLoaderTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                 {
                    Path="SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SearchText,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = GridToolbar;
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void GridInit()
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
                        MinWidth=55,
                        MaxWidth=55,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="PRODUCT_FULL_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                        MaxWidth=350,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Паллет на СОХ",
                        Path="PALLET_QUANTITY_SOH",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=100,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    //Нет доступных поддонов на СОХ
                                    if( row.CheckGet("PALLET_QUANTITY_SOH").ToInt() == 0 )
                                    {
                                        color = HColor.Red;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Штук на СОХ",
                        Path="PRODUCT_QUANTITY_SOH",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=48,
                        MaxWidth=90,
                                                Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    //Нет доступной продукции на СОХ
                                    if( row.CheckGet("PRODUCT_QUANTITY_SOH").ToInt() == 0 )
                                    {
                                        color = HColor.Red;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Категория",
                        Path="IDK1",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=30,
                        MaxWidth=75,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Артикул",
                        Path="ARTIKUL",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                        MaxWidth=120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Картон",
                        Path="DESCRIPTION",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                        MaxWidth=120,
                    },
                    new DataGridHelperColumn
                    {
                        Header="На паллете",
                        Path="QUANTITY_ON_PALLET",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=80,
                    },
                    new DataGridHelperColumn
                    {
                        Header="На складе Л-ПАК",
                        Path="QUANTITY_IN_STOCK",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=55,
                        MaxWidth=115,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Внутреннее наименование",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=120,
                        MaxWidth=350,
                    },

                    new DataGridHelperColumn
                    {
                        Header="Добавлен в заявку",
                        Path="ALREADY_SELECTED",
                        ColumnType=ColumnTypeRef.Boolean,
                        MinWidth=55,
                        MaxWidth=80,
                        Hidden=true,
                    },

                    new DataGridHelperColumn
                    {
                        Header=" ",
                        Path="_",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=2,
                        MaxWidth=2000,
                    },
                };
                Grid.SetColumns(columns);

                // раскраска строк
                Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            if(row.CheckGet("ALREADY_SELECTED").ToInt() == 1)
                            {
                                color = HColor.Blue;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };

                Grid.SearchText = SearchText;
                Grid.Init();

                //при выборе строки в гриде, обновляются актуальные действия для записи
                Grid.OnSelectItem = selectedItem =>
                {
                    GridSelectedItem = selectedItem;
                    UpdatePalletCount();
                    UpdateSpecification();
                    UpdatePrice();
                };

                //данные грида
                Grid.OnLoadItems = GridLoadItems;
                Grid.Run();

            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
            GridSelectedItem = new Dictionary<string, string>();
            GridDataSet = new ListDataSet();

            NoteStockmanTextBox.Text = "Точное количество изделий на поддон";
            NoteLoaderTextBox.Text = "Точное количество изделий на поддон";
        }

        public async void GridLoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>();

            if (ProductId > 0)
            {
                p.Add("ID2", ProductId.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "ResponsibleStock");
            q.Request.SetParam("Action", "ListProduct");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    GridDataSet = dataSet;

                    if (GridDataSet != null && GridDataSet.Items.Count > 0)
                    {
                        foreach (var item in GridDataSet.Items)
                        {
                            if (SelectedCodeList.Contains(item.CheckGet("ARTIKUL")))
                            {
                                item.CheckAdd("ALREADY_SELECTED", "1");
                            }
                        }
                    }

                    Grid.UpdateItems(GridDataSet);
                }
            }
            else
            {
                q.ProcessError();
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

            var dt = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");

            FrameName = $"{FrameName}_new_{dt}";

            if (OrderId > 0)
            {
                GetOrder();
                Central.WM.Show(FrameName, $"Позиция заявки #{OrderId}", true, "add", this);
            }
            else
            {
                Central.WM.Show(FrameName, $"Новая позиция заявки", true, "add", this);
            }
            
        }

        public void GetOrder()
        {
            var p = new Dictionary<string, string>();
            p.Add("IDORDERDATES", OrderId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "ResponsibleStock");
            q.Request.SetParam("Action", "GetOrderPositionById");
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
                    if (dataSet != null && dataSet.Items.Count > 0)
                    {
                        var firstDictionsry = dataSet.Items.First();

                        ProductId = firstDictionsry.CheckGet("PRODUCT_ID").ToInt();
                        QuantityTextBox.Text = firstDictionsry.CheckGet("QUANTITY").ToInt().ToString();
                        NoteLoaderTextBox.Text = firstDictionsry.CheckGet("NOTE_LOADER");
                        NoteStockmanTextBox.Text = firstDictionsry.CheckGet("NOTE_STOCKMAN");
                        ActualPriceWithoutNdsTextBox.Text = firstDictionsry.CheckGet("PRICE_VAT_EXCLUDED").ToDouble().ToString();

                        if (firstDictionsry.CheckGet("PRODUCTION_TASK_FLAG").ToInt() > 0)
                        {
                            QuantityTextBox.IsReadOnly = true;
                        }
                        else
                        {
                            QuantityTextBox.IsReadOnly = false;
                        }

                        GridLoadItems();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public void UpdatePalletCount()
        {
            if (CountPalletTextBox != null)
            {
                if (QuantityTextBox.Text.ToInt() > 0 && GridSelectedItem != null && GridSelectedItem.CheckGet("QUANTITY_ON_PALLET").ToInt() > 0)
                {
                    CountPalletTextBox.Text = Math.Ceiling(QuantityTextBox.Text.ToDouble() / GridSelectedItem.CheckGet("QUANTITY_ON_PALLET").ToDouble()).ToString();
                }
                else
                {
                    CountPalletTextBox.Clear();
                }
            }
        }

        public void UpdateActualFields()
        {
            if (OrderId > 0 || !(Finish == 1))
            {
                ActualPriceWithoutNdsTextBox.Text = PriceBySpecificationWithoNdsTextBox.Text;
                ActualSpecificationTextBox.Text = SpecificationTextBox.Text;
            }
        }

        public void Save()
        {
            DisableControls();

            if (GridSelectedItem != null && GridSelectedItem.CheckGet("PRODUCT_ID").ToInt() > 0)
            {
                if (Form.Validate())
                {
                    // если существующая позиция, то обновляем
                    if (OrderId > 0)
                    {
                        bool succesfullflag = false;

                        var p = new Dictionary<string, string>();

                        p.Add("NSTHET", Id.ToString());
                        p.Add("ID_TS", TransportId.ToString());

                        p.Add("IDORDERDATES", OrderId.ToString());
                        p.Add("QUANTITY", QuantityTextBox.Text);
                        p.Add("NOTE_LOADER", NoteLoaderTextBox.Text);
                        p.Add("NOTE_STOCKMAN", NoteStockmanTextBox.Text);
                        p.Add("ID_ADDRESS", AddressId.ToString());

                        if (!string.IsNullOrEmpty(ActualPriceWithoutNdsTextBox.Text))
                        {
                            p.Add("PRICE_VAT_EXCLUDED", ActualPriceWithoutNdsTextBox.Text);
                        }
                        else
                        {
                            p.Add("PRICE_VAT_EXCLUDED", null);
                        }

                        if (PriceBySpecificationWithoNdsTextBox.Text == ActualPriceWithoutNdsTextBox.Text)
                        {
                            p.Add("ID_DSD", SpecificationId.ToString());
                        }
                        else
                        {
                            p.Add("ID_DSD", null);
                        }

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Sales");
                        q.Request.SetParam("Object", "ResponsibleStock");
                        q.Request.SetParam("Action", "UpdateOrderPosition");
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
                                if (dataSet != null && dataSet.Items.Count > 0)
                                {
                                    int orderId = dataSet.Items.First().CheckGet("IDORDERDATES").ToInt();

                                    if (orderId > 0)
                                    {
                                        succesfullflag = true;
                                    }
                                }
                            }

                            if (succesfullflag)
                            {
                                // отправляем сообщение гриду заявок обновиться
                                {
                                    Messenger.Default.Send(new ItemMessage()
                                    {
                                        ReceiverGroup = "ResponsibleStock",
                                        ReceiverName = "ResponsibleStockList",
                                        SenderName = "ResponsibleStockOrderPosition",
                                        Action = "Refresh",
                                        Message = "",
                                    }
                                    );
                                }

                                string msg = "Успшное обновление позиции заявки";
                                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                d.ShowDialog();

                                Close();
                            }
                            else
                            {
                                string msg = "Ошибка обновления позиции заявки";
                                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                        }
                        else
                        {
                            q.ProcessError();
                        }
                    }
                    // если новая, то создаём новую запись
                    else
                    {
                        var percentageOfDivision = QuantityTextBox.Text.ToInt() % GridSelectedItem.CheckGet("QUANTITY_ON_PALLET").ToInt();

                        if (percentageOfDivision > 0)
                        {
                            int palletCount = QuantityTextBox.Text.ToInt() / GridSelectedItem.CheckGet("QUANTITY_ON_PALLET").ToInt();
                            palletCount += 1;
                            int quantityWithoutRemainder = palletCount * GridSelectedItem.CheckGet("QUANTITY_ON_PALLET").ToInt();

                            var message = $"Выбранное количество продукции не кратно поддону.{Environment.NewLine}" +
                                $"Для {palletCount} полных поддонов нужно выбрать {quantityWithoutRemainder} продукции.{Environment.NewLine}" +
                                $"Вы хотите продолжить c неполным поддоном?";

                            if (DialogWindow.ShowDialog(message, "Предупреждение", "", DialogWindowButtons.NoYes) != true)
                            {
                                EnableControls();
                                return;
                            }
                        }

                        bool succesfullflag = false;

                        var p = new Dictionary<string, string>();

                        p.Add("ID_TS", TransportId.ToString());

                        p.Add("NSTHET", Id.ToString());
                        p.Add("QUANTITY", QuantityTextBox.Text);
                        p.Add("ID2", GridSelectedItem.CheckGet("PRODUCT_ID"));
                        p.Add("IDK1", GridSelectedItem.CheckGet("IDK1"));
                        p.Add("NOTE_LOADER", NoteLoaderTextBox.Text);
                        p.Add("NOTE_STOCKMAN", NoteStockmanTextBox.Text);
                        p.Add("ID_ADDRESS", AddressId.ToString());

                        if (!string.IsNullOrEmpty(ActualPriceWithoutNdsTextBox.Text))
                        {
                            p.Add("PRICE_VAT_EXCLUDED", ActualPriceWithoutNdsTextBox.Text);
                        }
                        else
                        {
                            p.Add("PRICE_VAT_EXCLUDED", null);
                        }

                        if (PriceBySpecificationWithoNdsTextBox.Text == ActualPriceWithoutNdsTextBox.Text)
                        {
                            p.Add("ID_DSD", SpecificationId.ToString());
                        }
                        else 
                        {
                            p.Add("ID_DSD", null);
                        }

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Sales");
                        q.Request.SetParam("Object", "ResponsibleStock");
                        q.Request.SetParam("Action", "SaveOrderPosition");
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
                                if (dataSet != null && dataSet.Items.Count > 0)
                                {
                                    int orderId = dataSet.Items.First().CheckGet("IDORDERDATES").ToInt();

                                    if (orderId > 0)
                                    {
                                        succesfullflag = true;
                                    }
                                }
                            }

                            if (succesfullflag)
                            {
                                // отправляем сообщение гриду заявок обновиться
                                {
                                    Messenger.Default.Send(new ItemMessage()
                                    {
                                        ReceiverGroup = "ResponsibleStock",
                                        ReceiverName = "ResponsibleStockList",
                                        SenderName = "ResponsibleStockOrderPosition",
                                        Action = "Refresh",
                                        Message = "",
                                    }
                                    );
                                }

                                string msg = "Успшное создание позиции заявки";
                                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                d.ShowDialog();

                                Close();
                            }
                            else
                            {
                                string msg = "Ошибка создания позиции заявки";
                                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                                d.ShowDialog();
                            }
                        }
                        else
                        {
                            q.ProcessError();
                        }
                    }
                }
                else
                {
                    string msg = "Проверьте корректность введённых данных";
                    var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else 
            {
                string msg = "Выберите продукцию";
                var d = new DialogWindow($"{msg}", "Склад ответственного хранения", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }

            EnableControls();
        }

        public void UpdatePrice()
        {
            if(GridSelectedItem != null && GridSelectedItem.Count > 0)
            {
                var price = GridSelectedItem.CheckGet("PRICE").ToDouble();
                ActualPriceWithoutNdsTextBox.Text = price.ToString();
            }
        }

        public async void UpdateSpecification()
        {
            var p = new Dictionary<string, string>();
            p.Add("ID2", GridSelectedItem.CheckGet("PRODUCT_ID"));
            p.Add("IDK1", GridSelectedItem.CheckGet("IDK1"));
            p.Add("ID_DOG", ContractId.ToString());
            p.Add("DT", Data);
            p.Add("ID_DIR", DirId.ToString());
            p.Add("ID_ADDRESS", AddressId.ToString());
            p.Add("SELFSHIP", "0");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sales");
            q.Request.SetParam("Object", "ResponsibleStock");
            q.Request.SetParam("Action", "GetSpecification");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "ITEMS");
                    if (dataSet != null && dataSet.Items.Count > 0)
                    {
                        var firstDictionary = dataSet.Items.First();

                        if (firstDictionary.CheckGet("ID_DSD").ToInt() > 0)
                        {
                            SpecificationId = firstDictionary.CheckGet("ID_DSD").ToInt();
                        }

                        if (firstDictionary.ContainsKey("SPECIFICATION") && string.IsNullOrEmpty(firstDictionary.CheckGet("SPECIFICATION")))
                        {
                            SpecificationTextBox.Clear();
                            PriceBySpecificationWithoNdsTextBox.Clear();
                        }
                        else
                        {
                            SpecificationTextBox.Text = SpecificationId.ToString();

                            if (QuantityTextBox.Text.ToInt() > 0 && GridSelectedItem.CheckGet("VOLUM").ToInt() > 0 && firstDictionary.CheckGet("SQUARE_LIMIT").ToInt() > 0)
                            {
                                if ((QuantityTextBox.Text.ToInt() * GridSelectedItem.CheckGet("VOLUM").ToInt()) < firstDictionary.CheckGet("SQUARE_LIMIT").ToInt())
                                {
                                    PriceBySpecificationWithoNdsTextBox.Text = firstDictionary.CheckGet("PRICE_VAT_EXCLUDED_LIMIT");
                                }
                                else
                                {
                                    PriceBySpecificationWithoNdsTextBox.Text = firstDictionary.CheckGet("PRICE_VAT_EXCLUDED");
                                }
                            }
                            else
                            {
                                PriceBySpecificationWithoNdsTextBox.Text = firstDictionary.CheckGet("PRICE_VAT_EXCLUDED");
                            }
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
        /// Деактивация контроллов
        /// </summary>
        public void DisableControls()
        {
            GridToolbar.IsEnabled = false;
            Grid.IsEnabled = false;
        }

        /// <summary>
        /// Активация контроллов
        /// </summary>
        public void EnableControls()
        {
            GridToolbar.IsEnabled = true;
            Grid.IsEnabled = true;
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
                ReceiverGroup = "Sales",
                ReceiverName = "",
                SenderName = "ResponsibleStockOrderPosition",
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
            //FIXME: Нужно сделать документацию
            Central.ShowHelp("/doc/l-pack-erp/");
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePalletCount();
        }

        private void PriceBySpecificationWithoNdsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateActualFields();
        }
    }
}
