using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Common.LPackClientRequest;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма редактирования задания на расчет оснастки
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class RigCalculationTask : ControlBase
    {
        public RigCalculationTask()
        {
            InitializeComponent();

            InitForm();
            InitGrid();
            SetDefaults();
            LoadRef();

            OnLoad = () =>
            {
            };
        }

        /// <summary>
        /// ID задания
        /// </summary>
        public int RigTaskId;
        /// <summary>
        /// ID расчета цены, из которого сформировано задание на расчет оснастки
        /// </summary>
        public int PriceCalcId;
        /// <summary>
        /// ID техкарты, связанной с расчетом цены
        /// </summary>
        public int TechnologicalMapId;
        /// <summary>
        /// Имя вкладки, откуда вызвана форма и куда передается ответ
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Код группы пользователей. Используется для управления доступом
        /// </summary>
        private List<string> UserGroups;

        private bool Initialized;

        /// <summary>
        /// Начальные значения статусов расчета
        /// </summary>
        private int BlankCompleted;
        private int StampCompleted;
        private int ClicheCompleted;

        private Dictionary<string, string> ManagersDict { get; set; }

        /// <summary>
        /// Словарь с названиями статусов выполнения расчетов оснастки
        /// </summary>
        private Dictionary<string, string> StatusDict { get; set; }

        /// <summary>
        /// Форма редактирования задания
        /// </summary>
        FormHelper Form { get; set; }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            UserGroups = new List<string>();
            Initialized = false;
            ManagersDict = new Dictionary<string, string>();

            TechnologicalMapId = 0;
            AddFileButton.IsEnabled = false;
            DeleteFileButton.IsEnabled = false;
            DrawingFileSelectButton.IsEnabled = false;
            DrawingFileClearButton.IsEnabled = false;
            Form.SetDefaults();

            StatusDict = new Dictionary<string, string>()
            {
                { "0", "Не требуется" },
                { "1", "В разработке" },
                { "2", "Выполнено" },
                { "3", "Отклонено" },
            };
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
                    Path="CUSTOMER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CustomerName,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_CLASS_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductClass,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PROFILE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Profile,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=SLength,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null},
                    },
                },
                new FormHelperField()
                {
                    Path="WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=SWidth,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null},
                    },
                },
                new FormHelperField()
                {
                    Path="HEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=SHeight,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null},
                    },
                },
                new FormHelperField()
                {
                    Path="ID_FEFCO",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Fefco,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QTY_RANGE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=OrderQty,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Note,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BLANK_STATUS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=BlankStatus,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BLANK_STATUS_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=BlankStatusId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BLANK_LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=BlankLength,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null},
                    },
                },
                new FormHelperField()
                {
                    Path="BLANK_WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=BlankWidth,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null},
                    },
                },
                new FormHelperField()
                {
                    Path="QTY_ON_STAMP",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QtyOnStamp,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null},
                    },
                },
                new FormHelperField()
                {
                    Path="STAMP_STATUS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=StampStatus,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STAMP_STATUS_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=StampStatusId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STAMP_PRICE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=StampPrice,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null},
                    },
                },
                new FormHelperField()
                {
                    Path="STAMP_CURR_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=StampCurrency,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DRAWING_FILE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DrawingFile,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="_DRAWING_FILE_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DrawingFileName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CONSTRUCTOR_NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ConstructorNote,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CLICHE_STATUS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ClicheStatus,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CLICHE_STATUS_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ClicheStatusId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CLICHE_PRICE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ClichePrice,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null},
                    },
                },
                new FormHelperField()
                {
                    Path="CLICHE_CURR_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ClicheCurrency,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DESIGNER_NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DesignerNote,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

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
                    case "save":
                        Save();
                        break;
                    case "close":
                        Close();
                        break;
                    case "similar":
                        ShowSimilar();
                        break;
                    case "openfile":
                        OpenAttachment();
                        break;
                    case "savefile":
                        SaveAttachment();
                        break;
                    case "addfile":
                        AddAttachment();
                        break;
                    case "deletefile":
                        DeleteAttachment();
                        break;
                    case "drawingselect":
                        SelectDrawingFile();
                        break;
                    case "drawingshow":
                        ShowDrawingFile();
                        break;
                    case "drawingclear":
                        ClearDrawingFile();
                        break;
                }
            }
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
                    Header="Название файла",
                    Path="FILE_NAME_ORIGINAL",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="Загружен",
                    Path="DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width=10,
                    Format="dd.MM.yyyy HH:mm"
                },
                new DataGridHelperColumn
                {
                    Header="Владелец",
                    Path="OWNER",
                    ColumnType=ColumnTypeRef.String,
                    Width=10,
                },
                new DataGridHelperColumn
                {
                    Header="Имя файла",
                    Path="FILE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Владелец",
                    Path="OWNER_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Hidden=true,
                },
            };
            AttachmentGrid.SetColumns(columns);
            AttachmentGrid.SetPrimaryKey("_ROWNUMBER");
            AttachmentGrid.SetSorting("_ROWNUMBER", ListSortDirection.Descending);
            AttachmentGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            AttachmentGrid.AutoUpdateInterval = 0;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            AttachmentGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    UpdateActions(selectedItem);
                }
            };

            //данные грида
            AttachmentGrid.OnLoadItems = LoadItems;
            AttachmentGrid.Init();
        }

        /// <summary>
        /// Загрузка справочников
        /// </summary>
        private async void LoadRef()
        {
            UserGroups.Clear();
            // Загружаем список групп пользователя
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "RigCalculationTask");
            q.Request.SetParam("Action", "GetRef");
            q.Request.SetParam("EMPLOYEE_ID", Central.User.EmployeeId.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            }
            );

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var employeeGroups = ListDataSet.Create(result, "USER_GROUPS");
                    if (employeeGroups.Items.Count > 0)
                    {
                        foreach (var item in employeeGroups.Items)
                        {
                            if (item.CheckGet("IN_GROUP").ToInt() == 1)
                            {
                                switch (item.CheckGet("CODE"))
                                {
                                    case "manager":
                                        UserGroups.Add("manager");
                                        break;
                                    case "preproduction_design_engineer":
                                        UserGroups.Add("constructor");
                                        break;
                                    case "preproduction_designer":
                                        UserGroups.Add("designer");
                                        break;
                                    case "programmer":
                                        UserGroups.Add("programmer");
                                        break;
                                }
                            }
                        }
                    }

                    if (UserGroups.Count == 0)
                    {
                        UserGroups.Add("read-only");
                    }

                    var currencyDS = ListDataSet.Create(result, "CURRENCY");
                    // По умолчанию используется евро. Найдем его id
                    int idx = 0;
                    foreach (var c in currencyDS.Items)
                    {
                        if (c.CheckGet("CODE") == "EUR")
                        {
                            idx = c.CheckGet("ID").ToInt();
                        }
                    }
                    StampCurrency.Items = currencyDS.GetItemsList("ID", "NAME");
                    ClicheCurrency.Items = currencyDS.GetItemsList("ID", "NAME");
                    if (idx > 0)
                    {
                        string key = idx.ToString();
                        StampCurrency.SetSelectedItemByKey(key);
                        ClicheCurrency.SetSelectedItemByKey(key);
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            // В зависимости от группы регулируем доступ к элементам формы
            SetReadOnly();
        }

        /// <summary>
        /// Загрузка данных в таблицу приложенных файлов
        /// </summary>
        private async void LoadItems()
        {
            // Загружаем данные только если определен ID расчета цены
            if (TechnologicalMapId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "RigCalculationTask");
                q.Request.SetParam("Action", "ListAttachment");
                q.Request.SetParam("TECH_MAP_ID", TechnologicalMapId.ToString());

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                }
                );

                if (q.Answer.Status == 0)
                {
                    OpenFileButton.IsEnabled = false;
                    SaveFileButton.IsEnabled = false;
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var attachmentDS = ListDataSet.Create(result, "ATTACHMENT");
                        AttachmentGrid.UpdateItems(attachmentDS);
                        //AttachmentGrid.CellHeaderWidthProcess();
                        if (attachmentDS.Items.Count > 0)
                        {
                            OpenFileButton.IsEnabled = true;
                            SaveFileButton.IsEnabled = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Настройка доступа к полям формы
        /// </summary>
        /// <param name="role"></param>
        public void SetReadOnly(string role="")
        {
            if (!Initialized)
            {
                if (UserGroups.Contains("read-only") || (role == "read-only"))
                {
                    CustomerName.IsReadOnly = true;
                    ProductClass.IsReadOnly = true;
                    Profile.IsReadOnly = true;
                    SLength.IsReadOnly = true;
                    SWidth.IsReadOnly = true;
                    SHeight.IsReadOnly = true;
                    Fefco.IsReadOnly = true;
                    Note.IsReadOnly = true;
                    BlankLength.IsReadOnly = true;
                    BlankWidth.IsReadOnly = true;
                    QtyOnStamp.IsReadOnly = true;
                    StampPrice.IsReadOnly = true;
                    StampCurrency.IsEnabled = false;
                    ClichePrice.IsReadOnly = true;
                    ClicheCurrency.IsEnabled = false;
                    SaveButton.IsEnabled = false;
                    AddFileButton.IsEnabled = false;
                    SimilarButton.Visibility = Visibility.Collapsed;
                    DrawingFileSelectButton.IsEnabled = false;
                    DrawingFileClearButton.IsEnabled = false;
                }
                else if (UserGroups.Contains("manager"))
                {
                    BlankLength.IsReadOnly = true;
                    BlankWidth.IsReadOnly = true;
                    QtyOnStamp.IsReadOnly = true;
                    StampPrice.IsReadOnly = true;
                    StampCurrency.IsEnabled = false;
                    ClichePrice.IsReadOnly = true;
                    ClicheCurrency.IsEnabled = false;
                    SimilarButton.Visibility = Visibility.Collapsed;
                    DrawingFileSelectButton.IsEnabled = false;
                    DrawingFileClearButton.IsEnabled = false;
                }
                else if (UserGroups.Contains("constructor"))
                {
                    ClichePrice.IsReadOnly = true;
                    ClicheCurrency.IsEnabled = false;
                    DrawingFileSelectButton.IsEnabled = true;
                }
                else if (UserGroups.Contains("designer"))
                {
                    BlankLength.IsReadOnly = true;
                    BlankWidth.IsReadOnly = true;
                    QtyOnStamp.IsReadOnly = true;
                    StampPrice.IsReadOnly = true;
                    StampCurrency.IsEnabled = false;
                }
            }
        }

        /// <summary>
        /// Получение данных для полей формы
        /// </summary>
        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "RigCalculationTask");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("ID", RigTaskId.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            }
            );

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // профиль картона
                    var profilesDS = ListDataSet.Create(result, "PROFILES");
                    Profile.Items = profilesDS.GetItemsList("ID", "NAME");

                    // Покупатель
                    var customerDS = ListDataSet.Create(result, "CUSTOMERS");
                    CustomerName.Items = customerDS.GetItemsList("ID", "NAME");
                    // заполняем менеджеров
                    foreach (var item in customerDS.Items)
                    {
                        string managerId = item.CheckGet("MANAGER_ID");
                        if (managerId.ToInt() > 0)
                        {
                            ManagersDict.Add(item["ID"], managerId);
                        }
                    }

                    // вид изделия
                    var prodClassDS = ListDataSet.Create(result, "PRODUCT_CLASSES");
                    ProductClass.Items = prodClassDS.GetItemsList("ID", "NAME");

                    // FEFCO
                    var fefcoDS = ListDataSet.Create(result, "FEFCO");
                    Fefco.Items = fefcoDS.GetItemsList("ID", "NAME");

                    var qtyRange = new Dictionary<string, string>()
                    {
                        {"1", "0-3000 оттисков"},
                        {"2", "3001-5000 оттисков"},
                        {"3", "5001 и выше оттисков"},
                    };
                    OrderQty.Items = qtyRange;

                    if (RigTaskId > 0)
                    {
                        var taskDS = ListDataSet.Create(result, "RIG_TASK");
                        // Проверяем и заполняем поля статусов
                        var blankStatusId = taskDS.Items[0].CheckGet("BLANK_STATUS_ID");
                        if (StatusDict.ContainsKey(blankStatusId))
                        {
                            taskDS.Items[0].CheckAdd("BLANK_STATUS", StatusDict[blankStatusId]);
                        }
                        var stampStatusId = taskDS.Items[0].CheckGet("STAMP_STATUS_ID");
                        if (StatusDict.ContainsKey(stampStatusId))
                        {
                            taskDS.Items[0].CheckAdd("STAMP_STATUS", StatusDict[stampStatusId]);
                        }
                        var clicheStatusId = taskDS.Items[0].CheckGet("CLICHE_STATUS_ID");
                        if (StatusDict.ContainsKey(clicheStatusId))
                        {
                            taskDS.Items[0].CheckAdd("CLICHE_STATUS", StatusDict[clicheStatusId]);
                        }

                        //Показываем имя файла чертежа
                        var drawingFile = taskDS.Items[0].CheckGet("DRAWING_FILE");
                        if (!drawingFile.IsNullOrEmpty())
                        {
                            DrawingFileName.Text = Path.GetFileName(drawingFile);
                            DrawingFileShowButton.IsEnabled = true;
                            DrawingFileClearButton.IsEnabled = DrawingFileSelectButton.IsEnabled;
                        }

                        Form.SetValues(taskDS);
                        PriceCalcId = taskDS.Items[0].CheckGet("PRICE_CALC_ID").ToInt();
                        TechnologicalMapId = taskDS.Items[0].CheckGet("ID_TK").ToInt();

                        if (TechnologicalMapId > 0)
                        {
                            AddFileButton.IsEnabled = true;
                        }

                        if (PriceCalcId > 0)
                        {
                            AttachmentGrid.LoadItems();
                        }

                        int inWorkFlag = taskDS.Items[0].CheckGet("IN_WORK_FLAG").ToInt();
                        if (inWorkFlag > 0)
                        {
                            CustomerName.IsReadOnly = true;
                            ProductClass.IsReadOnly = true;
                            Profile.IsReadOnly = true;
                            SLength.IsReadOnly = true;
                            SWidth.IsReadOnly = true;
                            SHeight.IsReadOnly = true;
                            Note.IsReadOnly = true;
                        }

                        BlankCompleted = 0;
                        StampCompleted = 0;
                        ClicheCompleted = 0;
                    }

                    Initialized = true;
                    Show();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Обновление действий с записью
        /// </summary>
        /// <param name="selectedItem"></param>
        private void UpdateActions(Dictionary<string, string> selectedItem)
        {
            DeleteFileButton.IsEnabled = false;
            if (AttachmentGrid.Items != null)
            {
                if (AttachmentGrid.Items.Count > 0)
                {
                    if (selectedItem.CheckGet("OWNER_ID").ToInt() == 1)
                    {
                        DeleteFileButton.IsEnabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// Вызов формы редактирования
        /// </summary>
        /// <param name="id"></param>
        public void Edit(int id)
        {
            RigTaskId = id;
            GetData();
        }

        /// <summary>
        /// Отображение вкладки с формой
        /// </summary>
        public void Show()
        {
            ControlName = $"RigCalcTask{RigTaskId}";
            ControlTitle = $"Задача расчета оснастки {RigTaskId}";

            Central.WM.Show(ControlName, ControlTitle, true, "add", this);
            Central.WM.SetActive(ControlName);
        }

        /// <summary>
        /// Закрытие вкладки с формой
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab(ControlName);
            Central.WM.SetActive(ReceiverName);
        }

        /// <summary>
        /// Проверка и подготовка данных перед сохранением
        /// </summary>
        private void Save()
        {
            var p = Form.GetValues();
            bool resume = true;

            p.CheckAdd("ID", RigTaskId.ToString());
            // Если покупатель "ТД Л-ПАК", то менеджером ставим текущего пользователя
            // В остальных случаях менеджером будет прикрепленный менеджер
            string managerId = Central.User.EmployeeId.ToString();
            if (CustomerName.SelectedItem.Key.ToInt() != 4202)
            {
                if (ManagersDict.ContainsKey(p["CUSTOMER_ID"]))
                {
                    managerId = ManagersDict[p["CUSTOMER_ID"]];
                }
            }
            p.CheckAdd("MANAGER_ID", managerId);

            // Размер диапазона партии
            if (resume)
            {
                var qtyRange = p.CheckGet("QTY_RANGE").ToInt();
                if(!qtyRange.ContainsIn(1, 2, 3))
                {
                    resume = false;
                    Form.SetStatus("Не заполнен размер партии", 1);
                }
            }

            // Отметки о выполнении работ.
            p.CheckAdd("BLANK_COMPLETED", BlankCompleted.ToString());
            p.CheckAdd("STAMP_COMPLETED", StampCompleted.ToString());
            p.CheckAdd("CLICHE_COMPLETED", ClicheCompleted.ToString());
            // Если стоит отметка заполнения полей развертки, поверяем заполненность всех полей и передадим статус Выполнено
            if (resume)
            {

                var ln = p.CheckGet("BLANK_LENGTH").ToInt();
                var wd = p.CheckGet("BLANK_WIDTH").ToInt();
                var qt = p.CheckGet("QTY_ON_STAMP").ToInt();

                if (BlankCompleted == 1)
                {
                    if ((ln == 0) || (wd == 0) || (qt == 0))
                    {
                        resume = false;
                        Form.SetStatus("Заполнены не все поля развертки", 1);
                    }

                    // Проверяем ограничения значений
                    // Длины развертки не больше 6 символов, количество не больше 2
                    if (ln >= 1000000)
                    {
                        resume = false;
                        Form.SetStatus("Длина развертки слишком большая", 1);
                    }

                    if (wd >= 1000000)
                    {
                        resume = false;
                        Form.SetStatus("Ширина развертки слишком большая", 1);
                    }

                    if (qt >= 100)
                    {
                        resume = false;
                        Form.SetStatus("Количество на штампе слишком большое", 1);
                    }

                    // Если всё нормально, ставим статус расчета развертки Выполнено
                    if (resume)
                    {
                        p.CheckAdd("BLANK_STATUS_ID", "2");
                    }
                }
            }

            // Если у стоимости штампа стоит отметска заполнения, проверяем, что все поля заполнены и ставим статус Выполнено
            if (resume)
            {
                if (StampCompleted == 1)
                {
                    var sp = p.CheckGet("STAMP_PRICE").ToInt();
                    var sc = p.CheckGet("STAMP_CURR_ID").ToInt();

                    if ((sp == 0) || (sc == 0))
                    {
                        resume = false;
                        Form.SetStatus("Заполнены не все поля стоимости штампа", 1);
                    }

                    if (resume)
                    {
                        p.CheckAdd("STAMP_STATUS_ID", "2");
                    }
                }
            }

            if (resume)
            {
                if (ClicheCompleted == 1)
                {
                    var sp = p.CheckGet("CLICHE_PRICE").ToInt();
                    var sc = p.CheckGet("CLICHE_CURR_ID").ToInt();

                    if ((sp == 0) || (sc == 0))
                    {
                        resume = false;
                        Form.SetStatus("Заполнены не все поля стоимости клише", 1);
                    }

                    if (resume)
                    {
                        p.CheckAdd("CLICHE_STATUS_ID", "2");
                    }
                }
            }

            if (resume)
            {
                SaveData(p);
            }
        }

        /// <summary>
        /// Запись данных в БД
        /// </summary>
        /// <param name="p"></param>
        private async void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "RigCalculationTask");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            }
            );

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (result.ContainsKey("ITEMS"))
                    {
                        // Отправляем сообщение гриду о необходимости обновить таблицу
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "PreproductionSample",
                            ReceiverName = ReceiverName,
                            SenderName = "Sample",
                            Action = "Refresh",
                        });
                        Close();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Получает и открывает приложенный к образцу файл
        /// </summary>
        private async void OpenAttachment()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "RigCalculationTask");
            q.Request.SetParam("Action", "GetAttachment");
            q.Request.SetParam("ID", AttachmentGrid.SelectedItem.CheckGet("ID").ToInt().ToString());
            q.Request.SetParam("FILE_NAME", AttachmentGrid.SelectedItem.CheckGet("FILE_NAME_ORIGINAL"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Central.OpenFile(q.Answer.DownloadFilePath);
            }
            else
            {
                Form.SetStatus("Файл не найден", 1);
            }
        }

        /// <summary>
        /// Сохранение приложенного файла
        /// </summary>
        private async void SaveAttachment()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "RigCalculationTask");
            q.Request.SetParam("Action", "GetAttachment");
            q.Request.SetParam("ID", AttachmentGrid.SelectedItem.CheckGet("ID").ToInt().ToString());
            q.Request.SetParam("FILE_NAME", AttachmentGrid.SelectedItem.CheckGet("FILE_NAME_ORIGINAL"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Central.SaveFile(q.Answer.DownloadFilePath);
            }
            else
            {
                Form.SetStatus("Файл не найден", 1);
            }
        }

        /// <summary>
        /// Открытие формы с похожими заданиями и изделиями
        /// </summary>
        private void ShowSimilar()
        {
            var p = Form.GetValues();
            p.Add("TASK_ID", RigTaskId.ToString());
            p.Add("PRODUCT_NAME", $"{CustomerName.SelectedItem.Value} {SLength.Text}x{SWidth.Text}x{SHeight.Text}({Profile.SelectedItem.Value}) {ProductClass.SelectedItem.Value}");

            var similarForm = new RigCalculationTaskSimilar();
            similarForm.TaskValues = p;
            similarForm.ReceiverName = ControlName;
            similarForm.Show();
        }

        /// <summary>
        /// Добавление файл к приложенным файлам
        /// </summary>
        private async void AddAttachment()
        {
            FormStatus.Text = "";
            bool resume = true;

            var fd = new OpenFileDialog();
            var fdResult = (bool)fd.ShowDialog();

            if (fdResult)
            {
                var fileName = Path.GetFileName(fd.FileName);
                // Исключаем дублирование файлов
                if (AttachmentGrid.Items != null)
                {
                    foreach (var item in AttachmentGrid.Items)
                    {
                        if (item["FILE_NAME_ORIGINAL"] == fileName)
                        {
                            Form.SetStatus("Такой файл уже есть в списке", 1);
                            resume = false;
                        }
                    }
                }
            }
            else
            {
                resume = false;
            }

            // Сохраняем файл
            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "RigCalculationTask");
                q.Request.SetParam("Action", "AddAttachment");
                q.Request.SetParam("ID", TechnologicalMapId.ToString());
                q.Request.Type = RequestTypeRef.MultipartForm;
                q.Request.UploadFilePath = fd.FileName;

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
                            LoadItems();
                        }
                    }
                }
                else
                {
                    if (q.Answer.Error.Code == 145)
                    {
                        Form.SetStatus(q.Answer.Error.Message, 1);
                    }
                }
            }
        }

        /// <summary>
        /// Удаление файла из таблицы приложенных файлов
        /// </summary>
        private async void DeleteAttachment()
        {
            FormStatus.Text = "";
            if (AttachmentGrid.SelectedItem != null)
            {
                int fileId = AttachmentGrid.SelectedItem.CheckGet("ID").ToInt();
                string fileName = AttachmentGrid.SelectedItem.CheckGet("FILE_NAME");

                if (!fileName.IsNullOrEmpty() && (fileId > 0))
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Preproduction");
                    q.Request.SetParam("Object", "RigCalculationTask");
                    q.Request.SetParam("Action", "DeleteAttachment");
                    q.Request.SetParam("ID", fileId.ToString());
                    q.Request.SetParam("FILE_NAME", fileName);

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
                                LoadItems();
                            }
                            else
                            {
                                Form.SetStatus("Ошибка! Выберите файл снова", 1);
                            }
                        }
                    }
                    else
                    {
                        if (q.Answer.Error.Code == 145)
                        {
                            Form.SetStatus(q.Answer.Error.Message, 1);
                        }
                    }
                }
                else
                {
                    Form.SetStatus("Ошибка выбора файла для удаления. Выберите снова", 1);
                }
            }
        }

        /// <summary>
        /// Выбор файла чертежа
        /// </summary>
        private void SelectDrawingFile()
        {
            var fd = new OpenFileDialog();
            if ((bool)fd.ShowDialog())
            {
                var drawingPath = fd.FileName;
                DrawingFile.Text = drawingPath;
                DrawingFileName.Text = Path.GetFileName(drawingPath);
                DrawingFileShowButton.IsEnabled = true;
                DrawingFileClearButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Открыть файл чертежа развертки
        /// </summary>
        private void ShowDrawingFile()
        {
            if (!DrawingFile.Text.IsNullOrEmpty())
            {
                Central.OpenFile(DrawingFile.Text);
            }
        }

        /// <summary>
        /// Очистка привязанного файла чертежа развертки
        /// </summary>
        private void ClearDrawingFile()
        {
            DrawingFile.Text = "";
            DrawingFileName.Text = "";
            DrawingFileShowButton.IsEnabled = false;
            DrawingFileClearButton.IsEnabled = false;
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

        /// <summary>
        /// Обработка изменений полей развертки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BlankTextFieldChanged(object sender, TextChangedEventArgs e)
        {
            if (Initialized)
            {
                BlankCompleted = 1;
                if (BlankStatusId.Text.ToInt() == 2)
                {
                    BlankStatus.Text = "Изменено";
                }
            }
        }

        /// <summary>
        /// Обработка изменений полей стоимости штампа
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StampTextChanged(object sender, TextChangedEventArgs e)
        {
            if (Initialized)
            {
                StampCompleted = 1;
                if (StampStatusId.Text.ToInt() == 2)
                {
                    StampStatus.Text = "Изменено";
                }
            }
        }

        /// <summary>
        /// Обработка изменений полей стоимости клише
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClicheTextChanged(object sender, TextChangedEventArgs e)
        {
            if (Initialized)
            {
                ClicheCompleted = 1;
                if (ClicheStatusId.Text.ToInt() == 2)
                {
                    ClicheStatus.Text = "Изменено";
                }
            }
        }

        private void StampCurrency_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (Initialized)
            {
                StampCompleted = 1;
                if (StampStatusId.Text.ToInt() == 2)
                {
                    StampStatus.Text = "Изменено";
                }
            }
        }

        private void ClicheCurrency_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (Initialized)
            {
                ClicheCompleted = 1;
                if (ClicheStatusId.Text.ToInt() == 2)
                {
                    ClicheStatus.Text = "Изменено";
                }
            }
        }
    }
}
