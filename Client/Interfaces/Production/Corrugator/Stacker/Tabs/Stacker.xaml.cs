using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
using DevExpress.Xpf.Editors.Helpers;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Форма автоматической печати ярлыков на выходе гофроагрегата
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class Stacker : UserControl
    {
        public Stacker()
        {
            InitializeComponent();

            if (Central.Config.PrintPalletByStackerDataFlag > 0)
            {
                StackBufferStackPanel.Visibility = Visibility.Visible;
                PrintPalletByStackerDataFlag = true;
            }
            else
            {
                StackBufferStackPanel.Visibility = Visibility.Collapsed;
                PrintPalletByStackerDataFlag = false;
            }

            if (Central.Config.PrintPalletNumberByRange > 0)
            {
                PrintPalletNumberByRange = true;
            }

            var random = new Random(Thread.CurrentThread.ManagedThreadId);
            InterfaceId = random.Next(999999).ToString();

            LogTableName = "corrugator_label";
            LogPrimaryKey = "PRODUCTION_TASK_ID_ID2";
            Loaded += (object sender, RoutedEventArgs e) =>
            {
                FrameName = Central.WM.TabItems.FirstOrDefault(x => x.Value.Content == this).Key;
                Central.WM.SelectedTab = FrameName;
                SetFocus();
            };

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitTaskForm();
            InitTailForm();
            SetDefaults();

            if (CorrugatorMachineNumberTextBox != null)
            {
                CorrugatorMachineNumberTextBox.ToolTip = $"ГА 1 -- БХС1/Антон/Гофра-5&#xD;&#xA;ГА 2 -- БХС2/Шейла/Гофра-3&#xD;&#xA;ГА 3 -- Фосбер";
            }

            if (StackerNumberTextBox != null)
            {
                StackerNumberTextBox.ToolTip = $"Ст 1 -- Нижний/Первый стекер&#xD;&#xA;Ст 2 -- Верхний/Второй стекер";
            }

            RunAutoUpdateTimer();
            RunComplectationWarningTimer();

            ProcessPermissions();
        }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper TaskForm { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper TailForm { get; set; }

        /// <summary>
        /// Таймер получения данных по текущему производственному заданию
        /// </summary>
        public DispatcherTimer AutoUpdateTimer { get; set; }

        /// <summary>
        /// интервал обновления таймера (сек)
        /// </summary>
        public int AutoUpdateTimerInterval { get; set; }

        /// <summary>
        /// Таймер для предупреждения о поддонах в К0
        /// </summary>
        public DispatcherTimer ComplectationWarningTimer { get; set; }

        /// <summary>
        /// интервал обновления таймера для предупреждения о поддонах в К0 (сек)
        /// </summary>
        public int ComplectationWarningTimerInterval { get; set; }

        /// <summary>
        /// Идентификатор гофроагрегата (stanok.id_st).
        /// 1(2) -- БХС1;
        /// 2(21) -- БХС2;
        /// 3(22) -- Фосбер.
        /// 4(23) -- JS (ГА Кашира)
        /// </summary>
        public int CorrugatingMachineId { get; set; }

        /// <summary>
        /// Номер стекера (prihod_pz.cutoff_allocation).
        /// 1 -- Нижний стекер;
        /// 2 -- Верхний стекер.
        /// </summary>
        public int StackerNumber { get; set; }

        /// <summary>
        /// Флаг того, что запрос при сканировании ярлыка ещё работает
        /// </summary>
        public bool QueryInProgress { get; set; }

        /// <summary>
        /// Условный идентификатор созданного экземпляра класса Stacker
        /// </summary>
        public string InterfaceId { get; set; }

        /// <summary>
        /// Текущее производственное задание (proiz_zad.id_pz)
        /// </summary>
        public int CurrentProductionTaskId { get; set; }

        /// <summary>
        /// Продукция по текущему производственному заданию (tovar.id2)
        /// </summary>
        public int CurrentProductId { get; set; }

        /// <summary>
        /// Ид категории продукции по текущему производственному заданию (tovar.idk1)
        /// </summary>
        public int CurrentProductIdk1 { get; set; }

        /// <summary>
        /// Количество стоп на поддоне по текущему производсвтенному заданию 
        /// </summary>
        public int CurrentQuantityStackOnPallet { get; set; }

        /// <summary>
        /// Предыдущее производственное задание (proiz_zad.id_pz)
        /// </summary>
        public int LastProductionTaskId { get; set; }

        /// <summary>
        /// Продукция по предыдущему производственному заданию (tovar.id2)
        /// </summary>
        public int LastProductId { get; set; }

        /// <summary>
        /// Ид категории продукции по предыдущему производственному заданию (tovar.idk1)
        /// </summary>
        public int LastProductIdk1 { get; set; }

        /// <summary>
        /// Количество стоп на поддоне по предыдущему производственному заданию
        /// </summary>
        public int LastQuantityStackOnPallet { get; set; }

        /// <summary>
        /// Новое производственное задание (proiz_zad.id_pz)
        /// </summary>
        public int NewProductionTaskId { get; set; }

        /// <summary>
        /// Продукция по новому производственному заданию (tovar.id2)
        /// </summary>
        public int NewProductId { get; set; }

        /// <summary>
        /// Ид категории продукции по новому производственному заданию (tovar.idk1)
        /// </summary>
        public int NewProductIdk1 { get; set; }

        /// <summary>
        /// Количество стоп на поддоне по новому производственному заданию
        /// </summary>
        public int NewQuantityStackOnPallet { get; set; }

        /// <summary>
        /// Флаг того, что стекер может создавать ярлыки.
        /// Если флаг поднят, то при получении данных запусскается блок с логикой по обработке данных и формированию новых ярлыков.
        /// Если флаг опущен, то форма позволяет тольк просматривать текущую информацию по ПЗ и сканировать ярлыки.
        /// </summary>
        public int StackerWorkFlag { get; set; }

        /// <summary>
        /// Имя папки верхнего уровня, в которой хранятся лог файлы по работе стекера
        /// </summary>
        public string LogTableName { get; set; }

        /// <summary>
        /// Нименование первичного ключа лог файла по работе стекера
        /// </summary>
        public string LogPrimaryKey { get; set; }

        /// <summary>
        /// (Новый механизм)
        /// Буфер не использованных стоп по этому пз.
        /// Формат словаря стопы:
        /// PCSD_ID -- Ид съёма;
        /// PRCA_ID -- Ид стекера;
        /// ID_PZ -- Ид производственного задания;
        /// ID2 -- Ид продукции;
        /// DROP_CNT -- Номер съёма;
        /// DROP_ITEM_QTY -- Количество в съёме;
        /// STACK_QTY -- Количество в стопе;
        /// PROCESSED_FLAG -- Флаг обработки записи;
        /// DROP_MANUALLY_FLAG -- Флаг ручного съёма;
        /// STACK -- Количество стоп в съёме;
        /// STACK_NUMBER -- Номер этой стопы в этом съёме.
        /// </summary>
        public List<Dictionary<string, string>> StackBuffer { get; set; }

        /// <summary>
        /// (Новый механизм)
        /// Датасет с данными по съёмам на этом ГА.
        /// Содержит данные по съёмам на всех стекерах этого ГА (по CurrentProductionTaskData + NewProductionTaskDataList)
        /// </summary>
        public ListDataSet StackerDropDataSet { get; set; }

        /// <summary>
        /// (Старый механизм)
        /// Флаг того, что нужно распечатать последний ярлык для проехавшего производственного задания.
        /// Если флаг поднят, то вместо выполнения основной работы по печати ярлыков будет вызвана печать последнего ярлыка прошлого задания
        /// </summary>
        public int LastPalletFlag { get; set; }

        /// <summary>
        /// (Старый механизм)
        /// Флаг того, что начало текущего производственного задания было обработано (заполнено количество поддонов по заданию, созданы все ярлыки кроме последнего)
        /// </summary>
        public bool CurrentProductionTaskProcessedFlag { get; set; }

        /// <summary>
        /// (Старый механизм)
        /// Флаг ручного старта обработки производственного задания.
        /// Когда флаг поднят, задание начнёт обрабатываться, даже если оно не едет (его скорость = 0)
        /// </summary>
        public bool ManuallyStartFlag { get; set; }

        /// <summary>
        /// (Новый механизм)
        /// Флаг того, что ярлыки нужно печатать по новому механизму (поподдонная печать ярлыков по данным о съёмах стекера)
        /// </summary>
        public bool PrintPalletByStackerDataFlag { get; set; }

        /// <summary>
        /// (Старый механизм)
        /// Флаг того, что при создании поддонов номера поддонов пытаемся получить через диапазон свободных номеров.
        /// Если true -- Рассчитываем диапазон свободных номеров поддонов для раждого стекера, создаём поддоны с заранее определённым номером поддона;
        /// Если false -- Номер поддона до создания записи по поддону не известен, получаем следующий номер поддона при создании поддона.
        /// </summary>
        public bool PrintPalletNumberByRange { get; set; }

        /// <summary>
        /// Количество символов, которое может быть записано в текстовое поле лога работы стекера
        /// </summary>
        public int LogTextBoxMaxLength { get; set; }

        public string RoleName = "[erp]corrugator_stacker";

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
                    ManuallyStartButton.IsEnabled = false;

                    BarCodeTextBox.IsEnabled = false;
                    BarCodeTextBox.IsReadOnly = true;
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
        }

        /// <summary>
        /// Инициализация формы текущего задания
        /// </summary>
        public void InitTaskForm()
        {
            // Левый блок - текущее задание
            TaskForm = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="CURRENT_PRODUCTION_TASK_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CurrentTaskIdTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_PRODUCTION_TASK_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CurrentTaskNumTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_CREASE_COUNT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CurrentCreaseCountTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_BLANK_SIZE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CurrentBlankSizeTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_PALLET_SIZE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CurrentPalletSizeTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_PLACE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CurrentPlaceTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_DEMANDS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CurrentDemandsTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_QUANTITY_ON_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CurrentQtyOnPalletTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_QUANTITY_BY_TASK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CurrentQtyByTaskTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_STACK",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CurrentStackTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_COMMENT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CurrentCommentTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="CURRENT_LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CurrentTaskRestTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_TIME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CurrentTimeTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="CURRENT_BARCODE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=BarCodeTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="CORRUGATOR_MACHINE_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CorrugatorMachineNumberTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=StackerNumberTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACK_BUFFER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=StackBufferTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NEEDED_STACK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=NeededStackTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="CURRENT_IDK1",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_ID2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_MACHINE_SPEED",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_FILL_PRINTING_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_QUANTITY_REAM",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_QUANTITY_IN_REAM",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Doc="Количество продукции в стопе по умолчанию",
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_BLOCKED_LAST_LABEL_PRINT_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Doc="Флаг того, что для этого задания заблокирована печать последнего ярлыка",
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            TaskForm.SetFields(fields);
        }

        /// <summary>
        /// Инициализация формы для последнего ярлыка
        /// </summary>
        public void InitTailForm()
        {
            // Правый блок - поддон для сканирования
            TailForm = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="TAIL_PRODUCTION_TASK_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TailTaskNumTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TAIL_PRODUCTION_TASK_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TailTaskIdTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TAIL_PALLET_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PalletNumTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TAIL_CREASE_COUNT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TailCreaseCountTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TAIL_BLANK_SIZE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TailBlankSizeTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TAIL_PALLET_SIZE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TailPalletSizeTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TAIL_PLACE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TailPlaceTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TAIL_DEMANDS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TailDemandsTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TAIL_QUANTITY_ON_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TailQtyOnPalletTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TAIL_QUANTITY_BY_TASK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TailQtyByTaskTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TAIL_STACK",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TailStackTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TAIL_COMMENT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TailCommentTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="TAIL_IDK1",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TAIL_SUBPRODUCT_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            TailForm.SetFields(fields);
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            StackBuffer = new List<Dictionary<string, string>>();
            StackerDropDataSet = new ListDataSet();

            NewProductionTaskDataList = new List<ProductionTaskData>();
            NewProductionTaskData = new ProductionTaskData();
            LastProductionTaskData = new ProductionTaskData();
            CurrentProductionTaskData = new ProductionTaskData();

            if (TaskForm != null)
            {
                TaskForm.SetDefaults();
            }

            if (TailForm != null)
            {
                TailForm.SetDefaults();
            }

            // Если в конфиге указан станок, выбираем его
            if (Central.Config.CurrentMachineId > 0)
            {
                CorrugatingMachineId = Central.Config.CurrentMachineId;
            }
            // Если в конфиге указан стекер, то выбираем его
            if (Central.Config.CurentCutoffAllocation > 0)
            {
                StackerNumber = Central.Config.CurentCutoffAllocation;
            }

            AutoUpdateTimerInterval = 5;
            ComplectationWarningTimerInterval = 60;

            LogTextBoxMaxLength = 5000;
        }

        /// <summary>
        /// Получение данных для форм
        /// </summary>
        private async void GetData()
        {
            if (CorrugatingMachineId > 0 && StackerNumber > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Stacker");
                q.Request.SetParam("Action", "GetProductionTask");
                q.Request.SetParam("MACHINE_ID", CorrugatingMachineId.ToString());
                q.Request.SetParam("STACKER_NUM", StackerNumber.ToString());

                q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
                
                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null && result.Count > 0)
                    {
                        // Содержимое для полей формы текущего задания
                        var taskDS = ListDataSet.Create(result, "TASK");
                        if (taskDS.Items.Count > 0)
                        {
                            var item = taskDS.Items.First();
                            FillForm("CURRENT", item);
                        }
                        else
                        {
                            TaskForm.SetDefaults();
                        }

                        // Содержимое для полей формы последнего ярлыка
                        var tailDS = ListDataSet.Create(result, "TAIL");
                        if (tailDS.Items.Count > 0)
                        {
                            var item = tailDS.Items.First();
                            FillForm("TAIL", item);
                        }
                        else
                        {
                            TailForm.SetDefaults();
                        }

                        // Данные для центрального верхнего поля (оставшаяся длина и время)
                        var remainDS = ListDataSet.Create(result, "REMAIN_LENGTH");
                        int remainLength = 0;
                        if (remainDS.Items.Count > 0)
                        {
                            remainLength = remainDS.Items.First().CheckGet("REMAINT_LENGTH").ToInt();
                            TaskForm.SetValueByPath("CURRENT_LENGTH", remainLength.ToString());
                        }
                        TaskForm.SetValueByPath("CURRENT_TIME", DateTime.Now.ToString("HH:mm"));

                        if (taskDS != null && taskDS.Items != null && taskDS.Items.Count > 0)
                        {
                            if (taskDS.Items[0].CheckGet("PRODUCTION_TASK_NOTE").ToString() != null)
                            {
                                if (CurrentCommentBorder.ActualWidth > 0 && CurrentCommentBorder.ActualHeight > 0)
                                {
                                    CurrentCommentTextBox.FontSize = CommentGetFontSize(CurrentCommentTextBox.Text, CurrentCommentBorder.ActualWidth - 30, CurrentCommentBorder.ActualHeight - 30);
                                }
                            }
                        }

                        if (tailDS != null && tailDS.Items != null && tailDS.Items.Count > 0)
                        {
                            if (tailDS.Items[0].CheckGet("PRODUCTION_TASK_NOTE").ToString() != null)
                            {
                                if (TailCommentBorder.RenderSize.Width > 0 && TailCommentBorder.RenderSize.Height > 0)
                                {
                                    TailCommentTextBox.FontSize = CommentGetFontSize(TailCommentTextBox.Text, TailCommentBorder.RenderSize.Width - 30, TailCommentBorder.RenderSize.Height - 30);
                                }
                            }
                        }
                    }
                }
                else
                {
                    q.SilentErrorProcess = true;
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Получение данных по предупреждению К0
        /// </summary>
        public async void GetComplectationWarningData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Stacker");
            q.Request.SetParam("Action", "GetComplectationWarning");

            q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null && result.Count > 0)
                {
                    // Работаем со отображением значка К0
                    var complectationWarningDs = ListDataSet.Create(result, "COMPLECTATION_WARNING");
                    if (complectationWarningDs != null && complectationWarningDs.Items != null && complectationWarningDs.Items.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(complectationWarningDs.Items.First().CheckGet("HOUR")))
                        {
                            double minHour = complectationWarningDs.Items.First().CheckGet("HOUR").ToDouble();
                            if (minHour < 3 && minHour > 0)
                            {
                                // Красный
                                var color = "#ffee0000";
                                var bc = new BrushConverter();
                                var brush = (Brush)bc.ConvertFrom(color);
                                ComplectationWarningTextBox.Foreground = brush;
                                ComplectationWarningTextBox.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                // Зелёный
                                var color = "#cc289744";
                                var bc = new BrushConverter();
                                var brush = (Brush)bc.ConvertFrom(color);
                                ComplectationWarningTextBox.Foreground = brush;
                                ComplectationWarningTextBox.Visibility = Visibility.Visible;
                            }
                        }
                        else
                        {
                            ComplectationWarningTextBox.Visibility = Visibility.Hidden;
                        }
                    }
                }
            }
            else
            {
                q.SilentErrorProcess = true;
                q.ProcessError();
            }
        }

        /// <summary>
        /// Заполнение полей формы
        /// </summary>
        /// <param name="head">Ключевое слово заполняемой формы
        /// CURRENT -- Левая форма(текущее ПЗ);
        /// TAIL -- Правая форма(поддон для сканирования).
        /// </param>
        /// <param name="item"></param>
        private void FillForm(string head, Dictionary<string, string> item)
        {
            var formValues = new Dictionary<string, string>();
            formValues.CheckAdd($"{head}_PRODUCTION_TASK_ID", item.CheckGet("PRODUCTION_TASK_ID"));
            formValues.CheckAdd($"{head}_PRODUCTION_TASK_NUMBER", item.CheckGet("PRODUCTION_TASK_NUMBER"));
            formValues.CheckAdd($"{head}_PALLET_NUMBER", item.CheckGet("PALLET_NUMBER"));

            // габариты
            var lenght = item.CheckGet("LENGHT").ToInt();
            var width = item.CheckGet("WIDTH").ToInt();
            string blankSize = "";
            if (lenght > 0 && width > 0)
            {
                blankSize = $"{lenght}x{width}";
            }
            formValues.CheckAdd($"{head}_BLANK_SIZE", blankSize);

            // следующий станок
            string place = item.CheckGet("PLACE");
            if (string.IsNullOrEmpty(place))
            {
                place = $"СГП {item.CheckGet("OBVAZ")}";
            }
            formValues.CheckAdd($"{head}_PLACE", place);

            // Доп. требования
            if (item.CheckGet("IDK1").ToInt() == 5)
            {
                formValues.CheckAdd($"{head}_DEMANDS", item.CheckGet("DEMANDS"));
            }

            // поддон
            string palletSize = item.CheckGet("PALLET_NAME");
            place = place.Substring(0, 2);
            if ((place == "Rd") || (place == "P1") || (place == "КГ"))
            {
                palletSize = $"{palletSize} ^";
            }
            else if ((place == "Mn") || (place == "Md") || (place == "Rt"))
            {
                palletSize = $"{palletSize} c";
            }
            formValues.CheckAdd($"{head}_PALLET_SIZE", palletSize);

            // считаем рилевки
            int creaseCount = 0;
            if (item.CheckGet("SYMMETRIC").ToInt() > 0)
            {
                creaseCount = 2;
            }
            else
            {
                for (int i = 1; i <= 24; i++)
                {
                    if (item.CheckGet($"P{i}").ToInt() > 0)
                    {
                        creaseCount++;
                    }
                }
            }
            formValues.CheckAdd($"{head}_CREASE_COUNT", creaseCount > 0 ? creaseCount.ToString() : "Нет");

            // Собираем комментарии
            var comments = new List<string>();
            var s = item.CheckGet("PRODUCTION_TASK_NOTE");
            if (!s.IsNullOrEmpty())
            {
                comments.Add(s);
            }
            s = item.CheckGet("CORRUGATED_NOTE");
            if (!s.IsNullOrEmpty())
            {
                comments.Add(s);
            }
            s = item.CheckGet("PRODUCT_NOTE");
            if (!s.IsNullOrEmpty())
            {
                comments.Add(s);
            }
            string fullComment = string.Join(", ", comments);
            formValues.CheckAdd($"{head}_COMMENT", fullComment);
            // корректиуем информацию по рилёвкам
            if ((creaseCount > 0) && (fullComment.IndexOf("Сверьте рилевки с техкартой") > -1))
            {
                formValues.CheckAdd($"{head}_CREASE_COUNT", $"{formValues.CheckGet($"{head}_CREASE_COUNT")} ТК");
            }

            // Верхнее текстовое поле подкрашиваем в цвет картона
            string colorOuter = "";
            switch (item.CheckGet("COLOR_CODE").ToInt())
            {
                //целлюлоза, белый
                case 1:
                    //белый
                    colorOuter = "#ffffff";
                    break;

                //целлюлоза, бурый
                case 2:
                    //коричневый
                    colorOuter = "#804000";
                    break;

                //макулатура, бурый
                case 3:
                    //серый
                    colorOuter = "#808080";
                    break;

                //макулатура, крашеный
                case 4:
                    //оранжевый
                    colorOuter = "#ff8000";
                    break;

                default:
                    break;
            }

            formValues.CheckAdd($"{head}_IDK1", item.CheckGet("IDK1"));

            // расчитываем укладку
            var quantityReam = item.CheckGet("QUANTITY_REAM").ToInt();
            int quantityInReam = item.CheckGet("QUANTITY_IN_REAM").ToInt();
            var quantityManually = item.CheckGet("PRINTING_MANUALLY").ToInt();
            string quantityOnPallet = (quantityReam * quantityInReam).ToString();
            string quantityStack = $"{quantityReam}x{quantityInReam}";
            if (quantityManually > 0)
            {
                quantityOnPallet = quantityManually.ToString();
                quantityStack = $"{quantityReam}x{Math.Round((double)quantityManually / (double)quantityReam)}";
            }
            formValues.CheckAdd($"{head}_QUANTITY_IN_REAM", quantityInReam.ToString());
            formValues.CheckAdd($"{head}_STACK", quantityStack);
            formValues.CheckAdd($"{head}_QUANTITY_BY_TASK", item.CheckGet("QUANTITY_BY_TASK"));

            switch (head)
            {
                case "CURRENT":
                    formValues.CheckAdd($"{head}_QUANTITY_ON_PALLET", quantityOnPallet);
                    formValues.CheckAdd("CURRENT_ID2", item.CheckGet("ID2"));
                    formValues.CheckAdd("CURRENT_MACHINE_SPEED", item.CheckGet("MACHINE_SPEED"));
                    formValues.CheckAdd("CURRENT_FILL_PRINTING_FLAG", item.CheckGet("FILL_PRINTING_FLAG"));
                    formValues.CheckAdd("CURRENT_QUANTITY_REAM", item.CheckGet("QUANTITY_REAM"));

                    formValues.CheckAdd($"{head}_BLOCKED_LAST_LABEL_PRINT_FLAG", item.CheckGet("BLOCKED_LAST_LABEL_PRINT_FLAG"));

                    TaskForm.SetValues(formValues);

                    // Красим поля текущего задания в соответствии с полученными данными
                    {
                        // Верхнее текстовое поле подкрашиваем в цвет картона
                        CurrentCadboard.Background = colorOuter.ToBrush();
                        CurrentCreaseCountTextBox.Background = colorOuter.ToBrush();

                        // Если есть заливная печать, то красим поле Укладка в красный цвет
                        if (formValues.CheckGet("CURRENT_FILL_PRINTING_FLAG").ToInt() > 0)
                        {
                            // Красный
                            var color = "#ffee0000";
                            var bc = new BrushConverter();
                            var brush = (Brush)bc.ConvertFrom(color);
                            CurrentPalletSizeBorder.Background = brush;
                            CurrentPalletSizeTextBox.Background = brush;
                        }
                        else
                        {
                            // Светло-серый
                            var color = Colors.LightGray;
                            SolidColorBrush brush = new SolidColorBrush(color);
                            CurrentPalletSizeBorder.Background = brush;
                            CurrentPalletSizeTextBox.Background = brush;
                        }

                        // Проверяем, если текущаяя скорость на гофроагрегате больше 0
                        // зелёный цвет - задание выполняется
                        if (formValues.CheckGet("CURRENT_MACHINE_SPEED").ToInt() > 0)
                        {
                            // Зелёный
                            var color = "#A5C4AA";
                            var bc = new BrushConverter();
                            var brush = (Brush)bc.ConvertFrom(color);
                            CurrentTaskRestTextBox.Background = brush;
                        }
                        // белый цвет - задание не выполняется
                        else
                        {
                            // белый
                            var color = "#ffffff";
                            var bc = new BrushConverter();
                            var brush = (Brush)bc.ConvertFrom(color);
                            CurrentTaskRestTextBox.Background = brush;
                        }
                    }

                    if (formValues.CheckGet("CURRENT_BLOCKED_LAST_LABEL_PRINT_FLAG").ToInt() > 0)
                    {
                        BlockedLastLabelPrintFlagTextBox.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        BlockedLastLabelPrintFlagTextBox.Visibility = Visibility.Collapsed;
                    }

                    if (StackerWorkFlag == 1)
                    {
                        // Используем поподдонную печать
                        if (PrintPalletByStackerDataFlag)
                        {
                            ProcessProductionTaskNew();
                        }
                        else
                        {
                            ProcessProductionTaskOld(item);
                        }
                    }

                    break;

                case "TAIL":
                    formValues.CheckAdd($"{head}_QUANTITY_ON_PALLET", item.CheckGet("QUANTITY_ON_PALLET"));
                    formValues.CheckAdd("TAIL_SUBPRODUCT_FLAG", item.CheckGet("SUBPRODUCT_FLAG"));

                    TailForm.SetValues(formValues);
                    TailCardboard.Background = colorOuter.ToBrush();
                    TailCreaseCountTextBox.Background = colorOuter.ToBrush();
                    break;
            }
        }

        #region NewAlgoritm

        /// <summary>
        /// Очередь заданий на обработку (список новых заданий)
        /// </summary>
        public List<ProductionTaskData> NewProductionTaskDataList { get; set; }

        /// <summary>
        /// Текущее обрабатываемое производственное задание
        /// </summary>
        public ProductionTaskData CurrentProductionTaskData { get; set; }

        /// <summary>
        /// Предыдущее отработанное производственное задание
        /// </summary>
        public ProductionTaskData LastProductionTaskData { get; set; }

        /// <summary>
        /// Следующее произодственное задание. Ищем появление данных о схёме по этому заданию, чтобы закончить работу с CurrentProductionTaskData и перейти к работе с NewProductionTaskData
        /// </summary>
        public ProductionTaskData NewProductionTaskData { get; set; }

        /// <summary>
        /// Структура с данными по производственному заданию
        /// </summary>
        public struct ProductionTaskData
        {
            /// <summary>
            /// Идентифкатор произодственного задания
            /// </summary>
            public int ProductionTaskId { get; set; }

            /// <summary>
            /// Идентификатор продукции по этому заданию
            /// </summary>
            public int ProductId { get; set; }

            /// <summary>
            /// Идентификатор категории продукции по этому заданию
            /// </summary>
            public int ProductIdk1 { get; set; }

            /// <summary>
            /// Количество стоп на поддон по заданию
            /// </summary>
            public int QuantityStackOnPalletByTask { get; set; }

            /// <summary>
            /// Количество стоп из съёма стекера, необходимое для одного поддона
            /// </summary>
            public int QuantityStackOnPalletByDrop { get; set; }

            /// <summary>
            /// Укладка на поддон из стоп съёма
            /// </summary>
            public string StackingOnPalletByDrop { get; set; }

            /// <summary>
            /// Номер производственного задания
            /// </summary>
            public string ProductionTaskNumber { get; set; }

            /// <summary>
            /// Количество продукции по заданию
            /// </summary>
            public int ProductQuantityByTask { get; set; }

            /// <summary>
            /// Фактическое количество продукции на поддон
            /// </summary>
            public int ProductQuantityOnPallet { get; set; }

            /// <summary>
            /// Количество продукции в стопе по умолчанию
            /// </summary>
            public int ProductQuantityInReam { get; set; }
        }

        /// <summary>
        /// (Новый механизм)
        /// Обрабатываем производственное задание.
        /// </summary>
        public void ProcessProductionTaskNew()
        {
            if (StackerWorkFlag == 1)
            {
                // Проверяем, поменялось ли производственное задние
                if (CurrentProductionTaskData.ProductionTaskId != TaskForm.GetValueByPath("CURRENT_PRODUCTION_TASK_ID").ToInt() || CurrentProductionTaskData.ProductId != TaskForm.GetValueByPath("CURRENT_ID2").ToInt())
                {
                    // Мы будем работать со старым ПЗ до тех пор, пока в данных по съёму не появится запись о съёме по новому ПЗ
                    // после чего новое ПЗ перейдёт в переменную для текущего ПЗ и мы будем работать с ним

                    // Если задание поменялось
                    // Записывает данные нового производственного задания в структуру
                    ProductionTaskData newProductionTaskData = new ProductionTaskData();
                    newProductionTaskData.ProductionTaskId = TaskForm.GetValueByPath("CURRENT_PRODUCTION_TASK_ID").ToInt();
                    newProductionTaskData.ProductId = TaskForm.GetValueByPath("CURRENT_ID2").ToInt();
                    newProductionTaskData.ProductIdk1 = TaskForm.GetValueByPath("CURRENT_IDK1").ToInt();
                    newProductionTaskData.QuantityStackOnPalletByTask = TaskForm.GetValueByPath("CURRENT_QUANTITY_REAM").ToInt();
                    newProductionTaskData.ProductionTaskNumber = TaskForm.GetValueByPath("CURRENT_PRODUCTION_TASK_NUMBER");
                    newProductionTaskData.ProductQuantityByTask = TaskForm.GetValueByPath("CURRENT_QUANTITY_BY_TASK").ToInt();
                    newProductionTaskData.ProductQuantityOnPallet = TaskForm.GetValueByPath("CURRENT_QUANTITY_ON_PALLET").ToInt();
                    newProductionTaskData.ProductQuantityInReam = TaskForm.GetValueByPath("CURRENT_QUANTITY_IN_REAM").ToInt();

                    // Проверяем, если в очереди заданий на обработку нет такого задания, то добавляем
                    if (!NewProductionTaskDataList.Contains(newProductionTaskData))
                    {
                        NewProductionTaskDataList.Add(newProductionTaskData);
                    }

                    // Если старое ПЗ не ноль
                    if (CurrentProductionTaskData.ProductionTaskId != 0)
                    {
                        // Работаем со старым пз, пока не появятся съёмы по новому пз

                        GetStackerDataNew();
                        if (StackerDropDataSet != null && StackerDropDataSet.Items.Count > 0)
                        {
                            List<Dictionary<string, string>> newProductionTaskDrop = new List<Dictionary<string, string>>();
                            foreach (var newProductionTaskDataListItem in NewProductionTaskDataList)
                            {
                                // Проверяем, есть ли в данных по съёму съёмы по новому пз
                                newProductionTaskDrop = StackerDropDataSet.Items.Where(x => x.CheckGet("ID_PZ").ToInt() == newProductionTaskDataListItem.ProductionTaskId && x.CheckGet("ID2").ToInt() == newProductionTaskDataListItem.ProductId && x.CheckGet("PROCESSED_FLAG").ToInt() == 0).ToList();
                                if (newProductionTaskDrop != null && newProductionTaskDrop.Count > 0)
                                {
                                    NewProductionTaskData = newProductionTaskDataListItem;
                                    break;
                                }
                            }

                            // Если есть съём по новому пз, то 
                            // проверяем старое пз на наличие последнего поддона и начинаем работать с новым пз
                            if (newProductionTaskDrop.Count > 0)
                            {
                                // Заполняем буфер всеми оставшимися не обработанными стопами старого пз
                                // и при возможности создаём полные поддоны
                                CheckStackForPalletNew();

                                if (StackBuffer != null)
                                {
                                    // Проверяем, если в буфере нет не обработанных стоп, то очищаем буфер
                                    if (StackBuffer.FirstOrDefault(x => x.CheckGet("PROCESSED_FLAG").ToInt() == 0) == null)
                                    {
                                        StackBuffer.Clear();
                                    }
                                    // Остались не обработанные стопы с прошлого ПЗ
                                    else
                                    {
                                        // создаём последний поддон старого пз
                                        CreateLastPalletNew(CurrentProductionTaskData.ProductionTaskId, CurrentProductionTaskData.ProductId, CurrentProductionTaskData.ProductIdk1);
                                    }
                                }

                                // Меняем ПЗ
                                // Записываем пз с которым больше не будем работать в переменную для старого пз
                                LastProductionTaskData = CurrentProductionTaskData;

                                // записываем новое пз в рабочую переменную и начинаем работать с этим пз
                                CurrentProductionTaskData = NewProductionTaskData;

                                // Удаляем производственное задание из очереди, так как оно начало обрабатываться
                                NewProductionTaskDataList.Remove(NewProductionTaskData);

                                // Поменялось ПЗ, вызываем метод для получения данных по количеству стоп на поддоне по этому производственному заданию
                                // Заполняем расчитанное количество поддонов по этому ПЗ.
                                ProcessStartCurrentProductionTaskNew(CurrentProductionTaskData);
                            }
                            // Если нет съёмов по новому пз, то продолжаем работать со старым пз
                            else
                            {
                                CheckStackForPalletNew();
                            }
                        }
                    }
                    // Если сменилось пз и старое пз == 0
                    // то начинаем работать с новым пз, так как по старому пз ничего сделать нельзя
                    else
                    {
                        NewProductionTaskData = newProductionTaskData;

                        // Записываем пз с которым больше не будем работать в переменную для старого пз
                        LastProductionTaskData = CurrentProductionTaskData;

                        // записываем новое пз в рабочую переменную и начинаем работать с этим пз
                        CurrentProductionTaskData = NewProductionTaskData;
                        
                        // Удаляем производственное задание из очереди, так как оно начало обрабатываться
                        NewProductionTaskDataList.Remove(NewProductionTaskData);

                        // Поменялось ПЗ, вызываем метод для получения данных по количеству стоп на поддоне по этому производственному заданию
                        // Заполняем расчитанное количество поддонов по этому ПЗ.
                        ProcessStartCurrentProductionTaskNew(CurrentProductionTaskData);
                    }
                }
                // Если ПЗ не поменялось
                else
                {
                    GetStackerDataNew();
                    if (StackerDropDataSet != null)
                    {
                        CheckStackForPalletNew();
                    }
                }

                StackBufferTextBox.Text = StackBuffer.Where(x => x.CheckGet("PROCESSED_FLAG").ToInt() == 0 && x.CheckGet("DROP_MANUALLY_FLAG").ToInt() == 0 
                && x.CheckGet("ID_PZ").ToInt() == CurrentProductionTaskData.ProductionTaskId && x.CheckGet("ID2").ToInt() == CurrentProductionTaskData.ProductId).Count().ToString();
                NeededStackTextBox.Text = $"{CurrentProductionTaskData.QuantityStackOnPalletByDrop}";

                if (!string.IsNullOrEmpty(CurrentProductionTaskData.StackingOnPalletByDrop))
                {
                    CurrentStackTextBox.Text = $"{CurrentStackTextBox.Text} [{CurrentProductionTaskData.StackingOnPalletByDrop}]";
                }
            }
        }

        /// <summary>
        /// (Новый механизм)
        /// Обрабатываем начало текущего производственного задания.
        /// Сохраняем расчитанное количество поддонов по текущему ПЗ
        /// </summary>
        public void ProcessStartCurrentProductionTaskNew(ProductionTaskData currentProductionTaskData)
        {
            string productionTaskNumber = currentProductionTaskData.ProductionTaskNumber;
            string productionTaskId = currentProductionTaskData.ProductionTaskId.ToString();
            string productId = currentProductionTaskData.ProductId.ToString();
            string productIdk1 = currentProductionTaskData.ProductIdk1.ToString();
            int stackQuantityOnPalletByTask = currentProductionTaskData.QuantityStackOnPalletByTask;
            int productQuantityByTask = currentProductionTaskData.ProductQuantityByTask;
            int productQuantityOnPallet = currentProductionTaskData.ProductQuantityOnPallet;
            // Количество поддонов, которое должно быть по заданию
            int countPalletByTask = Math.Ceiling((double)productQuantityByTask / (double)productQuantityOnPallet).ToInt();
            // Количество поддонов, которое уже создали
            int countCreatedPallet = GetCountCreatedPallet(productionTaskId, productId);
            // Количество поддонов, которое осталось создать
            int requiredPalletCount = countPalletByTask - countCreatedPallet;

            {
                string msg = $"Началось производственное задание {productionTaskNumber}." +
                    $"{Environment.NewLine}Количество поддонов по заданию: {countPalletByTask}." +
                    $"{Environment.NewLine}Количество поддонов, которое уже создали: {countCreatedPallet}." +
                    $"{Environment.NewLine}Количество поддонов, которое осталось создать: {requiredPalletCount}." +
                    $"{Environment.NewLine}Количество стоп на поддоне по заданию: {stackQuantityOnPalletByTask}.";
                string tableDirectory = $"{productionTaskId}_{productId}";
                string primaryKeyValue = $"{productionTaskId}~{productId}_start";
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("MESSAGE", msg);
                SendLog(data, tableDirectory, primaryKeyValue);

                msg = $"Началось производственное задание." +
                    $"{Environment.NewLine}Поддонов по заданию: {countPalletByTask}." +
                    $"{Environment.NewLine}Поддонов уже создали: {countCreatedPallet}." +
                    $"{Environment.NewLine}Поддонов осталось создать: {requiredPalletCount}." +
                    $"{Environment.NewLine}Стоп на поддоне по заданию: {stackQuantityOnPalletByTask}.";
                WriteWorkLog(msg, productionTaskNumber);
            }

            // 1 -- Сохраняем расчитанное количество поддонов по текущему ПЗ;
            SetCalculationPalletCount(countPalletByTask, productionTaskId, productId);
        }

        /// <summary>
        /// (Новый механизм)
        /// Получаем данные по съёму для CurrentProductionTaskData и NewProductionTaskDataList на текущем ГА
        /// </summary>
        public void GetStackerDataNew()
        {
            string productionTaskIdList = $"{CurrentProductionTaskData.ProductionTaskId}";
            foreach (var newProductionTaskData in NewProductionTaskDataList)
            {
                productionTaskIdList = $"{productionTaskIdList};{newProductionTaskData.ProductionTaskId}";
            }

            var p = new Dictionary<string, string>();
            p.Add("PRODUCTION_TASK_ID_LIST", productionTaskIdList);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Stacker");
            q.Request.SetParam("Action", "ListStackerData");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    StackerDropDataSet = ListDataSet.Create(result, "ITEMS");

                    if (CurrentProductionTaskData.QuantityStackOnPalletByDrop == 0)
                    {
                        if (StackerDropDataSet != null && StackerDropDataSet.Items != null && StackerDropDataSet.Items.Count > 0)
                        {
                            var tempList = StackerDropDataSet.Items.Where(x => x.CheckGet("ID_PZ").ToInt() == CurrentProductionTaskData.ProductionTaskId 
                            && x.CheckGet("ID2").ToInt() == CurrentProductionTaskData.ProductId);
                            if (tempList != null)
                            {
                                var firstItem = tempList.ToList().OrderBy(x => x.CheckGet("DROP_CNT").ToInt()).First();
                                int quantityStackOnPalletByDrop = firstItem.CheckGet("DROP_STACK_COUNT_ON_PALLET").ToInt();
                                if (quantityStackOnPalletByDrop > 0)
                                {
                                    var tempCurrentProductionTaskData = CurrentProductionTaskData;
                                    tempCurrentProductionTaskData.QuantityStackOnPalletByDrop = quantityStackOnPalletByDrop;
                                    tempCurrentProductionTaskData.StackingOnPalletByDrop = firstItem.CheckGet("STACKING_ON_PALLET_BY_DROP");
                                    CurrentProductionTaskData = tempCurrentProductionTaskData;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                q.SilentErrorProcess = true;
                q.ProcessError();
            }
        }

        /// <summary>
        /// (Новый механизм)
        /// Создаём последний поддон в ПЗ
        /// Вызывается, если ПЗ сменилось, но в буфере остались не обработанные стопы
        /// </summary>
        public void CreateLastPalletNew(int productionTaskId, int productId, int productIdk1)
        {
            if (StackBuffer != null)
            {
                // лист стоп, которые пойдут на последний поддон
                List<Dictionary<string, string>> listStackForLastPallet = StackBuffer.Where(x => x.CheckGet("PROCESSED_FLAG").ToInt() == 0).ToList();

                if (listStackForLastPallet != null)
                {
                    if (listStackForLastPallet.Count > 0)
                    {
                        // получаем количество картона на поддоне
                        int quantityOnPallet = listStackForLastPallet.Sum(x => x.CheckGet("DROP_STACK_QTY").ToInt());

                        // Флаг успешного отрабатывания запроса на создание поддона
                        bool resultQuery = false;

                        // Список PCSD_ID, стопы которых использовались для данного поддона
                        List<int> lictCorrugatingStackerId = new List<int>();
                        foreach (var item in listStackForLastPallet)
                        {
                            int pcsdId = item.CheckGet("PCSD_ID").ToInt();
                            if (!lictCorrugatingStackerId.Contains(pcsdId))
                            {
                                lictCorrugatingStackerId.Add(pcsdId);
                            }
                        }

                        // Строка с набором pcsdId (ид съёма), стопы которых использовались для данного поддона
                        string corrugatingStackerIdSet = "";
                        foreach (var corrugatingStackerId in lictCorrugatingStackerId)
                        {
                            var s = $"{corrugatingStackerId};";
                            corrugatingStackerIdSet += s;
                        }

                        if (!string.IsNullOrEmpty(corrugatingStackerIdSet))
                        {
                            // создаём новый поддон и для указанного съёма устанавливаем PROCESSED_FLAG = 1
                            resultQuery = CreateOnePallet(productionTaskId, productId, productIdk1, quantityOnPallet, corrugatingStackerIdSet);
                        }
                        else
                        {
                            // exeption
                        }

                        if (resultQuery)
                        {
                            StackBuffer.Clear();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// (Новый механизм)
        /// Вариант проверки, что у нас достаточно стоп для создания поддона. ОБРАБОТКА В РАМКАХ ТЕКУЩЕГО ЗАДАНИЯ (CurrentProductionTaskData).
        /// Обработка съёма (PROCESSED_FLAG) происодит при использовании первой стопы, а не последней.
        /// 
        /// 1. Получаем данные (по съёмам) из таблицы prod_corrgtr_stacker_data, для которых PROCESSED_FLAG = 0
        /// 2. Формируем из полученных съёмов список стоп, которые получились после этих съёмов
        /// 3. Если есть стопы, которых нет в буфере программы, то добавляем их в буфер
        /// 4. Проверяем, что в буфере достаточно стоп для формирования поддона, не учитывая стопы, у которых PROCESSED_FLAG = 1
        ///     4.1 Если да
        ///         4.1.1 Создаём поддон
        ///         4.1.2 Для записи таблицы prod_corrgtr_stacker_data, стопы которой были использованы для нового поддона, устанавливаем PROCESSED_FLAG = 1, 
        ///                 чтобы не учитывать этот съём при следующем получении данных и работать только с теми стопами этого съёма, которые уже в буфере программы
        ///         4.1.3 В буфере программы для стоп, которые были использованы, устанавливаем PROCESSED_FLAG = 1, чтобы не учитывать их при формировании следующего нового поддона для этого ПЗ
        ///     4.2 Если нет
        ///         4.2.1 Ждём следующий съём
        ///         
        /// В случае перезагрузки программы (или любой другой ситуации, при которой мы теряем буфер):
        /// Если мы уже начали использовать стопы съёма, но не использовали их все, то
        ///     Так как у съёма (prod_corrgtr_stacker_data) PROCESSED_FLAG = 1, то мы не получим данные по его стопам в буфер
        ///         В таком случае используется ручная печать ярлыка
        /// Если мы не начали использовать стопы съёма, то
        ///     Так как у съёма (prod_corrgtr_stacker_data) PROCESSED_FLAG = 0, то мы получим данные по его стопам обратно в буфер
        /// </summary>
        public void CheckStackForPalletNew()
        {
            int productionTaskId = CurrentProductionTaskData.ProductionTaskId;
            int productId = CurrentProductionTaskData.ProductId;
            int productIdk1 = CurrentProductionTaskData.ProductIdk1;

            // список подходищих записей по съёму из бд
            List<Dictionary<string, string>> items = new List<Dictionary<string, string>>();

            // Из данных по съёмам получаем съёмы по выбранному ПЗ
            if (StackerDropDataSet.Items != null && StackerDropDataSet.Items.Count > 0)
            {
                items = StackerDropDataSet.Items.Where(x => x.CheckGet("ID_PZ").ToInt() == productionTaskId
                                                            && x.CheckGet("ID2").ToInt() == productId
                                                            && x.CheckGet("PROCESSED_FLAG").ToInt() == 0).ToList();
                items = items.OrderBy(x => x.CheckGet("PCSD_ID").ToInt()).ToList();
            }

            // промежуточный буфер стоп, хранит данные по стопам, полученые после преобразования данных по съёмам
            List<Dictionary<string, string>> localBuffer = new List<Dictionary<string, string>>();
            if (items.Count > 0)
            {
                // преобразуем записи по съёму в локальный список стоп
                foreach (var item in items)
                {
                    if (StackBuffer.FirstOrDefault(x => x.CheckGet("PCSD_ID") == item.CheckGet("PCSD_ID")) == null)
                    {
                        string log = $"Съём стекера." +
                            $"{Environment.NewLine}Номер съёма: {item.CheckGet("DROP_CNT").ToInt()}." +
                            $"{Environment.NewLine}В съёме: {item.CheckGet("DROP_ITEM_QTY").ToInt()}." +
                            $"{Environment.NewLine}В стопе: {item.CheckGet("DROP_STACK_QTY").ToInt()}." +
                            $"{Environment.NewLine}Стоп: {item.CheckGet("DROP_STACK_COUNT").ToInt()}." +
                            $"{Environment.NewLine}Ручной сброс: {item.CheckGet("DROP_MANUALLY_FLAG").ToInt()}.";
                        WriteWorkLog(log, CurrentProductionTaskData.ProductionTaskNumber);
                    }

                    // Если автоматический съём, то для каждой стопы этого съёма создаём запись
                    if (item.CheckGet("DROP_MANUALLY_FLAG").ToInt() == 0)
                    {
                        // для каждой стопы этого съёма создаём запись
                        for (int i = 1; i <= item.CheckGet("DROP_STACK_COUNT").ToInt(); i++)
                        {
                            var stack = new Dictionary<string, string>();

                            stack.Add("PCSD_ID", item.CheckGet("PCSD_ID"));
                            stack.Add("PRCA_ID", item.CheckGet("PRCA_ID"));
                            stack.Add("ID_PZ", item.CheckGet("ID_PZ"));
                            stack.Add("ID2", item.CheckGet("ID2"));
                            stack.Add("DROP_CNT", item.CheckGet("DROP_CNT"));
                            stack.Add("DROP_ITEM_QTY", item.CheckGet("DROP_ITEM_QTY"));
                            stack.Add("DROP_STACK_QTY", item.CheckGet("DROP_STACK_QTY"));
                            stack.Add("PROCESSED_FLAG", item.CheckGet("PROCESSED_FLAG"));
                            stack.Add("DROP_MANUALLY_FLAG", item.CheckGet("DROP_MANUALLY_FLAG"));
                            stack.Add("DROP_STACK_COUNT", item.CheckGet("DROP_STACK_COUNT"));

                            // добавляем дополнительные параметры
                            // номер этой стопы в этом съёме
                            stack.Add("STACK_NUMBER", i.ToString());

                            localBuffer.Add(stack);
                        }
                    }
                    // Если ручной съём, то формируем одну запись для такого съёма, все стопы этого съёма пойдут на один ярлык
                    else
                    {
                        var stack = new Dictionary<string, string>();
                        stack.Add("PCSD_ID", item.CheckGet("PCSD_ID"));
                        stack.Add("PRCA_ID", item.CheckGet("PRCA_ID"));
                        stack.Add("ID_PZ", item.CheckGet("ID_PZ"));
                        stack.Add("ID2", item.CheckGet("ID2"));
                        stack.Add("DROP_CNT", item.CheckGet("DROP_CNT"));
                        stack.Add("DROP_ITEM_QTY", item.CheckGet("DROP_ITEM_QTY"));
                        stack.Add("DROP_STACK_QTY", item.CheckGet("DROP_STACK_QTY"));
                        stack.Add("PROCESSED_FLAG", item.CheckGet("PROCESSED_FLAG"));
                        stack.Add("DROP_MANUALLY_FLAG", item.CheckGet("DROP_MANUALLY_FLAG"));
                        stack.Add("DROP_STACK_COUNT", item.CheckGet("DROP_STACK_COUNT"));
                        stack.Add("STACK_NUMBER", "1");
                        localBuffer.Add(stack);
                    }
                }

                localBuffer = localBuffer.OrderBy(x => x.CheckGet("PCSD_ID").ToInt()).ToList();
            }

            // Добавляем новые значения в глобальный буфер не использованных стоп по данному пз
            if (localBuffer.Count > 0)
            {
                foreach (var item in localBuffer)
                {
                    // ищем в глобальном буфере такую же стопу
                    var i = StackBuffer.FirstOrDefault(x =>
                       x.CheckGet("PCSD_ID").ToInt() == item.CheckGet("PCSD_ID").ToInt()
                    //&& x.CheckGet("PRCA_ID") == item.CheckGet("PRCA_ID")
                    //&& x.CheckGet("ID_PZ") == item.CheckGet("ID_PZ")
                    //&& x.CheckGet("ID2") == item.CheckGet("ID2")
                    //&& x.CheckGet("DROP_CNT") == item.CheckGet("DROP_CNT")
                    //&& x.CheckGet("DROP_ITEM_QTY") == item.CheckGet("DROP_ITEM_QTY")
                    //&& x.CheckGet("DROP_STACK_QTY") == item.CheckGet("DROP_STACK_QTY")
                    //&& x.CheckGet("DROP_STACK_COUNT") == item.CheckGet("DROP_STACK_COUNT")
                    && x.CheckGet("STACK_NUMBER").ToInt() == item.CheckGet("STACK_NUMBER").ToInt());

                    // если такой записи нет, то добавляем в буфер стопу
                    if (i == null)
                    {
                        StackBuffer.Add(item);
                    }
                }
            }

            // Обрабатываем глобальный буфер стоп
            if (StackBuffer.Count > 0)
            {
                // Отсортированный список не обработанных АВТОМАТИЧЕСКИХ стоп по этому производственному заданию 
                List<Dictionary<string, string>> listNoProcessedBufferStack = new List<Dictionary<string, string>>();

                // Рабочий буфер -- буфер стоп, которые относятся к выбранному ПЗ и не были обработаны
                var workBuffer = StackBuffer.Where(x => x.CheckGet("PROCESSED_FLAG").ToInt() == 0
                                                        && x.CheckGet("ID_PZ").ToInt() == productionTaskId
                                                        && x.CheckGet("ID2").ToInt() == productId).
                                                        OrderBy(x => x.CheckGet("PCSD_ID").ToInt()).ToList();
                if (workBuffer != null && workBuffer.Count > 0)
                {
                    // Проходим по буферу стоп для этого ПЗ
                    foreach (var stack in workBuffer)
                    {
                        // Добавляем в список те стопы, которые вышли в автоматическом режиме и не были обработаны
                        if (stack.CheckGet("DROP_MANUALLY_FLAG").ToInt() == 0)
                        {
                            listNoProcessedBufferStack.Add(stack);
                        }
                        // Если встречаем стопу, которая вышла в ручном режиме и не была обработана, то
                        // 1 -- Проверяем, хватает ли текущего количества стоп в списке, для того, чтобы создать поддон. Если хватает, то создаём.
                        // 2 -- Создаём поддон для всего ручного съёма.
                        else
                        {
                            // 1 -- Проверяем, хватает ли текущего количества стоп в списке, для того, чтобы создать поддон. Если хватает, то создаём.
                            if (listNoProcessedBufferStack.Count > 0)
                            {
                                // Проверяем, что в буфере достаточно стоп для формирования хотя бы одного поддона 
                                if (listNoProcessedBufferStack.Count >= CurrentProductionTaskData.QuantityStackOnPalletByDrop)
                                {
                                    // расчитываем количество новых поддонов, которое можно получить из имеющихся стоп
                                    int quantityNewPallet = listNoProcessedBufferStack.Count / CurrentProductionTaskData.QuantityStackOnPalletByDrop;

                                    // проходим циклом столько раз, сколько можем создать новых поддонов
                                    // создаём новые поддоны
                                    // и удаляем из буфера использованные стопы
                                    for (int i = 0; i < quantityNewPallet; i++)
                                    {
                                        // лист стоп из которых состоит новый поддон
                                        List<Dictionary<string, string>> stacks = listNoProcessedBufferStack.Take(CurrentProductionTaskData.QuantityStackOnPalletByDrop).ToList();
                                        // получаем количество картона на поддоне
                                        int quantityOnPallet = stacks.Sum(x => x.CheckGet("DROP_STACK_QTY").ToInt());

                                        // Флаг успешного отрабатывания запроса на создание поддона
                                        bool resultQuery = false;

                                        // Список PCSD_ID, стопы которых использовались для данного поддона
                                        List<int> listCorrugatingStackerId = new List<int>();
                                        foreach (var item in stacks)
                                        {
                                            int corrugatingStackerId = item.CheckGet("PCSD_ID").ToInt();
                                            if (!listCorrugatingStackerId.Contains(corrugatingStackerId))
                                            {
                                                listCorrugatingStackerId.Add(corrugatingStackerId);
                                            }
                                        }

                                        // Строка с набором pcsdId (ид съёма), стопы которых использовались для данного поддона
                                        string corrugatingStackerIdSet = "";
                                        foreach (var corrugatingStackerId in listCorrugatingStackerId)
                                        {
                                            var s = $"{corrugatingStackerId};";
                                            corrugatingStackerIdSet += s;
                                        }

                                        if (!string.IsNullOrEmpty(corrugatingStackerIdSet))
                                        {
                                            // создаём новый поддон и для указанного съёма устанавливаем PROCESSED_FLAG = 1
                                            resultQuery = CreateOnePallet(productionTaskId, productId, productIdk1, quantityOnPallet, corrugatingStackerIdSet);
                                        }
                                        else
                                        {
                                            // exeption
                                        }

                                        if (resultQuery)
                                        {
                                            // помечаем использованные стопы, чтобы не использовать их при создании новых поддонов
                                            // но и не добавлять эти поддоны в глобальный буфер при обновлении данных
                                            foreach (var item in stacks)
                                            {
                                                listNoProcessedBufferStack.Remove(item);

                                                // флаг того, что поддон обработан
                                                // но не удаляем его до тех пор, пока запись о съёме не будет PROCESSED_FLAG = 1
                                                item.CheckAdd("PROCESSED_FLAG", "1");

                                                //FIXME сделать удаление стоп из буфера
                                            }
                                        }
                                    }
                                }
                            }

                            // 2 -- Создаём поддон для всего ручного съёма.
                            {
                                // создаём новый поддон и для указанного ручного съёма, устанавливаем PROCESSED_FLAG = 1
                                bool resultQuery = CreateOnePallet(productionTaskId, productId, productIdk1, stack.CheckGet("DROP_ITEM_QTY").ToInt(), $"{stack.CheckGet("PCSD_ID")};");
                                if (resultQuery)
                                {
                                    stack.CheckAdd("PROCESSED_FLAG", "1");

                                    //FIXME сделать удаление стоп из буфера
                                }
                            }
                        }
                    }

                    // Если остались не использованные стопы по этому ПЗ
                    if (listNoProcessedBufferStack.Count > 0)
                    {
                        // Проверяем, что в буфере достаточно стоп для формирования хотя бы одного поддона 
                        if (listNoProcessedBufferStack.Count >= CurrentProductionTaskData.QuantityStackOnPalletByDrop)
                        {
                            // расчитываем количество новых поддонов, которое можно получить из имеющихся стоп
                            int quantityNewPallet = listNoProcessedBufferStack.Count / CurrentProductionTaskData.QuantityStackOnPalletByDrop;

                            // проходим циклом столько раз, сколько можем создать новых поддонов
                            // создаём новые поддоны
                            // и удаляем из буфера использованные стопы
                            for (int i = 0; i < quantityNewPallet; i++)
                            {
                                // лист стоп из которых состоит новый поддон
                                List<Dictionary<string, string>> stacks = listNoProcessedBufferStack.Take(CurrentProductionTaskData.QuantityStackOnPalletByDrop).ToList();
                                // получаем количество картона на поддоне
                                int quantityOnPallet = stacks.Sum(x => x.CheckGet("DROP_STACK_QTY").ToInt());

                                // Флаг успешного отрабатывания запроса на создание поддона
                                bool resultQuery = false;

                                // Список PCSD_ID, стопы которых использовались для данного поддона
                                List<int> listCorrugatingStackerId = new List<int>();
                                foreach (var item in stacks)
                                {
                                    int corrugatingStackerId = item.CheckGet("PCSD_ID").ToInt();
                                    if (!listCorrugatingStackerId.Contains(corrugatingStackerId))
                                    {
                                        listCorrugatingStackerId.Add(corrugatingStackerId);
                                    }
                                }

                                // Строка с набором pcsdId (ид съёма), стопы которых использовались для данного поддона
                                string corrugatingStackerIdSet = "";
                                foreach (var corrugatingStackerId in listCorrugatingStackerId)
                                {
                                    var s = $"{corrugatingStackerId};";
                                    corrugatingStackerIdSet += s;
                                }

                                if (!string.IsNullOrEmpty(corrugatingStackerIdSet))
                                {
                                    // создаём новый поддон и для указанного съёма устанавливаем PROCESSED_FLAG = 1
                                    resultQuery = CreateOnePallet(productionTaskId, productId, productIdk1, quantityOnPallet, corrugatingStackerIdSet);
                                }
                                else
                                {
                                    // exeption
                                }

                                if (resultQuery)
                                {
                                    // помечаем использованные стопы, чтобы не использовать их при создании новых поддонов
                                    // но и не добавлять эти поддоны в глобальный буфер при обновлении данных
                                    foreach (var item in stacks)
                                    {
                                        listNoProcessedBufferStack.Remove(item);

                                        // флаг того, что поддон обработан
                                        // но не удаляем его до тех пор, пока запись о съёме не будет PROCESSED_FLAG = 1
                                        item.CheckAdd("PROCESSED_FLAG", "1");

                                        //FIXME сделать удаление стоп из буфера
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// (Новый механизм)
        /// Получаем текущее обрабатываемое задание
        /// </summary>
        public void ShowCurrentTask()
        {
            if (PrintPalletByStackerDataFlag)
            {
                string msg = $"[LAST]---{Environment.NewLine}" +
                    $"LastProductionTaskData.ProductionTaskId = [{LastProductionTaskData.ProductionTaskId}]{Environment.NewLine}" +
                    $"LastProductionTaskData.ProductId = [{LastProductionTaskData.ProductId}]{Environment.NewLine}" +
                    $"LastProductionTaskData.ProductIdk1 = [{LastProductionTaskData.ProductIdk1}]{Environment.NewLine}" +
                    $"LastProductionTaskData.QuantityStackOnPallet = [{LastProductionTaskData.QuantityStackOnPalletByTask}]{Environment.NewLine}" +
                    $"LastProductionTaskData.QuantityStackOnPalletByStacking = [{LastProductionTaskData.QuantityStackOnPalletByDrop}]{Environment.NewLine}" +
                    $"[CURRENT]---{Environment.NewLine}" +
                    $"CurrentProductionTaskData.ProductionTaskId = [{CurrentProductionTaskData.ProductionTaskId}]{Environment.NewLine}" +
                    $"CurrentProductionTaskData.ProductId = [{CurrentProductionTaskData.ProductId}]{Environment.NewLine}" +
                    $"CurrentProductionTaskData.ProductIdk1 = [{CurrentProductionTaskData.ProductIdk1}]{Environment.NewLine}" +
                    $"CurrentProductionTaskData.QuantityStackOnPallet = [{CurrentProductionTaskData.QuantityStackOnPalletByTask}]{Environment.NewLine}" +
                    $"CurrentProductionTaskData.QuantityStackOnPalletByStacking = [{CurrentProductionTaskData.QuantityStackOnPalletByDrop}]{Environment.NewLine}" +
                    $"[NEW]---{Environment.NewLine}" +
                    $"NewProductionTaskData.ProductionTaskId = [{NewProductionTaskData.ProductionTaskId}]{Environment.NewLine}" +
                    $"NewProductionTaskData.ProductId = [{NewProductionTaskData.ProductId}]{Environment.NewLine}" +
                    $"NewProductionTaskData.ProductIdk1 = [{NewProductionTaskData.ProductIdk1}]{Environment.NewLine}" +
                    $"NewProductionTaskData.QuantityStackOnPallet = [{NewProductionTaskData.QuantityStackOnPalletByTask}]{Environment.NewLine}" +
                    $"NewProductionTaskData.QuantityStackOnPalletByStacking = [{NewProductionTaskData.QuantityStackOnPalletByDrop}]{Environment.NewLine}" +
                    $"---{Environment.NewLine}" +
                    $"[QUEUE]---{Environment.NewLine}" +
                    $"---{Environment.NewLine}";

                int i = 0;
                foreach (var newProductionTaskDataListItem in NewProductionTaskDataList)
                {
                    i++;
                    msg = $"{msg}" +
                        $"[{i}]{Environment.NewLine}" +
                        $"QueueProductionTaskData.ProductionTaskId = [{newProductionTaskDataListItem.ProductionTaskId}]{Environment.NewLine}" +
                        $"QueueProductionTaskData.ProductId = [{newProductionTaskDataListItem.ProductId}]{Environment.NewLine}" +
                        $"---{Environment.NewLine}";
                }

                var d = new DialogWindow(msg, "Текущее обрабатываемое задание", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
            else
            {
                var msg = $"Интерфейс печатает ярлыки при старте задания (старый механизм). В текущей конфигурации нельзя получить текущее обрабатываемое задание.";
                var d = new DialogWindow($"{msg}", "Текущее обрабатываемое задание", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        #endregion

        #region OldAlgoritm

        /// <summary>
        /// (Старый механизм)
        /// Обрабатываем производственное задание.
        /// </summary>
        public void ProcessProductionTaskOld(Dictionary<string, string> item)
        {
            // Работаем с последним ярлыком
            if (LastPalletFlag > 0)
            {
                if (LastPalletFlag > 1)
                {
                    CreateLastPalletOld();
                    LastPalletFlag = 0;
                    CurrentProductionTaskProcessedFlag = false;
                }
                else
                {
                    LastPalletFlag++;
                }
            }
            
            // Работаем с заданием
            if (LastPalletFlag == 0)
            {
                int newProductionTaskId = item.CheckGet("PRODUCTION_TASK_ID").ToInt();
                int newProductId = item.CheckGet("ID2").ToInt();
                int newProductIdk1 = item.CheckGet("IDK1").ToInt();

                // Если производственное задание и выпускаемая продукция не поменялись
                if (newProductionTaskId == CurrentProductionTaskId && newProductId == CurrentProductId)
                {
                    ProcessCurrentProductionTaskOld();
                }
                // Если производственное задание или выпускаемая продукция поменялось
                else
                {
                    LastProductionTaskId = CurrentProductionTaskId;
                    LastProductId = CurrentProductId;
                    LastProductIdk1 = CurrentProductIdk1;
                    CurrentProductionTaskId = newProductionTaskId;
                    CurrentProductId = newProductId;
                    CurrentProductIdk1 = newProductIdk1;

                    // Если задание поменялось и прошлое задание == 0,
                    // то не нужно печатать последний ярлык, так как его нет,
                    // сразу начинаем работать с новым заданием
                    if (LastProductionTaskId == 0)
                    {
                        ProcessCurrentProductionTaskOld();
                    }
                    // Если задание поменялось и прошлое задание != 0,
                    // то печатаем последний ярлык прошлого задания,
                    // после чего начинаем работать с новым заданием
                    // upd
                    // Для того, чтобы печатать последний ярлык используются данные из plan_history_ga.qty_bhs
                    // Данные в plan_history_ga.qty_bhs приходят с задержкой
                    // Поэтому для корректного получения данных будем поднимать флаг того,
                    // что на следующей итерации таймера будет печататься последний ярлык прошлого пз
                    else
                    {
                        LastPalletFlag = 1;
                    }
                }
            }
        }

        /// <summary>
        /// (Старый механизм)
        /// Обрабатываем текущее производственное задание:
        /// 0 -- Проверяем, что начало задания не обработано;
        /// 1 -- Заполняем данные по количеству поддонов по заданию (prihod_pz.num_pallet);
        /// 2 -- Создаём все ярлыки, кроме последнего
        /// </summary>
        public void ProcessCurrentProductionTaskOld()
        {
            // Если начало текущего производственного задания не обработано
            if (!CurrentProductionTaskProcessedFlag)
            {
                // Проверям, что задание начало выполняться
                if (TaskForm.GetValueByPath("CURRENT_MACHINE_SPEED").ToInt() > 0 || ManuallyStartFlag)
                {
                    CurrentProductionTaskProcessedFlag = true;
                    ManuallyStartFlag = false;

                    string productionTaskNumber = TaskForm.GetValueByPath("CURRENT_PRODUCTION_TASK_NUMBER");
                    string productionTaskId = TaskForm.GetValueByPath("CURRENT_PRODUCTION_TASK_ID");
                    string productId = TaskForm.GetValueByPath("CURRENT_ID2");
                    string productIdk1 = TaskForm.GetValueByPath("CURRENT_IDK1");
                    int productQuantityByTask = TaskForm.GetValueByPath("CURRENT_QUANTITY_BY_TASK").ToInt();
                    int productQuantityOnPallet = TaskForm.GetValueByPath("CURRENT_QUANTITY_ON_PALLET").ToInt();
                    // Количество поддонов, которое должно быть по заданию
                    int countPalletByTask = Math.Ceiling((double)productQuantityByTask / (double)productQuantityOnPallet).ToInt();
                    // Количество поддонов, которое уже создали
                    int countCreatedPallet = GetCountCreatedPallet(productionTaskId, productId);
                    // Количество поддонов, которое осталось создать
                    int requiredPalletCount = countPalletByTask - countCreatedPallet;

                    {
                        string msg =    $"Началось производственное задание {productionTaskNumber}." +
                                        $"{Environment.NewLine}Количество поддонов по заданию: {countPalletByTask}." +
                                        $"{Environment.NewLine}Количество поддонов, которое уже создали: {countCreatedPallet}." +
                                        $"{Environment.NewLine}Количество поддонов, которое осталось создать: {requiredPalletCount}.";
                        string tableDirectory = $"{productionTaskId}_{productId}";
                        string primaryKeyValue = $"{productionTaskId}~{productId}_start";
                        Dictionary<string, string> data = new Dictionary<string, string>();
                        data.Add("MESSAGE", msg);
                        SendLog(data, tableDirectory, primaryKeyValue);

                        msg =   "Началось производственное задание." +
                                $"{Environment.NewLine}Поддонов по заданию: {countPalletByTask}." +
                                $"{Environment.NewLine}Поддонов уже создали: {countCreatedPallet}." +
                                $"{Environment.NewLine}Поддонов осталось создать: {requiredPalletCount}." +
                                $"{Environment.NewLine}Сейчас будет создано: {requiredPalletCount - 1}.";
                        WriteWorkLog(msg, productionTaskNumber);
                    }

                    if (requiredPalletCount > 0)
                    {
                        // Заполняем рассчитанное количество поддонов по заданию
                        SetCalculationPalletCount(countPalletByTask, productionTaskId, productId);
                        
                        Dictionary<string, string> palletNumberRange = new Dictionary<string, string>();
                        // Получаем диапазон доступных номеров поддонов (без учёта последнего поддона)
                        if (PrintPalletNumberByRange)
                        {
                            palletNumberRange = GetAvailablePalletNumberListOld(productionTaskId);
                        }

                        // Если удалось получить диапазон, то создаём поддоны с номерами из этого диапазона (без учёта последнего поддона)
                        if (palletNumberRange != null && palletNumberRange.Count > 0)
                        {
                            string msg = $"Получаем диапазон свободных номеров поддонов." +
                                            $"{Environment.NewLine}Номер стекера: {palletNumberRange.CheckGet("STACKER_NUMBER")}." +
                                            $"{Environment.NewLine}Начальный номер: {palletNumberRange.CheckGet("START_NUMBER")}." +
                                            $"{Environment.NewLine}Конечный номер: {palletNumberRange.CheckGet("END_NUMBER")}.";
                            string tableDirectory = $"{productionTaskId}_{productId}";
                            string primaryKeyValue = $"{productionTaskId}~{productId}_palletNumberRange";
                            Dictionary<string, string> data = new Dictionary<string, string>();
                            data.Add("MESSAGE", msg);
                            SendLog(data, tableDirectory, primaryKeyValue);

                            msg = $"Получаем диапазон номеров поддонов." +
                                    $"{Environment.NewLine}Начальный номер: {palletNumberRange.CheckGet("START_NUMBER")}." +
                                    $"{Environment.NewLine}Конечный номер: {palletNumberRange.CheckGet("END_NUMBER")}." +
                                    $"{Environment.NewLine}Поддонов осталось создать: {palletNumberRange.CheckGet("END_NUMBER").ToInt() - palletNumberRange.CheckGet("START_NUMBER").ToInt() + 2}." +
                                    $"{Environment.NewLine}Сейчас будет создано: {palletNumberRange.CheckGet("END_NUMBER").ToInt() - palletNumberRange.CheckGet("START_NUMBER").ToInt() + 1}.";
                            WriteWorkLog(msg, productionTaskNumber);

                            CreateProductionTaskPalletsByNumberRangeOld(palletNumberRange, productQuantityOnPallet, productionTaskId, productId, productIdk1, productionTaskNumber);
                        }
                        // Если не удалось получить диапазон, что пытаемся получить следующий свободный номер поддона в процессе создания записи
                        else
                        {
                            CreateProductionTaskPalletsOld(requiredPalletCount, productQuantityOnPallet, productionTaskId, productId, productIdk1, productionTaskNumber);
                        }
                    }
                    else
                    {
                        string tableDirectory = $"{productionTaskId}_{productId}";
                        string primaryKeyValue = $"{productionTaskId}~{productId}_first";
                        Dictionary<string, string> data = new Dictionary<string, string>();
                        string msg = $"Началась обработка производственного задания." +
                            $"{Environment.NewLine}Количество поддонов, которое осталось распечатать: {requiredPalletCount}." +
                            $"{Environment.NewLine}По заданию все поддоны уже распечатаны." +
                            $"{Environment.NewLine}Новые ярлыки не будут распечатаны в автоматическом режиме.";
                        data.Add("MESSAGE", msg);
                        SendLog(data, tableDirectory, primaryKeyValue);

                        msg =   $"По заданию все поддоны уже распечатаны." +
                                $"{Environment.NewLine}Новые ярлыки не будут распечатаны в автоматическом режиме.";
                        WriteWorkLog(msg, productionTaskNumber);
                    }
                }
                // Задание не начало выполняться
                else
                {
                    return;
                }
            }
            // Начало задания уже обработано
            else
            {
                ManuallyStartFlag = false;
                return;
            }
        }

        /// <summary>
        /// (Старый механизм)
        /// Пытаемся получить диапазон свободных номеров поддонов (без последнего поддона)
        /// </summary>
        /// <param name="productionTaskId"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetAvailablePalletNumberListOld(string productionTaskId, int lastPalletFlag = 0)
        {
            Dictionary<string, string> palletNumberRange = new Dictionary<string, string>();

            if (PrintPalletNumberByRange)
            {
                var p = new Dictionary<string, string>();
                p.Add("PRODUCTION_TASK_ID", productionTaskId);
                p.Add("LAST_PALLET_FLAG", lastPalletFlag.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "CorrugatingLabel");
                q.Request.SetParam("Action", "ListAvailablePalletNumber");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
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
                            palletNumberRange = ds.Items.FirstOrDefault(x => x.CheckGet("STACKER_NUMBER").ToInt() == StackerNumber);
                        }
                    }
                }
                else
                {
                    q.SilentErrorProcess = true;
                    q.ProcessError();
                }
            }

            return palletNumberRange;
        }

        /// <summary>
        /// (Старый механизм)
        /// Создаём последний ярлык предыдущего производственного задания
        /// </summary>
        public void CreateLastPalletOld()
        {
            string productionTaskId = LastProductionTaskId.ToString();
            string productId = LastProductId.ToString();
            string productIdk1 = LastProductIdk1.ToString();

            bool succesfullFlag = false;
            // Количество продукции, которое должно пойти на последний ярлык
            int requiredProductQuantity = 0;
            // Количество продукции по заданию
            int quantityByTask = 0;
            // Количество продукции на созданных ярлыках
            int quantityOnCreatedPallet = 0;
            // Количество продукции, которое выпустил гофроагрегат
            int quantityByCorrugatorMachine = 0;
            // Количество отбракованной продукции
            int quantityOfRejected = 0;
            // Номер производственного задания
            string productionTaskNumber = "";
            // Количество картона на поддоне
            int quantityOnPallet = 0;
            // Флаг того, что для этого задания заблокирована печать последнего ярлыка
            int lastLabelBlockedPrintFlag = 0;

            {
                var p = new Dictionary<string, string>();
                p.Add("PRODUCTION_TASK_ID", productionTaskId);
                p.Add("PRODUCT_ID2", productId);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "CorrugatingLabel");
                q.Request.SetParam("Action", "GetLastPalletData");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
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
                            var firstDictionary = ds.Items.First();
                            requiredProductQuantity = firstDictionary.CheckGet("REQUIRED_QUANTITY").ToInt();
                            quantityByTask = firstDictionary.CheckGet("QUANTITY_BY_TASK").ToInt();
                            quantityOnCreatedPallet = firstDictionary.CheckGet("QUANTITY_ON_CREATED_PALLET").ToInt();
                            quantityByCorrugatorMachine = firstDictionary.CheckGet("QUANTITY_BY_CORRUGATOR_MACHINE").ToInt();
                            quantityOfRejected = firstDictionary.CheckGet("QUANTITY_OF_REJECTED").ToInt();
                            productionTaskNumber = firstDictionary.CheckGet("PRODUCTION_TASK_NUMBER");
                            quantityOnPallet = firstDictionary.CheckGet("QUANTITY_ON_PALLET").ToInt();
                            lastLabelBlockedPrintFlag = firstDictionary.CheckGet("BLOCKED_LAST_LABEL_PRINT_FLAG").ToInt();

                            succesfullFlag = true;

                            {
                                string msg = $"Завершено производственное задание {productionTaskNumber}." +
                                                $"{Environment.NewLine}Количество продукции по заданию: {quantityByTask}." +
                                                $"{Environment.NewLine}Количество продукции, которое выпустил гофроагрегат: {quantityByCorrugatorMachine}." +
                                                $"{Environment.NewLine}Количество отбракованной продукции: {quantityOfRejected}." +
                                                $"{Environment.NewLine}Количество продукции на созданных ярлыках: {quantityOnCreatedPallet}." +
                                                $"{Environment.NewLine}Количество продукции, которое осталось распечатать: {requiredProductQuantity}." +
                                                $"{Environment.NewLine}Блокировка печати последнего ярлыка: {lastLabelBlockedPrintFlag}.";
                                string tableDirectory = $"{productionTaskId}_{productId}";
                                string primaryKeyValue = $"{productionTaskId}~{productId}_finish";
                                Dictionary<string, string> data = new Dictionary<string, string>();
                                data.Add("MESSAGE", msg);
                                SendLog(data, tableDirectory, primaryKeyValue);

                                msg = $"Завершено производственное задание." +
                                    $"{Environment.NewLine}По заданию: {quantityByTask}." +
                                    $"{Environment.NewLine}Выпустил гофроагрегат: {quantityByCorrugatorMachine}." +
                                    $"{Environment.NewLine}Отбраковано: {quantityOfRejected}." +
                                    $"{Environment.NewLine}На созданных ярлыках: {quantityOnCreatedPallet}." +
                                    $"{Environment.NewLine}На последние ярлыки: {requiredProductQuantity}." +
                                    $"{Environment.NewLine}На поддон: {quantityOnPallet}.";

                                if (quantityOnPallet > 0)
                                {
                                    msg =   $"{msg}" +
                                            $"{Environment.NewLine}Количество новых ярлыков: {Math.Ceiling((double)requiredProductQuantity / (double)quantityOnPallet)}.";
                                }
                                else
                                {
                                    msg = $"{msg}" +
                                            $"{Environment.NewLine}Нет информации по количеству на поддоне, вся оставшаяся продукция пойдёт на один ярлык.";
                                }

                                WriteWorkLog(msg, productionTaskNumber);
                            }
                        }
                    }
                }
                else
                {
                    string msg = $"При получении инфомации по оставшемуся количеству продукции произошла ошибка.{Environment.NewLine}Задание: {productionTaskNumber}";

                    string tableDirectory = $"{productionTaskId}_{productId}";
                    string primaryKeyValue = $"{productionTaskId}~{productId}_finish";
                    Dictionary<string, string> data = new Dictionary<string, string>();
                    data.Add("MESSAGE", msg);
                    SendLog(data, tableDirectory, primaryKeyValue);

                    var d = new StackerScanedLableInfo(msg, 1);
                    d.ShowAndAutoClose(1);

                    msg = "При получении инфомации по оставшемуся количеству продукции произошла ошибка.";
                    WriteWorkLog(msg, productionTaskNumber);

                    q.SilentErrorProcess = true;
                    q.ProcessError();
                }
            }

            if (lastLabelBlockedPrintFlag > 0)
            {
                int status = 1;

                string msg = "Последний ярлык не распечатан!" +
                     $"{Environment.NewLine}Печать последнего ярлыка заблокирована оператором.";

                {
                    string tableDirectory = $"{productionTaskId}_{productId}";
                    string primaryKeyValue = $"{productionTaskId}~{productId}_last";
                    Dictionary<string, string> data = new Dictionary<string, string>();
                    data.Add("MESSAGE", msg);
                    SendLog(data, tableDirectory, primaryKeyValue);

                    WriteWorkLog(msg, productionTaskNumber);
                }

                var d2 = new StackerScanedLableInfo(msg, status);
                d2.WindowMaxSizeFlag = false;
                d2.ShowAndAutoClose(3);
            }
            else
            {
                if (succesfullFlag)
                {
                    // Если есть продукция, которая должна пойти на последний ярлык, то создаём последний ярлык
                    if (requiredProductQuantity > 0)
                    {
                        // Может быть так, что последних ярлыков будет несколько (или вообще для всего задания нужно будет напечатать ярлыки, если их удалили вручную)
                        if (quantityOnPallet > 0)
                        {
                            {
                                string msg = $"Формируем последний ярлык." +
                                    $"{Environment.NewLine}Количество продукции, для которой осталось распечатать ярлык: {requiredProductQuantity}." +
                                    $"{Environment.NewLine}Количество продукции на поддоне: {quantityOnPallet}." +
                                    $"{Environment.NewLine}Количество новых ярлыков, которое нужно создать: {Math.Ceiling((double)requiredProductQuantity / (double)quantityOnPallet)}.";
                                string tableDirectory = $"{productionTaskId}_{productId}";
                                string primaryKeyValue = $"{productionTaskId}~{productId}_last";
                                Dictionary<string, string> data = new Dictionary<string, string>();
                                data.Add("MESSAGE", msg);
                                SendLog(data, tableDirectory, primaryKeyValue);
                            }

                            if (Math.Ceiling((double)requiredProductQuantity / (double)quantityOnPallet) > 0)
                            {
                                Dictionary<string, string> palletNumberRange = new Dictionary<string, string>();
                                // Получаем диапазон доступных номеров поддонов (без учёта последнего поддона)
                                if (PrintPalletNumberByRange)
                                {
                                    palletNumberRange = GetAvailablePalletNumberListOld(productionTaskId, 1);
                                }

                                // Если удалось получить диапазон, то создаём поддоны с номерами из этого диапазона (без учёта последнего поддона)
                                if (palletNumberRange != null && palletNumberRange.Count > 0)
                                {
                                    string msg = $"Попытка получить диапазон свободных номеров поддонов." +
                                                    $"{Environment.NewLine}Номер стекера: {palletNumberRange.CheckGet("STACKER_NUMBER")}." +
                                                    $"{Environment.NewLine}Начальный номер: {palletNumberRange.CheckGet("START_NUMBER")}." +
                                                    $"{Environment.NewLine}Конечный номер: {palletNumberRange.CheckGet("END_NUMBER")}.";
                                    string tableDirectory = $"{productionTaskId}_{productId}";
                                    string primaryKeyValue = $"{productionTaskId}~{productId}_lastPalletNumberRange";
                                    Dictionary<string, string> data = new Dictionary<string, string>();
                                    data.Add("MESSAGE", msg);
                                    SendLog(data, tableDirectory, primaryKeyValue);

                                    msg = $"Получаем диапазон номеров поддонов." +
                                            $"{Environment.NewLine}Начальный номер: {palletNumberRange.CheckGet("START_NUMBER")}." +
                                            $"{Environment.NewLine}Конечный номер: {palletNumberRange.CheckGet("END_NUMBER")}." +
                                            $"{Environment.NewLine}Сейчас будет создано: {palletNumberRange.CheckGet("END_NUMBER").ToInt() - palletNumberRange.CheckGet("START_NUMBER").ToInt() + 1}.";
                                    WriteWorkLog(msg, productionTaskNumber);

                                    for (int i = palletNumberRange.CheckGet("START_NUMBER").ToInt(); i <= palletNumberRange.CheckGet("END_NUMBER").ToInt(); i++)
                                    {
                                        if (requiredProductQuantity >= quantityOnPallet)
                                        {
                                            CreateOnePallet(productionTaskId.ToInt(), productId.ToInt(), productIdk1.ToInt(), quantityOnPallet, "", i);
                                            requiredProductQuantity = requiredProductQuantity - quantityOnPallet;
                                        }
                                        else
                                        {
                                            CreateOnePallet(productionTaskId.ToInt(), productId.ToInt(), productIdk1.ToInt(), requiredProductQuantity, "", i);
                                            requiredProductQuantity = requiredProductQuantity - requiredProductQuantity;
                                        }
                                    }
                                }
                                // Если не удалось получить диапазон, что пытаемся получить следующий свободный номер поддона в процессе создания записи
                                else
                                {
                                    while (requiredProductQuantity > 0)
                                    {
                                        if (requiredProductQuantity >= quantityOnPallet)
                                        {
                                            CreateOnePallet(productionTaskId.ToInt(), productId.ToInt(), productIdk1.ToInt(), quantityOnPallet);
                                            requiredProductQuantity = requiredProductQuantity - quantityOnPallet;
                                        }
                                        else
                                        {
                                            CreateOnePallet(productionTaskId.ToInt(), productId.ToInt(), productIdk1.ToInt(), requiredProductQuantity);
                                            requiredProductQuantity = requiredProductQuantity - requiredProductQuantity;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            {
                                string msg = $"Формируем последний ярлык по задананию." +
                                    $"{Environment.NewLine}Количество продукции, для которой осталось распечатать ярлык: {requiredProductQuantity}." +
                                    $"{Environment.NewLine}Количество продукции на поддоне: {quantityOnPallet}." +
                                    $"{Environment.NewLine}Нет информации по количеству на поддоне, поэтому вся оставшаяся продукция пойдёт на один ярлык.";
                                string tableDirectory = $"{productionTaskId}_{productId}";
                                string primaryKeyValue = $"{productionTaskId}~{productId}_last";
                                Dictionary<string, string> data = new Dictionary<string, string>();
                                data.Add("MESSAGE", msg);
                                SendLog(data, tableDirectory, primaryKeyValue);
                            }

                            CreateOnePallet(productionTaskId.ToInt(), productId.ToInt(), productIdk1.ToInt(), requiredProductQuantity);
                        }
                    }
                    // Нет продукции, которая должна пойти на последний ярлык
                    // Информируем пользователя, почему так произошло
                    else
                    {
                        int status = 1;
                        string msg = "";

                        if (quantityByCorrugatorMachine > 0)
                        {
                            if (quantityByCorrugatorMachine > quantityOfRejected)
                            {
                                if (quantityByCorrugatorMachine - quantityOfRejected == quantityOnCreatedPallet)
                                {
                                    msg = $"Последний ярлык не распечатан!" +
                                            $"{Environment.NewLine}Для всей неотбракованной продукции уже распечатаны ярлыки." +
                                            $"{Environment.NewLine}Количество неотбракованной продукции, которое выпустил гофроагрегат: {quantityByCorrugatorMachine - quantityOfRejected}." +
                                            $"{Environment.NewLine}Количество на распечатанных ярлыках: {quantityOnCreatedPallet}.";

                                    // Выделяем зелёным, потому что в теории всё в порядке.
                                    status = 2;
                                }
                            }
                            else
                            {
                                msg = $"Последний ярлык не распечатан!" +
                                        $"{Environment.NewLine}Вся продукция отбракована." +
                                        $"{Environment.NewLine}Количество продукции, которое выпустил гофроагрегат: {quantityByCorrugatorMachine}." +
                                        $"{Environment.NewLine}Количество отбракованной продукции: {quantityOfRejected}.";
                            }
                        }
                        else
                        {
                            msg = "Последний ярлык не распечатан!" +
                                    $"{Environment.NewLine}Нет данных по количеству продукции, которое выпустил гофроагрегат.";
                        }

                        {
                            string tableDirectory = $"{productionTaskId}_{productId}";
                            string primaryKeyValue = $"{productionTaskId}~{productId}_last";
                            Dictionary<string, string> data = new Dictionary<string, string>();
                            data.Add("MESSAGE", msg);
                            SendLog(data, tableDirectory, primaryKeyValue);

                            WriteWorkLog(msg, productionTaskNumber);
                        }

                        var d2 = new StackerScanedLableInfo(msg, status);
                        d2.WindowMaxSizeFlag = false;
                        d2.ShowAndAutoClose(3);
                    }
                }
            }
        }

        /// <summary>
        /// (Старый механизм)
        /// Создаём поддоны по текущему производственному заданию (Все поддоны, кроме последнего) используя полученный диапазон свободных номеров поддонов (без последнего поддона) 
        /// </summary>
        public void CreateProductionTaskPalletsByNumberRangeOld(Dictionary<string, string> palletNumberRange, int productQuantityOnPallet, string productionTaskId, string productId, string productIdk1, string productionTaskNumber = "")
        {
            if (palletNumberRange != null && palletNumberRange.Count > 0)
            {
                {
                    string tableDirectory = $"{productionTaskId}_{productId}";
                    string primaryKeyValue = $"{productionTaskId}~{productId}_first";
                    Dictionary<string, string> data = new Dictionary<string, string>();
                    string msg = $"Началась обработка производственного задания." +
                        $"{Environment.NewLine}Количество поддонов, которое осталось распечатать: {palletNumberRange.CheckGet("END_NUMBER").ToInt() - palletNumberRange.CheckGet("START_NUMBER").ToInt() + 2}." +
                        $"{Environment.NewLine}Количество ярлыков, которое сейчас будет распечатано: {palletNumberRange.CheckGet("END_NUMBER").ToInt() - palletNumberRange.CheckGet("START_NUMBER").ToInt() + 1}.";
                    data.Add("MESSAGE", msg);
                    SendLog(data, tableDirectory, primaryKeyValue);
                }

                for (int i = palletNumberRange.CheckGet("START_NUMBER").ToInt(); i <= palletNumberRange.CheckGet("END_NUMBER").ToInt(); i++)
                {
                    CreateOnePallet(productionTaskId.ToInt(), productId.ToInt(), productIdk1.ToInt(), productQuantityOnPallet.ToInt(), "", i);
                }
            }
        }

        /// <summary>
        /// (Старый механизм)
        /// Создаём поддоны по текущему производственному заданию (Все поддоны, кроме последнего)
        /// </summary>
        public void CreateProductionTaskPalletsOld(int countPallet, int productQuantityOnPallet, string productionTaskId, string productId, string productIdk1, string productionTaskNumber = "")
        {
            if (countPallet > 0)
            {
                {
                    string tableDirectory = $"{productionTaskId}_{productId}";
                    string primaryKeyValue = $"{productionTaskId}~{productId}_first";
                    Dictionary<string, string> data = new Dictionary<string, string>();
                    string msg = $"Началась обработка производственного задания." +
                        $"{Environment.NewLine}Количество поддонов, которое осталось распечатать: {countPallet}." +
                        $"{Environment.NewLine}Количество ярлыков, которое сейчас будет распечатано: {countPallet - 1}.";
                    data.Add("MESSAGE", msg);
                    SendLog(data, tableDirectory, primaryKeyValue);
                }

                for (int i = 0; i < countPallet - 1; i++)
                {
                    CreateOnePallet(productionTaskId.ToInt(), productId.ToInt(), productIdk1.ToInt(), productQuantityOnPallet.ToInt());
                }
            }
            else
            {
                string msg = "Нет поддонов, для которых сейчас необходимо распечатать ярлыки.";
                int status = 1;
                var d = new StackerScanedLableInfo(msg, status);
                d.WindowMaxSizeFlag = false;
                d.ShowAndAutoClose(1);
            }
        }

        /// <summary>
        /// (Старый механизм)
        /// Принудительно начинает обработку текущего производственного задания
        /// </summary>
        public void ManuallyStart()
        {
            if (PrintPalletByStackerDataFlag)
            {
                var msg = $"Интерфейс печатает ярлыки по факту выхода продукции. В текущей конфигурации нельзя запускать вручную обработку производственного задания.";
                var d = new DialogWindow($"{msg}", "Ручной запуск обработки задания", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
            else
            {
                if (!CurrentProductionTaskProcessedFlag)
                {
                    ManuallyStartFlag = true;
                }
                else
                {
                    var msg = $"Текущее задание уже обработано. Все нужные ярлыки уже были созданы." +
                        $"{Environment.NewLine}Для повторной печати уже созданных ярлыков воспользуйтесь Ручной печатью." +
                        $"{Environment.NewLine}Если необходимо принудительно повторить обработку задания, перезапустите программу и повторите операцию.";
                    var d = new DialogWindow($"{msg}", "Ручной запуск обработки задания", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }

        #endregion

        /// <summary>
        /// Получаем количество уже созданных ярлыков
        /// </summary>
        public int GetCountCreatedPallet(string productionTaskId, string productId)
        {
            int countCreatedPallet = 0;

            var p = new Dictionary<string, string>();
            p.Add("PRODUCTION_TASK_ID", productionTaskId);
            p.Add("PRODUCT_ID2", productId);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatingLabel");
            q.Request.SetParam("Action", "GetPalletCount");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
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
                        countCreatedPallet = ds.Items.First().CheckGet("PALLET_COUNT").ToInt();
                    }
                }
            }
            else
            {
                string msg = "При получении информации по количеству уже созданных поддонов произошла ошибка. Пожалуйста, сообщите о проблеме.";
                int status = 1;
                var d = new StackerScanedLableInfo(msg, status);
                d.WindowMaxSizeFlag = false;
                d.ShowAndAutoClose(1);

                q.SilentErrorProcess = true;
                q.ProcessError();
            }

            return countCreatedPallet;
        }

        /// <summary>
        /// Заполняет количество поддонов по заданию расчитанным количеством поддонов
        /// </summary>
        public void SetCalculationPalletCount(int countPallet, string productionTaskId, string productId)
        {
            if (countPallet > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("PALLET_COUNT", countPallet.ToString());
                p.Add("PRODUCTION_TASK_ID", productionTaskId);
                p.Add("PRODUCT_ID2", productId);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "CorrugatingLabel");
                q.Request.SetParam("Action", "SavePalletCount");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 145)
                {
                    string msg = q.Answer.Error.Message;
                    int status = 1;
                    var d = new StackerScanedLableInfo(msg, status);
                    d.WindowMaxSizeFlag = false;
                    d.ShowAndAutoClose(1);

                    q.SilentErrorProcess = true;
                    q.ProcessError();
                }
            }
            else
            {
                string msg = "Нет поддонов, которые необходимо обработать.";
                int status = 1;
                var d = new StackerScanedLableInfo(msg, status);
                d.WindowMaxSizeFlag = false;
                d.ShowAndAutoClose(1);
            }
        }

        /// <summary>
        /// Создание одного поддона
        /// </summary>
        /// <param name="productionTaskId"></param>
        /// <param name="productId"></param>
        /// <param name="productIdk1"></param>
        /// <param name="quantity"></param>
        /// <param name="corrugatingStackerId"></param>
        /// <returns></returns>
        public bool CreateOnePallet(int productionTaskId, int productId, int productIdk1, int quantity, string corrugatingStackerId = "", int palletNumber = 0)
        {
            SetSplash(true, "Печать ярлыка");

            bool queryResult = false;

            var p = new Dictionary<string, string>();
            p.Add("ID_PZ", productionTaskId.ToString());
            p.Add("ID2", productId.ToString());
            p.Add("KOL", quantity.ToString());
            p.Add("ID_ST", CorrugatingMachineId.ToString());

            if (!string.IsNullOrEmpty(corrugatingStackerId))
            {
                p.Add("PCSD_ID", corrugatingStackerId);
            }

            if (palletNumber > 0)
            {
                p.Add("NUM_PALLET", palletNumber.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatingLabel");
            q.Request.SetParam("Action", "CreatePallet");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
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
                        queryResult = true;

                        string productionTaskNumber = ds.Items.First().CheckGet("PRODUCTION_TASK_NUMBER");
                        int createdPalletNumber = ds.Items.First().CheckGet("NUM").ToInt();
                        int palletId = ds.Items.First().CheckGet("ID_PODDON").ToInt();
                        string corrugatingMachineId = ds.Items.First().CheckGet("ID_ST");
                        int tovarSubFlag = ds.Items.First().CheckGet("TOVAR_SUB_FLAG").ToInt();
                        if (tovarSubFlag > 0)
                        {
                            productIdk1 = 4;
                        }

                        LabelReport2 report = new LabelReport2();
                        report.PrintLabel(productionTaskId.ToString(), createdPalletNumber.ToString(), productIdk1.ToString());

                        {
                            string tableDirectory = $"{productionTaskId}_{productId}";
                            string primaryKeyValue = $"{productionTaskId}~{productId}_{createdPalletNumber}";
                            Dictionary<string, string> data = new Dictionary<string, string>();
                            string msg = $"Создан новый ярлык по заданию." +
                                $"{Environment.NewLine}Номер поддона: {createdPalletNumber}." +
                                $"{Environment.NewLine}Количество на поддоне: {quantity}." +
                                $"{Environment.NewLine}Ид поддона: {palletId}." +
                                $"{Environment.NewLine}Ид станка: {corrugatingMachineId}." +
                                $"{Environment.NewLine}Ид категории товара: {productIdk1}.";
                            data.Add("MESSAGE", msg);
                            SendLog(data, tableDirectory, primaryKeyValue);

                            msg = $"Создан новый ярлык." +
                                $"{Environment.NewLine}Номер поддона: {createdPalletNumber}." +
                                $"{Environment.NewLine}Количество на поддоне: {quantity}." +
                                $"{Environment.NewLine}Ид поддона: {palletId}.";
                            WriteWorkLog(msg, productionTaskNumber);
                        }
                    }
                }
            }
            else if (q.Answer.Status == 145)
            {
                string msg = q.Answer.Error.Message;
                int status = 1;
                var d = new StackerScanedLableInfo(msg, status);
                d.WindowMaxSizeFlag = true;
                d.ShowAndAutoClose(1);

                q.SilentErrorProcess = true;
                q.ProcessError();
            }
            else
            {
                q.SilentErrorProcess = true;
                q.ProcessError();
            }

            SetSplash(false);

            return queryResult;
        }

        /// <summary>
        /// Запись информации по работе стекера в текстовое поле
        /// </summary>
        /// <param name="message"></param>
        public void WriteWorkLog(string message, string messageHeader = "")
        {
            if (!string.IsNullOrEmpty(messageHeader))
            {
                messageHeader = $"---------[{messageHeader}]---------{Environment.NewLine}";
            }

            LogTextBox.Text = $"{LogTextBox.Text}" +
                $"-----{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}-----{Environment.NewLine}" +
                $"{messageHeader}" +
                $"{message}{Environment.NewLine}{Environment.NewLine}";

            if (LogTextBox.Text.Length > LogTextBoxMaxLength)
            {
                LogTextBox.Text = LogTextBox.Text.Substring(LogTextBox.Text.Length - LogTextBoxMaxLength);
            }

            LogTextBox.ScrollToEnd();
        }

        /// <summary>
        /// Отправляем данные для сохранения в лог файле
        /// </summary>
        /// <param name="data"></param>
        /// <param name="tableName"></param>
        /// <param name="tableDirectory"></param>
        /// <param name="primaryKey"></param>
        /// <param name="primaryKeyValue"></param>
        public async void SendLog(Dictionary<string, string> data, string tableDirectory, string primaryKeyValue)
        {
            if (data != null && tableDirectory != null && primaryKeyValue != null && LogTableName != null && LogPrimaryKey != null)
            {
                var jsonString = JsonConvert.SerializeObject(data);
                if (jsonString != null)
                {
                    var p = new Dictionary<string, string>();

                    p.Add("ITEMS", jsonString);
                    // 1=global,2=local,3=net
                    p.Add("STORAGE_TYPE", "3");
                    p.Add("TABLE_NAME", LogTableName);
                    p.Add("TABLE_DIRECTORY", tableDirectory);
                    p.Add("PRIMARY_KEY", LogPrimaryKey);
                    p.Add("PRIMARY_KEY_VALUE", primaryKeyValue);

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Service");
                    q.Request.SetParam("Object", "LiteBase");
                    q.Request.SetParam("Action", "SaveData");
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
                    q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q.DoQuery();
                    });

                    if (q.Answer.Status == 0)
                    {
                    }
                    else
                    {
                        q.SilentErrorProcess = true;
                        q.ProcessError();
                    }
                }
            }
        }

        /// <summary>
        /// Установка фокуса на поле ввода штрихкода
        /// </summary>
        public void SetFocus()
        {
            if (Central.MainWindow != null && this.IsVisible && Central.MainWindow.IsActive)
            {
                if (BarCodeTextBox != null)
                {
                    if (!BarCodeTextBox.IsFocused)
                    {
                        BarCodeTextBox.Focus();
                    }
                }
            }
        }

        public void RunAutoUpdateTimer()
        {
            if (AutoUpdateTimerInterval != 0)
            {
                if (AutoUpdateTimer == null)
                {
                    AutoUpdateTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, AutoUpdateTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", AutoUpdateTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("Stacker_RunAutoUpdateTimer", row);
                    }

                    AutoUpdateTimer.Tick += (s, e) =>
                    {
                        GetData();
                        SetFocus();
                    };
                }

                if (AutoUpdateTimer.IsEnabled)
                {
                    AutoUpdateTimer.Stop();
                }

                AutoUpdateTimer.Start();
            }
        }

        public void RunComplectationWarningTimer()
        {
            if (ComplectationWarningTimerInterval != 0)
            {
                if (ComplectationWarningTimer == null)
                {
                    ComplectationWarningTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, ComplectationWarningTimerInterval)
                    };

                    {
                        var row = new Dictionary<string, string>();
                        row.CheckAdd("TIMEOUT", ComplectationWarningTimerInterval.ToString());
                        row.CheckAdd("DESCRIPTION", "");
                        Central.Stat.TimerAdd("Stacker_RunComplectationWarningTimer", row);
                    }

                    ComplectationWarningTimer.Tick += (s, e) =>
                    {
                        GetComplectationWarningData();
                        SetFocus();
                    };
                }

                if (ComplectationWarningTimer.IsEnabled)
                {
                    ComplectationWarningTimer.Stop();
                }

                ComplectationWarningTimer.Start();
            }
        }

        /// <summary>
        /// обработчик системы навигации по URL
        /// </summary>
        public void ProcessNavigation()
        {
            //параметры запуска
            var p = Central.Navigator.Address.Params;

            var machineId = p.CheckGet("machine_id").ToInt();
            if (machineId > 0)
            {
                if (machineId.ContainsIn(2, 21, 22))
                {
                    CorrugatingMachineId = machineId;
                }
            }

            var stackerNumber = p.CheckGet("stacker_number").ToInt();
            if (stackerNumber > 0)
            {
                StackerNumber = stackerNumber;
            }

            if (TaskForm != null)
            {
                string corrugatorMachineNumber = "";
                switch (CorrugatingMachineId)
                {
                    case 2:
                        corrugatorMachineNumber = "ГА1";
                        break;

                    case 21:
                        corrugatorMachineNumber = "ГА2";
                        break;

                    case 22:
                        corrugatorMachineNumber = "ГА3";
                        break;

                    default:
                        break;
                }

                TaskForm.SetValueByPath("CORRUGATOR_MACHINE_NUMBER", corrugatorMachineNumber);
                TaskForm.SetValueByPath("STACKER_NUMBER", $"Ст{stackerNumber}");
            }

            // Если задан параметр read_only=1, то мы открываем форму только для просмотра данных по ПЗ и сканирования ярлыка не зависимо от данных конфиг файла
            // Это сделано для того, чтобы при открытии формы через интерфейс ручной печати ярлыков мы не запускали автоматическую печать ярлыков
            if (p.CheckGet("read_only").ToInt() == 1)
            {
                StackerWorkFlag = 0;
            }
            else
            {
                StackerWorkFlag = Central.Config.TempStackerFlag;
            }

        }

        /// <summary>
        /// Обработчик ввода
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;

            switch (e.Key)
            {
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.F5:
                    GetData();
                    e.Handled = true;
                    break;

                case Key.Down:
                case Key.Enter:

                    // Если в данный момент не выполняется запрос, то можем обрабатывать новый введённый штрихкод
                    if (!QueryInProgress)
                    {
                        var code = Central.WM.GetScannerInput();
                        if (!string.IsNullOrEmpty(code) && code.Length >= 13 && code.Contains("777"))
                        {
                            TaskForm.SetValueByPath("CURRENT_BARCODE", code);
                        }

                        if (!string.IsNullOrEmpty(TaskForm.GetValueByPath("CURRENT_BARCODE")) && TaskForm.GetValueByPath("CURRENT_BARCODE").Length >= 13 && TaskForm.GetValueByPath("CURRENT_BARCODE").Contains("777"))
                        {
                            MovePallet();
                        }
                        else
                        {
                            TaskForm.SetValueByPath("CURRENT_BARCODE", "");
                        }
                    }
                    // Если в данный момент выполняется запрос, то не даём обрабатывать новый введённый штрихкод
                    else
                    {
                        TaskForm.SetValueByPath("CURRENT_BARCODE", "");
                    }

                    break;
            }
        }

        public void SetSplash(bool inProgressFlag, string msg = "Загрузка")
        {
            BarCodeTextBox.IsReadOnly = inProgressFlag;
            QueryInProgress = inProgressFlag;
            SplashControl.Visible = inProgressFlag;

            SplashControl.Message = msg;
        }

        /// <summary>
        /// Запрос на перемещение поддона
        /// Вызывается в моемент сканирования ярлыка
        /// </summary>
        /// <param name="code"></param>
        public async void MovePallet()
        {
            SetSplash(true, "Обработка поддона");

            var str = TaskForm.GetValueByPath("CURRENT_BARCODE").Trim();
            TaskForm.SetValueByPath("CURRENT_BARCODE", "");

            if (str.Length == 13 && int.TryParse(str.Substring(3, 9), out var palletId))
            {
                WriteWorkLog("Сканирование ярлыка...", $"{palletId}");

                if (palletId.ToString() == "777777777")
                {
                    string productionTaskId = TailForm.GetValueByPath("TAIL_PRODUCTION_TASK_ID");
                    string palletNumber = TailForm.GetValueByPath("TAIL_PALLET_NUMBER");

                    if (!string.IsNullOrEmpty(productionTaskId) && !string.IsNullOrEmpty(palletNumber))
                    {
                        var p = new Dictionary<string, string>();
                        p.Add("PRODUCTION_TASK_ID", productionTaskId);
                        p.Add("PALLET_NUMBER", palletNumber);
                        p.Add("SKIP_TAIL_FLAG", "1");

                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "Production");
                        q.Request.SetParam("Object", "CorrugatingLabel");
                        q.Request.SetParam("Action", "UpdateSkipScanPalletFlag");
                        q.Request.SetParams(p);

                        q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
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
                                if (ds != null && ds.Items != null && ds.Items.Count > 0)
                                {
                                    if (ds.Items.First().CheckGet("PALLET_NUMBER").ToInt() > 0)
                                    {
                                        string msg = "Поддон успешно отставлен";
                                        int status = 2;
                                        var d = new StackerScanedLableInfo(msg, status);
                                        d.WindowMaxSizeFlag = true;
                                        d.ShowAndAutoClose(1);

                                        WriteWorkLog(msg, $"{productionTaskId}/{palletNumber}");
                                    }
                                }
                            }
                        }
                        else if (q.Answer.Status == 145)
                        {
                            string msg = q.Answer.Error.Message;
                            int status = 1;
                            var d = new StackerScanedLableInfo(msg, status);
                            d.WindowMaxSizeFlag = true;
                            d.ShowAndAutoClose(1);

                            WriteWorkLog(msg, $"{productionTaskId}/{palletNumber}");

                            q.SilentErrorProcess = true;
                            q.ProcessError();
                        }
                        else
                        {
                            q.SilentErrorProcess = true;
                            q.ProcessError();
                        }
                    }
                    else
                    {
                        string msg = $"Невозможно отставить поддон!{Environment.NewLine}Не указано ПЗ или номер поддона.";
                        int status = 1;
                        var d = new StackerScanedLableInfo($"{msg}", status);
                        d.WindowMaxSizeFlag = true;
                        d.ShowAndAutoClose(1);

                        WriteWorkLog(msg, $"{productionTaskId}/{palletNumber}");
                    }
                }
                else
                {
                    var p = new Dictionary<string, string>();
                    p.Add("PALLET_ID", palletId.ToString());
                    p.Add("INTERFACE_ID", InterfaceId);
                    p.Add("QUERY_IN_PROGRESS", QueryInProgress.ToString());
                    p.Add("TEXT_BOX_IS_READ_ONLY", BarCodeTextBox.IsReadOnly.ToString());

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Production");
                    q.Request.SetParam("Object", "CorrugatingLabel");
                    q.Request.SetParam("Action", "MovePallet"); 
                    q.Request.SetParams(p);

                    q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
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
                            if (ds != null && ds.Items != null && ds.Items.Count > 0)
                            {
                                if (ds.Items.First().CheckGet("ID_PODDON").ToInt() > 0)
                                {
                                    string msg = "Ярлык успешно отсканирован";
                                    int status = 2;
                                    var d = new StackerScanedLableInfo(msg, status, palletId.ToString());
                                    d.WindowMaxSizeFlag = true;
                                    d.ShowAndAutoClose(1);

                                    WriteWorkLog(msg, $"{palletId}");
                                }
                            }
                        }
                    }
                    else if (q.Answer.Status == 145)
                    {
                        string msg = q.Answer.Error.Message;
                        int status = 1;
                        var d = new StackerScanedLableInfo(msg, status, palletId.ToString());
                        d.WindowMaxSizeFlag = true;
                        d.ShowAndAutoClose(1);

                        WriteWorkLog(msg, $"{palletId}");

                        q.SilentErrorProcess = true;
                        q.ProcessError();
                    }
                    else
                    {
                        q.SilentErrorProcess = true;
                        q.ProcessError();
                    }
                }
            }

            SetSplash(false);
            SetFocus();
        }

        /// <summary>
        /// Установка настроек для принтера
        /// </summary>
        public void SetPrintSettings()
        {
            LabelReport2.SetPrintingProfile();
        }

        /// <summary>
        /// Определение размера шрифта в комментариях, при котором не будет превышения заданного количества строк
        /// </summary>
        private double CommentGetFontSize(string text, double width, double height)
        {

            int result = 0;
            TextBox tmp = new TextBox();
            tmp.Text = text;
            var fontSize = 32.0;
            var fontFamily = new FontFamily("Segue UI");


            bool flag = true;
            do
            {
                tmp.FontSize = fontSize;
                tmp.Width = width;
                var height2 = GetBlockHeight(text, width-30, fontSize, fontFamily);
                if (height2 > height)
                {
                    fontSize--;
                }
                else
                {
                    flag = false;
                }
            } while (flag && fontSize > 14);

            return fontSize;
        }

        public static double GetBlockHeight(string text, double width, double fontSize, FontFamily fontFamily)
        {
            Typeface typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            string[] words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            double currentLineWidth = 0;
            double lineHeight = 0;
            int numberOfLines = 1;

            foreach (var word in words)
            {
                var formattedText = new FormattedText(
                    word,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    Brushes.Black,
                    VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip);

                if (currentLineWidth + formattedText.Width > width)
                {
                    numberOfLines++;
                    currentLineWidth = formattedText.Width;
                }
                else
                {
                    currentLineWidth += formattedText.Width;
                }

                lineHeight = formattedText.Height;
            }

            double totalHeight = numberOfLines * lineHeight;

            return totalHeight;
        }

        /// <summary>
        /// Отображение логов автоматической печати ярлыков по выбранному производственному заданию
        /// </summary>
        public async void ShowLog()
        {
            string productionTaskNumber = TaskForm.GetValueByPath("CURRENT_PRODUCTION_TASK_NUMBER");
            int productionTaskId = TaskForm.GetValueByPath("CURRENT_PRODUCTION_TASK_ID").ToInt();
            int productId = TaskForm.GetValueByPath("CURRENT_ID2").ToInt();
            string tableDirectory = $"{productionTaskId}_{productId}";

            var p = new Dictionary<string, string>();
            // 1=global,2=local,3=net
            p.Add("STORAGE_TYPE", "3");
            p.Add("TABLE_NAME", LogTableName);
            p.Add("TABLE_DIRECTORY", tableDirectory);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Service");
            q.Request.SetParam("Object", "LiteBase");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestTimeoutDefault;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                string logMsg = "";

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, LogTableName);
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        List<Dictionary<string, string>> logList = new List<Dictionary<string, string>>();
                        logList = ds.Items.OrderBy(x => x.CheckGet("ON_DATE").ToDateTime()).ToList();

                        logMsg = $"Задание: {productionTaskNumber}. Стекер: {StackerNumber}.";
                        foreach (var logItem in logList)
                        {
                            logMsg = $"{logMsg}" +
                                $"{Environment.NewLine}-----{logItem.CheckGet("ON_DATE")}-----" +
                                $"{Environment.NewLine}{logItem.CheckGet("MESSAGE")}";
                        }

                        var d = new DialogWindow($"{logMsg}", "История автоматической печати ярлыков", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }

                if (string.IsNullOrEmpty(logMsg))
                {
                    var d = new DialogWindow($"Не найдена история по выбранной заявке.", "История автоматической печати ярлыков", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                q.SilentErrorProcess = true;
                q.ProcessError();
            }
        }

        /// <summary>
        /// Информация по текущему состоянию программы (версия, БД и т.д.)
        /// </summary>
        public void ShowInfo()
        {
            var informationMessage = Central.MakeInfoString();
            var d = new DialogWindow($"{informationMessage}", "Отладочная информация", "", DialogWindowButtons.OK);
            d.ShowDialog();
        }

        /// <summary>
        /// Покажет ярлык, который соответсвтует поддону из правой половины формы
        /// </summary>
        public void ShowLabel()
        {
            if (TailForm != null)
            {
                string productionTaskId = TailForm.GetValueByPath("TAIL_PRODUCTION_TASK_ID");
                string palletNumber = TailForm.GetValueByPath("TAIL_PALLET_NUMBER");
                string productIdk1 = "";
                // Если продукция на поддоне - Перестил, то должны печатать ярлык заготовки
                if (TailForm.GetValueByPath("TAIL_SUBPRODUCT_FLAG").ToInt() > 0)
                {
                    productIdk1 = "4";
                }
                else
                {
                    productIdk1 = TailForm.GetValueByPath("TAIL_IDK1");
                }

                if (!string.IsNullOrEmpty(productionTaskId) && !string.IsNullOrEmpty(palletNumber) && !string.IsNullOrEmpty(productIdk1))
                {
                    LabelReport2 report = new LabelReport2();
                    report.ShowLabel(productionTaskId, palletNumber, productIdk1);
                }
                else
                {
                    var msg = "Не найдены данные для отображения ярлыка.";
                    var d = new DialogWindow($"{msg}", "Просмотр ярлыка", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }

        /// <summary>
        /// Открывает документацию по интерфейсу
        /// </summary>
        private void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/gofroproduction/steker");
            //Central.ShowHelp("/doc/l-pack-erp/production/stacker_cm/stacker");
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

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
            if (AutoUpdateTimer != null)
            {
                AutoUpdateTimer.Stop();
            }

            if (ComplectationWarningTimer != null)
            {
                ComplectationWarningTimer.Stop();
            }

            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = FrameName,
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PrintSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SetPrintSettings();
        }

        private void LogButton_Click(object sender, RoutedEventArgs e)
        {
            ShowLog();
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Main",
                ReceiverName = "MainWindow",
                SenderName = "Navigator",
                Action = "Restart",
                Message = "",
            });
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Main",
                ReceiverName = "MainWindow",
                SenderName = "Navigator",
                Action = "Exit",
                Message = "",
            });
        }

        private void BurgerButton_Click(object sender, RoutedEventArgs e)
        {
            BurgerMenu.IsOpen = true;
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            ShowInfo();
        }

        private void ShowLabelButton_Click(object sender, RoutedEventArgs e)
        {
            ShowLabel();
        }

        private void ManuallyStartButton_Click(object sender, RoutedEventArgs e)
        {
            ManuallyStart();
        }

        private void ShowCurrentTaskButton_Click(object sender, RoutedEventArgs e)
        {
            ShowCurrentTask();
        }
    }
}
