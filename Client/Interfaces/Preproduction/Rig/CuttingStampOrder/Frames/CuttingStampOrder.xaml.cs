using Client.Common;
using Client.Interfaces.Main;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction.Rig
{
    /// <summary>
    /// Форма редактирования заказа штанцформы
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class CuttingStampOrder : ControlBase
    {
        public CuttingStampOrder()
        {
            InitializeComponent();
            SetDefaults();
            InitForm();
            InitGrid();
        }

        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// Форма редактирования техкарты
        /// </summary>
        FormHelper Form { get; set; }
        /// <summary>
        /// Идентификатор заказа штанцформы
        /// </summary>
        public int CuttingStampOrderId;
        /// <summary>
        /// Статус заказа
        /// </summary>
        private int OrderStatus;
        /// <summary>
        /// Стартовая папка для загрузки файла чертежа
        /// </summary>
        private string InitialFileFolder;

        private Dictionary<string, string> ProductParams;
        /// <summary>
        /// Данные для таблицы полумуфт/элементов штанцформы
        /// </summary>
        private ListDataSet StampItemDS { get; set; }
        /// <summary>
        /// Данные для 
        /// </summary>
        private ListDataSet SupplierDS { get; set; }
        /// <summary>
        /// Данные полученных полумуфт для перезаказа
        /// </summary>
        private ListDataSet StampItemsReceivedDS { get; set; }
        /// <summary>
        /// Данные о станках
        /// </summary>
        private ListDataSet MachineDS { get; set; }
        /// <summary>
        /// Идентификатор производственной площадки: 1 - Липецк, 2 - Кашира
        /// </summary>
        public int FactoryId { get; set; }
        /// <summary>
        /// Сохраненная дата доставки заказа
        /// </summary>
        private DateTime DeliveryDttm { get; set; }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            StampItemDS = new ListDataSet();
            StampItemDS.Init();
            CuttingStampOrderId = 0;
            OrderStatus = 0;
            FactoryId = 1;
            DeliveryDttm = DateTime.MinValue;
            ProductParams = new Dictionary<string, string>();

            DrawingFileShowButton.IsEnabled = false;

            //Время доставки по умолчанию
            var deliveryDT = DateTime.Now.Date.AddDays(7).AddHours(12);
            DeliveryDateTime.DateTime = deliveryDT;

            InitialFileFolder = Central.GetStorageNetworkPathByCode("techcards");
            if (InitialFileFolder.IsNullOrEmpty())
            {
                InitialFileFolder = "\\\\file-server-4\\Техкарты\\";
            }
            InitialFileFolder = $"{InitialFileFolder}_РАСТРтехнология";
        }

        /// <summary>
        /// Обработка команд
        /// </summary>
        /// <param name="command"></param>
        private void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "save":
                        Save();
                        break;
                    case "close":
                        Close();
                        break;
                    case "formqtyinc":
                        IncreaseFormQty();
                        break;

                    case "formqtydec":
                        DecreaseFormQty();
                        break;
                    case "selectdrawing":
                        SelectDrawingFile();
                        break;
                    case "showdrawing":
                        if (File.Exists(DrawingFile.Text))
                        {
                            Central.OpenFile(DrawingFile.Text);
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
            //список полей формы
            var fields = new List<FormHelperField>
            {
                new FormHelperField()
                {
                    Path="FACTORY_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control = Factory,
                    ControlType = "SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="SUPPLIER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control = Supplier,
                    ControlType = "SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CLIENT_STAMP_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control = ClientStampFlag,
                    ControlType = "CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ORDER_NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control = OrderNum,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="OUTER_NUM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control = OuterNum,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PAYER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control = Payer,
                    ControlType = "SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="DELIVERY_DTTM",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Control = DeliveryDateTime,
                    ControlType = "DateEdit",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="DRAWING_FILE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control = DrawingFile,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DRAWING_FILE_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control = DrawingFileName,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="TECHCARD_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control = TechcardId,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control = ProductName,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="MACHINE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control = Machine,
                    ControlType = "SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FEFCO_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control = Fefco,
                    ControlType = "SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PD",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control = PD,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CUTTING_LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control = CuttingLength,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CUTTING_WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control = CuttingWidth,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="WASTE_SQUARE",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control = WasteSquare,
                    ControlType = "TextBox",
                    Format="N6",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CREASE_PERFORATION_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=CreasePerforationCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FORM_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control = FormQty,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="RECEIVED_FORM_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control = ReceivedFormQty,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="VERSATILE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Versatile,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control = Note,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="REPAIR_KIT_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control = RepairKitFlag,
                    ControlType = "CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="MODIFICATION_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control = ModificationFlag,
                    ControlType = "CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STAMP_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control = CuttingStampName,
                    ControlType = "TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STAMP_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control = CuttingStampId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void InitGrid()
        {
            // 
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=4,
                },
                new DataGridHelperColumn
                {
                    Header=" ",
                    Path="FOR_ORDER",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=4,
                    Editable=true,
                    OnClickAction = (row,el) =>
                    {
                        if (el != null)
                        {
                            if(OrderStatus.ContainsIn(0, 4))
                            {
                                SetReorderItem(row);
                            }
                        }
                        return false;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Название",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=32,
                },
                new DataGridHelperColumn
                {
                    Header="Статус",
                    Path="STATUS",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="OLD_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("_ROWNUMBER");
            Grid.SetSorting("_ROWNUMBER", System.ComponentModel.ListSortDirection.Ascending);
            Grid.OnLoadItems = () =>
            {
                if (StampItemDS.Items != null)
                {
                    Grid.UpdateItems(StampItemDS);
                }
            };

            Grid.Init();
        }

        /// <summary>
        /// Загрузка данных в форму
        /// </summary>
        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStampOrder");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("ID", CuttingStampOrderId.ToString());
            q.Request.SetParam("TECHCARD_ID", TechcardId.Text);
            //q.Request.SetParam("FACTORY_ID", FactoryId.ToString());

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
                    {
                        //Производственные площадки
                        var ds = ListDataSet.Create(result, "FACTORY");
                        Factory.Items = ds.GetItemsList("ID", "NAME");
                    }

                    {
                        //Поставщики
                        SupplierDS = ListDataSet.Create(result, "SUPPLIERS");
                        var sp = new Dictionary<string, string>()
                        {
                            { "0", " " },
                        };
                        foreach (var item in SupplierDS.Items)
                        {
                            sp.CheckAdd(item["ID_POST"], item["NAME"]);
                        }
                        Supplier.Items = sp;
                        //По умолчанию Растр-технология
                        //Отменил заполнение по умолчанию по просьбе Митрофановой
                        Supplier.SetSelectedItemByKey("0");
                    }

                    {
                        //Станки
                        MachineDS = ListDataSet.Create(result, "MACHINES");
                        Machine.Items = MachineDS.GetItemsList("ID", "NAME");
                    }

                    {
                        //Плательщики (покупатели)
                        var payerDS = ListDataSet.Create(result, "PAYERS");
                        Payer.Items = payerDS.GetItemsList("ID", "NAME");
                        //Если среди покупателей есть с таким же именем, что и потребитель изделия, предложим его в качестве плательщика
                        if (ProductParams.ContainsKey("CUSTOMER_NAME"))
                        {
                            foreach (var cs in Payer.Items)
                            {
                                if (cs.Value == ProductParams["CUSTOMER_NAME"])
                                {
                                    Payer.SelectedItem = cs;
                                    break;
                                }
                            }
                        }
                    }

                    {
                        //FEFCO
                        var fefcoDS = ListDataSet.Create(result, "FEFCO");
                        Fefco.Items = fefcoDS.GetItemsList("ID", "NAME");
                    }

                    //Универсальная
                    if (result.ContainsKey("VERSATILE"))
                    {
                        var vesatileDS = ListDataSet.Create(result, "VERSATILE");
                        var versatileList = new Dictionary<string, string>
                        {
                            { "0", " " },
                        };
                        foreach (var ve in vesatileDS.Items)
                        {
                            int veId = ve.CheckGet("ID").ToInt();
                            versatileList.CheckAdd(veId.ToString(), ve.CheckGet("NAME"));
                        }
                        Versatile.Items = versatileList;
                        Versatile.SetSelectedItemByKey("0");
                    }

                    {
                        // Данные по полученным и заказанным полумуфтам, привязанным к выбранной техкарте
                        StampItemsReceivedDS = ListDataSet.Create(result, "STAMP_RECEIVED");
                    }

                    if (result.ContainsKey("STAMP_ORDER"))
                    {
                        var orderDS = ListDataSet.Create(result, "STAMP_ORDER");
                        var rec = orderDS.Items[0];
                        OrderStatus = rec.CheckGet("STATUS_ID").ToInt();
                        DeliveryDttm = rec.CheckGet("DELIVERY_DTTM").ToDateTime();

                        //Заполняем ProductParams, он нам понадобится при редактировании заявки
                        ProductParams.CheckAdd("ID", rec.CheckGet("TECHCARD_ID"));
                        ProductParams.CheckAdd("ID", rec.CheckGet("TECHCARD_ID"));
                        ProductParams.CheckAdd("CUTTING_LENGTH", rec.CheckGet("BLANK_LENGTH"));
                        ProductParams.CheckAdd("CUTTING_WIDTH", rec.CheckGet("BLANK_WIDTH"));
                        ProductParams.CheckAdd("TECHCARD_SIZE", rec.CheckGet("TECHCARD_SIZE"));

                        // Показываем имя файла
                        string drawingFile = rec.CheckGet("DRAWING_FILE");
                        if (!drawingFile.IsNullOrEmpty())
                        {
                            string drawingFileName = Path.GetFileName(drawingFile);
                            rec.CheckAdd("DRAWING_FILE_NAME", drawingFileName);
                            if (File.Exists(drawingFile))
                            {
                                DrawingFileShowButton.IsEnabled = true;
                            }
                        }

                        Form.SetValues(orderDS);
                    }
                    else
                    {
                        Factory.SetSelectedItemByKey(FactoryId.ToString());
                        UpdateFactory();
                    }

                    SetFieldsAvailable();
                    Show();
                }
            }
        }

        /// <summary>
        /// Создание заказа штанцформы
        /// </summary>
        /// <param name="data"></param>
        public void Create(Dictionary<string, string> data)
        {
            if (data != null)
            {
                if (data.ContainsKey("ID"))
                {
                    TechcardId.Text = data["ID"];
                    ProductParams.CheckAdd("ID", data["ID"]);
                    string skuCode = data.CheckGet("SKU_CODE");
                    ProductParams.CheckAdd("SKU_CODE", skuCode);
                    ProductParams.CheckAdd("SHORT_SKU_CODE", "");
                    if (!skuCode.IsNullOrEmpty())
                    {
                        ProductParams["SHORT_SKU_CODE"] = skuCode.Substring(0, 7);
                    }

                    string customer = data.CheckGet("CUSTOMER_NAME");
                    ProductParams.CheckAdd("CUSTOMER_NAME", customer);
                    string tcSize = data.CheckGet("TECHCARD_SIZE");
                    ProductParams.CheckAdd("TECHCARD_SIZE", tcSize);

                    ProductParams.CheckAdd("CUTTING_LENGTH", data.CheckGet("BLANK_LENGTH"));
                    ProductParams.CheckAdd("CUTTING_WIDTH", data.CheckGet("BLANK_WIDTH"));

                    ProductName.Text = $"{skuCode} {customer} {data.CheckGet("TECHCARD_NAME")}";

                    GetData();
                }
                else
                {
                    var dw = new DialogWindow("Не удалось определить данные для техкарты", "Заказ штанцформы");
                    dw.SetIcon("alert");
                    dw.ShowDialog();
                }
            }
            else
            {
                var dw = new DialogWindow("Не удалось получить данные для техкарты", "Заказ штанцформы");
                dw.SetIcon("alert");
                dw.ShowDialog();
            }
        }

        /// <summary>
        /// Запуск редактирования заказа штанцформы
        /// </summary>
        /// <param name="id"></param>
        public void Edit(int id, int techcardId)
        {
            CuttingStampOrderId = id;
            ControlName = $"CuttingStampOrder_{id}";

            if (techcardId > 0)
            {
                TechcardId.Text = techcardId.ToString();
                RepairKitFlagBlock.Visibility = Visibility.Collapsed;
                RepairKitLabelBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                //Ремкомплект
                StampParamsBlock.Visibility = Visibility.Collapsed;
                RepairKitFlag.IsChecked = true;
            }
            GetData();
        }

        public void CreateRepairKit()
        {
            StampParamsBlock.Visibility = Visibility.Collapsed;
            GetData();
        }

        /// <summary>
        /// Настройка доступности полей
        /// </summary>
        private void SetFieldsAvailable()
        {
            Supplier.IsEnabled = OrderStatus == 0;
            DrawingFileSelectButton.IsEnabled = OrderStatus.ContainsIn(0, 4);
            Machine.IsEnabled = OrderStatus == 0;
            CuttingLength.IsReadOnly = OrderStatus == 7;
            CuttingWidth.IsReadOnly = OrderStatus == 7;
            Payer.IsEnabled = OrderStatus != 7;
            DeliveryDateTime.IsEnabled = OrderStatus != 7;
            WasteSquare.IsReadOnly = OrderStatus == 7;
            PD.IsReadOnly = OrderStatus == 7;
            RepairKitFlag.IsEnabled = OrderStatus == 0;
            ModificationFlag.IsEnabled = OrderStatus == 0;
            ClientStampFlag.IsEnabled = OrderStatus == 0;
        }

        /// <summary>
        /// Обработка увеличения количества полумуфт
        /// </summary>
        private void IncreaseFormQty()
        {
            int qty1 = FormQty.Text.ToInt();
            qty1++;
            FormQty.Text = qty1.ToString();
            FormQtyDecButton.IsEnabled = true;
            if (qty1 == 6)
            {
                FormQtyIncButton.IsEnabled = false;
            }

            var itemsList = StampItemDS.Items;
            int rowNum = StampItemDS.Items.Count + 1;
            string[] subNum = new string[6] { "а", "б", "в", "г", "д", "е" };

            // Убираем лишние символы из имени плательщика
            char[] trimmed = { '.', ' ' };
            string payerName = Payer.SelectedItem.Value;
            payerName = payerName.TrimEnd(trimmed);
            payerName = payerName.Replace("\"", "");

            // Если была только одна полумуфта, добавляем ей подномер
            if (StampItemDS.Items.Count == 1)
            {
                var rec1 = StampItemDS.Items[0];
                string oldName = rec1.CheckGet("NAME");
                rec1["NAME"] = $"{oldName} (а)";
            }

            string newRowName = "00000";
            if (!OuterNum.Text.IsNullOrEmpty())
            {
                newRowName = $"{OuterNum.Text}";
            }
            newRowName = $"{newRowName} {payerName} {ProductParams["TECHCARD_SIZE"]} ({subNum[rowNum - 1]})";
            int newId = 0;
            string newStatus = "Отсутствует";

            //Проверяем, нет ли такой полумуфты среди существующих
            foreach (var stampItem in StampItemsReceivedDS.Items)
            {
                if (stampItem.CheckGet("STAMP_ITEM_NAME") == newRowName)
                {
                    newId = stampItem.CheckGet("ID").ToInt();
                    newStatus = stampItem.CheckGet("STATUS");
                }
            }

            var d = new Dictionary<string, string>
            {
                { "_ROWNUMBER", rowNum.ToString() },
                { "FOR_ORDER", "1" },
                { "NAME", newRowName },
                { "STATUS", newStatus },
                { "ID", newId.ToString() },
                { "OLD_ID", newId.ToString() },
            };
            itemsList.Add(d);

            StampItemDS = ListDataSet.Create(itemsList);
            Grid.LoadItems();

            // Обновим количество полумуфт в названии штанцформы
            int ls = CuttingStampName.Text.Length;
            string newStampName = CuttingStampName.Text.Substring(0, ls - 3);
            newStampName = $"{newStampName}({rowNum})";
            CuttingStampName.Text = newStampName;
        }

        /// <summary>
        /// Обработка уменьшения количества полумуфт
        /// </summary>
        private void DecreaseFormQty()
        {
            int qty2 = FormQty.Text.ToInt();
            qty2--;
            FormQty.Text = qty2.ToString();
            FormQtyIncButton.IsEnabled = true;
            if (qty2 == 1)
            {
                FormQtyDecButton.IsEnabled = false;
            }

            var itemsList = StampItemDS.Items;
            int rowNum = StampItemDS.Items.Count;
            itemsList.RemoveAt(rowNum - 1);

            if (qty2 == 1)
            {
                //Осталась последняя полумуфта. Удалим букву из названия
                string lastName = itemsList[0].CheckGet("NAME");
                lastName = lastName.Substring(0, lastName.Length - 4);
                itemsList[0].CheckAdd("NAME", lastName);
            }

            StampItemDS = ListDataSet.Create(itemsList);
            Grid.LoadItems();

            // Обновим количество полумуфт в названии штанцформы
            int ls = CuttingStampName.Text.Length;
            string newStampName = CuttingStampName.Text.Substring(0, ls - 3);
            newStampName = $"{newStampName}({rowNum - 1})";
            CuttingStampName.Text = newStampName;
        }

        /// <summary>
        /// Выбор файла чертежа
        /// </summary>
        private void SelectDrawingFile()
        {
            var fd = new OpenFileDialog();
            fd.Filter = "Изображения (*.jpg;*.jpeg)|*.jpg;*.jpeg|Все файлы (*.*)|*.*";
            fd.FilterIndex = 0;
            fd.InitialDirectory = InitialFileFolder;

            if ((bool)fd.ShowDialog())
            {
                DrawingFile.Text = fd.FileName;
                DrawingFileName.Text = Path.GetFileName(fd.FileName);
                DrawingFileShowButton.IsEnabled = true;
            }

        }

        /// <summary>
        /// Изменение имени штанцформы и полумуфт при выборе станка
        /// </summary>
        private void UpdateStampNameByMachine()
        {
            var itemsList = new List<Dictionary<string, string>>();
            int machimeId = Machine.SelectedItem.Key.ToInt();

            // Убираем лишние символы из имени плательщика
            char[] trimmed = { '.', ' ' };
            string payerName = Payer.SelectedItem.Value;
            payerName = payerName.TrimEnd(trimmed);
            payerName = payerName.Replace("\"", "");

            int i = 1;
            foreach (var item in StampItemsReceivedDS.Items)
            {
                if(item.CheckGet("MACHINE_ID").ToInt() == machimeId)
                {
                    int itemOrderId = item.CheckGet("ORDER_ID").ToInt();
                    string forOrder = itemOrderId == CuttingStampOrderId ? "1" : "0";

                    var d = new Dictionary<string, string>
                    {
                        { "_ROWNUMBER", i.ToString() },
                        { "FOR_ORDER", forOrder },
                        { "NAME", item.CheckGet("STAMP_ITEM_NAME") },
                        { "STATUS", item.CheckGet("STATUS") },
                        { "ID", item.CheckGet("ID") },
                        { "OLD_ID", item.CheckGet("ID") },
                    };

                    //Если открыли существующий заказ, то показываем только те полумуфты, которые входят в этот заказ
                    if (!OrderStatus.ContainsIn(0, 4))
                    {
                        if (forOrder == "1")
                        {
                            itemsList.Add(d);
                            i++;
                        }
                    }
                    else
                    {
                        itemsList.Add(d);
                        i++;
                    }

                    //Имя штанцформы уже определено. Если надо поменять, то меняйте в интерфейсе управления штанцформами
                    CuttingStampName.Text = item.CheckGet("STAMP_NAME");
                    CuttingStampId.Text = item.CheckGet("STAMP_ID").ToInt().ToString();
                    CuttingStampName.IsReadOnly = true;

                    //Если перезаказываем, то заполняем в заказе размеры развертки
                    if (item.CheckGet("STAMP_ID").ToInt() > 0)
                    {
                        CuttingLength.Text = ProductParams.CheckGet("CUTTING_LENGTH").ToInt().ToString();
                        CuttingWidth.Text = ProductParams.CheckGet("CUTTING_WIDTH").ToInt().ToString();
                    }
                }
            }

            if ((itemsList.Count == 0) && (ProductParams.ContainsKey("SHORT_SKU_CODE")))
            {
                // Добавим хотя бы одну полумуфту
                string name = "00000";
                if (!OuterNum.Text.IsNullOrEmpty())
                {
                    name = $"{OuterNum.Text}";
                }
                name = $"{name} {payerName} {ProductParams["TECHCARD_SIZE"]}";
                var d = new Dictionary<string, string>
                {
                    { "_ROWNUMBER", "1" },
                    { "FOR_ORDER", "1" },
                    { "NAME", name },
                    { "STATUS", "Отсутствует" },
                    { "ID", "0" },
                    { "OLD_ID", "0" },
                };
                itemsList.Add(d);
                CuttingStampName.IsEnabled = true;
                CuttingStampId.Text = "0";

                string machineType = "РШФ";
                if (machimeId == 16)
                {
                    machineType = "ПШФ";
                }
                else if (machimeId == 9)
                {
                    machineType = "ПШФ Et";
                }
                else
                {
                    foreach (var m in MachineDS.Items)
                    {
                        if (m.CheckGet("ID").ToInt() == machimeId)
                        {
                            machineType = $"{machineType} {m.CheckGet("CODE")}";
                        }
                    }
                }

                CuttingStampName.Text = $"{machineType} 000000 {ProductParams["TECHCARD_SIZE"]} (1)";
            }

            StampItemDS = ListDataSet.Create(itemsList);
            Grid.LoadItems();
            FormQty.Text = StampItemDS.Items.Count.ToString();
            //Если в списк полумуфт только одна строка, заблокируем кнопку уменьшения количества
            if (itemsList.Count == 1)
            {
                FormQtyDecButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Создание доработанной штанцформы
        /// </summary>
        private void CreateModification()
        {
            int machimeId = Machine.SelectedItem.Key.ToInt();
            bool found = false;
            int i = 0;
            string[] subNum = new string[6] { "а", "б", "в", "г", "д", "е" };

            // Убираем лишние символы из имени плательщика
            char[] trimmed = { '.', ' ' };
            string payerName = Payer.SelectedItem.Value;
            payerName = payerName.TrimEnd(trimmed);
            payerName = payerName.Replace("\"", "");

            List<Dictionary<string, string>> itemsList = new List<Dictionary<string, string>>();

            foreach (var item in StampItemsReceivedDS.Items)
            {
                if (item.CheckGet("MACHINE_ID").ToInt() == machimeId)
                {
                    found = true;
                    i++;
                    string name = "00000";
                    string oldName = item.CheckGet("STAMP_ITEM_NAME");
                    // Сохраним старый растровый номер, добавим букву Д, чтобы видеть, что это доработка
                    if (!oldName.IsNullOrEmpty())
                    {
                        var s = oldName.Split(' ');
                        name = s[0] + " (Д)";
                    }

                    if (!OuterNum.Text.IsNullOrEmpty())
                    {
                        name = $"{OuterNum.Text}";
                    }
                    name = $"{name} {payerName} {ProductParams["TECHCARD_SIZE"]} ({subNum[i-1]})";

                    var d = new Dictionary<string, string>
                    {
                        { "_ROWNUMBER", i.ToString() },
                        { "FOR_ORDER", "1" },
                        { "NAME", name },
                        { "STATUS", "Отсутствует" },
                        { "ID", "0" },
                        { "OLD_ID", item.CheckGet("ID") },
                    };
                    itemsList.Add(d);

                    CuttingStampName.Text = item.CheckGet("STAMP_NAME");
                    CuttingStampId.Text = item.CheckGet("STAMP_ID").ToInt().ToString();
                }
            }

            if (found)
            {
                if (i == 1)
                {
                    //Если элемент 1, уберем букву
                    string lastName = itemsList[0].CheckGet("NAME");
                    lastName = lastName.Substring(0, lastName.Length - 4);
                    itemsList[0].CheckAdd("NAME", lastName);
                }

                StampItemDS = ListDataSet.Create(itemsList);
                Grid.LoadItems();
                FormQty.Text = StampItemDS.Items.Count.ToString();
            }
            else
            {
                var dw = new DialogWindow($"Не найдена штанцфрма для станка {Machine.SelectedItem.Value}.\nДорабатывать можно только существующие штанцформы.", "Доработка штанцформы");
                ModificationFlag.IsChecked = false;
                dw.ShowDialog();
            }
        }

        /// <summary>
        /// Обновление имени полумуфты при изменении номера заказа поставщика
        /// </summary>
        private void UpdateItemNameForOrder()
        {
            var itemsList = new List<Dictionary<string, string>>();
            string[] subNum = new string[6] { "а", "б", "в", "г", "д", "е" };

            // Убираем лишние символы из имени плательщика
            char[] trimmed = { '.', ' ' };
            string payerName = Payer.SelectedItem.Value;
            payerName = payerName.TrimEnd(trimmed);
            payerName = payerName.Replace("\"", "");

            foreach (var item in StampItemDS.Items)
            {
                if (item.CheckGet("FOR_ORDER").ToBool())
                {
                    string name = "00000";
                    if (!OuterNum.Text.IsNullOrEmpty())
                    {
                        name = $"{OuterNum.Text}";
                    }
                    name = $"{name} {payerName} {ProductParams["TECHCARD_SIZE"]}";
                    int rowNum = item.CheckGet("_ROWNUMBER").ToInt();
                    string itemSubNum = $"({subNum[rowNum - 1]})";
                    int l = item.CheckGet("NAME").Length;
                    if (item.CheckGet("NAME").Substring(l-3, 3) == itemSubNum)
                    {
                        name = $"{name} {itemSubNum}";
                    }
                    var d = new Dictionary<string, string>
                    {
                        { "_ROWNUMBER", rowNum.ToString()},
                        { "FOR_ORDER", "1" },
                        { "NAME", name },
                        { "STATUS", "Отсутствует" },
                        { "ID", item.CheckGet("ID") },
                        { "OLD_ID", item.CheckGet("OLD_ID") },
                    };
                    itemsList.Add(d);
                }
                else
                {
                    itemsList.Add(item);
                }
            }

            StampItemDS = ListDataSet.Create(itemsList);
            Grid.LoadItems();
        }

        /// <summary>
        /// Добавление или исключение элемента в заказ
        /// </summary>
        /// <param name="oldRow"></param>
        private void SetReorderItem(Dictionary<string, string> oldRow)
        {
            int forOrder = oldRow.CheckGet("FOR_ORDER").ToInt();
            int rowNum = oldRow.CheckGet("_ROWNUMBER").ToInt();
            var itemsList = new List<Dictionary<string, string>>();

            // Убираем лишние символы из имени плательщика
            char[] trimmed = { '.', ' ' };
            string payerName = Payer.SelectedItem.Value;
            payerName = payerName.TrimEnd(trimmed);
            payerName = payerName.Replace("\"", "");

            if (forOrder == 0)
            {
                //Будем перезаказывать. Создаем строку для элемента
                foreach (var item in Grid.Items)
                {
                    if (item.CheckGet("_ROWNUMBER").ToInt() == rowNum)
                    {
                        string name = "00000";
                        if (!OuterNum.Text.IsNullOrEmpty())
                        {
                            name = $"{OuterNum.Text}";
                        }
                        name = $"{name} {payerName} {ProductParams["TECHCARD_SIZE"]}";

                        //Если есть буква подномера, восстанавливаем ее
                        string oldName = item.CheckGet("NAME");
                        int l = oldName.Length;
                        string subNum = oldName.Substring(l-3, 3);
                        if ((subNum[0] == '(') && (subNum[2] == ')'))
                        {
                            name = $"{name} {subNum}";
                        }

                        var d = new Dictionary<string, string>
                        {
                            { "_ROWNUMBER", rowNum.ToString()},
                            { "FOR_ORDER", "1" },
                            { "NAME", name },
                            { "STATUS", "Отсутствует" },
                            { "ID", "0" },
                            { "OLD_ID", oldRow.CheckGet("ID") },
                        };
                        itemsList.Add(d);
                    }
                    else
                    {
                        itemsList.Add(item);
                    }
                }
            }
            else
            {
                //Восстанавливаем значение для существующей штанцформы
                var oldId = oldRow.CheckGet("OLD_ID");
                foreach (var item in Grid.Items)
                {
                    if (item.CheckGet("_ROWNUMBER").ToInt() == rowNum)
                    {
                        //Ищем нужный элемент в данных для существующих элементах
                        Dictionary<string, string> d = null;
                        foreach (var di in StampItemsReceivedDS.Items)
                        {
                            if (di.CheckGet("MACHINE_ID").ToInt() == Machine.SelectedItem.Key.ToInt())
                            {
                                if (di.CheckGet("ID") == oldId)
                                {
                                    d = new Dictionary<string, string>
                                    {
                                        { "_ROWNUMBER", rowNum.ToString() },
                                        { "FOR_ORDER", "0" },
                                        { "NAME", di.CheckGet("STAMP_ITEM_NAME") },
                                        { "STATUS", di.CheckGet("STATUS") },
                                        { "ID", di.CheckGet("ID") },
                                        { "OLD_ID", di.CheckGet("ID") },
                                    };
                                    

                                }
                            }
                        }

                        if (d != null)
                        {
                            itemsList.Add(d);
                        }
                        else
                        {
                            itemsList.Add(item);
                        }
                    }
                    else
                    {
                        itemsList.Add(item);
                    }
                }
            }

            if (itemsList.Count > 0)
            {
                StampItemDS = ListDataSet.Create(itemsList);
                Grid.LoadItems();
            }
        }

        /// <summary>
        /// Обновление полей при смене производственной площадки
        /// </summary>
        private void UpdateFactory()
        {
            //Обновляем список станков
                
            var machineList = new Dictionary<string, string>();
            foreach (var m in MachineDS.Items)
            {
                if (m.CheckGet("FACTORY_ID").ToInt() == FactoryId)
                {
                    machineList.Add(m["ID"], m["NAME"]);
                }
            }
                
            Machine.Items = machineList;
            //Очищаем название штанцформы и список элементов
            CuttingStampName.Text = "";
            CuttingStampId.Text = "";
            Grid.ClearItems();
        }

        /// <summary>
        /// При изменении плательщика меняем имена элементов в заказе
        /// </summary>
        private void UpdateStampNameByPayer()
        {
            var itemsList = new List<Dictionary<string, string>>();
            string[] subNum = new string[6] { "а", "б", "в", "г", "д", "е" };
            int n = FormQty.Text.ToInt();
            int i = 0;

            // Убираем лишние символы
            char[] trimmed = { '.', ' ' };

            string payerName = Payer.SelectedItem.Value;

            payerName = payerName.TrimEnd(trimmed);
            payerName = payerName.Replace("\"", "");

            foreach (var row in Grid.Items)
            {
                if (row.CheckGet("FOR_ORDER").ToBool())
                {
                    string name = "00000";
                    if (!OuterNum.Text.IsNullOrEmpty())
                    {
                        name = $"{OuterNum.Text}";
                    }
                    name = $"{name} {payerName} {ProductParams["TECHCARD_SIZE"]}";
                    if (n > 1)
                    {
                        name = $"{name} ({subNum[i]})";
                    }

                    row.CheckAdd("NAME", name);
                    i++;
                }
                itemsList.Add(row);
            }

            StampItemDS = ListDataSet.Create(itemsList);
            Grid.LoadItems();
        }

        /// <summary>
        /// Отображение формы редактирования
        /// </summary>
        public void Show()
        {
            Central.WM.Show(ControlName, ControlTitle, true, "add", this);
        }

        /// <summary>
        /// Закрытие формы
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            Central.WM.SetActive(ReceiverName);
        }

        /// <summary>
        /// Сохранение заказа
        /// </summary>
        public void Save()
        {
            bool resume = true;

            if (Form.Validate())
            {
                var v = Form.GetValues();

                bool repairKitFlag = v.CheckGet("REPAIR_KIT_FLAG").ToBool();
                bool clientStampFlag = v.CheckGet("CLIENT_STAMP_FLAG").ToBool();
                int q = 0;

                if (resume)
                {
                    if ((v.CheckGet("SUPPLIER_ID").ToInt() == 0) && !clientStampFlag)
                    {
                        resume = false;
                        Form.SetStatus("Не выбран поставщик", 1);
                    }
                }

                if (!repairKitFlag && (Machine.SelectedItem.Key.ToInt() == 0))
                {
                    resume = false;
                    Form.SetStatus("Не выбран станок для заказа", 1);
                }

                if (resume && !repairKitFlag)
                {
                    foreach (var stampItem in Grid.Items)
                    {
                        if (stampItem.CheckGet("FOR_ORDER").ToBool())
                        {
                            q++;
                        }
                    }

                    if (q == 0)
                    {
                        Form.SetStatus("Не выбрано ни одной полумуфты для заказа", 1);
                        resume = false;
                    }
                }

                if (resume && !repairKitFlag && (CuttingStampOrderId == 0))
                {
                    //Проверка размеров штампа по отношению к заготовке
                    int blankLength = ProductParams.CheckGet("CUTTING_LENGTH").ToInt();
                    int blankWidth = ProductParams.CheckGet("CUTTING_WIDTH").ToInt();

                    if ((blankLength < v.CheckGet("CUTTING_LENGTH").ToInt())
                        || (blankLength > v.CheckGet("CUTTING_LENGTH").ToInt() + 35)
                        || (blankWidth < v.CheckGet("CUTTING_WIDTH").ToInt())
                        || (blankWidth > v.CheckGet("CUTTING_WIDTH").ToInt() + 35))
                    {
                        resume = false;
                        Form.SetStatus("Габариты развертки некорректны к габаритам заготовки", 1);
                    }
                }

                if (!repairKitFlag && resume)
                {
                    //Добавим в заказ только отмеченные полумуфты. И пересчитаем их

                    int cnt = 0;
                    var itemsList = new List<Dictionary<string, string>>();
                    foreach (var item in StampItemDS.Items)
                    {
                        if (item.CheckGet("FOR_ORDER").ToBool())
                        {
                            itemsList.Add(item);
                            cnt++;
                        }
                    }

                    v.CheckAdd("FORM_QTY", cnt.ToString());
                    string json = JsonConvert.SerializeObject(itemsList);
                    v.CheckAdd("STAMP_ITEM_LIST", json);
                }

                if (resume)
                {
                    v.CheckAdd("ID", CuttingStampOrderId.ToString());
                    v.CheckAdd("STATUS_ID", OrderStatus.ToString());

                    char[] trimmed = { '.', ' ' };
                    //Название поставщика и плательщика для создания папки заявки
                    //Очистим от проблемных символов
                    string supplierName = Supplier.SelectedItem.Value;
                    supplierName = supplierName.TrimEnd(trimmed);
                    supplierName = supplierName.Replace("\"", "");

                    string payerName = Payer.SelectedItem.Value;
                    payerName = payerName.TrimEnd(trimmed);
                    payerName = payerName.Replace("\"", "");

                    v.CheckAdd("SUPPLIER_NAME", supplierName);
                    v.CheckAdd("PAYER_NAME", payerName);

                    SaveData(v);
                }
            }
        }

        /// <summary>
        /// Сохранение данных заказа в БД
        /// </summary>
        /// <param name="data"></param>
        public async void SaveData(Dictionary<string, string> data)
        {
            SaveButton.IsEnabled = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStampOrder");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParams(data);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result.ContainsKey("ITEMS"))
                {
                    //Если ответ не пустой, отправляем сообщение Гриду о необходимости обновить данные
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Preproduction/Rig",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "Refresh",
                    });

                    Close();
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
            }

            SaveButton.IsEnabled = true;
        }

        private async void CheckOrderBeforeChangeDelivery()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "CuttingStampOrder");
            q.Request.SetParam("Action", "CheckDelivery");
            q.Request.SetParam("ID", CuttingStampOrderId.ToString());

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
                    var ds = ListDataSet.Create(result, "SHIPMENT_DTTM");
                    if (ds.Items.Count > 0)
                    {
                        var shipmentDttm = ds.Items[0].CheckGet("SHIPMENT_DTTM").ToDateTime();
                        if (shipmentDttm > DateTime.MinValue)
                        {
                            if (shipmentDttm < DeliveryDateTime.DateTime)
                            {
                                DeliveryDateTime.DateTime = DeliveryDttm;
                                var dw = new DialogWindow($"С этим заказом связана отгрузка на {shipmentDttm:dd.MM HH:mm}", "Перенос доставки");
                                dw.ShowDialog();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Обработка нажатия на кнопку
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

        private void OuterNum_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateItemNameForOrder();
        }

        private void Machine_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateStampNameByMachine();
        }

        private void Factory_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateFactory();
        }

        private void ModificationClick(object sender, RoutedEventArgs e)
        {
            var modificationFlag = (bool)ModificationFlag.IsChecked;

            if (modificationFlag)
            {
                CreateModification();
            }
            else
            {
                UpdateStampNameByMachine();
            }
        }

        private void Payer_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (Payer.SelectedItem.Key != null)
            {
                UpdateStampNameByPayer();
            }
        }

        private void ClientStampFlag_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)ClientStampFlag.IsChecked)
            {
                Supplier.SetSelectedItemByKey("0");
                Supplier.IsEnabled = false;
            }
            else
            {
                Supplier.IsEnabled = false;
            }
        }

        private void DateChanged(object sender, TextChangedEventArgs e)
        {
            if (DeliveryDttm > DateTime.MinValue)
            {
                if (DateTime.Compare(DeliveryDateTime.DateTime, DeliveryDttm) > 0)
                {
                    CheckOrderBeforeChangeDelivery();
                }
            }
        }
    }
}
