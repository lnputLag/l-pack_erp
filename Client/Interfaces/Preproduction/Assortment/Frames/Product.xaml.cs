using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Common.LPackClientRequest;
using System.Windows.Input;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Фрейм редактирования заготовки на изделие
    /// </summary>
    public partial class Product : ControlBase
    {
        public Product()
        {
            InitializeComponent();

            InitForm();
            InitGrid();
            SetDefaults();

            OnLoad = () =>
            {
                SetFieldsAvailable();
                //GetData();
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    switch (e.Key)
                    {
                        case Key.F1:
                            ProcessCommand("help");
                            e.Handled = true;
                            break;
                    }
                }
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessCommand(msg.Action, msg);
                }
            };
        }

        /// <summary>
        /// Идентификатор изделия
        /// </summary>
        public int ProductId;
        /// <summary>
        /// Имя вкладки, откуда вызвана форма и куда передается ответ
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Форма редактирования задания
        /// </summary>
        FormHelper Form { get; set; }

        /// <summary>
        /// Тип изделия. По типу настраивается доступность полей для редактирования
        /// </summary>
        public int CategoryId;
        /// <summary>
        /// Идентификатор техкарты для внесения
        /// </summary>
        public int IdTk = 0;
        /// <summary>
        /// Наличие схемы производства. Для изделий без схемы производства (листы и фанфолд) показываем блок настройки рилёвок
        /// </summary>
        public bool SchemeExists;
        /// <summary>
        /// Признак, что были изменены размеры заготовки, при сохранении надо пересчитать площадь
        /// </summary>
        private bool SizeChanged;
        /// <summary>
        /// Признак, что загрузка завершена
        /// </summary>
        private bool Initialized;
        /// <summary>
        /// Признак, что изменилось количество ярлыков для печати
        /// </summary>
        private bool LabelQtyChanged;

        /// <summary>
        /// Список названий типов рилевок для выпадающего списка
        /// </summary>
        private Dictionary<string, string> CreaseTypeList { get; set; }
        /// <summary>
        /// Данные для таблицы рилевок
        /// </summary>
        private ListDataSet CreaseDS { get; set; }

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
        public void ProcessCommand(string command, ItemMessage m = null)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "add":
                        int lastNum = 0;
                        if (CreaseDS.Items.Count > 0)
                        {
                            lastNum = CreaseDS.Items.Last().CheckGet("NUM").ToInt();
                        }
                        EditCrease(new Dictionary<string, string>() { { "NUM", (lastNum + 1).ToString() }, { "CREASE", "" } });
                        break;
                    case "delete":
                        DeleteCrease();
                        break;
                    case "close":
                        Close();
                        break;
                    case "save":
                        Save();
                        break;
                    case "savecrease":
                        if (m.ContextObject != null)
                        {
                            var v = (Dictionary<string, string>)m.ContextObject;
                            SaveCrease(v);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="NAME2",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=AdvancedName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DETAILS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Details,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ARTICLE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Article,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ARTICLE_FOR_PRINT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ArticleForPrint,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CUSTOMER_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CustomerName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRINT_CUSTOMER_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=PrintCustomerFlag,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="INTERMEDIARY_LABEL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=IntermediaryLabel,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LABEL_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=LabelQty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FIX_PRICE",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=FixPrice,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ADDITIONAL_PALLET_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=AdditionalPalletFlag,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SPECIAL_TASK_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=SpecialTaskNumber,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PALLET_PLASTIC_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PalletPlasticFlag,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="UNDISPOSAL_PALLET_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=UndisposalPalletFlag,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TECHNICAL_CONDITION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TechnicalCondition,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FOREIGN_INVOICE_CODE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ForeignInvoiceCode,
                    Doc="Код для печати на документах покупателей из Белоруссии",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="EDM_PRODUCT_CODE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=EdmCode,
                    Doc="Код продукции для отображения в УПД ЭДО",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BARCODE_CUSTOMER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=BarcodeCustomer,
                    Doc="",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BARCODE_CUSTOMER_NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=BarcodeCustomerNum,
                    Doc="Код для печати на документах покупателей из Белоруссии",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCTION_LIPETSK",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=ProductionLipetsk,
                    ControlType="CheckBox",
                    Doc="Признак производства в Липецке",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCTION_KASHIRA",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=ProductionKashira,
                    ControlType="CheckBox",
                    Doc="Признак производства в Кашире",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="MEASURE_UNIT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=MeasureUnit,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Length,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Width,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="_CREASE_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CreaseType,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CREASE_SYMMETRIC",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CreaseSymmetric,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE_FOR_LOADER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NoteForLoader,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE_FOR_STOREKEEPER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NoteForStorekeeper,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE_ORDER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=NoteForOrder,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ECT_MIN",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Format = "0.############",
                    Control=EctMinTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="ECT_MAX",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Format = "0.############",
                    Control=EctMaxTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="BCT_MIN",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Format = "0.############",
                    Control=BctMinTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="BCT_MAX",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Format = "0.############",
                    Control=BctMaxTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="BST_MIN",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Format = "0.############",
                    Control=BstMinTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="BST_MAX",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Format = "0.############",
                    Control=BstMaxTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="WEIGHT_STANDARDIZED",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Format = "0.############",
                    Control=WeightTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                    },
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// Инициализация таблицы с несимметричными рилевками
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="NUM",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=3,
                },
                new DataGridHelperColumn
                {
                    Header="Рилевка",
                    Path="CREASE",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
            };
            CreaseGrid.SetColumns(columns);
            CreaseGrid.SetPrimaryKey("NUM");
            CreaseGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            CreaseGrid.AutoUpdateInterval = 0;

            CreaseGrid.OnLoadItems = CreaseLoadItems;

            CreaseGrid.OnDblClick = selectedItem =>
            {
                EditCrease(selectedItem);
            };


            CreaseGrid.Init();
        }

        private void CreaseLoadItems()
        {
            if (CreaseDS.Items != null)
            {
                if (CreaseDS.Items.Count > 0)
                {
                    CreaseGrid.UpdateItems(CreaseDS);
                }
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            CreaseDS = new ListDataSet();
            CreaseDS.Init();

            CreaseTypeList = new Dictionary<string, string>()
            {
                { "3", "любой" },
                { "1", "папа/мама" },
                { "2", "плоская" },
                { "4", "папа/папа" },
            };

            MeasureUnit.Items = new Dictionary<string, string>()
            {
                { "55", "кв.м" },
                { "778", "упаковка" },
                { "796", "шт" },
                { "728", "пачка" },
            };

            PalletPlasticFlag.Items = new Dictionary<string, string>()
            {
                { "0", "Не определено" },
                { "1", "Использовать" },
                { "2", "Запрещено использовать" },
            };
            PalletPlasticFlag.SetSelectedItemByKey("0");

            Initialized = false;
        }

        /// <summary>
        /// Получение данных для формы
        /// </summary>
        public async void GetData()
        {
            if (ProductId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Products");
                q.Request.SetParam("Object", "Assortment");
                q.Request.SetParam("Action", "Get");
                q.Request.SetParam("ID", ProductId.ToString());

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
                        var noteOrder = ListDataSet.Create(result, "NOTE_ORDER");
                        if (noteOrder.Items.Count > 0)
                        {
                            var items = new Dictionary<string, string>();
                            items.Add("0", " ");
                            items.AddRange(noteOrder.GetItemsList("ID", "NOTE"));

                            NoteForOrder.Items = items;
                        }

                        var ds = ListDataSet.Create(result, "PRODUCT");
                        if (ds.Items.Count > 0)
                        {
                            var rec = ds.Items[0];

                            CategoryId = rec.CheckGet("CATEGORY_ID").ToInt();
                            IdTk = rec.CheckGet("ID_TK").ToInt();
                            SchemeExists = rec.CheckGet("SCHEME_EXISTS").ToBool();

                            string creaseType = rec.CheckGet("CREASE_TYPE").ToInt().ToString();
                            if (CreaseTypeList.ContainsKey(creaseType))
                            {
                                rec.CheckAdd("_CREASE_TYPE", CreaseTypeList[creaseType]);
                            }

                            // Заполнение рилевок
                            int creaseSymmetric = rec.CheckGet("CREASE_SYMMETRIC").ToInt();
                            if (creaseSymmetric == 0)
                            {
                                int crease_i = 0;
                                for (var i = 1; i <= 24; i++)
                                {
                                    var creaseKey = $"P{i}";
                                    crease_i = rec.CheckGet(creaseKey).ToInt();
                                    if (crease_i > 0)
                                    {
                                        CreaseDS.Items.Add(new Dictionary<string, string>() {
                                            { "NUM", i.ToString() },
                                            { "CREASE", crease_i.ToString() },
                                        });
                                    }
                                }
                                CreaseGrid.LoadItems();
                            }

                            if(rec.CheckGet("PRODUCTION_LIPETSK").ToInt() == 1)
                            {
                                ProductionLipetsk.IsEnabled = false;
                            }
                            
                            if(rec.CheckGet("PRODUCTION_KASHIRA").ToInt() == 1)
                            {
                                ProductionKashira.IsEnabled = false;
                            }

                        }
                        Form.SetValues(ds);
                        // Выставляем флаги
                        SizeChanged = false;
                        LabelQtyChanged = false;
                        Initialized = true;

                        ShowTab();
                    }
                }
            }
        }

        /// <summary>
        /// Настройка доступности кнопок и полей
        /// </summary>
        private void SetFieldsAvailable()
        {
            AddCreaseButton.IsEnabled = false;
            DeleteCreaseButton.IsEnabled = false;
            CreaseSymmetric.IsEnabled = false;

            //Настройка рилевок. Доступна для заготовки и листов без схемы производства
            bool creaseEditing = false;

            // Для заготовк блокируем поля
            if (CategoryId == 4)
            {
                // Прячем блок настройки ярлыка
                LabelBlock.Visibility = Visibility.Collapsed;
                creaseEditing = true;
            }
            else if ((CategoryId == 5) || (CategoryId == 6))
            {
                // Если есть схема производства - прячем блок настройки заготовки
                if (SchemeExists)
                {
                    BlankBlock.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Настройка рилёвок доступна, а размеры - нет, они должны настраиваться в техкарте
                    Length.IsEnabled = false;
                    Width.IsEnabled = false;
                    creaseEditing = true;
                }

                BctEctBstWeightBlock.Visibility = Visibility.Visible;
            }
            // литая тара
            else if (CategoryId == 16)
            {
                // Прячем поле с типом рилевки и таблицу для несимметричных рилевок
                CreaseGridPanel.Visibility = Visibility.Collapsed;
                CreaseTypeLabel.Visibility = Visibility.Collapsed;
                CreaseTypeValue.Visibility = Visibility.Collapsed;
                SymmetricLabelContent.Content = "Высота:  ";
                Length.IsEnabled = false;
                Width.IsEnabled = false;
            }

            if (creaseEditing)
            {
                if (CreaseDS.Items.Count > 0)
                {
                    CreaseSymmetric.IsEnabled = false;
                    AddCreaseButton.IsEnabled = true;
                    DeleteCreaseButton.IsEnabled = true;
                }
                else
                {
                    CreaseSymmetric.IsEnabled = true;
                }

                if (CreaseSymmetric.Text.IsNullOrEmpty())
                {
                    AddCreaseButton.IsEnabled = true;

                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="productId"></param>
        public void Edit(int productId)
        {
            ProductId = productId;
            GetData();
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void ShowTab()
        {
            ControlName = $"Product{ProductId}";
            ControlTitle = $"Изделие {ProductId}";

            Central.WM.Show(ControlName, ControlTitle, true, "add", this);
        }

        private void EditCrease(Dictionary<string, string> item)
        {
            var creaseEdit = new AssortmentCreaseEdit();
            creaseEdit.ReceiverName = ControlName;
            creaseEdit.Edit(item);
        }

        /// <summary>
        /// Сохранение значения несимметричной рилевки
        /// </summary>
        /// <param name="v"></param>
        private void SaveCrease(Dictionary<string, string> v)
        {
            var num = v.CheckGet("NUM").ToInt();
            string value = v.CheckGet("CREASE");

            if (num == CreaseDS.Items.Count + 1)
            {
                CreaseDS.Items.Add(v);
            }
            else if (num <= CreaseDS.Items.Count)
            {
                foreach (var item in CreaseDS.Items)
                {
                    if (item.CheckGet("NUM").ToInt() == num)
                    {
                        item.CheckAdd("CREASE", value);
                    }
                }
            }

            CreaseGrid.LoadItems();
        }

        /// <summary>
        /// Удаление последнего значения несимметричной рилевки
        /// </summary>
        private void DeleteCrease()
        {
            int lastNum = CreaseDS.Items.Last().CheckGet("NUM").ToInt();
            var dw = new DialogWindow($"Удалить рилевку {lastNum}?", "Удаление рилевки", "", DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                if (dw.ResultButton == DialogResultButton.Yes)
                {
                    CreaseDS.Items.RemoveAt(lastNum - 1);

                    CreaseGrid.LoadItems();
                }
            }
        }

        /// <summary>
        /// Обновление дополнительного наименования при изменении размеров заготовки
        /// </summary>
        private void UpdateName2()
        {
            AdvancedName.Text = $"{Length.Text}х{Width.Text} заготовка";
        }

        /// <summary>
        /// Закрытие вкладки с формой
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            Central.WM.SetActive(ReceiverName);
        }

        /// <summary>
        /// Проверки перед сохранением
        /// </summary>
        public void Save()
        {
            bool resume = true;
            string errorMsg = "";
            var v = Form.GetValues();

            if (Form.Validate())
            {
                v.Add("ID", ProductId.ToString());
                v.Add("ID_TK", IdTk.ToString());
                v.CheckAdd("PRODUCT_CATEGORY_ID", $"{CategoryId}");

                // Для заготовок обязательно должны быть длина и ширина
                if (resume)
                {
                    if (CategoryId == 4)
                    {
                        int l = v.CheckGet("LENGTH").ToInt();
                        int w = v.CheckGet("WIDTH").ToInt();

                        if (l == 0 || w == 0)
                        {
                            errorMsg = "Не заданы размеры заготовки";
                            resume = false;
                        }
                    }
                }

                // Заполняем поля для правильного сохранения рилевок
                if (resume)
                {
                    if (CreaseSymmetric.Text.IsNullOrEmpty())
                    {
                        if (CreaseDS.Items.Count > 0)
                        {
                            foreach (var item in CreaseDS.Items)
                            {
                                v.CheckAdd($"P{item["NUM"]}", item["CREASE"]);
                            }
                        }
                    }
                }

                // Дополнительные переменные
                if (resume)
                {
                    //Добавляем признак, что размер изменился и надо пересчитать площадь
                    v.Add("SIZE_CHANGED", SizeChanged ? "1" : "0");
                    //Добавляем признак, что изменилось количество ярлыков для печати, необходимо внести изменения в техкарту
                    v.Add("LABEL_QTY_CHANGED", LabelQtyChanged ? "1" : "0");
                }
            }
            else
            {
                errorMsg = "Не все поля заполнены верно";
                resume = false;
            }

            if (resume)
            {
                SaveData(v);
            }
            else
            {
                Form.SetStatus(errorMsg, 1);
            }
        }

        /// <summary>
        /// Сохранение данных в БД
        /// </summary>
        /// <param name="p"></param>
        private async void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Products");
            q.Request.SetParam("Object", "Assortment");
            q.Request.SetParam("Action", "Save");
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
                    if (result.ContainsKey("ITEMS"))
                    {
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "Preproduction",
                            ReceiverName = ReceiverName,
                            SenderName = ControlName,
                            Action = "Refresh",
                            ContextObject = Form.GetValues(),
                        });
                        Close();
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
            }

        }

        /// <summary>
        /// Обработчик нажатия на кнопку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            if (b != null)
            {
                var t = b.Tag.ToString();
                ProcessCommand(t);
            }
        }

        private void SizeTextChanged(object sender, TextChangedEventArgs e)
        {
            if (Initialized)
            {
                SizeChanged = true;
                UpdateName2();
            }
        }

        private void LabelQty_TextChanged(object sender, TextChangedEventArgs e)
        {
            LabelQtyChanged = true;
        }

        private void PalletPlasticFlag_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (Initialized)
            {
                LabelQtyChanged = true;
            }
        }
    }
}
