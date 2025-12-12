using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Редактирование технологической техкарты литой тары
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class MoldedContainerTechCard : ControlBase
    {
        public MoldedContainerTechCard()
        {
            DocumentationUrl = "/doc/l-pack-erp/preproduction/tk_grid/molded_container";
            InitializeComponent();

            InitForm();
            InitPrintGrid();
            CopyMode = false;

            OnLoad = () =>
            {
            };

            OnUnload = () =>
            {
                PrintGrid.Destruct();
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "PreproductionContainer",
                    ReceiverName = ReceiverName,
                    SenderName = ControlName,
                    Action = "refresh",
                });
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    switch (e.Key)
                    {
                        case Key.F1:
                            Commander.ProcessCommand("help");
                            e.Handled = true;
                            break;
                    }
                }
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessMessage(msg);
                }
            };

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Enabled = true,
                    Title = "Сохранить",
                    Description = "Сохранить/пересохранить и закрыть",
                    ButtonUse = true,
                    ButtonName = "SaveButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Save(1);
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "apply",
                    Enabled = true,
                    Title = "Применить",
                    Description = "Сохранить/пересохранить без закрытия",
                    ButtonUse = true,
                    ButtonName = "ApplyButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Save(0);
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "close",
                    Enabled = true,
                    Title = "Отмена",
                    Description = "Отмена",
                    ButtonUse = true,
                    ButtonName = "CancelButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Close();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "archive",
                    Enabled = true,
                    Title = "В архив",
                    Description = "Отправить техкарту в архив",
                    ButtonUse = true,
                    ButtonName = "ArchiveButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        SetArchive();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        if (StatusId.ContainsIn(6, 7))
                        {
                            result = true;
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "help",
                    Enabled = true,
                    Title = "Справка",
                    Description = "Показать справочную информацию",
                    ButtonUse = true,
                    ButtonName = "HelpButton",
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "stickerselect",
                    Enabled = true,
                    Title = "",
                    Description = "Выбрать этикетку",
                    ButtonUse = true,
                    ButtonName = "StickerSelectButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        var stickerFrame = new MoldedContainerStickerSelect();
                        stickerFrame.ReceiverName = ControlName;
                        stickerFrame.TechCardId = TechCardId;
                        stickerFrame.Show();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        if (Article.Text.IsNullOrEmpty())
                        {
                            var s = ProductType.SelectedItem.Key.ToInt();
                            if (s == 3 || s == 4)
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "stickerclear",
                    Enabled = true,
                    Title = "",
                    Description = "Очистить этикетку",
                    ButtonUse = true,
                    ButtonName = "StickerClearButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        StickerId.Text = "";
                        StickerName.Text = "";
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        if (Article.Text.IsNullOrEmpty())
                        {
                            result = !StickerName.Text.IsNullOrEmpty();
                        }
                        return result;
                    },
                });

                Commander.Add(new CommandItem()
                {
                    Name = "createexcel",
                    Enabled = true,
                    Title = "Создать Excel",
                    Description = "Создать Excel",
                    ButtonUse = true,
                    ButtonName = "CreateExcelButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        CreateExcel();
                    },
                    CheckEnabled = () =>
                    {
                        bool fileExists = File.Exists(FilePath);
                        var result = !fileExists && (TechCardId > 0);
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "recreateexcel",
                    Enabled = true,
                    Title = "Пересоздать Excel",
                    Description = "Пересоздать файл Excel",
                    ButtonUse = true,
                    ButtonName = "RecreateExcelButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        CreateExcel(true);
                    },
                    CheckEnabled = () =>
                    {
                        var result = File.Exists(FilePath) && StatusId.ContainsIn(1, 3, 4);
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "openexcel",
                    Enabled = true,
                    Title = "Открыть Excel",
                    Description = "Открыть Excel",
                    ButtonUse = true,
                    ButtonName = "OpenExcelButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Central.OpenFile(FilePath);
                    },
                    CheckEnabled = () =>
                    {
                        var result = File.Exists(FilePath);
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "updateexcel",
                    Enabled = true,
                    Title = "Перенести данные в Excel",
                    Description = "Перенести данные в Excel",
                    ButtonUse = true,
                    ButtonName = "UpdateExcelButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        UpdateExcel();
                    },
                    CheckEnabled = () =>
                    {
                        var result = File.Exists(FilePath) && !ArchivedFlag;
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "accept",
                    Enabled = true,
                    Title = "Создать товар",
                    Description = "Создать товар",
                    ButtonUse = true,
                    ButtonName = "AcceptButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        AcceptProduct();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        if (!Article.Text.IsNullOrEmpty() && GoodsName.Text.IsNullOrEmpty() && StatusId.ContainsIn(1, 3, 4, 5))
                        {
                            // Если техкарта с этикеткой, то этикетка должна быть привязана
                            if ((bool)StickerFlag.IsChecked)
                            {
                                result = !StickerId.Text.IsNullOrEmpty();
                            }
                            else
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });
            }
            Commander.SetCurrentGridName("PrintGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "createcolor",
                    Enabled = true,
                    Title = "Добавить",
                    Description = "Добавить цвет",
                    ButtonUse = true,
                    ButtonName = "CreateButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        CreateColor();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        bool printingFlag = (bool)PrintingFlag.IsChecked;
                        if (printingFlag && StatusId.ContainsIn(1, 3, 4))
                        {
                            result = true;
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "editcolor",
                    Enabled = true,
                    Title = "Изменить",
                    Description = "Изменить цвет",
                    ButtonUse = true,
                    ButtonName = "EditButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        EditColor();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        bool printingFlag = (bool)PrintingFlag.IsChecked;
                        if (printingFlag && StatusId.ContainsIn(1, 3, 4))
                        {
                            if (PrintGrid.Items != null)
                            {
                                if (PrintGrid.Items.Count > 0)
                                {
                                    var row = PrintGrid.SelectedItem;
                                    if (row.CheckGet("_ROWNUMBER").ToInt() != 0)
                                    {
                                        result = true;
                                    }
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "deletecolor",
                    Enabled = true,
                    Title = "Удалить",
                    Description = "Изменить цвет",
                    ButtonUse = true,
                    ButtonName = "DeleteButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        DeleteColor();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        bool printingFlag = (bool)PrintingFlag.IsChecked;
                        if (printingFlag && StatusId.ContainsIn(1, 3, 4))
                        {
                            if (PrintGrid.Items != null)
                            {
                                var row = PrintGrid.SelectedItem;
                                if (row.CheckGet("_ROWNUMBER").ToInt() != 0)
                                {
                                    result = true;
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "printingarea",
                    Enabled = true,
                    Title = "Площади",
                    Description = "Изменить площади запечатки цветов",
                    ButtonUse = true,
                    ButtonName = "PrintingAreaButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        var printingAreaFrame = new MoldedContainerPrintingArea();
                        printingAreaFrame.ReceiverName = ControlName;
                        printingAreaFrame.Edit(TechCardId);
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        bool printingFlag = (bool)PrintingFlag.IsChecked;
                        // Площадь запечатки вносим для готовых техкарт
                        if (printingFlag && (StatusId == 6) && !ArchivedFlag)
                        {
                            if (PrintGrid.Items != null)
                            {
                                result = true;
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "colorup",
                    Enabled = true,
                    Title = "",
                    Description = "Переместить цвет вверх",
                    ButtonUse = true,
                    ButtonName = "UpButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        SwapColors(-1);
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        bool printingFlag = (bool)PrintingFlag.IsChecked;
                        if (printingFlag && StatusId.ContainsIn(1, 3, 4))
                        {
                            if (PrintGrid.Items != null)
                            {
                                var rowNum = PrintGrid.SelectedItem.CheckGet("_ROWNUMBER").ToInt();
                                if (rowNum > 1)
                                {
                                    result = true;
                                }
                            }
                        }
                        return result;
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "colordown",
                    Enabled = true,
                    Title = "",
                    Description = "Переместить цвет вниз",
                    ButtonUse = true,
                    ButtonName = "DownButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        SwapColors(1);
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        bool printingFlag = (bool)PrintingFlag.IsChecked;
                        if (printingFlag && StatusId.ContainsIn(1, 3, 4))
                        {
                            if (PrintGrid.Items != null)
                            {
                                var rowNum = PrintGrid.SelectedItem.CheckGet("_ROWNUMBER").ToInt();
                                if ((rowNum > 0) && (rowNum < PrintGrid.Items.Count))
                                {
                                    result = true;
                                }
                            }
                        }
                        return result;
                    },
                });
            }

            Commander.SetCurrentGridName("Confirm");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "send_to_confirm",
                    Enabled = true,
                    Title = "Отправить на подтверждение",
                    Description = "Отправить на подтверждение клиентом",
                    ButtonUse = true,
                    ButtonName = "SendTkToConfirmButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        //TkSendToConfirm();
                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        if (StatusId == 3)
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
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// Форма редактирования техкарты
        /// </summary>
        FormHelper Form { get; set; }
        /// <summary>
        /// Идентификатор открытой техкарты
        /// </summary>
        public int TechCardId;
        /// <summary>
        /// Идентификатор копируемой техкарты
        /// </summary>
        public int CopiedId;
        /// <summary>
        /// ID статуса техкарты. Если техкарта в архиве, статус ставим 10. Используем для настройки доступа к полям и кнопкам
        /// </summary>
        public int StatusId;
        /// <summary>
        /// Флаг окончания заполнения формы
        /// </summary>
        private bool InitializedFlag;
        /// <summary>
        /// Флаг изменения схемы укладки, отправляем на сервер команду перезалить рисунок укладки в файле техкарты
        /// </summary>
        private bool LayingSchemeCanged;
        /// <summary>
        /// Режим создания копии техкарты
        /// </summary>
        public bool CopyMode { get; set; }
        /// <summary>
        /// Данные по покупателям и потребителям
        /// </summary>
        private ListDataSet BuyerDS { get; set; }
        /// <summary>
        /// Данные по типу контейнера
        /// </summary>
        private ListDataSet ProductTypeDS { get; set; }
        /// <summary>
        /// Области печати
        /// </summary>
        private ListDataSet PrintingSpot { get; set; }
        private Dictionary<string, string> SpotList { get; set; }
        /// <summary>
        /// Краски для печати
        /// </summary>
        private ListDataSet PrintingColor { get; set; }
        /// <summary>
        /// Цвета печати на контейнере, данные для таблицы печати
        /// </summary>
        private ListDataSet ContainerColorDS { get; set; }
        /// <summary>
        /// Список идентификаторов удаленных областей печати
        /// </summary>
        private List<int> DeletedPrintingColor { get; set; }

        /// <summary>
        /// Путь к файлу техкарты
        /// </summary>
        private string FilePath { get; set; }
        /// <summary>
        /// Путь к файлу дизайна. При переименовании файла техкарты присваиваем такое же имя и файлу дизайна
        /// </summary>
        private string DesignFilePath { get; set; }

        private bool ArchivedFlag;

        /// <summary>
        /// Обработка сообщений из шины сообщений
        /// </summary>
        /// <param name="msg"></param>
        public void ProcessMessage(ItemMessage msg)
        {
            string action = msg.Action;
            if (!action.IsNullOrEmpty())
            {
                switch (action)
                {
                    case "EditColor":
                        var obj = (Dictionary<string, string>)msg.ContextObject;
                        if (obj.Count > 0)
                        {
                            int orderNum = obj.CheckGet("ORDER_NUM").ToInt();
                            if (orderNum == 0)
                            {
                                InsertColor(obj);
                            }
                            else
                            {
                                UpdateColor(obj);
                            }
                        }
                        break;
                    case "StickerSelected":
                        var stobj = (Dictionary<string, string>)msg.ContextObject;
                        if (stobj.Count > 0)
                        {
                            var stickerId = stobj.CheckGet("ID").ToInt();
                            if (stickerId > 0)
                            {
                                StickerId.Text = stickerId.ToString();
                                StickerName.Text = stobj.CheckGet("NAME");
                            }
                            else
                            {
                                Form.SetStatus("Ошибка выбора этикетки", 1);
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Вызов справки
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/preproduction_new/tk/tare");
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            ArchivedFlag = false;
            PrintingSpot = new ListDataSet();
            PrintingSpot.Init();
            SpotList = new Dictionary<string, string>();
            PrintingColor = new ListDataSet();
            PrintingColor.Init();
            ContainerColorDS = new ListDataSet();
            ContainerColorDS.Init();
            DeletedPrintingColor = new List<int>();

            var p = new Dictionary<string, string>()
            {
                { "PACKAGING_FLAG", "1" },
                { "CORNERS_FLAG", "1" },
                { "WRAP_STRETCH_FLAG", "1" },
                { "SHEET_LAYER_TYPE_ID", "1" },
            };

            Form.SetValues(p);
        }

        /// <summary>
        /// Заполнение полей укладки значениями по умолчанию при выборе поддона
        /// </summary>
        private void SetDefaultsPalletSelection(int palletId)
        {
            // Заполняем для поддона 1200x100
            var p = new Dictionary<string, string>()
            {
                { "LAYING_SCHEME_ID", "496" },
                { "PER_STACK_QTY", "103" },
                { "STACK_IN_ROW_QTY", "5" },
                { "ROW_IN_PALLET_QTY", "9" },
                { "PALLET_LENGTH", "1240" },
                { "PALLET_WIDTH", "1000" },
                { "PALLET_HEIGHT", "2380" },
            };
            switch (palletId)
            {
                // 1200x800
                case 3:
                    p["LAYING_SCHEME_ID"] = "497";
                    p["PER_STACK_QTY"] = "72";
                    p["PALLET_LENGTH"] = "1200";
                    p["PALLET_WIDTH"] = "800";
                    break;
            }
            Form.SetValues(p);
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
                    Path="SKU_CODE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Article,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_TYPE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductType,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="CONTNR_COLOR_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductColor,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="BUYER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CntnBuyer,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CUSTOMER_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CntnCustomerName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CUSTOMER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CntnCustomerId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PROD_SCHEME_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductionScheme,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STICKER_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=StickerFlag,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRINTING_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=PrintingFlag,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STICKER_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=StickerName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STICKER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=StickerId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PALLET_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Pallet,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LAYING_SCHEME_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=LayingScheme,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PALLET_LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PalletLength,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PALLET_WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PalletWidth,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PALLET_HEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PalletHeight,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PER_STACK_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PerStackQty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PER_PALLET_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PerPalletQty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACK_IN_ROW_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=StackInRowQty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ROW_IN_PALLET_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=RowInPalletQty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PACKAGING_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=PackagingFlag,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="BAG_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=BagFlag,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CORNERS_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=CornersFlag,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="WRAP_STRETCH_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=WrapStretchFlag,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SHEET_LAYER_TYPE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=SheetLayerType,
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
                    Path="ENGINEER_NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=EngeneerNoteTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="GOODS_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=GoodsName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.StatusControl = FormStatus;

            // Блокируем кнопку сохранения, пока не выполнена загрузка данных
            SaveButton.IsEnabled = false;
        }

        /// <summary>
        /// Инициализация таблицы печати
        /// </summary>
        private void InitPrintGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Номер",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Место печати",
                    Path="PRINTING_SPOT",
                    ColumnType=ColumnTypeRef.String,
                    Width2=16,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование цвета",
                    Path="COLOR_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=8,
                },
                new DataGridHelperColumn
                {
                    Header="Цвет",
                    Path="_COLOR",
                    Options="hexcolor",
                    ColumnType=ColumnTypeRef.String,
                    Width2=5,
                    Stylers=new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor,
                            row =>
                            {
                                var result=DependencyProperty.UnsetValue;
                                var color = row.CheckGet("COLOR_HEX");

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = HexToBrush(color);
                                }

                                return result;
                            }
                        },
                    },
                },
                new DataGridHelperColumn
                {
                    Header="Код цвета",
                    Path="COLOR_HEX",
                    ColumnType=ColumnTypeRef.String,
                    Hidden=true,
                },
            };
            PrintGrid.SetColumns(columns);
            PrintGrid.SetPrimaryKey("_ROWNUMBER");
            PrintGrid.SetSorting("_ROWNUMBER", ListSortDirection.Ascending);
            PrintGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            PrintGrid.AutoUpdateInterval = 0;
            PrintGrid.Commands = Commander;

            PrintGrid.OnLoadItems = PrintLoadItems;

            PrintGrid.Init();
        }

        /// <summary>
        /// Выделяет название области печати из полного названия
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        private string GetAreaName(string fullName)
        {
            string result = "";
            int l = fullName.Length;
            if (l > 0)
            {
                result = fullName.Substring(0, l - 2);
            }

            return result;
        }

        private void PrintLoadItems()
        {
            if (ContainerColorDS.Items != null)
            {
                PrintGrid.UpdateItems(ContainerColorDS);
            }
        }

        /// <summary>
        /// Получение данных для формы
        /// </summary>
        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "GetTechCard");
            q.Request.SetParam("ID", TechCardId.ToString());


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
                    InitializedFlag = false;
                    LayingSchemeCanged = false;
                    FilePath = "";
                    DesignFilePath = "";
                    // Создаем вкладку
                    Show();
                    SetDefaults();
                    // По умолчанию статус В работе. Новые техкарты создаются клиентом в ЛК
                    StatusId = 3;

                    // Типы изделий
                    if (result.ContainsKey("TYPES"))
                    {
                        ProductTypeDS = ListDataSet.Create(result, "TYPES");
                        ProductType.Items = ProductTypeDS.GetItemsList("ID", "FULL_TYPE_NAME");
                        // По умолчанию контейнер без печати
                        ProductType.SetSelectedItemByKey("1");
                    }

                    // Цвет материала изделий
                    if (result.ContainsKey("RAW_COLORS"))
                    {
                        var productColorDS = ListDataSet.Create(result, "RAW_COLORS");
                        ProductColor.Items = productColorDS.GetItemsList("ID", "NAME");
                        // По умолчанию контейнер без печати
                        ProductColor.SetSelectedItemByKey("1");
                    }

                    // Покупатели
                    if (result.ContainsKey("BUYERS"))
                    {
                        BuyerDS = ListDataSet.Create(result, "BUYERS");
                        CntnBuyer.Items = BuyerDS.GetItemsList("ID", "NAME");
                    }

                    // Схемы производства
                    if (result.ContainsKey("PRODUCTION_SCHEMES"))
                    {
                        var dssc = ListDataSet.Create(result, "PRODUCTION_SCHEMES");
                        ProductionScheme.Items = dssc.GetItemsList("ID", "NAME");
                    }

                    //Добавляем в список только используемые поддоны
                    var palletList = new Dictionary<string, string>()
                        {
                            { "3", "1200x800" },
                            { "4", "1200x1000" },
                        };
                    Pallet.Items = palletList;

                    // Схемы укладки
                    if (result.ContainsKey("LAYING"))
                    {
                        var palletDS = ListDataSet.Create(result, "LAYING");
                        LayingScheme.Items = palletDS.GetItemsList("ID", "NAME");
                    }

                    // варианты перестила
                    if (result.ContainsKey("SHEET_LAYER_TYPE"))
                    {
                        var sheetsDS = ListDataSet.Create(result, "SHEET_LAYER_TYPE");
                        SheetLayerType.Items = sheetsDS.GetItemsList("ID", "NAME");
                    }

                    // Краски для печати и области печати
                    if (result.ContainsKey("COLORS"))
                    {
                        PrintingColor = ListDataSet.Create(result, "COLORS");
                    }
                    if (result.ContainsKey("PRINT_SPOT"))
                    {
                        PrintingSpot = ListDataSet.Create(result, "PRINT_SPOT");
                        ProcessPrintingSpot();
                    }

                    if (result.ContainsKey("TECHCARD"))
                    {
                        var mainDS = ListDataSet.Create(result, "TECHCARD");
                        if (CopyMode && CopiedId == 0)
                        {
                            if (mainDS.Items.Count > 0)
                            {
                                // При копировании затираем информацию об артикуле, товаре, затираем данные файла техкарты
                                var rec = mainDS.Items[0];
                                rec["SKU_CODE"] = "";
                                rec["STICKER_NAME"] = "";
                                rec["STICKER_ID"] = "";
                                rec["GOODS_NAME"] = "";
                                rec["PRODUCT_NAME"] = "КОПИЯ " + rec["PRODUCT_NAME"];
                                TechCardId = 0;
                            }
                        }

                        Form.SetValues(mainDS);

                        if (!CopyMode)
                        {
                            // Достаем статус и для согласованных техкарт бех артикула показываем кнопку присвоения артикула
                            if (mainDS.Items.Count > 0)
                            {
                                StatusId = mainDS.Items[0].CheckGet("STATUS_ID").ToInt();

                                //Если у техкарты стоит флаг архивации, 
                                if (StatusId == 7)
                                {
                                    ArchivedFlag = true;
                                }
                                else
                                {
                                    ArchivedFlag = false;
                                }

                                // Сохраняем путь к техкарте
                                string fileName = mainDS.Items[0].CheckGet("FILE_NAME");
                                if (!fileName.IsNullOrEmpty())
                                {
                                    string folderName = mainDS.Items[0].CheckGet("FOLDER_NAME");
                                    FilePath = Path.Combine(folderName, fileName);
                                }

                                DesignFilePath = mainDS.Items[0].CheckGet("DESIGN_FILE");
                            }
                        }
                    }
                    else
                    {
                        Pallet.SetSelectedItemByKey("4");
                        SetDefaultsPalletSelection(4);
                    }

                    // Цвета печати
                    if (result.ContainsKey("PRINT"))
                    {
                        ContainerColorDS = ListDataSet.Create(result, "PRINT");

                        if (CopyMode && CopiedId == 0)
                        {
                            if (ContainerColorDS.Items.Count > 0)
                            {
                                foreach (var row in ContainerColorDS.Items)
                                {
                                    row.CheckAdd("ID", "0");
                                    row.CheckAdd("PRINTING_AREA", "0");
                                    row.CheckAdd("CHANGED", "1");
                                }
                            }
                        }

                        PrintGrid.LoadItems();
                    }

                    // После загрузки разблокируем кнопку сохранения
                    SaveButton.IsEnabled = true;
                    if (CopiedId > 0)
                    {
                        SetCopied();
                    }
                    ProcessPackaging();
                    SetFieldsAvailable();

                    InitializedFlag = true;
                }
            }

        }

        /// <summary>
        /// Установить данные из скопированной заявки
        /// </summary>
        private async void SetCopied()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "GetTechCard");
            q.Request.SetParam("ID", CopiedId.ToString());


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
                    if (result.ContainsKey("TECHCARD"))
                    {
                        string error = "";
                        var dictionary = new Dictionary<string, string>();

                        var attachDS = ListDataSet.Create(result, "TECHCARD");
                        var formValues = Form.GetValues();

                        if (attachDS.Items.Count > 0)
                        {
                            var rec = attachDS?.Items[0];
                            rec["SKU_CODE"] = "";
                            rec["STICKER_NAME"] = "";
                            rec["STICKER_ID"] = "";
                            rec["GOODS_NAME"] = "";

                            if (rec["CUSTOMER_ID"].ToInt() != 0)
                            {
                                if (formValues.CheckGet("CUSTOMER_ID").ToInt() != 0 && rec["CUSTOMER_ID"].ToInt() != formValues.CheckGet("CUSTOMER_ID").ToInt())
                                {
                                    error = error + "покупатель, ";
                                }
                                else if (formValues.CheckGet("CUSTOMER_ID").ToInt() == 0)
                                {
                                    CntnCustomerId.Text = rec["CUSTOMER_ID"].ToInt().ToString();
                                }
                            }
                            if (rec["PRODUCT_TYPE_ID"].ToInt() != 0)
                            {
                                if (formValues.CheckGet("PRODUCT_TYPE_ID").ToInt() != 0 && rec["PRODUCT_TYPE_ID"].ToInt() != formValues.CheckGet("PRODUCT_TYPE_ID").ToInt())
                                {
                                    error = error + "тип продукции, ";
                                }
                                else if (formValues.CheckGet("PRODUCT_TYPE_ID").ToInt() == 0)
                                {
                                    ProductType.SetSelectedItemByKey(rec["PRODUCT_TYPE_ID"].ToInt().ToString());
                                }
                            }
                            if (rec["CONTNR_COLOR_ID"].ToInt() != 0)
                            {
                                if (formValues.CheckGet("CONTNR_COLOR_ID").ToInt() != 0 && rec["CONTNR_COLOR_ID"].ToInt() != formValues.CheckGet("CONTNR_COLOR_ID").ToInt())
                                {
                                    error = error + "цвет продукции, ";
                                }
                                else if (formValues.CheckGet("CONTNR_COLOR_ID").ToInt() == 0)
                                {
                                    ProductColor.SetSelectedItemByKey(rec["CONTNR_COLOR_ID"].ToInt().ToString());
                                }
                            }
                            if (!rec["PRODUCT_NAME"].IsNullOrEmpty())
                            {
                                if (formValues.CheckGet("PRODUCT_NAME").IsNullOrEmpty())
                                {
                                    ProductName.Text = rec["PRODUCT_NAME"];
                                }
                                else
                                {
                                    error = error + "наименование, ";
                                }
                            }
                            if (rec["PROD_SCHEME_ID"].ToInt() != 0)
                            {
                                if (formValues.CheckGet("PROD_SCHEME_ID").ToInt() != 0 && rec["PROD_SCHEME_ID"].ToInt() != formValues.CheckGet("PROD_SCHEME_ID").ToInt())
                                {
                                    error = error + "схема производства, ";
                                }
                                else if (formValues.CheckGet("PROD_SCHEME_ID").ToInt() == 0)
                                {
                                    ProductionScheme.SetSelectedItemByKey(rec["PROD_SCHEME_ID"].ToInt().ToString());
                                }
                            }
                            if (rec["PALLET_ID"].ToInt() != 0)
                            {
                                if (formValues.CheckGet("PALLET_ID").ToInt() != 0 && rec["PALLET_ID"].ToInt() != formValues.CheckGet("PALLET_ID").ToInt())
                                {
                                    error = error + "поддон, ";
                                }
                                else if (formValues.CheckGet("PALLET_ID").ToInt() == 0)
                                {
                                    Pallet.SetSelectedItemByKey(rec["PALLET_ID"].ToInt().ToString());
                                }
                            }
                            if (rec["LAYING_SCHEME_ID"].ToInt() != 0)
                            {
                                if (formValues.CheckGet("LAYING_SCHEME_ID").ToInt() != 0 && rec["LAYING_SCHEME_ID"].ToInt() != formValues.CheckGet("LAYING_SCHEME_ID").ToInt())
                                {
                                    error = error + "схема укладки, ";
                                }
                                else if (formValues.CheckGet("LAYING_SCHEME_ID").ToInt() == 0)
                                {
                                    LayingScheme.SetSelectedItemByKey(rec["LAYING_SCHEME_ID"].ToInt().ToString());
                                }
                                LayingSchemeCanged = true;
                            }
                            if (rec["PER_STACK_QTY"].ToInt() != 0)
                            {
                                if (formValues.CheckGet("PER_STACK_QTY").ToInt() != 0 && rec["PER_STACK_QTY"].ToInt() != formValues.CheckGet("PER_STACK_QTY").ToInt())
                                {
                                    error = error + "кол-во в пачке, ";
                                }
                                else if (formValues.CheckGet("PER_STACK_QTY").ToInt() == 0)
                                {
                                    PerStackQty.Text = rec["PER_STACK_QTY"].ToInt().ToString();
                                }
                            }
                            if (rec["STACK_IN_ROW_QTY"].ToInt() != 0)
                            {
                                if (formValues.CheckGet("STACK_IN_ROW_QTY").ToInt() != 0 && rec["STACK_IN_ROW_QTY"].ToInt() != formValues.CheckGet("STACK_IN_ROW_QTY").ToInt())
                                {
                                    error = error + "кол-во пачек в ряду, ";
                                }
                                else if (formValues.CheckGet("STACK_IN_ROW_QTY").ToInt() == 0)
                                {
                                    StackInRowQty.Text = rec["STACK_IN_ROW_QTY"].ToInt().ToString();
                                }
                            }
                            if (rec["ROW_IN_PALLET_QTY"].ToInt() != 0)
                            {
                                if (formValues.CheckGet("ROW_IN_PALLET_QTY").ToInt() != 0 && rec["ROW_IN_PALLET_QTY"].ToInt() != formValues.CheckGet("ROW_IN_PALLET_QTY").ToInt())
                                {
                                    error = error + "кол-во рядов на паллете, ";
                                }
                                else if (formValues.CheckGet("ROW_IN_PALLET_QTY").ToInt() == 0)
                                {
                                    RowInPalletQty.Text = rec["ROW_IN_PALLET_QTY"].ToInt().ToString();
                                }
                            }
                            if (rec["PER_PALLET_QTY"].ToInt() != 0)
                            {
                                if (formValues.CheckGet("PER_PALLET_QTY").ToInt() != 0 && rec["PER_PALLET_QTY"].ToInt() != formValues.CheckGet("PER_PALLET_QTY").ToInt())
                                {
                                    error = error + "кол-во на паллете, ";
                                }
                                else if (formValues.CheckGet("PER_PALLET_QTY").ToInt() == 0)
                                {
                                    PerPalletQty.Text = rec["PER_PALLET_QTY"].ToInt().ToString();
                                }
                            }
                            if (rec["PACKAGING_FLAG"].ToBool() != formValues.CheckGet("PACKAGING_FLAG").ToBool())
                            {
                                PackagingFlag.IsChecked = true;
                                error = error + "упаковка паллеты, ";
                            }
                            if (rec["SHEET_LAYER_TYPE_ID"].ToInt() != 0)
                            {
                                if (formValues.CheckGet("SHEET_LAYER_TYPE_ID").ToInt() != 0 && rec["SHEET_LAYER_TYPE_ID"].ToInt() != formValues.CheckGet("SHEET_LAYER_TYPE_ID").ToInt())
                                {
                                    error = error + "перестил, ";
                                }
                                else if (formValues.CheckGet("SHEET_LAYER_TYPE_ID").ToInt() == 0)
                                {
                                    SheetLayerType.SetSelectedItemByKey(rec["SHEET_LAYER_TYPE_ID"].ToInt().ToString());
                                }
                            }
                            if (rec["BAG_FLAG"].ToBool() != formValues.CheckGet("BAG_FLAG").ToBool())
                            {
                                BagFlag.IsChecked = formValues.CheckGet("BAG_FLAG").ToBool();
                                error = error + "упаковка в пакет, ";
                            }
                            if (rec["CORNERS_FLAG"].ToBool() != formValues.CheckGet("CORNERS_FLAG").ToBool())
                            {
                                CornersFlag.IsChecked = formValues.CheckGet("CORNERS_FLAG").ToBool();
                                error = error + "уголки, ";
                            }
                            if (rec["WRAP_STRETCH_FLAG"].ToBool() != formValues.CheckGet("WRAP_STRETCH_FLAG").ToBool())
                            {
                                WrapStretchFlag.IsChecked = formValues.CheckGet("WRAP_STRETCH_FLAG").ToBool();
                                error = error + "обмотка пленкой, ";
                            }
                            if (rec["PALLET_LENGTH"].ToInt() != 0)
                            {
                                if (formValues.CheckGet("PALLET_LENGTH").ToInt() != 0 && rec["PALLET_LENGTH"].ToInt() != formValues.CheckGet("PALLET_LENGTH").ToInt())
                                {
                                    error = error + "длина паллета, ";
                                }
                                else if (formValues.CheckGet("PALLET_LENGTH").ToInt() == 0)
                                {
                                    PalletLength.Text = rec["PALLET_LENGTH"].ToInt().ToString();
                                }
                            }
                            if (rec["PALLET_WIDTH"].ToInt() != 0)
                            {
                                if (formValues.CheckGet("PALLET_WIDTH").ToInt() != 0 && rec["PALLET_WIDTH"].ToInt() != formValues.CheckGet("PALLET_WIDTH").ToInt())
                                {
                                    error = error + "ширина паллета, ";
                                }
                                else if (formValues.CheckGet("PALLET_WIDTH").ToInt() == 0)
                                {
                                    PalletWidth.Text = rec["PALLET_WIDTH"].ToInt().ToString();
                                }
                            }
                            if (rec["PALLET_HEIGHT"].ToInt() != 0)
                            {
                                if (formValues.CheckGet("PALLET_HEIGHT").ToInt() != 0 && rec["PALLET_HEIGHT"].ToInt() != formValues.CheckGet("PALLET_HEIGHT").ToInt())
                                {
                                    error = error + "высота паллета, ";
                                }
                                else if (formValues.CheckGet("PALLET_HEIGHT").ToInt() == 0)
                                {
                                    PalletHeight.Text = rec["PALLET_HEIGHT"].ToInt().ToString();
                                }
                            }

                            if (result.ContainsKey("PRINT"))
                            {
                                ContainerColorDS = ListDataSet.Create(result, "PRINT");

                                if (CopyMode)
                                {
                                    if (ContainerColorDS.Items.Count > 0)
                                    {
                                        foreach (var row in ContainerColorDS.Items)
                                        {
                                            row.CheckAdd("ID", "0");
                                            row.CheckAdd("PRINTING_AREA", "0");
                                            row.CheckAdd("CHANGED", "1");
                                        }
                                    }
                                }
                                PrintGrid.LoadItems();

                            }

                            if (error.Length > 2)
                            {
                                error = error.Substring(0, error.Length - 2);
                                error = "Найдено несоответствие в полях: " + error + ".\r";
                                error = error + "Будут применены текущие параметры.";

                                var dw = new DialogWindow(error, "Привязка техкарты", "");
                                dw.ShowDialog();
                            }
                        }

                    }


                    ProcessPackaging();
                    SetFieldsAvailable();

                    InitializedFlag = true;
                }
            }
        }

        /// <summary>
        /// Обработка данных об областях печати
        /// </summary>
        private void ProcessPrintingSpot()
        {
            if (PrintingSpot.Items != null)
            {
                if (PrintingSpot.Items.Count > 0)
                {
                    foreach (var item in PrintingSpot.Items)
                    {
                        string fullName = item.CheckGet("NAME");
                        int l = fullName.Length;
                        string spotName = "";
                        string numInSpot = "";
                        if (l > 0)
                        {
                            spotName = fullName.Substring(0, l - 2);
                            numInSpot = fullName.Substring(l - 1, 1);
                        }
                        item.CheckAdd("SPOT_NAME", spotName);
                        item.CheckAdd("NUM_IN_SPOT", numInSpot);

                        if (!SpotList.ContainsKey(spotName))
                        {
                            SpotList.Add(spotName, spotName);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Вызов формы добавления и изменения техкарты литой тары
        /// </summary>
        /// <param name="techCardId">идентификатор техкарты</param>
        public void Edit(int techCardId = 0, bool copyMode = false, int copiedId = 0)
        {
            TechCardId = techCardId;
            CopyMode = copyMode;
            ControlName = $"MoldedContainerTechCard_{TechCardId}";
            if (CopyMode)
            {
                ControlName = $"{ControlName}_copy";
                if (copiedId > 0)
                {
                    CopiedId = copiedId;
                }
            }
            GetData();

        }

        /// <summary>
        /// Добавление нового цвета печати
        /// </summary>
        private void CreateColor()
        {
            var v = new Dictionary<string, string>()
            {
                { "TECHCARD_ID", TechCardId.ToString() },
                { "ID", "0" },
                { "SPOT_NAME", "" },
                { "COLOR_ID", "0" },
                { "ORDER_NUM", "0" },
            };
            ShowColorForm(v);
        }

        /// <summary>
        /// Редактирование цвета печати
        /// </summary>
        private void EditColor()
        {
            var v = PrintGrid.SelectedItem;
            v.CheckAdd("TECHCARD_ID", TechCardId.ToString());
            string gridSpotName = v.CheckGet("PRINTING_SPOT");
            v.CheckAdd("SPOT_NAME", GetAreaName(gridSpotName));
            ShowColorForm(v);
        }

        /// <summary>
        /// Вызов формы редактирования места и цвета печати
        /// </summary>
        /// <param name="v"></param>
        private void ShowColorForm(Dictionary<string, string> v)
        {
            var editColorForm = new MoldedContainerTechCardColor();
            editColorForm.PrintingColorDS = PrintingColor;
            editColorForm.SpotList = SpotList;
            editColorForm.ReceiverName = ControlName;
            editColorForm.Edit(v);
        }

        /// <summary>
        /// Добавление в таблицу печати нового цвета
        /// </summary>
        /// <param name="values"></param>
        private void InsertColor(Dictionary<string, string> values)
        {
            int orderNum = values.CheckGet("ORDER_NUM").ToInt();
            string colorName = values.CheckGet("COLOR_NAME");
            // Находим место, куда надо вставить печать
            string spotName = values.CheckGet("SPOT_NAME");
            bool found = false;
            bool resume = true;
            int lastNum = 0;
            var newItem = new Dictionary<string, string>();

            // Если в таблице цветов печати есть записи, ищем выбранную область
            if (ContainerColorDS.Items != null)
            {
                foreach (var item in ContainerColorDS.Items)
                {
                    string gridSpotName = item.CheckGet("PRINTING_SPOT");
                    string shortGridSpotName = GetAreaName(gridSpotName);
                    // Если омена области совпадают, заодно проверим, что в пределах этой области нет такой краски
                    if (spotName == shortGridSpotName)
                    {
                        found = true;
                        lastNum = item.CheckGet("ORDER_NUM").ToInt();
                        if (item.CheckGet("COLOR_NAME") == colorName)
                        {
                            Form.SetStatus($"В области печати {spotName} уже используется {colorName}", 1);
                            resume = false;
                            break;
                        }
                    }
                    else
                    {
                        if (found)
                        {
                            break;
                        }
                    }
                }
            }

            string spotFullName = "";
            string spotId = "";

            // Ищем нужное место печати в справочнике мест печати
            if (resume)
            {
                foreach (var row in PrintingSpot.Items)
                {
                    // lastNum == 0 - эта область еще не заполнена. Ищем первое вхождение в список областей печати
                    if (lastNum == 0)
                    {
                        if (row.CheckGet("SPOT_NAME") == spotName)
                        {
                            lastNum = row.CheckGet("ORDER_NUM").ToInt();
                            spotFullName = row.CheckGet("NAME");
                            spotId = row.CheckGet("ID");
                            break;
                        }
                    }
                    else
                    {
                        // Проверяем, что следующая запись относится к той же области
                        if (row.CheckGet("ORDER_NUM").ToInt() == lastNum + 1)
                        {
                            if (row.CheckGet("SPOT_NAME") != spotName)
                            {
                                // Перешли к другой области печати, все доступные цвета заняты
                                Form.SetStatus($"К области печати {spotName} нельзя добавить цвет", 1);
                                resume = false;
                            }
                            else
                            {
                                spotFullName = row.CheckGet("NAME");
                                spotId = row.CheckGet("ID");
                                lastNum += 1;
                            }
                            break;
                        }
                    }
                }
            }

            // Переформируем таблицу, вставим новый цвет
            if (resume)
            {
                newItem.Add("ID", "0");
                newItem.Add("ORDER_NUM", (lastNum).ToString());
                newItem.Add("PRINTING_SPOT", spotFullName);
                newItem.Add("COLOR_NAME", colorName);
                newItem.Add("COLOR_HEX", values.CheckGet("HEX"));
                newItem.Add("SPOT_ID", spotId);
                newItem.Add("COLOR_ID", values.CheckGet("COLOR_ID"));
                newItem.Add("CHANGED", "1");
                newItem.Add("PRINTING_AREA", "");

                bool included = false;
                var list = new List<Dictionary<string, string>>();
                int i = 1;
                if (ContainerColorDS.Items != null)
                {
                    foreach (var item in ContainerColorDS.Items)
                    {
                        if (!included)
                        {
                            if (item.CheckGet("ORDER_NUM").ToInt() > lastNum)
                            {
                                newItem.Add("_ROWNUMBER", i.ToString());
                                list.Add(newItem);
                                included = true;
                                i++;
                            }
                        }
                        item["_ROWNUMBER"] = i.ToString();
                        list.Add(item);
                        i++;
                    }
                }

                // Не нашли, куда вставить в таблицу, добавляем в конец
                if (!included)
                {
                    newItem.Add("_ROWNUMBER", i.ToString());
                    list.Add(newItem);
                }

                ContainerColorDS = ListDataSet.Create(list);
                PrintGrid.LoadItems();
            }
        }

        /// <summary>
        /// Изменение цвета печати
        /// </summary>
        /// <param name="values"></param>
        private void UpdateColor(Dictionary<string, string> values)
        {
            bool resume = true;
            int orderNum = values.CheckGet("ORDER_NUM").ToInt();
            string colorName = values.CheckGet("COLOR_NAME");
            string spotName = values.CheckGet("SPOT_NAME");
            // Проходим по таблице, проверяем, что в этой области такого цвета нет
            foreach (var item in ContainerColorDS.Items)
            {
                string gridSpotName = item.CheckGet("PRINTING_SPOT");
                string shortGridSpotName = GetAreaName(gridSpotName);
                if (spotName == shortGridSpotName)
                {
                    if (item.CheckGet("COLOR_NAME") == colorName)
                    {
                        Form.SetStatus($"В области печати {spotName} уже используется {colorName}", 1);
                        resume = false;
                        break;
                    }

                }
            }

            if (resume)
            {
                var list = ContainerColorDS.Items;
                foreach (var item in list)
                {
                    if (item.CheckGet("ORDER_NUM").ToInt() == orderNum)
                    {
                        item["COLOR_NAME"] = colorName;
                        item["COLOR_ID"] = values.CheckGet("COLOR_ID");
                        item["COLOR_HEX"] = values.CheckGet("HEX");
                        item["CHANGED"] = "1";
                        break;
                    }
                }

                ContainerColorDS = ListDataSet.Create(list);
                PrintGrid.LoadItems();
            }
        }

        /// <summary>
        /// Удаление цвета из таблицы
        /// </summary>
        private void DeleteColor()
        {
            var v = PrintGrid.SelectedItem;
            int deletedId = v.CheckGet("ID").ToInt();
            if (deletedId > 0)
            {
                DeletedPrintingColor.Add(deletedId);
            }

            int deletedSpotNum = v.CheckGet("ORDER_NUM").ToInt();
            string spotName = "";
            var spotOrder = new Dictionary<string, Dictionary<string, string>>();
            foreach (var row in PrintingSpot.Items)
            {
                int orderNum = row.CheckGet("ORDER_NUM").ToInt();
                if (orderNum == deletedSpotNum)
                {
                    spotName = row.CheckGet("SPOT_NAME");
                }
                spotOrder.Add(orderNum.ToString(), row);
            }

            int i = 1;
            bool found = false;
            var list = new List<Dictionary<string, string>>();
            foreach (var item in ContainerColorDS.Items)
            {
                string colorSpotName = GetAreaName(item.CheckGet("PRINTING_SPOT"));
                int itemOrderNum = item.CheckGet("ORDER_NUM").ToInt();
                // Если нашли удаляемую строку, пропускаем
                if (itemOrderNum == deletedSpotNum)
                {
                    continue;
                }
                if (itemOrderNum > deletedSpotNum)
                {
                    // 
                    if (colorSpotName == spotName)
                    {
                        itemOrderNum--;
                        string newOrderNum = (itemOrderNum).ToString();
                        if (spotOrder.ContainsKey(newOrderNum))
                        {
                            var row = spotOrder[newOrderNum];
                            item.CheckAdd("ORDER_NUM", newOrderNum);
                            item.CheckAdd("PRINTING_SPOT", row.CheckGet("NAME"));
                            item.CheckAdd("SPOT_ID", row.CheckGet("ID"));
                            item["CHANGED"] = "1";
                        }
                    }
                }

                item["_ROWNUMBER"] = i.ToString();
                list.Add(item);
                i++;
            }

            ContainerColorDS = ListDataSet.Create(list);
            PrintGrid.LoadItems();
        }

        /// <summary>
        /// Перемещение цвета в таблице в пределах области печати
        /// </summary>
        /// <param name="move">направление: 1 - вниз, -1 - вверх</param>
        private void SwapColors(int move)
        {
            bool resume = true;

            int idx = PrintGrid.Items.IndexOf(PrintGrid.SelectedItem);
            int cnt = PrintGrid.Items.Count;

            //Первую запись нельзя сдвинуть вверх, последнюю запись нельзя сдвинуть вниз
            if ((idx == 0 && move == -1) || (idx + 1 == cnt && move == 1))
            {
                Form.SetStatus("Нельзя смещать за пределы таблицы", 1);
                resume = false;
            }

            //Первый цвет области нельзя сдвинуть вверх
            if (resume)
            {
                if (move == -1)
                {
                    string spotName = PrintGrid.SelectedItem.CheckGet("PRINTING_SPOT");
                    int lng = spotName.Length;
                    int spotNum = spotName[lng - 1].ToInt();
                    if (spotNum == 1)
                    {
                        Form.SetStatus("Нельзя смещать за пределы области печати", 1);
                        resume = false;
                    }
                }
            }

            // Последний цвет области нельзя сдвинуть вниз
            if (resume)
            {
                if (move == 1)
                {
                    var row = PrintGrid.Items[idx + 1];
                    string spotName = row.CheckGet("PRINTING_SPOT");
                    int lng = spotName.Length;
                    int spotNum = spotName[lng - 1].ToInt();
                    if (spotNum == 1)
                    {
                        Form.SetStatus("Нельзя смещать за пределы области печати", 1);
                        resume = false;
                    }
                }
            }

            if (resume)
            {
                int newIdx = idx + move;
                string cName1 = ContainerColorDS.Items[idx].CheckGet("COLOR_NAME");
                string csHex1 = ContainerColorDS.Items[idx].CheckGet("COLOR_HEX");
                string cId1 = ContainerColorDS.Items[idx].CheckGet("COLOR_ID");

                string cName2 = ContainerColorDS.Items[newIdx].CheckGet("COLOR_NAME");
                string csHex2 = ContainerColorDS.Items[newIdx].CheckGet("COLOR_HEX");
                string cId2 = ContainerColorDS.Items[newIdx].CheckGet("COLOR_ID");

                //Обмен
                ContainerColorDS.Items[idx].CheckAdd("COLOR_NAME", cName2);
                ContainerColorDS.Items[newIdx].CheckAdd("COLOR_NAME", cName1);

                ContainerColorDS.Items[idx].CheckAdd("COLOR_HEX", csHex2);
                ContainerColorDS.Items[newIdx].CheckAdd("COLOR_HEX", csHex1);

                ContainerColorDS.Items[idx].CheckAdd("COLOR_ID", cId2);
                ContainerColorDS.Items[newIdx].CheckAdd("COLOR_ID", cId1);

                ContainerColorDS.Items[idx].CheckAdd("CHANGED", "1");
                ContainerColorDS.Items[newIdx].CheckAdd("CHANGED", "1");

                PrintGrid.LoadItems();
            }
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void Show()
        {
            Central.WM.Show(ControlName, $"Техкарта тары {TechCardId}", true, "add", this);
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            Central.WM.SetActive(ReceiverName);
        }

        /// <summary>
        /// Функция перевода строки содержащей hex код цвета краски в цвет Brush
        /// <param name="hex_code">строка с hex числом</param>
        /// <return>Brush.цвет</return>
        /// </summary>
        private Brush HexToBrush(string hex_code)
        {
            SolidColorBrush result = null;
            var hexString = (hex_code as string).Replace("#", "");

            if (hexString.Length == 6)
            {
                var r = hexString.Substring(0, 2);
                var g = hexString.Substring(2, 2);
                var b = hexString.Substring(4, 2);

                result = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xff,
                   byte.Parse(r, System.Globalization.NumberStyles.HexNumber),
                   byte.Parse(g, System.Globalization.NumberStyles.HexNumber),
                   byte.Parse(b, System.Globalization.NumberStyles.HexNumber)));
            }

            return result;
        }

        /// <summary>
        /// Обработка полей упаковки
        /// </summary>
        private void ProcessPackaging()
        {
            bool packaging = (bool)PackagingFlag.IsChecked;

            SheetLayerType.IsEnabled = packaging;
            CornersFlag.IsEnabled = packaging;
            WrapStretchFlag.IsEnabled = packaging;
            PalletLength.IsEnabled = packaging;
            PalletWidth.IsEnabled = packaging;
            PalletHeight.IsEnabled = packaging;

            if (!packaging)
            {
                PalletLength.Text = "";
                PalletWidth.Text = "";
                PalletHeight.Text = "";
            }
        }

        /// <summary>
        /// Проверки перед записью в БД
        /// </summary>
        public void Save(int closeAfterSave = 0)
        {
            var v = Form.GetValues();
            bool resume = true;
            string errorMsg = "";

            if (Form.Validate())
            {
                // Если контейнер с печатью, должен быть хотя бы один цвет
                int containerType = ProductType.SelectedItem.Key.ToInt();
                if (containerType == 2 || containerType == 4)
                {
                    if (PrintGrid.Items == null)
                    {
                        errorMsg = "Для печати должен быть заполнен хотя бы один цвет";
                        resume = false;
                    }
                    else if (PrintGrid.Items.Count == 0)
                    {
                        errorMsg = "Для печати должен быть заполнен хотя бы один цвет";
                        resume = false;
                    }
                }

                if (resume)
                {
                    var printData = "";
                    var deletedData = "";
                    v.CheckAdd("ID", TechCardId.ToString());
                    if (PrintGrid.Items != null)
                    {
                        printData = JsonConvert.SerializeObject(PrintGrid.Items);
                    }
                    v.CheckAdd("PRINT_DATA", printData);
                    if (DeletedPrintingColor.Count > 0)
                    {
                        deletedData = JsonConvert.SerializeObject(DeletedPrintingColor);
                    }
                    v.CheckAdd("DELETED_DATA", deletedData);
                }
            }
            else
            {
                errorMsg = "Не все поля заполнены верно";
                resume = false;
            }

            if (resume)
            {
                // Если снят флаг упаковки, снимаем все связанные флаги
                if (!v.CheckGet("PACKAGING_FLAG").ToBool())
                {
                    v.CheckAdd("CORNERS_FLAG", "0");
                    v.CheckAdd("WRAP_STRETCH_FLAG", "0");
                    v.CheckAdd("SHEET_PARTITION_FLAG", "0");
                }

                // Путь к файлу, который изменяем
                v.CheckAdd("FILE_PATH", FilePath);

                v.CheckAdd("CLOSE_AFTER_SAVE", closeAfterSave.ToString());
                v.CheckAdd("STATUS_ID", StatusId.ToString());
                SaveData(v);
            }
            else
            {
                Form.SetStatus(errorMsg, 1);
            }


        }

        /// <summary>
        /// Запись данных в БД
        /// </summary>
        /// <param name="p"></param>
        public async void SaveData(Dictionary<string, string> p)
        {
            DisableControls();
            // Добавляем поля размера изделия из типа контейнера
            int productTypeId = ProductType.SelectedItem.Key.ToInt();
            foreach (var t in ProductTypeDS.Items)
            {
                int typeId = t.CheckGet("ID").ToInt();
                if (typeId == productTypeId)
                {
                    p.CheckAdd("PRODUCT_TYPE", t.CheckGet("TYPE_NAME"));
                    p.CheckAdd("CONTAINER_LENGTH", t.CheckGet("CONTAINER_LENGTH"));
                    p.CheckAdd("CONTAINER_WIDTH", t.CheckGet("CONTAINER_WIDTH"));
                    p.CheckAdd("CONTAINER_HEIGHT", t.CheckGet("CONTAINER_HEIGHT"));
                }
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "MoldedContainer");
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
                    if (result.ContainsKey("ITEM"))
                    {
                        var ds = ListDataSet.Create(result, "ITEM");
                        if (TechCardId == 0)
                        {
                            // Если форму после сохранения закрываем, присваиваем актуальный идентификатор
                            var id = ds.Items[0].CheckGet("ID").ToInt();
                            if (id > 0)
                            {
                                TechCardId = id;
                            }
                        }
                        bool closeAfterSave = p.CheckGet("CLOSE_AFTER_SAVE").ToBool();
                        if (closeAfterSave)
                        {
                            Close();
                        }
                        // Если форму после сохранения не закрываем
                        else
                        {

                            StatusId = ds.Items[0].CheckGet("STATUS_ID").ToInt();

                            if (!string.IsNullOrEmpty(ds.Items[0].CheckGet("FILE_NAME")))
                            {
                                var fileName = ds.Items[0].CheckGet("FILE_NAME");
                                var folderName = ds.Items[0].CheckGet("FOLDER_NAME");
                                FilePath = Path.Combine(folderName, fileName);

                                //SetFieldsAvailable();
                            }
                            if (!string.IsNullOrEmpty(ds.Items[0].CheckGet("STICKER_NAME")))
                            {
                                StickerName.Text = ds.Items[0].CheckGet("STICKER_NAME");
                            }
                        }

                        // Обновляем таблицу с цветами печати
                        if (result.ContainsKey("PRINT"))
                        {
                            ContainerColorDS = ListDataSet.Create(result, "PRINT");
                            PrintGrid.LoadItems();
                        }
                        SetFieldsAvailable();
                    }
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "MoldedContainerTechCard",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "Refresh",
                        Message = TechCardId.ToString(),
                    });
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                Form.SetStatus(q.Answer.Error.Message, 1);
            }
            EnableControls();
        }

        /// <summary>
        /// Получение артикула
        /// </summary>
        private async void GetSkuCode()
        {
            bool resume = true;

            // Если техкарта с этикеткой, то этикетка должна быть привязана
            if ((bool)StickerFlag.IsChecked)
            {
                if (StickerId.Text.IsNullOrEmpty())
                {
                    Form.SetStatus("Не привязана этикетка", 1);
                    resume = false;
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "MoldedContainer");
                q.Request.SetParam("Action", "GetSkuCode");
                q.Request.SetParam("ID", TechCardId.ToString());
                q.Request.SetParam("CUSTOMER_ID", CntnCustomerId.Text);
                q.Request.SetParam("STICKER_ID", StickerId.Text);

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
                        var skuCodeDS = ListDataSet.Create(result, "ITEMS");
                        if (skuCodeDS.Items.Count > 0)
                        {
                            var skuCode = skuCodeDS.Items[0].CheckGet("SKU_CODE");
                            if (!skuCode.IsNullOrEmpty())
                            {
                                Article.Text = skuCode;
                                SkuLabel.TextDecorations = null;

                            }

                            string stickerName = skuCodeDS.Items[0].CheckGet("STICKER_NAME");
                            if (!stickerName.IsNullOrEmpty())
                            {
                                StickerName.Text = stickerName;
                            }

                            string newFilePath = skuCodeDS.Items[0].CheckGet("NEW_FILE_PATH");
                            if (!newFilePath.IsNullOrEmpty())
                            {
                                FilePath = newFilePath;
                            }
                        }
                        // Обновим состояния полей и кнопок
                        SetFieldsAvailable();
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "MoldedContainerTechCard",
                            ReceiverName = ReceiverName,
                            SenderName = ControlName,
                            Action = "Refresh",
                            Message = TechCardId.ToString(),
                        });
                    }
                }
                else
                {
                    Form.SetStatus(q.Answer.Error.Message, 1);
                }
            }

        }

        /// <summary>
        /// Заполнение полей потребителя при изменении покупателя
        /// </summary>
        private void SetCustomer()
        {
            if (CntnBuyer.SelectedItem.Key != null)
            {
                int buyerId = CntnBuyer.SelectedItem.Key.ToInt();
                foreach (var item in BuyerDS.Items)
                {
                    if (item.CheckGet("ID").ToInt() == buyerId)
                    {
                        CntnCustomerName.Text = item.CheckGet("CUSTOMER_NAME");
                        CntnCustomerId.Text = item.CheckGet("CUSTOMER_ID").ToInt().ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Добавление изделия в ассортимент
        /// </summary>
        private async void AcceptProduct()
        {
            var v = Form.GetValues();
            bool resume = true;
            string errorMsg = "";

            if (Form.Validate())
            {
                // Добавим значения выбранных строк из выпадающих списков
                v.CheckAdd("ID", TechCardId.ToString());
                v.CheckAdd("FILE_PATH", FilePath);
                v.CheckAdd("STATUS_ID", StatusId.ToString());

                // Добавляем поля из типа контейнера
                int productTypeId = ProductType.SelectedItem.Key.ToInt();
                foreach (var t in ProductTypeDS.Items)
                {
                    int typeId = t.CheckGet("ID").ToInt();
                    if (typeId == productTypeId)
                    {
                        v.CheckAdd("PRODUCT_TYPE", t.CheckGet("TYPE_NAME"));
                        v.CheckAdd("CONTAINER_LENGTH", t.CheckGet("CONTAINER_LENGTH"));
                        v.CheckAdd("CONTAINER_WIDTH", t.CheckGet("CONTAINER_WIDTH"));
                        v.CheckAdd("CONTAINER_HEIGHT", t.CheckGet("CONTAINER_HEIGHT"));
                    }
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "MoldedContainer");
                q.Request.SetParam("Action", "AcceptProduct");
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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds.Items.Count > 0)
                        {
                            var goodsName = ds.Items[0].CheckGet("GOODS_NAME");
                            GoodsName.Text = goodsName;
                            StatusId = ds.Items[0].CheckGet("STATUS_ID").ToInt();
                            AcceptButton.IsEnabled = false;
                        }

                        // Обновляем таблицу с цветами печати
                        if (result.ContainsKey("PRINT"))
                        {
                            ContainerColorDS = ListDataSet.Create(result, "PRINT");
                            PrintGrid.LoadItems();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Создание файла техкарты. Если стоит признак пересоздания, то старый файл удаляется и создаётся заново
        /// </summary>
        private async void CreateExcel(bool recreate = false)
        {
            bool resume = true;
            string errorMsg = "";

            if (Form.Validate())
            {
                var v = Form.GetValues();

                // Добавим значения выбранных строк из выпадающих списков
                v.CheckAdd("ID", TechCardId.ToString());
                v.CheckAdd("BUYER", CntnBuyer.SelectedItem.Value);
                v.CheckAdd("CONTNR_COLOR", ProductColor.SelectedItem.Value);
                v.CheckAdd("PALLET", Pallet.SelectedItem.Value);
                v.CheckAdd("LAYING_SCHEME", LayingScheme.SelectedItem.Value);
                v.CheckAdd("LAYING_SCHEME_CHANGED", LayingSchemeCanged ? "1" : "0");
                v.CheckAdd("PRODUCTION_SCHEME", ProductionScheme.SelectedItem.Value);
                v.CheckAdd("SHEET_LAYER_NAME", SheetLayerType.SelectedItem.Value);

                // Добавляем поля размера изделия из типа контейнера
                int productTypeId = ProductType.SelectedItem.Key.ToInt();
                foreach (var t in ProductTypeDS.Items)
                {
                    int typeId = t.CheckGet("ID").ToInt();
                    if (typeId == productTypeId)
                    {
                        v.CheckAdd("PRODUCT_TYPE", t.CheckGet("TYPE_NAME"));
                        v.CheckAdd("CONTAINER_LENGTH", t.CheckGet("CONTAINER_LENGTH"));
                        v.CheckAdd("CONTAINER_WIDTH", t.CheckGet("CONTAINER_WIDTH"));
                        v.CheckAdd("CONTAINER_HEIGHT", t.CheckGet("CONTAINER_HEIGHT"));
                    }
                }

                var printData = JsonConvert.SerializeObject(PrintGrid.Items);
                v.CheckAdd("PRINT_DATA", printData);

                // Признак пересоздания файла
                string recreateFlag = recreate ? "1" : "0";
                v.CheckAdd("RECREATE_FLAG", recreateFlag);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "MoldedContainer");
                q.Request.SetParam("Action", "ExcelDocumentCreate");
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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds.Items.Count > 0)
                        {
                            var fileName = ds.Items[0].CheckGet("FILE_NAME");
                            var folderName = ds.Items[0].CheckGet("FOLDER_NAME");
                            FilePath = Path.Combine(folderName, fileName);

                            var dw = new DialogWindow("Успешно создано", "Создание Excel", "");
                            dw.ShowDialog();
                        }
                        SetFieldsAvailable();
                    }
                }
                else if (q.Answer.Error.Code == 145)
                {
                    Form.SetStatus(q.Answer.Error.Message, 1);
                }
            }
        }

        /// <summary>
        /// Сохраняем измененные данные и переносим их в существующи файл техкарты
        /// </summary>
        private async void UpdateExcel()
        {
            bool resume = true;
            string errorMsg = "";

            if (StatusId > 4)
            {
                resume = false;
                var dw = new DialogWindow("Техкарта согласована клиентом!\nВы уверены, что нужно обновить данные в файле?", "Перенести данные в Excel", "", DialogWindowButtons.NoYes);
                if ((bool)dw.ShowDialog())
                {
                    if (dw.ResultButton == DialogResultButton.Yes)
                    {
                        resume = true;
                    }
                }
            }

            if (resume && Form.Validate())
            {
                var v = Form.GetValues();

                // Добавим значения выбранных строк из выпадающих списков
                v.CheckAdd("ID", TechCardId.ToString());
                v.CheckAdd("BUYER", CntnBuyer.SelectedItem.Value);
                v.CheckAdd("CONTNR_COLOR", ProductColor.SelectedItem.Value);
                v.CheckAdd("PALLET", Pallet.SelectedItem.Value);
                v.CheckAdd("LAYING_SCHEME", LayingScheme.SelectedItem.Value);
                v.CheckAdd("LAYING_SCHEME_CHANGED", LayingSchemeCanged ? "1" : "0");
                v.CheckAdd("PRODUCTION_SCHEME", ProductionScheme.SelectedItem.Value);
                v.CheckAdd("SHEET_LAYER_NAME", SheetLayerType.SelectedItem.Value);

                // Добавляем поля размера изделия из типа контейнера
                int productTypeId = ProductType.SelectedItem.Key.ToInt();
                foreach (var t in ProductTypeDS.Items)
                {
                    int typeId = t.CheckGet("ID").ToInt();
                    if (typeId == productTypeId)
                    {
                        v.CheckAdd("PRODUCT_TYPE", t.CheckGet("TYPE_NAME"));
                        v.CheckAdd("CONTAINER_LENGTH", t.CheckGet("CONTAINER_LENGTH"));
                        v.CheckAdd("CONTAINER_WIDTH", t.CheckGet("CONTAINER_WIDTH"));
                        v.CheckAdd("CONTAINER_HEIGHT", t.CheckGet("CONTAINER_HEIGHT"));
                    }
                }

                var printData = JsonConvert.SerializeObject(PrintGrid.Items);
                v.CheckAdd("PRINT_DATA", printData);

                // Путь к файлу, который изменяем
                v.CheckAdd("FILE_PATH", FilePath);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "MoldedContainer");
                q.Request.SetParam("Action", "ExcelDocumentUpdate");
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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds.Items.Count > 0)
                        {
                            //SetFieldsAvailable();
                            Central.OpenFile(FilePath);
                        }
                    }
                }
                else if (q.Answer.Error.Code == 145)
                {
                    Form.SetStatus(q.Answer.Error.Message, 1);
                }
            }

        }

        /// <summary>
        /// Установка признака архивная
        /// </summary>
        private async void SetArchive()
        {
            bool resume = false;
            string action = "отправить в архив";
            if (ArchivedFlag)
            {
                action = "достать из архива";
            }

            var dw = new DialogWindow($"Вы действительно хотите техкарту {action}?", "Архивация техкарты", "", DialogWindowButtons.NoYes);
            if ((bool)dw.ShowDialog())
            {
                if (dw.ResultButton == DialogResultButton.Yes)
                {
                    resume = true;
                }
            }

            if (resume)
            {
                string archivedFlag = ArchivedFlag ? "0" : "1";

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "MoldedContainer");
                q.Request.SetParam("Action", "SetArchived");
                q.Request.SetParam("ID", TechCardId.ToString());
                q.Request.SetParam("ARCHIVED_FLAG", archivedFlag);

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
                        var ds = ListDataSet.Create(result, "ITEM");
                        if (ds.Items.Count > 0)
                        {
                            ArchivedFlag = !ArchivedFlag;
                            // Обновляем грид
                            Central.Msg.SendMessage(new ItemMessage()
                            {
                                ReceiverGroup = "MoldedContainerTechCard",
                                ReceiverName = ReceiverName,
                                SenderName = ControlName,
                                Action = "Refresh",
                                Message = TechCardId.ToString(),
                            });

                            SetFieldsAvailable();
                        }
                    }
                }
                else if (q.Answer.Error.Code == 145)
                {
                    Form.SetStatus(q.Answer.Error.Message, 1);
                }
                // Сообщения об остатках этикеток выводим к окне для акцентирования 
                else if (q.Answer.Error.Code == 148)
                {
                    var dwm = new DialogWindow(q.Answer.Error.Message, "Архивация техкарты");
                    dwm.ShowDialog();
                }

            }
        }

        /// <summary>
        /// Настройка доступности полей
        /// </summary>
        private void SetFieldsAvailable()
        {
            bool editableStatus = StatusId.ContainsIn(1, 3, 4, 5);
            // Кнопка архивации
            ArchiveButton.IsEnabled = false;
            if (TechCardId > 0)
            {
                ArchiveButton.IsEnabled = true;
                if (ArchivedFlag)
                {
                    ArchiveButton.Content = "Из архива";
                }
                else
                {
                    ArchiveButton.Content = "В архив";
                }
            }

            // Если техкарта согласована клиентом, показываем кнопку присваивания артикула
            if (Article.Text.IsNullOrEmpty())
            {
                SkuLabel.TextDecorations = TextDecorations.Underline;
            }
            else
            {
                SkuLabel.TextDecorations = null;
            }

            // Если краски для печати или области печати не загружены, панель печати не разблокируем
            bool printingDataLoaded = (PrintingColor.Items.Count > 0) && (PrintingSpot.Items.Count > 0);
            if (printingDataLoaded)
            {
                Form.SetStatus("");
            }
            else
            {
                Form.SetStatus("Не загрузились данные для печати", 1);
            }

            // Блок печати и этикетки
            var s = ProductType.SelectedItem.Key.ToInt();

            // Если создан артикул, нельзя менять тип и цвет изделия, данные по печати
            if (Article.Text.IsNullOrEmpty())
            {
                switch (s)
                {
                    case 1:
                        PrintingFlag.IsChecked = false;
                        StickerFlag.IsChecked = false;
                        PrintToolbar.IsEnabled = false;
                        break;
                    case 2:
                        PrintingFlag.IsChecked = true;
                        StickerFlag.IsChecked = false;
                        if (printingDataLoaded)
                        {
                            PrintToolbar.IsEnabled = true;
                        }
                        else
                        {
                            PrintToolbar.IsEnabled = false;
                        }
                        break;
                    case 3:
                        PrintingFlag.IsChecked = false;
                        StickerFlag.IsChecked = true;
                        PrintToolbar.IsEnabled = false;
                        break;
                    case 4:
                        PrintingFlag.IsChecked = true;
                        StickerFlag.IsChecked = true;
                        if (printingDataLoaded)
                        {
                            PrintToolbar.IsEnabled = true;
                        }
                        else
                        {
                            PrintToolbar.IsEnabled = false;
                        }
                        break;
                }
            }
            else
            {
                ProductType.IsEnabled = false;
                ProductColor.IsEnabled = false;
                ProductionScheme.IsEnabled = false;
                PrintToolbar.IsEnabled = true;
                StickerSelectButton.IsEnabled = false;
                StickerClearButton.IsEnabled = false;
            }

            Commander.UpdateActions();
        }

        /// <summary>
        /// Вычисляет общее количество изделий на поддоне
        /// </summary>
        private void CalcTotalQuantity()
        {
            int perStackQty = PerStackQty.Text.ToInt();
            if (perStackQty == 0)
            {
                perStackQty = 1;
                PerStackQty.Text = "1";
            }

            int stackInRow = StackInRowQty.Text.ToInt();
            if (stackInRow == 0)
            {
                stackInRow = 1;
                StackInRowQty.Text = "1";
            }

            int rowQty = RowInPalletQty.Text.ToInt();
            if (rowQty == 0)
            {
                rowQty = 1;
                RowInPalletQty.Text = "1";
            }

            PerPalletQty.Text = (perStackQty * stackInRow * rowQty).ToString();
            PalletHeight.Text = (157 + rowQty * 247).ToString();
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
                Commander.ProcessCommand(t);
            }
        }

        private void ProductType_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetFieldsAvailable();
        }

        private void CntnBuyer_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetCustomer();
        }

        private void SkuLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Article.Text.IsNullOrEmpty() && TechCardId > 0)
            {
                GetSkuCode();
            }
        }

        private void QtyTextChanged(object sender, TextChangedEventArgs e)
        {
            CalcTotalQuantity();
        }

        private void PackagingFlag_Click(object sender, RoutedEventArgs e)
        {
            ProcessPackaging();
        }

        private void Pallet_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Если загрузка формы завершена, то при смене выбранного поддона перезаполняем поля значениями по умолчанию для этого поддона
            if (InitializedFlag)
            {
                int selected = Pallet.SelectedItem.Key.ToInt();
                SetDefaultsPalletSelection(selected);
            }
        }

        private void LayingScheme_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //Если загрузка завершена, то при изменении схемы укладки поднимаем флаг
            if (InitializedFlag)
            {
                LayingSchemeCanged = true;
            }
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }
    }
}
