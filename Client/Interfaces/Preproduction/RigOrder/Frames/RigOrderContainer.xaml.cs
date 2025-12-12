using Client.Common;
using Client.Interfaces.Main;
using iTextSharp.text;
using iTextSharp.text.pdf;
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
    /// Форма редактирования заявки на клише для литой тары
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class RigOrderContainer : ControlBase
    {
        public RigOrderContainer()
        {
            InitializeComponent();
            InitForm();
            InitGrid();

            TechCard = new Dictionary<string, string>();
            ClicheItemDS = new ListDataSet();
            ClicheItemDS.Init();
            DeletedClicheId = new List<int>();
        }

        /// <summary>
        /// Режим работы формы: 1 - заявка на оснастку одного изделия, 2 - заявка на оснастку нескольких изделий
        /// </summary>
        public int Mode;
        /// <summary>
        /// Идентификатор заявки
        /// </summary>
        public int RigOrderId;
        /// <summary>
        /// Имя вкладки, которая вызвала открытие фрейма, и в которую возвращается фокус после закрытия фрейма
        /// </summary>
        public string ReceiverName { get; set; }
        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        public Dictionary<string,string> TechCard { get; set; }
        /// <summary>
        /// Данные по поставщикам оснастки
        /// </summary>
        private ListDataSet SupplierDS { get; set; }
        /// <summary>
        /// Данные по материалам, из которых изготавливается оснастка
        /// </summary>
        private ListDataSet MaterialDS { get; set; }
        /// <summary>
        /// Признак измеения файла дизайна. При редактировании заявки если файл дизайна меняется, переписываем новый в папку заявки
        /// </summary>
        private string OldDesignFile { get; set; }
        /// <summary>
        /// Папка заявок. При редактировании - папка заявки, при создании - папка, где будет заявка
        /// </summary>
        private string OrderFolder;

        private int Status;

        private List<int> DeletedClicheId { get; set; }

        private ListDataSet ClicheItemDS { get; set; }

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
                    case "selectdrawing":
                        SaveDrawingFile();
                        break;

                    case "cliche":
                        CreateCliche();
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
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="SUPPLIER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control = Supplier,
                    ControlType = "SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
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
                    Path="MACHINE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control = Machine,
                    ControlType = "SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField f, string v) =>
                    {
                        ClicheGrid.LoadItems();
                    },
                },
                new FormHelperField()
                {
                    Path="POLYMER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control = Polymer,
                    ControlType = "SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
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
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control = RepairKitFlag,
                    ControlType = "CheckBox",
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
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="_",
                    Path="FOR_ORDER",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=5,
                    Editable=true,
                    OnClickAction=(row, el) =>
                    {
                        if ((Status == 0) || (Status == 4))
                        {
                            if (el != null)
                            {
                                SetItemChecked(row);;
                            }
                        }
                        return false;
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Место печати",
                    Path="SPOT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Цвет",
                    Path="PANTONE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
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
                    Header="cliche_id",
                    Path="CLICHE_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            ClicheGrid.SetColumns(columns);
            ClicheGrid.SetPrimaryKey("_ROWNUMBER");
            ClicheGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            ClicheGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;

            ClicheGrid.OnLoadItems = () =>
            {
                if (ClicheItemDS.Items != null)
                {
                    ClicheGrid.UpdateItems(ClicheItemDS);
                }
            };
            ClicheGrid.AutoUpdateInterval = 0;

            ClicheGrid.Init();

        }

        /// <summary>
        /// Загрузка данных из датасета в таблицу
        /// </summary>
        private async void LoadGridItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "RigOrder");
            q.Request.SetParam("Action", "ListClicheForOrder");
            q.Request.SetParam("TECHCARD_ID", TechcardId.Text);
            q.Request.SetParam("MACHINE_ID", Machine.SelectedItem.Key);
            q.Request.SetParam("ID", RigOrderId.ToString());

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
                    ClicheItemDS = ListDataSet.Create(result, "CLICHE_LIST");
                    ClicheGrid.LoadItems();

                    //Посчитаем количество выбранных форм
                    int checkedItems = 0;
                    foreach (var item in ClicheItemDS.Items)
                    {
                        if (item.CheckGet("FOR_ORDER").ToBool())
                            checkedItems++;
                    }

                    FormQty.Text = checkedItems.ToString();
                }
            }
        }

        /// <summary>
        /// Получение данных из БД
        /// </summary>
        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "RigOrder");
            q.Request.SetParam("Action", "GetContainer");
            q.Request.SetParam("ID", RigOrderId.ToString());

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
                    Status = 0;

                    //Поставщики
                    SupplierDS = ListDataSet.Create(result, "SUPPLIERS");
                    Supplier.Items = SupplierDS.GetItemsList("ID_POST", "NAME");

                    // Датасет материалов, из которых изготавливается печатная форма. При смене поставщика обновляется поле выбора
                    MaterialDS = ListDataSet.Create(result, "POLYMERS");
                    Supplier.SetSelectedItemFirst();

                    var machineDS = ListDataSet.Create(result, "MACHINES");
                    Machine.Items = machineDS.GetItemsList("ID", "NAME");

                    // Покупатели
                    var payersDS = ListDataSet.Create(result, "PAYERS");
                    Payer.Items = payersDS.GetItemsList("ID", "NAME");

                    //Корневая папка для заявок
                    OrderFolder = "";
                    var folderDS = ListDataSet.Create(result, "ORDERS_FOLDER");


                    // Дата и время доставки по умолчанию
                    var deliveryDT = DateTime.Now.Date.AddDays(3).AddHours(18);
                    if ((int)deliveryDT.DayOfWeek > 5)
                    {
                        deliveryDT = deliveryDT.AddDays(2);
                    }
                    DeliveryDateTime.DateTime = deliveryDT;

                    var ds = new ListDataSet();
                    if (RigOrderId > 0)
                    {
                        ds = ListDataSet.Create(result, "RIG_ORDER");
                        if (ds.Items.Count > 0)
                        {
                            string drawingFilePath = ds.Items[0].CheckGet("DRAWING_FILE");
                            if (!string.IsNullOrEmpty(drawingFilePath))
                            {
                                string drawingFileName = Path.GetFileName(drawingFilePath);
                                ds.Items[0].CheckAdd("DRAWING_FILE_NAME", drawingFileName);
                                OrderFolder = Path.GetDirectoryName(drawingFilePath);
                            }

                            Status = ds.Items[0].CheckGet("STATUS_ID").ToInt();
                            if (ds.Items[0].CheckGet("CANCELED_FLAG").ToBool())
                            {
                                //Если заявка отменена, доступность полей как в полученной - всё заблокировано, кроме примечания
                                Status = 7;
                            }
                        }

                    }
                    else
                    {
                        OrderFolder = folderDS.Items[0].CheckGet("NETWORK_PATH");
                        TechCard.CheckAdd("MACHINE_ID", "12");
                        ds = ListDataSet.Create(new List<Dictionary<string, string>>() { TechCard });
                    }

                    Form.SetValues(ds);
                    LoadGridItems();

                    SetFieldsAvailable(Status);
                    Show();
                }
            }
        }

        /// <summary>
        /// Настройка доступности полей в зависимости от статуса заявки. 0 - новая
        /// </summary>
        /// <param name="status"></param>
        private void SetFieldsAvailable(int status = 0)
        {
            Supplier.IsEnabled = status == 0;
            Payer.IsEnabled = status == 0;
            DeliveryDateTime.IsEnabled = status.ContainsIn(0, 4, 5, 6);
            Machine.IsEnabled = status == 0;
            Polymer.IsEnabled = status == 0;
            DrawingFileSelectButton.IsEnabled = status.ContainsIn(0, 4);
            FormQty.IsEnabled = status.ContainsIn(0, 4, 5, 6);
            ReceivedFormQty.IsEnabled = status.ContainsIn(0, 4, 5, 6);
            RepairKitFlag.IsEnabled = status == 0;
        }

        /// <summary>
        /// Запуск открытия формы редактирования. 
        /// </summary>
        /// <param name="id"></param>
        public void Edit(int id=0)
        {
            RigOrderId = id;
            OldDesignFile = "";
            Mode = 1;

            if (id == 0)
            {
                string techCardId = TechCard.CheckGet("TECHCARD_ID");
                string customer = TechCard.CheckGet("CUSTOMER");
                string sku = TechCard.CheckGet("SKU");
                string techCardName = TechCard.CheckGet("TECHCARD_NAME");
                TechcardId.Text = techCardId;
                ProductName.Text = $"{customer} {sku} {techCardName}";
                FormQty.Text = TechCard.CheckGet("FORM_QTY");
            }

            ControlName = $"RigOrderContainer{RigOrderId}";

            GetData();
        }

        /// <summary>
        /// Сохранение пути для файла дизайна
        /// </summary>
        private void SaveDrawingFile()
        {
            var fd = new OpenFileDialog();
            if ((bool)fd.ShowDialog())
            {
                var drawingPath = fd.FileName;
                string drawingFileName = Path.GetFileName(drawingPath);
                // Если изменили файл при редактировании заявки, поднимаем флаг обновления файла дизайна
                if (RigOrderId > 0)
                {
                    if (OldDesignFile.IsNullOrEmpty())
                    {
                        OldDesignFile = DrawingFile.Text;
                    }
                }
                else
                {
                    //Копируем файл в папку обмена, доступную с сервера
                    string exchangeFolder = $"{OrderFolder}Рисунки\\_Обмен";
                    if (!Directory.Exists(exchangeFolder))
                    {
                        Directory.CreateDirectory(exchangeFolder);
                    }
                    string newDrawingPath = Path.Combine(exchangeFolder, drawingFileName);
                    File.Copy(drawingPath, newDrawingPath, true);
                    if (File.Exists(newDrawingPath))
                    {
                        drawingPath = newDrawingPath;
                    }
                }
                DrawingFile.Text = drawingPath;
                DrawingFileName.Text = drawingFileName;
            }
        }

        /// <summary>
        /// Обновление содержимого выпадающего списка материалов при изменении поставщика
        /// </summary>
        private void UpdateMaterialList()
        {
            int supplierId = Supplier.SelectedItem.Key.ToInt();
            var dict = new Dictionary<string, string>();
            foreach (var item in MaterialDS.Items)
            {
                if (item.CheckGet("ID_POST").ToInt() == supplierId)
                {
                    if (!item.CheckGet("ARCHIVE_FLAG").ToBool() && (item.CheckGet("RIG_TYPE").ToInt() == 3))
                    {
                        dict.Add(item.CheckGet("ID"), item.CheckGet("NAME"));
                    }
                }
            }

            Polymer.Items = dict;
            Polymer.SetSelectedItemFirst();
        }

        /// <summary>
        /// Управление отметками в списке клише
        /// </summary>
        /// <param name="oldRow"></param>
        private void SetItemChecked(Dictionary<string, string> oldRow)
        {
            bool resume = true;
            bool oldCheck = oldRow.CheckGet("FOR_ORDER").ToBool();
            var list = new List<Dictionary<string, string>>();
            int oldStatus = oldRow.CheckGet("STATUS_ID").ToInt();

            if (oldStatus.ContainsIn(3, 5, 6, 7))
            {
                resume = false;
                var dw = new DialogWindow("Для этого клише есть действующий заказ! Новый заказ невозможен", "Заказ клише");
                dw.ShowDialog();
            }
            else if (oldStatus.ContainsIn(1, 10, 12, 13))
            {
                resume = false;
                var dw = new DialogWindow("Это клише доступно для использования! Заказ не требуется", "Заказ клише");
                dw.ShowDialog();
            }
            else if ((oldStatus == 4) && (oldCheck))
            {
                // Клише сохранено в заказе, а мы снимаем флаг
                int clicheItemId = oldRow.CheckGet("CLICHE_ITEM_ID").ToInt();
                if (clicheItemId > 0)
                {
                    resume = false;
                    var dw = new DialogWindow("Удалить клише из заказа?", "Заказ клише", "", DialogWindowButtons.YesNo);
                    if ((bool)dw.ShowDialog())
                    {
                        if (dw.ResultButton == DialogResultButton.Yes)
                        {
                            resume = true;
                            DeletedClicheId.Add(clicheItemId);
                        }
                    }
                }
            }

            if (resume)
            { 
                int checkedQty = 0;

                foreach (var item in ClicheItemDS.Items)
                {
                    if (item.CheckGet("_ROWNUMBER").ToInt() == oldRow.CheckGet("_ROWNUMBER").ToInt())
                    {
                        string newCheck = oldCheck ? "0" : "1";
                        item.CheckAdd("FOR_ORDER", newCheck);
                    }

                    list.Add(item);
                }

                ClicheItemDS = ListDataSet.Create(list);
                ClicheGrid.LoadItems();

                foreach (var item in ClicheItemDS.Items)
                {
                    if (item.CheckGet("FOR_ORDER").ToBool())
                        checkedQty++;
                }
                FormQty.Text = checkedQty.ToString();
            }
        }

        /// <summary>
        /// Сохранение заявки на оснастку
        /// </summary>
        public void Save()
        {
            bool resume = true;
            string newDrawingPath = "";
            if (Mode == 1)
            {
                if (TechcardId.Text.IsNullOrEmpty())
                {
                    resume = false;
                    Form.SetStatus("Не выбрано изделие", 1);
                }
                else if (DrawingFile.Text.IsNullOrEmpty())
                {
                    resume = false;
                    Form.SetStatus("Не выбран файл дизайна", 1);
                }
            }

            if (DeliveryDateTime.Text.IsNullOrEmpty())
            {
                resume = false;
                Form.SetStatus("Не указано ожидаемое время доставки", 1);
            }

            if (resume)
            {
                var v = Form.GetValues();
                v.CheckAdd("ID", RigOrderId.ToString());

                // Если при редактировании заявки поменяли файл дизайна, то скопируем новый файл в папку заявки
                if (!OldDesignFile.IsNullOrEmpty())
                {
                    string newDesignName = Path.GetFileName(DrawingFile.Text);
                    string newDesignFile = Path.Combine(OrderFolder, newDesignName);
                    File.Copy(DrawingFile.Text, newDesignFile);
                    v.CheckAdd("DRAWING_FILE", newDesignFile);
                }

                var selectedItems = new List<Dictionary<string, string>>();
                foreach (var item in ClicheGrid.Items)
                {
                    //Добавляем строки с флагом
                    if (item.CheckGet("FOR_ORDER").ToBool())
                    {
                        if ((RigOrderId == 0) || (item.CheckGet("CLICHE_ITEM_ID").ToInt() == 0))
                        selectedItems.Add(item);
                    }
                }
                string clicheList = JsonConvert.SerializeObject(selectedItems);
                v.Add("CLICHE_LIST", clicheList);
                //ID для удаления
                v.Add("DELETED_IDS", string.Join(",", DeletedClicheId));

                SaveData(v);
            }
        }

        /// <summary>
        /// Запись данных для сохранения в БД
        /// </summary>
        /// <param name="data"></param>
        private async void SaveData(Dictionary<string, string> data)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "RigOrder");
            q.Request.SetParam("Action", "SaveContainer");
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
                if (result != null)
                {
                    if(result.ContainsKey("ITEMS"))
                    {
                        if (RigOrderId > 0)
                        {
                            if (!OldDesignFile.IsNullOrEmpty())
                            {
                                File.Delete(OldDesignFile);
                                OldDesignFile = "";
                            }

                        }
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "PreproductionContainer",
                            ReceiverName = ReceiverName,
                            SenderName = ControlName,
                            Action = "Refresh",
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
        /// Отображение формы редактирования заявки на оснастку ЛТ
        /// </summary>
        public void Show()
        {
            string title = $"Новая заявка на оснастку ЛТ";
            if (RigOrderId > 0)
            {
                title = $"Заявка на оснастку ЛТ {OrderNum.Text}";
            }

            Central.WM.Show(ControlName, title, true, "add", this);
        }

        /// <summary>
        /// Закрытие формы
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            if (!ReceiverName.IsNullOrEmpty())
            {
                Central.WM.SetActive(ReceiverName);
                ReceiverName = "";
            }
        }

        private async void CreateCliche()
        {
            var v = Form.GetValues();
            v.CheckAdd("ID", RigOrderId.ToString());

            v.CheckAdd("STATUS_ID", Status.ToString());

            var selectedItems = new List<Dictionary<string, string>>();
            foreach (var item in ClicheGrid.Items)
            {
                if (item.CheckGet("FOR_ORDER").ToBool())
                {
                    selectedItems.Add(item);
                }
            }
            string clicheList = JsonConvert.SerializeObject(selectedItems);
            v.Add("CLICHE_LIST", clicheList);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Rig");
            q.Request.SetParam("Object", "RigOrder");
            q.Request.SetParam("Action", "ClicheCreate");
            q.Request.SetParams(v);

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
                    var clicheds = ListDataSet.Create(result, "CLICHE_LIST");
                    ClicheGrid.UpdateItems(clicheds);
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

        private void Supplier_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateMaterialList();
        }

        private void Machine_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LoadGridItems();
        }
    }
}
