using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Client.Interfaces.Production

{
    /// <summary>
    /// Ручной раскрой.
    /// Создает кастомное производственное задание для ГА.
    /// Также используется для редактирования существуюущих заданий.
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>2</version>
    /// <released>2021-11-15</released>
    public partial class ProductionTask : UserControl
    {
        public ProductionTask()
        {
            InitializeComponent();   

            //максимальная обрезь, % [0-100]
            TrimPercentageMax=100;
            //большая обрезь, % [0-100]
            TrimPercentageBig=10;

            BackTabName="";
            Mode=0;
            ProductionTaskId=0;
            ProductionTaskOldId=0;
            ChainUpdate = true;
            ChangeQuantity=true;
            CalcQuantity=false;
            Debug = true;            
            Title.Text="";
            SourceChanged=false;      
            SourceChangedChecking=false;
            SelectedCardboardId=0;
            MasterRight = false;

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            ProcessPermissions();
            //регистрация обработчика клавиатуры
            //PreviewKeyDown += ProcessKeyboard;

            ActiveStackerGroup=0;
            ActiveStacker=0;
            ManualComposition=false;
            Report="";
            Stacker1=new Dictionary<string, string>(); 
            Stacker2=new Dictionary<string, string>();
            Stacker3=new Dictionary<string, string>();
            StackerPrimary=0;
            StackerSecondary=0;
            RefSourcesLoaded=false;
            ProfileParams = new Dictionary<int, Dictionary<string, string>>();
            MinDurableWidth = 300;

            Stacker1ApplicationDS = new ListDataSet();
            Stacker1ApplicationDS.Init();
            Stacker2ApplicationDS = new ListDataSet();
            Stacker2ApplicationDS.Init();
            Stacker3ApplicationDS = new ListDataSet();
            Stacker3ApplicationDS.Init();

            InitForm();
            //LoadRef();
            //SetDefaults();            
            //CheckMode();     

            if(Central.DebugMode)
            {
                CalculateBUtton.Visibility      =Visibility.Visible;
                LoadRefButton.Visibility        =Visibility.Visible;  

                Layer1PaperId.Visibility        =Visibility.Visible;  
                Layer2PaperId.Visibility        =Visibility.Visible;  
                Layer3PaperId.Visibility        =Visibility.Visible;  
                Layer4PaperId.Visibility        =Visibility.Visible;  
                Layer5PaperId.Visibility        =Visibility.Visible;  
            }
            else
            {
                CalculateBUtton.Visibility      =Visibility.Collapsed;
                LoadRefButton.Visibility        =Visibility.Collapsed;  
            }
            SourceChangedLabel.Visibility   =Visibility.Collapsed;
            ErrorCodes.Visibility=Visibility.Collapsed;
        }

        public bool Debug { get; set; }
        
        /// <summary>
        /// процессор формы
        /// </summary>
        private FormHelper Form { get; set; }
        /// <summary>
        /// датасет с марками картона
        /// для селектора "картон"
        /// получается в GetSources
        /// </summary>
        private ListDataSet CardboardDS { get; set; }
         /// <summary>
        /// датасет с бумагами
        /// для селектора "слой"
        /// получается в GetSources
        /// </summary>
        private ListDataSet PaperListDS { get; set; }
        /// <summary>
        /// строчка с кратким репортом по результатам работы
        /// </summary>
        private string Report { get;set; }
        /// <summary>
        /// данные стекера (as is, server)
        /// </summary>
        private Dictionary<string,string> Stacker1 { get; set; }
        /// <summary>
        /// данные стекера (as is, server)
        /// </summary>
        private Dictionary<string,string> Stacker2 { get; set; }
        /// <summary>
        /// данные стекера (as is, server)
        /// </summary>
        private Dictionary<string,string> Stacker3 { get; set; }
        /// <summary>
        /// Данные по существующим заявкам изделия на стекере 1
        /// </summary>
        private ListDataSet Stacker1ApplicationDS { get; set; }
        /// <summary>
        /// Данные по существующим заявкам изделия на стекере 2
        /// </summary>
        private ListDataSet Stacker2ApplicationDS { get; set; }
        /// <summary>
        /// Данные по существующим заявкам изделия на стекере 3
        /// </summary>
        private ListDataSet Stacker3ApplicationDS { get; set; }

        /// <summary>
        /// ID главного стекера
        /// </summary>
        private int StackerPrimary { get; set; }
        /// <summary>
        /// ID дополнительного стекера
        /// </summary>
        private int StackerSecondary { get; set; }
        /// <summary>
        /// флаг активности блока сырьевой композиции
        /// </summary>
        private bool ManualComposition { get; set; }
        /// <summary>
        /// режим работы,
        /// 0--создание, 1--редактирование частичное, 2--просмотр, 3--редактирование полное (пересоздание), 4--перевыпуск
        /// </summary>
        private int Mode { get; set; }
        /// <summary>
        /// id производственного задания
        /// (для процедур редактирования ПЗ)
        /// </summary>
        private int ProductionTaskId { get; set; }
        private int ProductionTaskOldId { get; set; }
        private DateTime ProductionTaskCreated { get; set; }

        /// <summary>
        /// механизм обновления связанных полей:
        /// картон-материал-вес
        /// используется только при редактировании, при загрузке данных
        /// отключается
        /// </summary>
        private bool ChainUpdate { get; set; }
        /// <summary>
        /// подстановка количества из заявки
        /// блокируется при смене главного стекера или ручной смене количества на любом стекере
        /// </summary>
        private bool ChangeQuantity { get; set; }
        /// <summary>
        /// когда сырьевая композиция изменена, флаг поднимается
        /// </summary>
        private bool SourceChanged { get; set; }
        /// <summary>
        /// проверка, что сырье изменилось проводится, только когда флаг поднят,
        /// а он поднимается, когда все данные прогружены в форму
        /// </summary>
        private bool SourceChangedChecking { get; set; }
        /// <summary>
        /// id выбранного картона
        /// </summary>
        private int SelectedCardboardId { get;set;}
       
        /// <summary>
        /// данные заготовок на стекерах
        /// оригинальные данные из Position (позиция для раскроя)
        /// </summary>
        private  Dictionary<int,Dictionary<string, string>> Stackers { get;set;}

        /// <summary>
        /// обратный просчет количества
        /// с главного на второй стекер
        /// этот механизм блокируется при открытии формы на редактирование данных
        /// до отработки всех алгоритмов просчета
        /// Без этого флага есть такой баг:
        /// при загрузке данных на редактирование, срабатывает Calculate
        /// и изменяет количество в соотв. с расчетом, а оно там не совпадает
        /// с указанным в пз в связи с округлением.
        /// </summary>
        private bool CalcQuantity { get; set; }

        /// <summary>
        /// максимальная обрезь, проценты
        /// если расчетная обрезь больше этого значения, сохранение блокируется
        /// </summary>
        private int TrimPercentageMax { get; set; }
        /// <summary>
        /// большая обрезь, проценты
        /// если расчетная обрезь больше этого значения, выдается предупреждение
        /// </summary>
        private int TrimPercentageBig { get; set; }

        /// <summary>
        /// Признак наличия в картоне слабого гофрослоя
        /// </summary>
        private bool WeakCorrugatedLayerFlag;
        /// <summary>
        /// Минимальная ширина потока, при котором гофрослой остается устойчивым. Зависит от плотности гофрослоя
        /// </summary>
        private int MinDurableWidth;
        /// <summary>
        /// Идентификатор производственной площадки, на которой выполняется ПЗГА
        /// </summary>
        public int FactoryId;
        /// <summary>
        /// Наличие спецправ для работы с формой
        /// </summary>
        private bool MasterRight;

        /// <summary>
        /// Дополнительные свойства перевыгона
        /// </summary>
        private Dictionary<string,string> ReworkParams { get; set; }
        
        /// <summary>
        /// Данные по профилям, включая коэффициенты гофрирования для каждого профиля
        /// </summary>
        public Dictionary<int, Dictionary<string, string>> ProfileParams { get; set; }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        public void ProcessPermissions(string roleCode="")
        {
            var mode = Central.Navigator.GetRoleLevel("[erp]production_task_cm");
            MasterRight = mode == Role.AccessMode.Special;
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        private void ProcessMessages(ItemMessage m)
        {
            //Group ProductionTask
            if (m.ReceiverGroup.IndexOf("ProductionTask") > -1)
            {
                if(m.ReceiverName.IndexOf("CuttingManualView")>-1)
                {
                    switch (m.Action)
                    {
                        case "SelectedBlank":
                        {
                            if(m.ContextObject!=null)
                            {
                                var v=(Dictionary<string,string>)m.ContextObject;
                                if (v.ContainsKey("PRIMARY_ID_PZ"))
                                {
                                    Mode = 4;
                                    ReworkParams = new Dictionary<string, string>();
                                    ReworkParams.Add("PRIMARY_ID_PZ", v["PRIMARY_ID_PZ"]);
                                    ReworkParams.Add("REWORK_REASON", v.CheckGet("REWORK_REASON"));
                                    ReworkParams.Add("REWORK_COMMENT", v.CheckGet("REWORK_COMMENT"));
                                    ReworkParams.Add("PRIMARY_ID2", v.CheckGet("PRIMARY_ID2"));
                                    ReworkParams.Add("NUM", v.CheckGet("NUM"));
                                }
                                
                                // При смене заготовки очищаем комментарий
                                if(!Comment.Text.IsNullOrEmpty())
                                {
                                    Comment.Text = "";
                                }

                                SetStackerBlank(v);
                                GetApplication(v);
                                ChangeActiveStacker();
                                EnableCalcQuantity();
                                Calculate();
                            }                            
                        }
                        break;

                        case "SelectedPaper":
                        {
                            if(m.ContextObject!=null)
                            {
                                var v=(Dictionary<string,string>)m.ContextObject;
                                SetLayerPaper(v);
                            }                            
                        }
                        break;

                        case "SelectedApplication":
                            if (m.ContextObject != null)
                            {
                                var v = (Dictionary<string, string>)m.ContextObject;
                                SetApplication(v);
                            }
                            break;
                    }
                }

            }
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e=Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
                
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// отображение статьи в справочной системе
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/creating_tasks/cutting_manual");
        }

        public void ShowHelpErrors()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/production_tasks/list/machine_errors");
        }
       
        

        /// <summary>
        /// деструктор интерфейса
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о фрейма
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup="ProductionTaskCreating",
                ReceiverName = "",
                SenderName = "CuttingAutoView",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            //возврат к предыдущему интерфейсу (если есть цепь навигации)
            GoBack();
        }

        /// <summary>
        /// инициализация формы
        /// </summary>
        public void InitForm()
        {
            Form=new FormHelper();
            var fields=new List<FormHelperField>()
            {                
                new FormHelperField()
                { 
                    Path="ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TaskId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },                    
                },
                new FormHelperField()
                { 
                    Path="NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Number,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },                    
                },
                new FormHelperField()
                { 
                    Path="VIRTUAL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Virtual,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },                    
                },
                new FormHelperField()
                { 
                    Path="FORMAT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=FormatList,
                    ControlType="SelectBox",
                    Default="0",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },                    
                },
                new FormHelperField()
                { 
                    Path="TRIM_PERCENTAGE",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=TrimPercentage,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },  
                                
                new FormHelperField()
                { 
                    Path="CUTTING_WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CuttingWidth,                    
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="CUTTING_LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CuttingLength,                    
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },                    
                },
                new FormHelperField()
                { 
                    Path="COMMENT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Comment,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                
                //стекер 1                
                new FormHelperField()
                { 
                    Path="_STACKER1_POSITION_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1PositionId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER1_BLANK_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1BlankId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER1_GOOD_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1GoodId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER1_BLANK",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker1Blank,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER1_CARDBOARD",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker1Cardboard,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
               
                new FormHelperField()
                { 
                    Path="STACKER1_CREASE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker1Crease,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER1_CREASE_NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker1CreaseNote,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER1_LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1Length,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER1_WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1Width,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER1_QUANTITY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1Quantity,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER1_QUANTITY_LIMIT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker1QuantityLimit,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER1_QUANTITY_LIMIT_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                { 
                    Path="STACKER1_PRODUCTS_FROM_BLANK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1ProductsFromBlank,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER1_QUANTITY_TO_CUT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1ToCut,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER1_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1StacksInPallet,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER1_QUANTITY_CALCULATED",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1QuantityCalculated,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER1_QUANTITY_LOWER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Options="zeroempty",
                    Control=Stacker1QuantityLower,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER1_QUANTITY_UPPER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Options="zeroempty",
                    Control=Stacker1QuantityUpper,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                { 
                    Path="STACKER1_THREADS",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1Threads,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER1_SAMPLE",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=Stacker1Sample,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER1_POSITION_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker1Application,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER1_POSITION_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker1ApplicationId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },


                //стекер 2                
                new FormHelperField()
                { 
                    Path="_STACKER2_POSITION_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2PositionId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER2_BLANK_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2BlankId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER2_GOOD_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2GoodId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER2_BLANK",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker2Blank,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER2_CARDBOARD",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker2Cardboard,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                
                new FormHelperField()
                { 
                    Path="STACKER2_CREASE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker2Crease,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER2_CREASE_NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker2CreaseNote,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },

                new FormHelperField()
                { 
                    Path="STACKER2_LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2Length,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER2_WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2Width,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER2_QUANTITY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2Quantity,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER2_QUANTITY_LIMIT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker2QuantityLimit,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER2_QUANTITY_LIMIT_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                { 
                    Path="STACKER2_PRODUCTS_FROM_BLANK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2ProductsFromBlank,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER2_QUANTITY_TO_CUT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2ToCut,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER2_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2StacksInPallet,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER2_QUANTITY_CALCULATED",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2QuantityCalculated,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER2_QUANTITY_LOWER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Options="zeroempty",
                    Control=Stacker2QuantityLower,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER2_QUANTITY_UPPER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Options="zeroempty",
                    Control=Stacker2QuantityUpper,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },

                new FormHelperField()
                { 
                    Path="STACKER2_THREADS",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2Threads,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER2_SAMPLE",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=Stacker2Sample,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER2_POSITION_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker2Application,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER2_POSITION_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker2ApplicationId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                //стекер 3
                new FormHelperField()
                { 
                    Path="_STACKER3_POSITION_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker3PositionId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER3_BLANK_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker3BlankId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER3_GOOD_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker3GoodId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER3_BLANK",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker3Blank,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER3_CARDBOARD",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker3Cardboard,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                
                new FormHelperField()
                { 
                    Path="STACKER3_CREASE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker3Crease,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER3_CREASE_NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker3CreaseNote,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER3_LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker3Length,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER3_WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker3Width,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER3_QUANTITY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker3Quantity,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER3_QUANTITY_LIMIT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker3QuantityLimit,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER3_QUANTITY_LIMIT_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                { 
                    Path="STACKER3_PRODUCTS_FROM_BLANK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker3ProductsFromBlank,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER3_QUANTITY_TO_CUT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker3ToCut,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER3_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker3StacksInPallet,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER3_QUANTITY_CALCULATED",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker3QuantityCalculated,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER3_QUANTITY_LOWER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Options="zeroempty",
                    Control=Stacker3QuantityLower,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER3_QUANTITY_UPPER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Options="zeroempty",
                    Control=Stacker3QuantityUpper,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },

                new FormHelperField()
                { 
                    Path="STACKER3_THREADS",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker3Threads,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="STACKER3_SAMPLE",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=Stacker3Sample,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER3_POSITION_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Stacker3Application,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKER3_POSITION_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Stacker3ApplicationId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },


                new FormHelperField()
                { 
                    Path="CARDBOARD_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CardboardList,
                    ControlType="SelectBox",
                    Default="0",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="CARDBOARD_QUALITY",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CardboardQuality,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="CARDBOARD_PROFILE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CardboardProfile,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                {
                    Path="CARDBOARD_PROFILE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CardboardProfileId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    AfterSet=(FormHelperField field, string value)=>{
                        var v=(bool)UseManualComposition.IsChecked;
                        CheckManualComposition(v);
                    }
                },
                new FormHelperField()
                {
                    Path="CARDBOARD_OUTER_COLOR_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CardboardColorId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    AfterSet=(FormHelperField field, string value)=>{
                        var v=(bool)UseManualComposition.IsChecked;
                        CheckManualComposition(v);
                    }
                },
                new FormHelperField()
                {
                    Path="TRIM",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TrimAbsolute,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                
                // Бумага по слоям

                new FormHelperField()
                { 
                    Path="LAYER1_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer1PaperId,
                    Default="0",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                    AfterSet=(FormHelperField field, string value)=>{
                        SetLayerPaperName(1,value.ToInt());
                    }
                },
                new FormHelperField()
                { 
                    Path="LAYER2_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer2PaperId,
                    Default="0",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                    AfterSet=(FormHelperField field, string value)=>{
                        SetLayerPaperName(2,value.ToInt());
                    }
                },
                new FormHelperField()
                { 
                    Path="LAYER3_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer3PaperId,
                    Default="0",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                    AfterSet=(FormHelperField field, string value)=>{
                        SetLayerPaperName(3,value.ToInt());
                    }
                },
                new FormHelperField()
                { 
                    Path="LAYER4_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer4PaperId,
                    Default="0",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                    AfterSet=(FormHelperField field, string value)=>{
                        SetLayerPaperName(4,value.ToInt());
                    }
                },
                new FormHelperField()
                { 
                    Path="LAYER5_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer5PaperId,
                    Default="0",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                    AfterSet=(FormHelperField field, string value)=>{
                        SetLayerPaperName(5,value.ToInt());
                    }
                },

                new FormHelperField()
                { 
                    Path="LAYER1_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Layer1Paper,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER2_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Layer2Paper,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER3_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Layer3Paper,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER4_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Layer4Paper,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER5_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Layer5Paper,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },

                new FormHelperField()
                {
                    Path="LAYER1_COLOR",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer1Color,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LAYER2_COLOR",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer2Color,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LAYER3_COLOR",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer3Color,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LAYER4_COLOR",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer4Color,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LAYER5_COLOR",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer5Color,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                { 
                    Path="LAYER1_TASK_WEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer1TaskWeight,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER2_TASK_WEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer2TaskWeight,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER3_TASK_WEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer3TaskWeight,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER4_TASK_WEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer4TaskWeight,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER5_TASK_WEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer5TaskWeight,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },

                
                new FormHelperField()
                { 
                    Path="LAYER1_RESIDUE_WEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer1ResidueWeight,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER2_RESIDUE_WEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer2ResidueWeight,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER3_RESIDUE_WEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer3ResidueWeight,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER4_RESIDUE_WEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer4ResidueWeight,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER5_RESIDUE_WEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer5ResidueWeight,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },

                new FormHelperField()
                {
                    Path="LAYER1_STOCK_ROLLS",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer1StockRolls,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LAYER2_STOCK_ROLLS",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer2StockRolls,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LAYER3_STOCK_ROLLS",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer3StockRolls,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LAYER4_STOCK_ROLLS",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer4StockRolls,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="LAYER5_STOCK_ROLLS",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer5StockRolls,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },


                new FormHelperField()
                { 
                    Path="LAYER1_PT_WEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer1PtWeight,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER2_PT_WEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer2PtWeight,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER3_PT_WEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer3PtWeight,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER4_PT_WEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer4PtWeight,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER5_PT_WEIGHT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Layer5PtWeight,
                    Options="zeroempty",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },


                new FormHelperField()
                { 
                    Path="LAYER1_GLUING",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=Layer1Gluing,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER2_GLUING",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=Layer2Gluing,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER3_GLUING",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=Layer3Gluing,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER4_GLUING",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=Layer4Gluing,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },
                new FormHelperField()
                { 
                    Path="LAYER5_GLUING",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=Layer5Gluing,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{                         
                    },
                },

                new FormHelperField()
                { 
                    Path="MACHINE_ERROR",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                { 
                    Path="MACHINE_ERROR_CODE",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=null,
                    ControlType="void",
                },
            };

            Form.SetFields(fields);
            Form.OnValidate=(bool valid, string message) =>
            {
                if(valid)
                {
                    FormStatus.Text="";
                }
                else
                {
                    FormStatus.Text="Не все поля заполнены верно";
                }
            };

            
            {
                FormatList.OnSelectItem=(Dictionary<string,string> selectedItem) => 
                {
                    bool result = false;
                    
                    if (ChainUpdate)
                    {
                        if (selectedItem!=null)
                        {
                             UpdateFormat();                        
                        }
                    }
                    result = true;
                    
                    return result;
                };
            }

            //материал слоев в блоке сырьевой композиции
            { 
                CardboardList.OnSelectItem=(Dictionary<string,string> selectedItem) => 
                {
                    bool result = false;

                    if (selectedItem.Count > 0 && ChainUpdate)
                    {
                        int id=selectedItem.CheckGet("ID").ToInt();                     
                        SelectedCardboardId=id;
                        CheckSourceChanging();
                        ShowSourceChanging();
                        LoadLayers(id);

                        foreach (var row in CardboardDS.Items)
                        {
                            if (row["ID"].ToInt() == SelectedCardboardId)
                            {
                                CardboardColorId.Text = row["OUTER_COLOR_ID"].ToInt().ToString();
                                break;
                            }
                        }

                        result = true;
                    }
                    return result;
                };
            }

        }

        private async void UpdateFormat()
        {
            LoadRef(false);
            
            await Task.Run(() =>
            {
                while(!RefSourcesLoaded)
                {
                    Task.Delay(100);
                }
            });

            EnableCalcQuantity();
            Calculate();       
        }

        /// <summary>
        /// пересчет веса сырья по всем слоям
        /// </summary>
        private bool UpdateLayersWeights()
        {
            bool result=true;

            var v=Form.GetValues();
            
            //проверим расход по каждому слою
            for(int i = 1; i <= 5; i++)
            {
                var paperId=v.CheckGet($"LAYER{i}_ID").ToInt();
                if(paperId!=0)
                {
                    var r=UpdateLayerWeights(paperId,i);
                    if(!r)
                    {
                        result=false;
                    }
                }
            }

            //просуммируем по схожим материалам
            var weights = new Dictionary<int,int>();
            for(int i = 1; i <= 5; i++)
            {
                var paperId=v.CheckGet($"LAYER{i}_ID").ToInt();
                if(paperId!=0)
                {
                    if(!weights.ContainsKey(paperId))
                    {
                        weights.Add(paperId,0);
                    }
                    weights[paperId]=weights[paperId]+v.CheckGet($"LAYER{i}_TASK_WEIGHT").ToInt();
                }
            }


            for(int i = 1; i <= 5; i++)
            {
                var control=Layer1TaskWeight;
                switch(i)
                {
                    case 1:
                        control=Layer1TaskWeight;
                        break;

                    case 2:
                        control=Layer2TaskWeight;
                        break;

                    case 3:
                        control=Layer3TaskWeight;
                        break;

                    case 4:
                        control=Layer4TaskWeight;
                        break;

                    case 5:
                        control=Layer5TaskWeight;
                        break;
                }

                var paperId=v.CheckGet($"LAYER{i}_ID").ToInt();
                if(paperId!=0)
                {
                    var taskWeight=v.CheckGet($"LAYER{i}_TASK_WEIGHT").ToInt();
                    var residueWeight=v.CheckGet($"LAYER{i}_RESIDUE_WEIGHT").ToInt();
                    var taskOthersWeight=0;
                    if(weights.ContainsKey(paperId))
                    {
                        taskOthersWeight=weights[paperId];
                    }
                    if(taskOthersWeight>0)
                    {
                        if((taskOthersWeight)>residueWeight)
                        {
                            SetControlBorder(control,1);
                            result=false;
                        }
                    }
                }
            }

            return result;
        }
        
        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            ActiveStackerGroup=0;
            ActiveStacker=0;
            ManualComposition=false;
            Report="";
            Stacker1=new Dictionary<string, string>(); 
            Stacker2=new Dictionary<string, string>();
            Stacker3=new Dictionary<string, string>();
            StackerPrimary=0;
            StackerSecondary=0;
            Title.Text="";
            SourceChanged=false;
            SourceChangedChecking=false;

            Stacker1ProductsFromBlank.Text="";
            Stacker2ProductsFromBlank.Text="";
            Stacker3ProductsFromBlank.Text="";

            Stacker1StacksInPallet.Text="";
            Stacker2StacksInPallet.Text="";
            Stacker3StacksInPallet.Text="";


            var d=Form.GetDefaults();
            d.CheckAdd("CARDBOARD_ID","0");
            d.CheckAdd("STACKER1_POSITION_ID","0");
            d.CheckAdd("STACKER2_POSITION_ID","0");
            d.CheckAdd("STACKER3_POSITION_ID","0");

            Form.SetValues(d);

           


            //зависимые контролы
            Stacker1NoProcessingTask.IsEnabled=false;
            Stacker2NoProcessingTask.IsEnabled=false;
            Stacker3NoProcessingTask.IsEnabled=false;

            SetControlBorder(Stacker1Cardboard);
            SetControlBorder(Stacker2Cardboard);
            SetControlBorder(Stacker3Cardboard);

            //стекеры и группы            
            SetActiveStacker();
            SetActiveStackerGroup();
            
            //состав сырья
            {
                UseManualComposition.IsChecked=false;
                CheckManualComposition(false);    
            }


            SchemeImage.Source=null;

            SetStatus("");
            SaveButton.IsEnabled=false;

        }

        /// <summary>
        /// установка некоторых параметров в зависимости от режима работы
        /// </summary>
        private void CheckMode()
        {
            switch(Mode)
            {
                default:                
                {
                    /*
                        создание или перевыпуск
                     */
                    CloseButton.Content = "Отмена";
                    FormatList.IsEnabled=true;
                    Comment.IsEnabled=true;

                    //Stacker1Blank.IsEnabled=true;
                    Stacker1SelectButton.IsEnabled=true;
                    Stacker1ClearButton.IsEnabled=true;
                    Stacker1NoProcessingTask.IsEnabled=true;
                    Stacker1SelectApplicationButton.IsEnabled=true;
                    
                    //Stacker2Blank.IsEnabled=true;
                    Stacker2SelectButton.IsEnabled=true;
                    Stacker2ClearButton.IsEnabled=true;
                    Stacker2NoProcessingTask.IsEnabled=true;
                    Stacker2SelectApplicationButton.IsEnabled=true;

                    //Stacker3Blank.IsEnabled=true;
                    Stacker3SelectButton.IsEnabled=true;
                    Stacker3ClearButton.IsEnabled=true;
                    Stacker3NoProcessingTask.IsEnabled=true;
                    Stacker3SelectApplicationButton.IsEnabled=true;

                    UseManualComposition.IsEnabled=true;
                    UseManualComposition.IsChecked = false;
                    CheckManualComposition(false);
                    
                    CardboardList.IsEnabled=true;

                    Stacker1QuantityCalculated.IsReadOnly=true;
                    Stacker1Threads.IsReadOnly=false;
                    Stacker1Sample.IsEnabled=true;

                    Stacker2QuantityCalculated.IsReadOnly=true;
                    Stacker2Threads.IsReadOnly=false;
                    Stacker2Sample.IsEnabled=true;

                    Stacker3QuantityCalculated.IsReadOnly=true;
                    Stacker3Threads.IsReadOnly=false;
                    Stacker3Sample.IsEnabled=true;

                    Stacker12Swap.IsEnabled=true;
                    Stacker23Swap.IsEnabled=true;

                    Stacker1Main.IsEnabled=true;
                    Stacker2Main.IsEnabled=true;
                    Stacker3Main.IsEnabled=true;
                    Stacker12Group.IsEnabled=true;
                    Stacker23Group.IsEnabled=true;

                    SaveButton.Visibility=Visibility.Visible;
                    CloseButton.Visibility=Visibility.Visible;
                    ResetButton.Visibility=Visibility.Visible;
                    ReworkButton.Visibility = Visibility.Visible;
                }
                break;

                case 3:
                {
                    /*
                        редактирование полное (пересоздание)
                        можно менять позиции и количество
                     */
                    CloseButton.Content = "Отмена";
                    FormatList.IsEnabled=true;
                    Comment.IsEnabled=true;

                    //Stacker1Blank.IsEnabled=true;
                    Stacker1SelectButton.IsEnabled=true;
                    Stacker1ClearButton.IsEnabled=true;
                    Stacker1NoProcessingTask.IsEnabled=true;
                    Stacker1SelectApplicationButton.IsEnabled=true;
                    
                    //Stacker2Blank.IsEnabled=true;
                    Stacker2SelectButton.IsEnabled=true;
                    Stacker2ClearButton.IsEnabled=true;
                    Stacker2NoProcessingTask.IsEnabled=true;
                    Stacker2SelectApplicationButton.IsEnabled=true;

                    //Stacker3Blank.IsEnabled=true;
                    Stacker3SelectButton.IsEnabled=true;
                    Stacker3ClearButton.IsEnabled=true;
                    Stacker3NoProcessingTask.IsEnabled=true;
                    Stacker3SelectApplicationButton.IsEnabled=true;

                    UseManualComposition.IsEnabled=true;
                    UseManualComposition.IsChecked = true;
                    CheckManualComposition(true);
                    
                    CardboardList.IsEnabled=false;

                    Stacker1QuantityCalculated.IsReadOnly=false;
                    Stacker1Threads.IsReadOnly=false;
                    Stacker1Sample.IsEnabled=true;

                    Stacker2QuantityCalculated.IsReadOnly=false;
                    Stacker2Threads.IsReadOnly=false;
                    Stacker2Sample.IsEnabled=true;

                    Stacker3QuantityCalculated.IsReadOnly=false;
                    Stacker3Threads.IsReadOnly=false;
                    Stacker3Sample.IsEnabled=true;

                    Stacker12Swap.IsEnabled=true;
                    Stacker23Swap.IsEnabled=true;

                    Stacker1Main.IsEnabled=true;
                    Stacker2Main.IsEnabled=true;
                    Stacker3Main.IsEnabled=true;
                    Stacker12Group.IsEnabled=true;
                    Stacker23Group.IsEnabled=true;

                    SaveButton.Visibility=Visibility.Visible;
                    CloseButton.Visibility=Visibility.Visible;
                    ResetButton.Visibility=Visibility.Collapsed;
                    ReworkButton.Visibility = Visibility.Collapsed;
                }
                break;

                case 1:
                {
                    /*
                        редактирование частичное
                        можно менять количество
                    */
                    CloseButton.Content = "Отмена";
                    FormatList.IsEnabled=true;
                    Comment.IsEnabled=true;

                    //Stacker1Blank.IsEnabled=false;
                    Stacker1SelectButton.IsEnabled=false;
                    Stacker1ClearButton.IsEnabled=false;
                    Stacker1NoProcessingTask.IsEnabled=false;
                    Stacker1SelectApplicationButton.IsEnabled=false;
                    
                    //Stacker2Blank.IsEnabled=false;
                    Stacker2SelectButton.IsEnabled=false;
                    Stacker2ClearButton.IsEnabled=false;
                    Stacker2NoProcessingTask.IsEnabled=false;
                    Stacker2SelectApplicationButton.IsEnabled=false;

                    //Stacker3Blank.IsEnabled=false;
                    Stacker3SelectButton.IsEnabled=false;
                    Stacker3ClearButton.IsEnabled=false;
                    Stacker3NoProcessingTask.IsEnabled=false;
                    Stacker3SelectApplicationButton.IsEnabled=false;

                    UseManualComposition.IsEnabled=true;
                    UseManualComposition.IsChecked = true;
                    CheckManualComposition(true);
                    
                    CardboardList.IsEnabled=false;

                    Stacker1QuantityCalculated.IsReadOnly=false;
                    Stacker1Threads.IsReadOnly=false;
                    Stacker1Sample.IsEnabled=true;

                    Stacker2QuantityCalculated.IsReadOnly=false;
                    Stacker2Threads.IsReadOnly=false;
                    Stacker2Sample.IsEnabled=true;

                    Stacker3QuantityCalculated.IsReadOnly=false;
                    Stacker3Threads.IsReadOnly=false;
                    Stacker3Sample.IsEnabled=true;

                    Stacker12Swap.IsEnabled=false;
                    Stacker23Swap.IsEnabled=false;

                    Stacker1Main.IsEnabled=true;
                    Stacker2Main.IsEnabled=true;
                    Stacker3Main.IsEnabled=true;
                    Stacker12Group.IsEnabled=false;
                    Stacker23Group.IsEnabled=false;

                    SaveButton.Visibility=Visibility.Visible;
                    CloseButton.Visibility=Visibility.Visible;
                    ResetButton.Visibility=Visibility.Collapsed;
                    ReworkButton.Visibility = Visibility.Collapsed;
                }
                break;

                case 2:
                {
                    /*
                        просмотр
                        только чтение
                    */
                    CloseButton.Content = "Закрыть";
                    FormatList.IsEnabled=false;
                    Comment.IsEnabled=false;

                    //Stacker1Blank.IsEnabled=false;
                    Stacker1SelectButton.IsEnabled=false;
                    Stacker1ClearButton.IsEnabled=false;
                    Stacker1NoProcessingTask.IsEnabled=false;
                    Stacker1SelectApplicationButton.IsEnabled=false;
                    
                    //Stacker2Blank.IsEnabled=false;
                    Stacker2SelectButton.IsEnabled=false;
                    Stacker2ClearButton.IsEnabled=false;
                    Stacker2NoProcessingTask.IsEnabled=false;
                    Stacker2SelectApplicationButton.IsEnabled=false;

                    //Stacker3Blank.IsEnabled=false;
                    Stacker3SelectButton.IsEnabled=false;
                    Stacker3ClearButton.IsEnabled=false;
                    Stacker3NoProcessingTask.IsEnabled=false;
                    Stacker3SelectApplicationButton.IsEnabled=false;

                    UseManualComposition.IsEnabled=false;
                    UseManualComposition.IsChecked = false;
                    CheckManualComposition(false);
                    
                    CardboardList.IsEnabled=false;                    

                    Stacker1QuantityCalculated.IsReadOnly=true;
                    Stacker1Threads.IsReadOnly=true;
                    Stacker1Sample.IsEnabled=false;

                    Stacker2QuantityCalculated.IsReadOnly=true;
                    Stacker2Threads.IsReadOnly=true;
                    Stacker2Sample.IsEnabled=false;

                    Stacker3QuantityCalculated.IsReadOnly=true;
                    Stacker3Threads.IsReadOnly=true;
                    Stacker3Sample.IsEnabled=false;

                    Stacker12Swap.IsEnabled=false;
                    Stacker23Swap.IsEnabled=false;

                    Stacker1Main.IsEnabled=false;
                    Stacker2Main.IsEnabled=false;
                    Stacker3Main.IsEnabled=false;
                    Stacker12Group.IsEnabled=false;
                    Stacker23Group.IsEnabled=false;

                    SaveButton.Visibility=Visibility.Collapsed;
                    CloseButton.Visibility=Visibility.Visible;
                    ResetButton.Visibility=Visibility.Collapsed;
                    ReworkButton.Visibility = Visibility.Collapsed;

                    Virtual.IsEnabled=false;

                    Stacker1ThreadsInc.IsEnabled=false;
                    Stacker1ThreadsDec.IsEnabled=false;
                    Stacker2ThreadsInc.IsEnabled=false;
                    Stacker2ThreadsDec.IsEnabled=false;
                    Stacker3ThreadsInc.IsEnabled=false;
                    Stacker3ThreadsDec.IsEnabled=false;
                   
                }
                break;
            }
        }

        /// <summary>
        /// изменение группы стекеров: 1=1-2, 2=2-3
        /// </summary>
        private int ActiveStackerGroup { get; set; }
        private void SetActiveStackerGroup(int mode=0)
        {
            Stacker1Splash.Visibility=System.Windows.Visibility.Hidden;
            Stacker2Splash.Visibility=System.Windows.Visibility.Hidden;
            Stacker3Splash.Visibility=System.Windows.Visibility.Hidden;

            Stacker12Swap.IsEnabled=false;
            Stacker23Swap.IsEnabled=false;

            if(mode==0)
            {
                mode=1;
            }
            ActiveStackerGroup=mode;

            switch(mode)
            {
                case 1:
                {
                    //стекеры 1-2
                    Stacker1Splash.Visibility = Visibility.Hidden;
                    Stacker2Splash.Visibility = Visibility.Hidden;
                    Stacker3Splash.Visibility = Visibility.Visible;
                    
                    Stacker12Group.IsChecked=true;
                    Stacker23Group.IsChecked=false;

                    Stacker12Swap.IsEnabled=true;

                    //если до этого активным был третий, переключим на 2 (из той же пары)
                    if(ActiveStacker==3)
                    {
                        SetActiveStacker(2);
                    }
                }
                break;

                case 2:
                {
                    //стекеры 2-3
                    Stacker1Splash.Visibility = Visibility.Visible;
                    Stacker2Splash.Visibility = Visibility.Hidden;
                    Stacker3Splash.Visibility = Visibility.Hidden;

                    Stacker12Group.IsChecked=false;
                    Stacker23Group.IsChecked=true;

                    Stacker23Swap.IsEnabled=true;

                    //если до этого активным был первый, переключим на 2 (из той же пары)
                    if(ActiveStacker==1)
                    {
                        SetActiveStacker(2);
                    }
                }
                break;
            }

            if(Mode==2 || Mode==1)
            {
                Stacker12Swap.IsEnabled=false;
                Stacker23Swap.IsEnabled=false;
            }

        }

        /// <summary>
        /// выбран главным один из стекеров: 1,2,3
        /// </summary>
        private int ActiveStacker { get; set; }
        private void SetActiveStacker(int mode=0)
        {
            if(mode==0)
            {
                mode=1;
            }
            ActiveStacker=mode;

            switch(mode)
            {
                case 1:
                {
                    //стекер 1
                    Stacker1Main.IsChecked=true;
                    Stacker2Main.IsChecked=false;
                    Stacker3Main.IsChecked=false;  
                    
                    Stacker1QuantityCalculated.IsReadOnly=false;
                    Stacker2QuantityCalculated.IsReadOnly=true;
                    Stacker3QuantityCalculated.IsReadOnly=true;
                }
                break;

                case 2:
                {
                    //стекер 2
                    Stacker1Main.IsChecked=false;
                    Stacker2Main.IsChecked=true;
                    Stacker3Main.IsChecked=false;

                    Stacker1QuantityCalculated.IsReadOnly=true;
                    Stacker2QuantityCalculated.IsReadOnly=false;
                    Stacker3QuantityCalculated.IsReadOnly=true;
                }
                break;

                case 3:
                {
                    //стекер 3
                    Stacker1Main.IsChecked=false;
                    Stacker2Main.IsChecked=false;
                    Stacker3Main.IsChecked=true;

                    Stacker1QuantityCalculated.IsReadOnly=true;
                    Stacker2QuantityCalculated.IsReadOnly=true;
                    Stacker3QuantityCalculated.IsReadOnly=false;
                }
                break;
            }


            if(Mode==2)
            {
                Stacker1QuantityCalculated.IsReadOnly=true;
                Stacker2QuantityCalculated.IsReadOnly=true;
                Stacker3QuantityCalculated.IsReadOnly=true;
            }

            //ChangeQuantity=false;
            //Calculate(true);
        }
        
        /// <summary>
        /// активация и блокировка блока сырьевой композиции
        /// mode= true-активировать | false-деактивировать
        /// </summary>
        private void CheckManualComposition(bool mode)
        {
            ManualComposition=mode;
            
            if(mode)
            {
                //выбор композиции вручную
                CardboardList.IsEnabled=false;
                {
                    Layer1PaperListSelectButton.IsEnabled=true;
                    Layer2PaperListSelectButton.IsEnabled=true;
                    Layer3PaperListSelectButton.IsEnabled=true;
                    Layer4PaperListSelectButton.IsEnabled=true;
                    Layer5PaperListSelectButton.IsEnabled=true;
                    
                    Layer1Gluing.IsEnabled = true;    
                    Layer2Gluing.IsEnabled = true;
                    Layer3Gluing.IsEnabled = true;
                    Layer4Gluing.IsEnabled = true;
                    Layer5Gluing.IsEnabled = true;
                }
            }
            else
            {
                //выбор композиции через выбор картона
                CardboardList.IsEnabled=true;
                {
                    Layer1PaperListSelectButton.IsEnabled=false;
                    Layer2PaperListSelectButton.IsEnabled=false;
                    Layer3PaperListSelectButton.IsEnabled=false;
                    Layer4PaperListSelectButton.IsEnabled=false;
                    Layer5PaperListSelectButton.IsEnabled=false;
                    
                    Layer1Gluing.IsEnabled = false;    
                    Layer2Gluing.IsEnabled = false;
                    Layer3Gluing.IsEnabled = false;
                    Layer4Gluing.IsEnabled = false;
                    Layer5Gluing.IsEnabled = false;
                }
            }

            if(Mode!=2)
            {
                CheckManualCompositionProfile();           
            }  
            
            //просмотр
            if(Mode==2)
            {
                CardboardList.IsEnabled=false;
            }
        }

        /// <summary>
        /// выбор прифиля картона, в зависимости от профиля
        /// активируются и деактивируются соотв. слои
        /// </summary>
        /// <param name="profileId"></param>
        public void CheckManualCompositionProfile(int profileId=0)
        {
            /*
                http://192.168.3.237/developer/l-pack-erp/common/base/corrugation/cardboard

                profile_id   title   layers
                ----------   -----   ------
                1            B       3       3,4,5
                2            C       3       1,2,5
                4            E       3       3,4,5
                3            BC      5
                *            **      5
            */

            if(profileId==0)
            {
                var v=Form.GetValues();
                profileId = v.CheckGet("CARDBOARD_PROFILE_ID").ToInt();
            }
            
            var v2=new Dictionary<string,string>();

            switch(profileId)
            {
                case 1:
                case 4:
                    //disable 1,2
                    Layer1PaperListSelectButton.IsEnabled = false;
                    Layer1Gluing.IsEnabled = false; 
                    v2.CheckAdd("LAYER1_ID","0");
                    v2.CheckAdd("LAYER1_NAME","");

                    Layer2PaperListSelectButton.IsEnabled = false;
                    Layer2Gluing.IsEnabled = false;    
                    v2.CheckAdd("LAYER2_ID","0");
                    v2.CheckAdd("LAYER2_NAME","");
                    break;

                case 2:
                    //disable 3,4
                    Layer3PaperListSelectButton.IsEnabled = false;
                    Layer3Gluing.IsEnabled = false;    
                    v2.CheckAdd("LAYER3_ID","0");
                    v2.CheckAdd("LAYER3_NAME","");

                    Layer4PaperListSelectButton.IsEnabled = false;
                    Layer4Gluing.IsEnabled = false;    
                    v2.CheckAdd("LAYER4_ID","0");
                    v2.CheckAdd("LAYER4_NAME","");
                    break;

                default:
                    Layer1PaperListSelectButton.IsEnabled = true;
                    Layer1Gluing.IsEnabled = true;   

                    Layer2PaperListSelectButton.IsEnabled = true;
                    Layer2Gluing.IsEnabled = true;   

                    Layer3PaperListSelectButton.IsEnabled = true;
                    Layer3Gluing.IsEnabled = true;   

                    Layer4PaperListSelectButton.IsEnabled = true;
                    Layer4Gluing.IsEnabled = true;   

                    Layer5PaperListSelectButton.IsEnabled = true;
                    Layer5Gluing.IsEnabled = true;   

                    break;
                
            }
            Form.SetValues(v2);
        }

        /// <summary>
        /// очиста данных в блоке "состав сырьевой композиции"
        /// </summary>
        public void ClearCompositionData()
        {
            var v=new Dictionary<string,string>();
            {
                v.CheckAdd("CARDBOARD_PROFILE_ID","");
                v.CheckAdd("CARDBOARD_PROFILE","");

                v.CheckAdd("LAYER1_ID","0");
                v.CheckAdd("LAYER2_ID","0");
                v.CheckAdd("LAYER3_ID","0");
                v.CheckAdd("LAYER4_ID","0");
                v.CheckAdd("LAYER5_ID","0");

                v.CheckAdd("LAYER1_ID_NAME","");
                v.CheckAdd("LAYER2_ID_NAME","");
                v.CheckAdd("LAYER3_ID_NAME","");
                v.CheckAdd("LAYER4_ID_NAME","");
                v.CheckAdd("LAYER5_ID_NAME","");

                v.CheckAdd("LAYER1_GLUING","0");
                v.CheckAdd("LAYER2_GLUING","0");
                v.CheckAdd("LAYER3_GLUING","0");
                v.CheckAdd("LAYER4_GLUING","0");
                v.CheckAdd("LAYER5_GLUING","0");
                    
                
                v.CheckAdd("LAYER1_TASK_WEIGHT","0");
                v.CheckAdd("LAYER2_TASK_WEIGHT","0");
                v.CheckAdd("LAYER3_TASK_WEIGHT","0");
                v.CheckAdd("LAYER4_TASK_WEIGHT","0");
                v.CheckAdd("LAYER5_TASK_WEIGHT","0");
                    
                v.CheckAdd("LAYER1_RESIDUE_WEIGHT","0");
                v.CheckAdd("LAYER2_RESIDUE_WEIGHT","0");
                v.CheckAdd("LAYER3_RESIDUE_WEIGHT","0");
                v.CheckAdd("LAYER4_RESIDUE_WEIGHT","0");
                v.CheckAdd("LAYER5_RESIDUE_WEIGHT","0");

                v.CheckAdd("LAYER1_PT_WEIGHT","0");
                v.CheckAdd("LAYER2_PT_WEIGHT","0");
                v.CheckAdd("LAYER3_PT_WEIGHT","0");
                v.CheckAdd("LAYER4_PT_WEIGHT","0");
                v.CheckAdd("LAYER5_PT_WEIGHT","0");

                v.CheckAdd("LAYER1_STOCK_ROLLS", "0");
                v.CheckAdd("LAYER2_STOCK_ROLLS", "0");
                v.CheckAdd("LAYER3_STOCK_ROLLS", "0");
                v.CheckAdd("LAYER4_STOCK_ROLLS", "0");
                v.CheckAdd("LAYER5_STOCK_ROLLS", "0");
            }

            Form.SetValues(v);
        }
        
        public bool RefSourcesLoaded { get;set;}
        /// <summary>
        /// загрузка вспомогательных данных для построения интерфейса
        /// список картонов и бумаги для слоев
        /// </summary>
        public async void LoadRef(bool updateFormat=true, int format=0, int profileId=0)
        {
            Central.Logger.Debug($"..LoadRef");

            RefSourcesLoaded=false;
            
            FormToolbar.IsEnabled = false;
            //PositionGrid.ShowSplash();
            bool resume = true;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    if(format!=0 && profileId!=0)
                    {
                        p.CheckAdd("FORMAT",    format.ToString());
                        p.CheckAdd("PROFILE_ID",profileId.ToString());
                    }
                    else
                    {
                        var v=Form.GetValues();
                        p.CheckAdd("FORMAT",    v.CheckGet("FORMAT"));
                        p.CheckAdd("PROFILE_ID",v.CheckGet("CARDBOARD_PROFILE_ID"));
                    }
                    
                    p.CheckAdd("ID_PZ", ProductionTaskId.ToString());
                    p.CheckAdd("FACTORY_ID", FactoryId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Production");
                q.Request.SetParam("Object","Cutter");
                q.Request.SetParam("Action","GetSources");
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

                q.Request.SetParams(p);
                             
                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if(q.Answer.Status == 0)                
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if(result!=null)
                    {

                        if(updateFormat)
                        {
                            if(result.ContainsKey("FORMATS"))
                            {
                                var ds=(ListDataSet)result["FORMATS"];
                                ds.Init();

                                var list=new Dictionary<string,string>();
                                list.Add("0","");
                                list.AddRange<string,string>(ds.GetItemsList("WIDTH","WIDTH"));
                            
                                FormatList.Items = list;
                                FormatList.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                            }
                        }
                        
                        
                        if(result.ContainsKey("CARDBOARD"))
                        {
                            CardboardDS=(ListDataSet)result["CARDBOARD"];
                            CardboardDS.Init();

                            var list=new Dictionary<string,string>();
                            list.Add("0","");
                            list.AddRange<string,string>(CardboardDS.GetItemsList("ID","NAME"));
                            
                            CardboardList.Items = list;

                            if(SelectedCardboardId!=0)
                            {
                                CardboardList.SelectedItem = list.FirstOrDefault((x) => x.Key == SelectedCardboardId.ToString());
                            }
                            else
                            {
                                CardboardList.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                            }
                            
                        }

                        // Данные по сырью
                        if(result.ContainsKey("RAW_GROUP"))
                        {
                            PaperListDS=ListDataSet.Create(result,"RAW_GROUP");
                        }

                        // Заполним данные по всем профилям. Ключом для поиска данных является ID профиля
                        if (result.ContainsKey("PROFILES"))
                        {
                            var ds = ListDataSet.Create(result, "PROFILES");
                            if (ds.Items.Count > 0)
                            {
                                ProfileParams.Clear();
                                foreach (var item in ds.Items)
                                {
                                    ProfileParams.Add(item["ID"].ToInt(), item);
                                }
                            }
                        }
                    }
                }
            }
            

            FormToolbar.IsEnabled = true;
            //PositionGrid.HideSplash();

            RefSourcesLoaded=true;
            Central.Logger.Debug($"..LoadRef complete");
        }

        /// <summary>
        /// загрузка данных по слоям для выбранного картона        
        /// </summary>
        public async void LoadLayers(int cardboardId)
        {
            FormToolbar.IsEnabled = false;
            //PositionGrid.ShowSplash();
            bool resume = true;

            if (resume)
            {
                if (!ChainUpdate)
                {
                    resume=false;
                }
            }
            
            if (resume)
            {
                Central.Logger.Trace($"....LoadLayers [{cardboardId}]");
                
                if(ManualComposition)
                {
                    resume=false;
                }
            }

            if (resume)
            {
                ClearCompositionData();
                
                var p = new Dictionary<string, string>();
                
                {
                    p.CheckAdd("ID",    cardboardId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Production");
                q.Request.SetParam("Object","Cutter");
                q.Request.SetParam("Action","GetLayers");
                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts= Central.Parameters.RequestAttemptsDefault;

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

               
                var v=new Dictionary<string,string>();

                if(q.Answer.Status == 0)                
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if(result!=null)
                    {
                        if(result.ContainsKey("DATA"))
                        {                          
                           
                            var ds=(ListDataSet)result["DATA"];
                            ds.Init();

                            if(ds.Items.Count>0)
                            {

                                var row=ds.Items.First();
                                if(row!=null)
                                {

                                    if(row.CheckGet("PROFILE_ID").ToInt()!=0)
                                    {
                                        v.CheckAdd("CARDBOARD_PROFILE_ID",row.CheckGet("PROFILE_ID"));
                                    }
                                                                
                                    if(!string.IsNullOrEmpty(row.CheckGet("PROFILE_NAME").ToString()))
                                    {
                                        v.CheckAdd("CARDBOARD_PROFILE", row.CheckGet("PROFILE_NAME").ToString());
                                    }

                                    if (!string.IsNullOrEmpty(row.CheckGet("OUTER_COLOR_ID").ToString()))
                                    {
                                        v.CheckAdd("CARDBOARD_OUTER_COLOR_ID", row.CheckGet("OUTER_COLOR_ID").ToString());
                                    }

                                    if (row.CheckGet("LAYER1_ID").ToInt()!=0)
                                    {
                                        v.CheckAdd("LAYER1_ID",row.CheckGet("LAYER1_ID"));
                                        v.CheckAdd("LAYER1_GLUING", row.CheckGet("LAYER1_GLUING"));
                                    }

                                    if(row.CheckGet("LAYER2_ID").ToInt()!=0)
                                    {
                                        v.CheckAdd("LAYER2_ID",row.CheckGet("LAYER2_ID"));
                                        v.CheckAdd("LAYER2_GLUING", row.CheckGet("LAYER2_GLUING"));
                                    }

                                    if (row.CheckGet("LAYER3_ID").ToInt()!=0)
                                    {
                                        v.CheckAdd("LAYER3_ID",row.CheckGet("LAYER3_ID"));
                                        v.CheckAdd("LAYER3_GLUING", row.CheckGet("LAYER3_GLUING"));
                                    }

                                    if (row.CheckGet("LAYER4_ID").ToInt()!=0)
                                    {
                                        v.CheckAdd("LAYER4_ID",row.CheckGet("LAYER4_ID"));
                                        v.CheckAdd("LAYER4_GLUING", row.CheckGet("LAYER4_GLUING"));
                                    }

                                    if (row.CheckGet("LAYER5_ID").ToInt()!=0)
                                    {
                                        v.CheckAdd("LAYER5_ID",row.CheckGet("LAYER5_ID"));
                                        v.CheckAdd("LAYER5_GLUING", row.CheckGet("LAYER5_GLUING"));
                                    }
                                
                                }
                            }

                        }
                     
                    }
                }

                Form.SetValues(v);
                //CheckManualCompositionProfile();
                //UpdateLayersWeights();
                //UpdateSampleWarning();
                Calculate();
              
            }
            
            FormToolbar.IsEnabled = true;
            //PositionGrid.HideSplash();
        }

        /// <summary>
        /// вызов диалога выбора материала для соотв. слоя композиции
        /// </summary>
        /// <param name="layerId"></param>
        private void SelectPaper(int layerId)
        {
            if(Mode!=2 && ManualComposition)
            {
                var b=(bool)UseManualComposition.IsChecked;
                if(b)
                {
                    var v=Form.GetValues();

                    var selectPaper=new SelectPaper();
                    selectPaper.LayerId=layerId;
                    selectPaper.ProfileId=v.CheckGet("CARDBOARD_PROFILE_ID").ToInt();
                    var k=$"LAYER{layerId}_ID";
                    selectPaper.PaperId=v.CheckGet(k).ToInt();
                    selectPaper.Format=v.CheckGet("FORMAT").ToInt();
                    selectPaper.FactoryId = FactoryId;
                    selectPaper.Init();                
                }
            }
            Calculate();
        }

        /// <summary>
        /// установка данных материала  для соотв. слоя композиции
        /// </summary>
        /// <param name="p"></param>
        private void SetLayerPaper(Dictionary<string,string> p)
        {
            var resume=true;

            if(resume)
            {
                var layerId=p.CheckGet("LAYER_ID").ToInt();
                var paperId=p.CheckGet("ID").ToInt();
                if(layerId!=0 && paperId!=0)
                {
                    var v=new Dictionary<string,string>();
                    {
                        var k1=$"LAYER{layerId}_ID";
                        var v1=paperId.ToString();
                        v.CheckAdd(k1,v1);
                        var k2 = $"LAYER{layerId}_COLOR";
                        v.CheckAdd(k2, p.CheckGet("COLOR"));
                    }
                    
                    Form.SetValues(v);
                }

                CheckSourceChanging();
            }
        }

        /// <summary>
        /// установка имени материала
        /// </summary>
        /// <param name="layerId"></param>
        /// <param name="paperId"></param>
        private void SetLayerPaperName(int layerId, int paperId)
        {
            if (PaperListDS != null)
            {
                if (layerId != 0 && paperId != 0)
                {
                    var v = new Dictionary<string, string>();
                    if (PaperListDS.Items.Count > 0)
                    {
                        foreach (Dictionary<string, string> row in PaperListDS.Items)
                        {
                            if (row.CheckGet("ID").ToInt() == paperId)
                            {
                                {
                                    var k1 = $"LAYER{layerId}_NAME";
                                    //var v1=$"{ow.CheckGet("GROUP_NAME")} ({row.CheckGet("ID")})";
                                    var v1 = row.CheckGet("GROUP_NAME");
                                    v.CheckAdd(k1, v1);
                                }
                            }
                        }
                    }
                    Form.SetValues(v);
                    UpdateLayerWeights(paperId, layerId);
                }
            }
        }

        /// <summary>
        /// Загрузка заявок на изделие и выбор строки полученной позиции
        /// </summary>
        /// <param name="position"></param>
        private async void GetApplication(Dictionary<string, string> position)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Position");
            q.Request.SetParam("Action", "ListByGoodId");
            q.Request.SetParam("GOODS_ID", position.CheckGet("GOODS_ID"));
            q.Request.SetParam("FACTORY_ID", FactoryId.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "APPLICATION");
                    int x = position.CheckGet("STACKER_ID").ToInt();
                    int positionId = position.CheckGet("POSITION_ID").ToInt();

                    string positionName = "";

                    if (positionId > 0)
                    {
                        if (ds.Items.Count > 0)
                        {
                            var item = new Dictionary<string, string>();
                            foreach (var row in ds.Items)
                            {
                                if (row.CheckGet("ID").ToInt() == positionId)
                                {
                                    item = row;
                                    break;
                                }
                            }

                            if (item.Count > 0)
                            {
                                positionName = item.CheckGet("NUM");
                            }
                            else
                            {
                                // Если в списке активных заявок не нашли выбранную, сбрасываем заявку
                                positionId = 0;
                            }
                        }
                        else
                        {
                            // Если список заявок пустой, поле активной заявки тоже делаем пустым
                            positionId = 0;
                        }
                    }

                    var p = new Dictionary<string, string>();
                    p.CheckAdd($"STACKER{x}_POSITION_ID", positionId.ToString());
                    p.CheckAdd($"_STACKER{x}_POSITION_ID", positionId.ToString());
                    p.CheckAdd($"STACKER{x}_POSITION_NAME", positionName);
                    Form.SetValues(p);

                    // Если не нашли заявку, выделим поле
                    int borderType = positionId == 0 ? 1 : 0;
                    switch (x)
                    {
                        case 1:
                            Stacker1ApplicationDS = ds;
                            SetControlBorder(Stacker1Application, borderType);
                            Stacker1.CheckAdd("_POSITION_ID", positionId.ToString());
                            Stacker1.CheckAdd("POSITION_ID", positionId.ToString());
                            break;
                        case 2:
                            Stacker2ApplicationDS = ds;
                            SetControlBorder(Stacker2Application, borderType);
                            Stacker2.CheckAdd("_POSITION_ID", positionId.ToString());
                            Stacker2.CheckAdd("POSITION_ID", positionId.ToString());
                            break;
                        case 3:
                            Stacker3ApplicationDS = ds;
                            SetControlBorder(Stacker3Application, borderType);
                            Stacker3.CheckAdd("_POSITION_ID", positionId.ToString());
                            Stacker3.CheckAdd("POSITION_ID", positionId.ToString());
                            break;
                    }

                    GetProductReam(x);
                }
            }
        }


        private void SetApplication(Dictionary<string,string> application)
        {
            int x = application.CheckGet("STACKER_ID").ToInt();
            if (x > 0)
            {
                int positionId = application.CheckGet("ID").ToInt();
                var p = new Dictionary<string, string>();
                p.CheckAdd($"STACKER{x}_POSITION_ID", positionId.ToString());
                p.CheckAdd($"_STACKER{x}_POSITION_ID", positionId.ToString());
                p.CheckAdd($"STACKER{x}_POSITION_NAME", application.CheckGet("NUM"));
                Form.SetValues(p);

                int borderType = positionId == 0 ? 1 : 0;
                switch (x)
                {
                    case 1:
                        SetControlBorder(Stacker1Application, borderType);
                        Stacker1.CheckAdd("_POSITION_ID", positionId.ToString());
                        Stacker1.CheckAdd("POSITION_ID", positionId.ToString());
                        break;
                    case 2:
                        SetControlBorder(Stacker2Application, borderType);
                        Stacker2.CheckAdd("_POSITION_ID", positionId.ToString());
                        Stacker2.CheckAdd("POSITION_ID", positionId.ToString());
                        break;
                    case 3:
                        SetControlBorder(Stacker3Application, borderType);
                        Stacker3.CheckAdd("_POSITION_ID", positionId.ToString());
                        Stacker3.CheckAdd("POSITION_ID", positionId.ToString());
                        break;
                }

                GetProductReam(x);
            }
            else
            {
                var dw = new DialogWindow("Ошибка определения стекера", "Выбор заявки");
                dw.ShowDialog();
            }
        }
        
        /// <summary>
        /// установка данных заготовки на стекер
        /// </summary>
        private void SetStackerBlank(Dictionary<string,string> b, int stackerId=0, bool noStackerChange=false, bool copy=false)
        {
            var resume=true;

            ChainUpdate=false;

            if(resume)
            {
                if(b.Count>0)
                {
                    if(stackerId==0)
                    {
                        stackerId=b.CheckGet("STACKER_ID").ToInt();
                    }
                    
                    if(stackerId==0)
                    {
                        resume=false;
                        ReportAdd("Неверно указан стекер");
                    }
                }
                else
                {
                    resume=false;
                    ReportAdd("Данные заготовки некорректны");
                }
            }

            var v=Form.GetValues();

            if (resume)
            {
                if (!noStackerChange)
                {
                    ChangeActiveStacker(stackerId);    
                }
            }

            if(resume)
            {
                CheckStackersPriority();
            }

            var mainStacker = false;

            if (resume)
            {
                var x=stackerId;
                
                var blank=$"{b.CheckGet("BLANK_NAME")} {b.CheckGet("BLANK_CODE")}";

                { 
                    v.CheckAdd($"STACKER{x}_POSITION_ID",               b.CheckGet("POSITION_ID"));
                    v.CheckAdd($"_STACKER{x}_POSITION_ID",              b.CheckGet("POSITION_ID"));
                    v.CheckAdd($"STACKER{x}_BLANK_ID",                  b.CheckGet("BLANK_ID"));
                    v.CheckAdd($"STACKER{x}_GOOD_ID",                   b.CheckGet("GOODS_ID"));

                    v.CheckAdd($"STACKER{x}_BLANK",                     blank);
                    v.CheckAdd($"STACKER{x}_CARDBOARD",                 b.CheckGet("CARDBOARD_NAME"));

                    v.CheckAdd($"STACKER{x}_CREASE",                    b.CheckGet("CREASE_TYPE"));

                    var sample=b.CheckGet("SAMPLE").ToBool();
                    {
                        if(b.CheckGet("FIRST_TIME_PRODUCTION").ToBool())
                        {
                            sample=true;
                        }
                    }
                    v.CheckAdd($"STACKER{x}_SAMPLE",sample.ToString().ToInt().ToString());

                    //Использавание упаковки
                    v.CheckAdd($"STACKER{x}_PACKAGING", b.CheckGet("PACKAGING"));


                    var pfb=1;
                    {
                        if( b.CheckGet("PRODUCTS_FROM_BLANK").ToInt()>0 )
                        {
                            pfb=b.CheckGet("PRODUCTS_FROM_BLANK").ToInt();
                        }
                    }
                    v.CheckAdd($"STACKER{x}_PRODUCTS_FROM_BLANK", pfb.ToString());
                    
                    
                    {
                        /*
                            Тип рилёвки для заготовки,
                            1 = СТРОГО 3 точки(в простонародье папа-мама), 
                            2 = СТРОГО плоская рилёвка, 
                            3 = НЕВАЖНО, т.е. будем смотреть по ситуации по конкретному заданию, 
                            4 - папа-папа (3 точки, смещение = 1)
                            */

                        var c1="";
                        {
                            switch(b.CheckGet("CREASE_TYPE").ToInt())
                            {
                                case 1:
                                    c1="п/м";
                                    break;

                                case 2:
                                    c1="пл";
                                    break;

                                case 4:
                                    c1="п/п";
                                    break;
                            }                                                     
                        }

                        v.CheckAdd($"STACKER{x}_CREASE_NOTE", c1);
                    }

                    v.CheckAdd($"STACKER{x}_LENGTH",                    b.CheckGet("LENGTH"));
                    v.CheckAdd($"STACKER{x}_WIDTH",                     b.CheckGet("WIDTH"));
                    v.CheckAdd($"STACKER{x}_QUANTITY",                  b.CheckGet("PRODUCTS_IN_APPLICATION"));
                    var productsInPallet = b.CheckGet("PRODUCTS_IN_PALLET").ToInt();

                    v.CheckAdd($"STACKER{x}_PALLET", productsInPallet.ToString());

                    if(b.CheckGet("QTY").ToInt()>0)
                    {
                        v.CheckAdd($"STACKER{x}_QUANTITY_LOWER",        b.CheckGet("LOWER_LIMIT"));
                        v.CheckAdd($"STACKER{x}_QUANTITY_UPPER",        b.CheckGet("UPPER_LIMIT"));
                    }
                    else
                    {
                        v.CheckAdd($"STACKER{x}_QUANTITY_LOWER",        "");
                        v.CheckAdd($"STACKER{x}_QUANTITY_UPPER",        "");
                    }


                    //ограничение количества
                    var quantityLimitType=0;
                    var qtyLimitS="";
                    {
                        quantityLimitType=b.CheckGet("QUANTITY_LIMIT_TYPE").ToInt();
                        if (quantityLimitType != 2 && b.CheckGet("KIT_ID").ToInt() != 0)
                        {
                            quantityLimitType = 3;
                        }

                        switch (quantityLimitType)
                        {
                            case 0:
                                qtyLimitS = "=";
                                break;

                            case 1:
                                qtyLimitS = ">";
                                break;

                            case 2:
                                qtyLimitS = "<";
                                break;

                            case 3:
                                qtyLimitS = "*";
                                break;
                        }
                    }
                    v.CheckAdd($"STACKER{x}_QUANTITY_LIMIT",        qtyLimitS);
                    v.CheckAdd($"STACKER{x}_QUANTITY_LIMIT_TYPE",   quantityLimitType.ToString());

                    

                    int t=0;
                    if(b.CheckGet("WIDTH").ToInt()>0)
                    {
                        t=1;
                    }

                    //if(Mode!=0)
                    {
                        if(b.CheckGet("THREADS").ToInt()>0)
                        {
                            t=b.CheckGet("THREADS").ToInt();
                        }
                    }
                    v.CheckAdd($"STACKER{x}_THREADS",      t.ToString());

                    
                    
                    // создание  -- рекомендация по количеству
                    /*
                    if(Mode==0)
                    {
                        if(b.CheckGet("QTY").ToInt()!=0)
                        {
                            q=b.CheckGet("QTY").ToInt();
                        }

                    }
                    else
                    {
                        if(b.CheckGet("QTY_CALC").ToInt()!=0)
                        {
                            q=b.CheckGet("QTY_CALC").ToInt();
                        }
                    }
                    */

                    //по дефолту подставим колво из заявки
                    int q=0;
                    if(b.CheckGet("QTY").ToInt()!=0)
                    {
                        q=b.CheckGet("QTY").ToInt();
                    }

                    v.CheckAdd($"STACKER{x}_QUANTITY_CALCULATED",   q.ToString());
                    v.CheckAdd($"STACKER{x}_QUANTITY_TO_CUT",   q.ToString());
                }

                
                var cardboardTypeText="";
                if(b.CheckGet("CARDBOARD_TYPE").ToInt()==0)
                {
                    cardboardTypeText="макулатура";
                }else if(b.CheckGet("CARDBOARD_TYPE").ToInt()==1)
                {
                    cardboardTypeText="целюллоза";
                }

                var cardboardStandart="нет";
                if(b.CheckGet("CARDBOARD_STANDART").ToBool()==true)
                {
                    cardboardStandart="да";
                }

                switch (stackerId)
                {
                    case 1:
                    { 
                        Stacker1=b;
                        Stacker1.CheckAdd("UNTRIMMED_EDGE_FLAG", b.CheckGet("UNTRIMMED_EDGE"));

                        Stacker1BlankCode.Text          =b.CheckGet("BLANK_CODE");
                        Stacker1BlankName.Text          =b.CheckGet("BLANK_NAME");
                        Stacker1GoodCode.Text           =b.CheckGet("GOODS_CODE");
                        Stacker1GoodName.Text           =b.CheckGet("GOODS_NAME");
                        Stacker1TrimMin.Text            =b.CheckGet("MIN_TRIM");

                        Stacker1ZCardboard.Text = "";
                        if (b.CheckGet("Z_CARDBOARD").ToBool()==true)
                        {
                            Stacker1ZCardboard.Text = "Z-картон";
                        }
                        
                        Stacker1CardboardName.Text      =b.CheckGet("CARDBOARD_NAME");
                        Stacker1CardboardId.Text        =b.CheckGet("CARDBOARD_ID");
                        Stacker1CardboardMarkId.Text    =b.CheckGet("CARDBOARD_MARK_ID");
                        Stacker1CardboardType.Text      =cardboardTypeText;
                        Stacker1CardboardStandart.Text  =cardboardStandart;
                        Stacker1ProfileId.Text          =b.CheckGet("ID_PROF");
                        Stacker1OuterColorCode.Text     = b.CheckGet("CARDBOARD_OUTER_COLOR_ID");

                        if ((bool)Stacker1Main.IsChecked)
                            {
                                mainStacker = true;
                            }
                    }
                    break;

                    case 2:
                    { 
                        Stacker2=b;
                        Stacker2.CheckAdd("UNTRIMMED_EDGE_FLAG", b.CheckGet("UNTRIMMED_EDGE"));

                        Stacker2BlankCode.Text          =b.CheckGet("BLANK_CODE");
                        Stacker2BlankName.Text          =b.CheckGet("BLANK_NAME");
                        Stacker2GoodCode.Text           =b.CheckGet("GOODS_CODE");
                        Stacker2GoodName.Text           =b.CheckGet("GOODS_NAME");
                        Stacker2TrimMin.Text            =b.CheckGet("MIN_TRIM");
                        
                        Stacker2ZCardboard.Text = "";
                        if (b.CheckGet("Z_CARDBOARD").ToBool()==true)
                        {
                            Stacker2ZCardboard.Text = "Z-картон";
                        }

                        Stacker2CardboardName.Text      =b.CheckGet("CARDBOARD_NAME");
                        Stacker2CardboardId.Text        =b.CheckGet("CARDBOARD_ID");
                        Stacker2CardboardMarkId.Text    =b.CheckGet("CARDBOARD_MARK_ID");
                        Stacker2CardboardType.Text      =cardboardTypeText;
                        Stacker2CardboardStandart.Text  =cardboardStandart;
                        Stacker2ProfileId.Text          =b.CheckGet("ID_PROF");
                        Stacker2OuterColorCode.Text     = b.CheckGet("CARDBOARD_OUTER_COLOR_ID");

                        if ((bool)Stacker2Main.IsChecked)
                        {
                            mainStacker = true;
                        }
                    }
                    break;

                    case 3:
                    { 
                        Stacker3=b;
                        Stacker3.CheckAdd("UNTRIMMED_EDGE_FLAG", b.CheckGet("UNTRIMMED_EDGE"));

                        Stacker3BlankCode.Text          =b.CheckGet("BLANK_CODE");
                        Stacker3BlankName.Text          =b.CheckGet("BLANK_NAME");
                        Stacker3GoodCode.Text           =b.CheckGet("GOODS_CODE");
                        Stacker3GoodName.Text           =b.CheckGet("GOODS_NAME");
                        Stacker3TrimMin.Text            =b.CheckGet("MIN_TRIM");
                        
                        Stacker3ZCardboard.Text = "";
                        if (b.CheckGet("Z_CARDBOARD").ToBool()==true)
                        {
                            Stacker3ZCardboard.Text = "Z-картон";
                        }

                        Stacker3CardboardName.Text      =b.CheckGet("CARDBOARD_NAME");
                        Stacker3CardboardId.Text        =b.CheckGet("CARDBOARD_ID");
                        Stacker3CardboardMarkId.Text    =b.CheckGet("CARDBOARD_MARK_ID");
                        Stacker3CardboardType.Text      =cardboardTypeText;
                        Stacker3CardboardStandart.Text  =cardboardStandart;
                        Stacker3ProfileId.Text          =b.CheckGet("ID_PROF");
                        Stacker3OuterColorCode.Text     = b.CheckGet("CARDBOARD_OUTER_COLOR_ID");

                        if ((bool)Stacker3Main.IsChecked)
                        {
                            mainStacker = true;
                        }
                    }
                    break;
                }
            }

            Form.SetValues(v);

            //bool updatePrimaryQuantity=false;
            if(resume)
            {
                var v0=Form.GetValues();

                //ширина заготовок на стекерах
                int w1 =v0.CheckGet($"STACKER1_LENGTH").ToInt();
                int w2 =v0.CheckGet($"STACKER2_LENGTH").ToInt();
                int w3 =v0.CheckGet($"STACKER3_LENGTH").ToInt();
                var cardboardId=v0.CheckGet("CARDBOARD_ID").ToInt();

                //количество занятых стекеров
                int stackersUsed=0;
                if(w1>0)
                {
                    stackersUsed++;
                }
                if(w2>0)
                {
                    stackersUsed++;
                }
                if(w3>0)
                {
                    stackersUsed++;
                }

                /*
                    поменяем картон в селекторе картона и материал, 
                    взяв его из этой заготовки                    
                 */

                bool setCardboard=false;

                //картон до сих пор не выбран
                //единственный стекер занят или никакой не занят
                
                if(cardboardId==0)
                {
                    setCardboard=true;
                }

                if(stackersUsed==1)
                {
                    setCardboard=true;
                }

                if (mainStacker && (cardboardId != b.CheckGet("CARDBOARD_ID").ToInt()))
                {
                    setCardboard = true;
                }

                //в режиме "выбор композиции вручную" не меняем
                if (ManualComposition)
                {
                    setCardboard=false;
                }

                //в режимах, отличных от "создание" не меняем
                if (Mode != 0 && Mode != 4)
                {
                    setCardboard=false;
                }

                if(copy)
                {
                    setCardboard=false;
                }

                v=new Dictionary<string, string>();
                if(setCardboard)
                {
                    v.CheckAdd("CARDBOARD_ID", b.CheckGet("CARDBOARD_ID").ToInt().ToString());
                }
                else
                {
                    //v.CheckAdd("CARDBOARD_ID", "0");
                }

                Form.SetValues(v);

            }


            SchemeImage.Source=null;
            if(resume)
            {
                Calculate();
            }

            ChainUpdate=true;

           

            
            if(resume)
            {
                var cardboardId=v.CheckGet("CARDBOARD_ID").ToInt();
                if(cardboardId!=0)
                {
                    LoadLayers(cardboardId);
                }
            }
            

        }

        /// <summary>
        /// вычисление веса бумаги на определенном слое сырьевой композиции
        /// (это значение заносится в поле "В ПЗ, кг" в списке слоев)
        /// </summary>
        public int GetLayerWeight(double length,int format,int density,int profileId,int layer)
        {
            /*
                length  -- длина, м.
                format  -- ширина, мм
                density -- плотность, г/кв мм
                profileId -- ID профиля
                layer -- номер слоя (1-5)

             */

            int result = 0;

            double length2 = (double)length;
            if (ProfileParams.ContainsKey(profileId))
            {
                var p = ProfileParams[profileId];
                if (layer == 2)
                {
                    var k = p.CheckGet("CORRUGATE_LAYER2").ToDouble();
                    length2 = Math.Ceiling((double)length * k);
                }
                else if (layer == 4)
                {
                    var k = p.CheckGet("CORRUGATE_LAYER4").ToDouble();
                    length2 = Math.Ceiling((double)length * k);
                }
            }

            /*
                format  = 2300 мм   *10^-3  => 2.3 m 
                density = 125 g/m^2 *10^-3  => 0.125 kg/m^2
             */
            result = (int)(length2 * format * density / 1000000);

            return result;
        }

        /// <summary>
        /// очистка данных стекера
        /// </summary>
        private void ClearStackerBlank(int stackerId)
        {
            var resume=true;

            if(resume)
            {
                if(stackerId==0)
                {
                    resume=false;
                    ReportAdd("Неверно указан стекер");
                }
            }

            if(resume)
            {
                var v=Form.GetValues();
                var x=stackerId;
                
                { 
                    v.CheckAdd($"STACKER{x}_POSITION_ID",  "0");
                    v.CheckAdd($"STACKER{x}_POSITION_NAME", "");
                    v.CheckAdd($"_STACKER{x}_POSITION_ID",  "0");
                    v.CheckAdd($"STACKER{x}_BLANK_ID",     "");
                    v.CheckAdd($"STACKER{x}_GOOD_ID",      "");
                        
                    v.CheckAdd($"STACKER{x}_BLANK",        "");
                    v.CheckAdd($"STACKER{x}_CARDBOARD",    "");                    

                    v.CheckAdd($"STACKER{x}_CREASE",       "");
                    v.CheckAdd($"STACKER{x}_LENGTH",       "");
                    v.CheckAdd($"STACKER{x}_WIDTH",        "");
                    v.CheckAdd($"STACKER{x}_QUANTITY",     "");
                    v.CheckAdd($"STACKER{x}_QUANTITY_LIMIT",     "");
                    v.CheckAdd($"STACKER{x}_QUANTITY_LOWER","");
                    v.CheckAdd($"STACKER{x}_QUANTITY_UPPER","");
                    v.CheckAdd($"STACKER{x}_QUANTITY_CALCULATED","");
                    v.CheckAdd($"STACKER{x}_PRODUCTS_FROM_BLANK","");
                    v.CheckAdd($"STACKER{x}_PALLET","");

                    v.CheckAdd($"STACKER{x}_THREADS",      "0");
                }

                var emptyDS = new ListDataSet();
                emptyDS.Init();
                
                switch(stackerId)
                {
                    case 1:
                    { 
                        Stacker1=new Dictionary<string, string>();
                        Stacker1ProfileId.Text = "";
                        Stacker1OuterColorCode.Text = "";
                        Stacker1ApplicationDS = emptyDS;
                    }
                    break;

                    case 2:
                    { 
                        Stacker2=new Dictionary<string, string>();
                        Stacker2ProfileId.Text = "";
                        Stacker2OuterColorCode.Text = "";
                        Stacker2ApplicationDS = emptyDS;
                    }
                    break;

                    case 3:
                    { 
                        Stacker3=new Dictionary<string, string>();
                        Stacker3ProfileId.Text = "";
                        Stacker3OuterColorCode.Text = "";
                        Stacker3ApplicationDS = emptyDS;
                    }
                    break;
                }

                Form.SetValues(v);
            }

            SchemeImage.Source=null;
            if(resume)
            {           
                EnableCalcQuantity();
                Calculate();
            }
        }
        
        /// <summary>
        /// обновление веса сырья для слоя
        /// (блок сырьевой композиции)
        /// </summary>
        //private bool UpdateLayerWeights(Dictionary<string,string> selectedItem, int layer)
        private bool UpdateLayerWeights(int paperId, int layer)
        {
            bool result=true;
            bool resume=true;

            var v2=new Dictionary<string,string>();
            int x=layer;
            
            var v=Form.GetValues();
            int length=v.CheckGet($"CUTTING_LENGTH").ToInt();
            int format=v.CheckGet($"FORMAT").ToInt();
            int profileId=v.CheckGet($"CARDBOARD_PROFILE_ID").ToInt();

            v2.CheckAdd($"LAYER{x}_RESIDUE_WEIGHT","0");
            v2.CheckAdd($"LAYER{x}_PT_WEIGHT","0");
            //v2.CheckAdd($"LAYER{x}_TASK_WEIGHT","0");

            /*
                Layer1TaskWeight        -- расход
                Layer1ResidueWeight     -- остаток на складе
             */

            var control=Layer1TaskWeight;
            switch(layer)
            {
                case 1:
                    control=Layer1TaskWeight;
                    break;

                case 2:
                    control=Layer2TaskWeight;
                    break;

                case 3:
                    control=Layer3TaskWeight;
                    break;

                case 4:
                    control=Layer4TaskWeight;
                    break;

                case 5:
                    control=Layer5TaskWeight;
                    break;
            }

            SetControlBorder(control);
            
            if (resume)
            {
                if (!ChainUpdate)
                {
                    resume=false;
                }
            }
            
            if(resume)
            {
                Central.Logger.Trace($"....UpdateLayerWeights [{layer}]");
                
                if(!PaperListDS.Initialized)
                {
                    resume=false;
                }
            }

            if(resume)
            {
                if(length<=0)
                {
                    resume=false;
                }

                if(format<=0)
                {
                    resume=false;
                }

                if(profileId<=0)
                {
                    resume=false;
                }
            }

            if(resume)
            {
                //int paperId=selectedItem.CheckGet("ID").ToInt();
                if(paperId>0)
                {
                    foreach(Dictionary<string,string> row in PaperListDS.Items)
                    {
                        if(row.CheckGet("ID").ToInt()==paperId)
                        {
                            int usedInAllTasks=0;
                            int residuInStock=0;
                            int usedInCurrentTask=0;

                            //всего в пз
                            { 
                                //usedInAllTasks=row.CheckGet("BALANCE_CM").ToInt();
                                usedInAllTasks=row.CheckGet("BALANCE_CM").ToInt();
                                if(usedInAllTasks>0)
                                {
                                    v2.CheckAdd($"LAYER{x}_PT_WEIGHT", usedInAllTasks.ToString());
                                    //result=true;
                                    Central.Logger.Trace($"........[{layer}] [{paperId}] pt[{usedInAllTasks}]");
                                }
                            }

                            //остаток на складе
                            { 
                                residuInStock=row.CheckGet("BALANCE_STOCK").ToInt();
                                if(residuInStock>0)
                                {
                                    int rollsQty = row.CheckGet("BALANCE_ROLLS").ToInt();
                                    v2.CheckAdd($"LAYER{x}_RESIDUE_WEIGHT", residuInStock.ToString());
                                    //result=true;
                                    Central.Logger.Trace($"........[{layer}] [{paperId}] stock[{residuInStock}]");

                                    v2.CheckAdd($"LAYER{x}_STOCK_ROLLS", rollsQty.ToString());
                                }
                            }

                            //в задании
                            int density=row.CheckGet("DENSITY").ToInt();
                            if(density>0)
                            {
                                usedInCurrentTask=GetLayerWeight(length, format, density, profileId, layer);
                                v2.CheckAdd($"LAYER{x}_TASK_WEIGHT",usedInCurrentTask.ToString());       
                            }

                            if(usedInCurrentTask>0)
                            {
                                if((usedInCurrentTask+usedInAllTasks) > residuInStock)
                                {
                                    SetControlBorder(control,1);
                                    result=false;
                                }                                
                            }

                        }
                    }
                }
                
            }

            Form.SetValues(v2);

            return result;
        }


        /// <summary>
        /// проверки задания перед сохранением
        /// </summary>
        private bool Check()
        {
            var result=true;
            var resume=true;

            var v=Form.GetValues();
            
            //ширина заготовок на стекерах
            int w1 =v.CheckGet($"STACKER1_LENGTH").ToInt();
            int w2 =v.CheckGet($"STACKER2_LENGTH").ToInt();
            int w3 =v.CheckGet($"STACKER3_LENGTH").ToInt();

            //количество занятых стекеров
            int stackersUsed=0;
            if(w1>0)
            {
                stackersUsed++;
            }
            if(w2>0)
            {
                stackersUsed++;
            }
            if(w3>0)
            {
                stackersUsed++;
            }


            if(resume)
            {

            }

            if(resume)
            {

            }

            return result;
        }

        private void EnableCalcQuantity()
        {
            if(ChainUpdate)
            {
                CalcQuantity=true;
            }            
        }

        /// <summary>
        /// Проверка совпадения всех профилей изделий и выбранного картона
        /// </summary>
        /// <returns>true, если все заполненые профили совпадают, иначе false</returns>
        private bool CheckProfiles()
        {
            var profile1 = 0;
            bool result = true;

            if (!string.IsNullOrEmpty(Stacker1ProfileId.Text))
            {
                profile1 = Stacker1ProfileId.Text.ToInt();
            }

            if (!string.IsNullOrEmpty(CardboardProfileId.Text))
            {
                if (profile1 == 0)
                {
                    profile1 = CardboardProfileId.Text.ToInt();
                }
                else if (profile1 != CardboardProfileId.Text.ToInt())
                {
                    result = false;
                }
            }

            if (!string.IsNullOrEmpty(Stacker2ProfileId.Text))
            {
                if (profile1 == 0)
                {
                    profile1 = CardboardProfileId.Text.ToInt();
                }
                else if (profile1 != Stacker2ProfileId.Text.ToInt())
                {
                    result = false;
                }
            }

            if (!string.IsNullOrEmpty(Stacker3ProfileId.Text))
            {
                if (profile1 != Stacker3ProfileId.Text.ToInt())
                {
                    result = false;
                }
            }

            if (!result)
            {
                var s = "Все профили должны быть одинаковыми";
                ReportAdd(s);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private bool CheckLayerColors()
        {
            bool result = true;
            var outerColorId = CardboardColorId.Text.ToInt();

            if (!string.IsNullOrEmpty(Stacker1OuterColorCode.Text))
            {
                if (outerColorId == 0)
                {
                    outerColorId = Stacker1OuterColorCode.Text.ToInt();
                }
                else if (outerColorId != Stacker1OuterColorCode.Text.ToInt())
                {
                    result = false;
                }
            }

            if (!string.IsNullOrEmpty(Stacker2OuterColorCode.Text))
            {
                if (outerColorId == 0)
                {
                    outerColorId = Stacker2OuterColorCode.Text.ToInt();
                }
                else if (outerColorId != Stacker2OuterColorCode.Text.ToInt())
                {
                    result = false;
                }
            }

            if (!string.IsNullOrEmpty(Stacker3OuterColorCode.Text))
            {
                if (outerColorId == 0)
                {
                    outerColorId = Stacker3OuterColorCode.Text.ToInt();
                }
                else if (outerColorId != Stacker3OuterColorCode.Text.ToInt())
                {
                    result = false;
                }
            }


            // Если отмечен выбор слоев вручную, проверяем цвета бумаги
            if (result && (bool)UseManualComposition.IsChecked)
            {
                // внешний слой
                string colorCode = Layer5Color.Text;
                if (Layer5Color.Text.ToInt() > 1)
                {
                    colorCode = "2";
                }

                int innerLayerColor = 0;
                if (!string.IsNullOrEmpty(Layer1Color.Text))
                {
                    innerLayerColor = Layer1Color.Text.ToInt();
                }
                else if (!string.IsNullOrEmpty(Layer3Color.Text))
                {
                    innerLayerColor = Layer3Color.Text.ToInt();
                }


                if (innerLayerColor > 1)
                {
                    innerLayerColor = 2;
                }

                colorCode = colorCode + innerLayerColor.ToString();
                int compositionOuterColorId = 0;

                switch (colorCode)
                {
                    // белый / белый
                    case "11":
                        compositionOuterColorId = 3;
                        break;

                    // белый / бурый
                    case "12":
                        compositionOuterColorId = 1;
                        break;

                    // бурый / белый
                    case "21":
                        compositionOuterColorId = 4;
                        break;

                    // бурый / бурый
                    case "22":
                        compositionOuterColorId = 2;
                        break;
                }

                if ((compositionOuterColorId > 0) && (compositionOuterColorId != outerColorId))
                {
                    result = false;
                }
            }

            if (!result)
            {
                var s = "Не совпадают цвета внешних слоев";
                ReportAdd(s);
            }

            return result;
        }

        /// <summary>
        /// Возвращает свойства картона: количество слоев и количество слоев из целлюлозы
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, int> GetLayersParams()
        {
            // значения по умолчанию
            var result = new Dictionary<string, int>()
            {
                { "LAYERS_QUANTITY", 3 },
                { "CELLULOSE_LAYERS_QUANTITY", 0 },
            };

            WeakCorrugatedLayerFlag = false;

            // По умолчанию 3 слоя. Если заполнены и 2й, и 4й слой, то 5 слоев
            if (!string.IsNullOrEmpty(Layer2Paper.Text) && !string.IsNullOrEmpty(Layer4Paper.Text))
            {
                result["LAYERS_QUANTITY"] = 5;
            }

            var v = Form.GetValues();
            for (var i = 1; i <= 5; i++)
            {
                var layerId = v.CheckGet($"LAYER{i}_ID").ToInt();
                if (layerId > 0)
                {
                    var paper = PaperListDS.Items.FirstOrDefault(x => x["ID"].ToInt() == layerId);
                    if (paper.CheckGet("RAW_TYPE").ToInt() == 1)
                    {
                        result["CELLULOSE_LAYERS_QUANTITY"]++;
                    }

                    if (i == 2 || i == 4)
                    {
                        // Если гофрослой склеенный, пропускаем
                        if (v.CheckGet($"LAYER{i}_GLUING").ToInt() == 0)
                        {
                            int density = paper.CheckGet("DENSITY").ToInt();
                            if (density <= 80)
                            {
                                if (MinDurableWidth < 600)
                                {
                                    MinDurableWidth = 600;
                                }
                                WeakCorrugatedLayerFlag = true;
                            }
                            else if (density <= 100)
                            {
                                if (MinDurableWidth < 400)
                                {
                                    MinDurableWidth = 400;
                                }
                                WeakCorrugatedLayerFlag = true;
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Возвращает минимальный размер обрези по матрице
        /// </summary>
        /// <param name="p">Набор параметров для расчета</param>
        /// <param name="getTrim">Рассчитывать обрезь, если false, то базовую обрезь для формирования примечания</param>
        /// <returns></returns>
        private int GetMinTrim(List<Dictionary<string, int>> p, bool getTrim=true)
        {
            int result = 0;
			var layersParams = GetLayersParams();
            // одна заготовка
            if (p.Count == 1)
            {
                var c1 = p[0];
                if (c1["Z_CARDBOARD"] == 1)
                {
                    if (layersParams["LAYERS_QUANTITY"] == 3)
                    {
                        result = 45;
                    }
                    else
                    {
                        result = 50;
                    }
                }
                else if ((c1["FULL_STAMP_RT"] == 1) && getTrim)
                {
                    if (layersParams["LAYERS_QUANTITY"] == 3)
                    {
                        result = 22;
                    }
                    else
                    {
                        result = 24;
                    }
                }
                else
                {
                    if (layersParams["LAYERS_QUANTITY"] == 3)
                    {
                        if (layersParams["CELLULOSE_LAYERS_QUANTITY"] < 2)
                        {
                            result = 37;
                        }
                        else
                        {
                            result = 40;
                        }
                    }
                    else
                    {
                        result = 45;
                    }

                }

            }
            else
            // две заготовки
            {
                var c1 = p[0];
                var c2 = p[1];
                // z-картон
                if (c1["Z_CARDBOARD"] == 1)
                {
                    // 3 слоя
                    if (layersParams["LAYERS_QUANTITY"] == 3)
                    {
                        if (c2["Z_CARDBOARD"] == 1)
                        {
                            // z-картон
                            result = 45;
                        }
                        else if (c2["FULL_STAMP_RT"] == 1)
                        {
                            // полный штамп
                            result = 45;
                        }
                        else
                        {
                            // обрезная кромка
                            result = 45;
                        }
                    }
                    else
                    // 5 слоев
                    {
                        if (c2["Z_CARDBOARD"] == 1)
                        {
                            // z-картон
                            result = 50;
                        }
                        else if (c2["FULL_STAMP_RT"] == 1)
                        {
                            // полный штамп
                            result = 50;
                        }
                        else
                        {
                            // обрезная кромка
                            result = 50;
                        }
                    }
                }
                else if (c1["FULL_STAMP_RT"] == 1)
                // полный штамп
                {
                    // 3 слоя
                    if (layersParams["LAYERS_QUANTITY"] == 3)
                    {
                        if (c2["Z_CARDBOARD"] == 1)
                        {
                            // z-картон
                            result = 45;
                        }
                        else if (c2["FULL_STAMP_RT"] == 1)
                        {
                            // полный штамп
                            result = 25;
                        }
                        else
                        {
                            // обрезная кромка
                            result = 37;
                        }
                    }
                    else
                    // 5 слоев
                    {
                        if (c2["Z_CARDBOARD"] == 1)
                        {
                            // z-картон
                            result = 50;
                        }
                        else if (c2["FULL_STAMP_RT"] == 1)
                        {
                            // полный штамп
                            result = 28;
                        }
                        else
                        {
                            // обрезная кромка
                            result = 37;
                        }
                    }
                }
                else
                // обрезная кромка
                {
                    if (layersParams["LAYERS_QUANTITY"] == 3)
                    {
                        if (layersParams["CELLULOSE_LAYERS_QUANTITY"] < 2)
                        {
                            if (c2["Z_CARDBOARD"] == 1)
                            {
                                // z-картон
                                result = 45;
                            }
                            else if (c2["FULL_STAMP_RT"] == 1)
                            {
                                // полный штамп
                                result = 37;
                            }
                            else
                            {
                                // обрезная кромка
                                result = 37;
                            }
                        }
                        else
                        {
                            if (c2["Z_CARDBOARD"] == 1)
                            {
                                // z-картон
                                result = 45;
                            }
                            else if (c2["FULL_STAMP_RT"] == 1)
                            {
                                // полный штамп
                                result = 40;
                            }
                            else
                            {
                                // обрезная кромка
                                result = 40;
                            }
                        }
                    }
                    else
                    {
                        if (c2["Z_CARDBOARD"] == 1)
                        {
                            // z-картон
                            result = 50;
                        }
                        else if (c2["FULL_STAMP_RT"] == 1)
                        {
                            // полный штамп
                            result = 45;
                        }
                        else
                        {
                            // обрезная кромка
                            result = 45;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Проверка на признак Тандем
        /// </summary>
        /// <param name="cc">количество рилевок</param>
        /// <param name="t">количество ручьев</param>
        /// <param name="format">формат</param>
        /// <param name="profileId">профиль</param>
        /// <returns></returns>
        private bool CheckTandem(int cc, int t, int format=2500, int profileId=1)
        {
            bool tandem = false;
            // тип га: 1 - BHS, 2 - Fosber
            int machineTypeId = 1;

            // Формат больше 2500 только на Фосбер
            if (format > 2500)
            {
                machineTypeId = 2;
            }
            // Профили С, ВС, СВ едут преимущественно на Фосбере
            else if (profileId.ContainsIn(2, 3, 10))
            {
                machineTypeId = 2;
            }
            // Профиль В на формате 2500 едут на Фосбере
            else if (profileId == 1 && format == 2500)
            {
                machineTypeId = 2;
            }

            if (machineTypeId == 1)
            {
                tandem = (t > 7) || (cc > 14);
            }
            else
            {
                tandem = (t > 6) || (cc > 11);
            }

            return tandem;
        }

        /// <summary>
        /// Проверка наличия связи изделия на стекере с заявкой
        /// </summary>
        /// <param name="stackerData"></param>
        private void CheckApplication(Dictionary<string, string> stackerData)
        {
            int b = stackerData.CheckGet("BLANK_ID").ToInt();

            if (b > 0)
            {
                int pos1 = stackerData.CheckGet("POSITION_ID").ToInt();

                // Если не указана заявка, проверим что производится.
                if (pos1 == 0)
                {
                    bool utility = false;
                    string name1 = stackerData.CheckGet("BLANK_NAME");
                    if (!string.IsNullOrEmpty(name1))
                    {
                        var names1 = name1.Split(' ');
                        if ((names1[0] == "Перестил") || (names1[0] == "Образец"))
                            utility = true;
                    }

                    if (!utility)
                    {
                        var s = $"Выберите заявку на {stackerData.CheckGet("STACKER_ID")}м стекере";
                        ReportAdd(s);
                    }
                }
            }
        }

        /// <summary>
        /// расчет параметров раскроя, главная рабочая функция
        /// не срабатывает в режиме "просмотр"
        /// </summary>
        private void Calculate(bool updatePrimaryQuantity=false, bool updateMap=true)
        {
            /*
                updatePrimaryQuantity -- обновить количество на главном стекере
                (при постановке заготовки на стекер, при выборе главного стекера)
             */
            var resume=true;
            ChainUpdate = false;
            SetStatus("", false);
            
            var v=Form.GetValues();
            int taskLenght=0;
            int taskWidth=0;         
            int format=0;
            int trim=0;
            double trimPercentage=0;
            int trimMin=0;
            bool goodCutting=true;
            //флаг "задание готово к сохранению"
            //если флаг опущен, кнопка "сохранить" неактивна
            bool taskComplete=true;

            Stackers=new Dictionary<int,Dictionary<string, string>>();
            Stackers.Add(1,Stacker1);
            Stackers.Add(2,Stacker2);
            Stackers.Add(3,Stacker3);
            
            Report="";

            //длина заготовок на стекерах
            int w1 =v.CheckGet($"STACKER1_LENGTH").ToInt();
            int w2 =v.CheckGet($"STACKER2_LENGTH").ToInt();
            int w3 =v.CheckGet($"STACKER3_LENGTH").ToInt();

            //количество занятых стекеров
            int stackersUsed=0;
            if(w1>0)
            {
                stackersUsed++;
            }
            if(w2>0)
            {
                stackersUsed++;
            }
            if(w3>0)
            {
                stackersUsed++;
            }


            //журнал
            if(resume)
            {
                Central.Logger.Debug($"Calculate");
            }


            //очистка расчетных полей
            if(resume)
            {
                v.CheckAdd("TRIM_PERCENTAGE","0");
                v.CheckAdd("CUTTING_WIDTH", "0");
                v.CheckAdd("TRIM", "0");

                if (Mode!=2)
                {
                    v.CheckAdd("CUTTING_LENGTH","0");
                }
                
                SetControlBorderTooltip(Stacker1QuantityCalculated,0);
                SetControlBorderTooltip(Stacker2QuantityCalculated,0);
                SetControlBorderTooltip(Stacker3QuantityCalculated,0);

                TrimTPercentage.Text="";
                TrimTAbsolute.Text="";
                TrimTMin.Text="";
            }

            // заготовка для Roda. Проверка только для Липецка
            if (Stacker1.ContainsKey("POSITION_ID") && (FactoryId == 1))
            {
                int schemeId = Stacker1.CheckGet("PRODUCTION_SCHEME_ID").ToInt();
                if (schemeId.ContainsIn(42, 81, 122, 1470, 1546, 1549, 1550, 1561))
                {
                    ReportAdd("Заготовка для RodaSC должна быть на втором стекере");
                    taskComplete = false;
                    resume = false;
                }
            }
          

            //формат
            if(resume)
            {
                if(stackersUsed>0)
                {
                    if(v.CheckGet("FORMAT").ToInt()==0)
                    {
                        var s=$"Не выбран формат";
                        SetControlBorder(FormatList,1);
                        ReportAdd(s);
                        taskComplete=false;
                        resume=false;
                    }
                    else
                    {
                        format=v.CheckGet("FORMAT").ToInt();
                        trimMin=format;
                        SetControlBorder(FormatList,0);
                    }
                }
            }


            //заполнен хотя бы один стекер
            //выбраны заготовки
            if(resume)
            {
                if(stackersUsed==0)
                {
                    resume=false;
                }
            }


            if(resume)
            {
                CheckStackersPriority();
            }


            //активный стекер
            if(resume)
            {
                if(ActiveStacker==0)
                {
                    resume=false;
                    ReportAdd("Не выбран главный стекер");
                }
            }


            //главный стекер
            if(resume)
            {
                if(StackerPrimary==0)
                {
                    resume=false;
                    ReportAdd("Не выбран главный стекер");
                }
            }

            if (resume)
            {
                //Для Каширы обязательно должен быть заполнен 1 стекер
                if ((FactoryId == 2) && (w1 == 0))
                {
                    ReportAdd("На стекере 1 должна стоять заготовка");
                    resume = false;
                    taskComplete=false;
                }
            }

            //ограничения раскроя
            if (resume)
            {
                // количество рилевок
                var cc1 = 0;
                var cc2 = 0;

                // количество ручьев
                var t1 = 0;
                var t2 = 0;

                // тип рилевок
                var c1 = 0;
                var c2 = 0;

                if (StackerPrimary != 0)
                {
                    c1 = Stackers[StackerPrimary].CheckGet($"CREASETYPE").ToInt();
                    t1 = v.CheckGet($"STACKER{StackerPrimary}_THREADS").ToInt();
                    cc1 = Stackers[StackerPrimary].CheckGet($"CREASE_COUNT").ToInt();
                }

                if (StackerSecondary != 0)
                {
                    c2 = Stackers[StackerSecondary].CheckGet($"CREASETYPE").ToInt();
                    t2 = v.CheckGet($"STACKER{StackerSecondary}_THREADS").ToInt();
                    cc2 = Stackers[StackerSecondary].CheckGet($"CREASE_COUNT").ToInt();
                }

                var t = t1 + t2;
                var cc = t1 * cc1 + t2 * cc2;

                if (FactoryId == 2)
                {
                    if (t2 == 0)
                    {
                        if (t1 > 14)
                        {
                            ReportAdd("Для Каширы не более 14 ручьев для самокройного изделия");
                            taskComplete = false;
                            resume = false;
                        }
                    }
                    //else if ((t1 > 7 && t2 > 0) || (t2 > 7 && t1 > 0))
                    else if (t1 + t2 > 7)
                    {
                        //ReportAdd("Для Каширы не более 7 ручьев на изделие");
                        ReportAdd("Для Каширы суммарно не более 7 ручьев");
                        taskComplete = false;
                        resume = false;
                    }

                    int wdt1 = Stackers[StackerPrimary].CheckGet($"WIDTH").ToInt();

                    if (t1 * wdt1 < 1100)
                    {
                        ReportAdd("Для Каширы на 1 стекере ширина не менее 1100");
                    }
                }

                if (FactoryId == 1)
                {
                    if (format > 2500)
                    {

                        //рилевка плоская и ручьев > 10
                        if (c1 == 2 || c2 == 2)
                        {
                            if (t > 10)
                            {
                                var s = "Запрет раскроя:\nПлоская рилевка и более 10 ручьев для формата >2500";
                                ReportAdd(s);
                                taskComplete = false;
                                resume = false;
                            }
                            else
                            {
                                if (cc > 20)
                                {
                                    var s = "Запрет раскроя:\nПлоская рилевка и более 20 рилевок для формата >2500";
                                    ReportAdd(s);
                                    taskComplete = false;
                                    resume = false;
                                }
                            }
                        }

                        //запрет раскроя более 12 ручьев
                        if (resume && (t > 12))
                        {
                            var s = "Запрет раскроя:\n более 12 ручьев на формате >2500";
                            ReportAdd(s);
                            taskComplete = false;
                            resume = false;
                        }

                        // запрет раскроя если рилевок больше 24
                        if (resume && (cc > 24))
                        {
                            var s = "Запрет раскроя:\nРилевок более 24 для формата >2500";
                            ReportAdd(s);
                            taskComplete = false;
                            resume = false;
                        }
                    }
                    else
                    {
                        if (cc > 28)
                        {
                            var s = "Запрет раскроя:\nРилевок более 28";
                            ReportAdd(s);
                            taskComplete = false;
                            resume = false;
                        }
                    }
                }

                // Проверка на признак Тандем
                int profileId = v.CheckGet("CARDBOARD_PROFILE_ID").ToInt();

                var tandemFlag = CheckTandem(cc, t, format, profileId);

                if (tandemFlag)
                {
                    ReportAdd("Задание с признаком Тандем");
                }
            }

            //в режиме "просмотр" не выполняем
            //if(Mode==2)
            //{
            //    //resume=false;
            //}

            //если на 2 стекере заготовка длинее 3600 -- блокировка
            if (resume)
            {
                if (format <= 2500)
                {
                    if(w2 > 3600)
                    {
                        resume = false;
                        //Если нет спецправ, то блокируем сохранение
                        taskComplete = MasterRight;
                        ReportAdd("На стекере 2 не должна быть заготовка длинее 3600 мм.");
                    }
                }
                else
                {
                    //Для Фосбера максимальная длина больше
                    if (w2 > 3900)
                    {
                        resume = false;
                        //Если нет спецправ, то блокируем сохранение
                        taskComplete = MasterRight;
                        ReportAdd("На стекере 2 не должна быть заготовка длинее 3900 мм.");
                    }
                }
            }

            // минимальная ширина заготовки
            if (resume)
            {
                if (Stacker1 != null)
                {
                    if (Stacker1.Count > 0)
                    {
                        var bw1 = Stacker1.CheckGet("WIDTH").ToInt();
                        if ((format > 2500) && (bw1 > 0) && (bw1 < 270))
                        {
                            //resume = false;
                            //taskComplete = false;
                            ReportAdd("На Фосбере заготовка шириной меньше 270 мм.");
                        }
                    }
                }

                if (Stacker2 != null)
                {
                    if (Stacker2.Count > 0)
                    {
                        var bw2 = Stacker2.CheckGet("WIDTH").ToInt();
                        if ((format > 2500) && (bw2 > 0) && (bw2 < 270))
                        {
                            //resume = false;
                            //taskComplete = false;
                            ReportAdd("На Фосбере заготовка шириной меньше 270 мм.");
                        }
                        else if ((bw2 > 0) && (bw2 < 300) && (FactoryId == 1))
                        {
                            // В Кашире не проверяем
                            //resume = false;
                            //taskComplete = false;
                            // Только предупреждение. Сохранение не блокируется. Требование Летуновской
                            ReportAdd("На 2-м стекере заготовка шириной меньше 300 мм.");
                        }
                    }
                }
            }

            //минимальная обрезь
            if(resume)
            {
                var trimParams = new List<Dictionary<string, int>>();
                if (Stacker1 != null)
                {
                    if (Stacker1.Count > 0)
                    {
                        var c1 = new Dictionary<string, int>()
                        {
                            { "Z_CARDBOARD", Stacker1.CheckGet("Z_CARDBOARD").ToInt() },
                            { "FULL_STAMP_RT", Stacker1.CheckGet("FULL_STAMP_RT").ToInt() },
                            { "TREADS", v.CheckGet($"STACKER1_THREADS").ToInt() }
                        };
                        trimParams.Add(c1);
                    }
                }
                if (Stacker2 != null)
                {
                    if (Stacker2.Count > 0)
                    {
                        var c2 = new Dictionary<string, int>()
                        {
                            { "Z_CARDBOARD", Stacker2.CheckGet("Z_CARDBOARD").ToInt() },
                            { "FULL_STAMP_RT", Stacker2.CheckGet("FULL_STAMP_RT").ToInt() },
                            { "TREADS", v.CheckGet($"STACKER2_THREADS").ToInt() }
                        };
                        trimParams.Add(c2);
                    }
                }
                if (Stacker3 != null)
                {
                    if (Stacker3.Count > 0)
                    {
                        var c3 = new Dictionary<string, int>()
                        {
                            { "Z_CARDBOARD", Stacker3.CheckGet("Z_CARDBOARD").ToInt() },
                            { "FULL_STAMP_RT", Stacker3.CheckGet("FULL_STAMP_RT").ToInt() },
                            { "TREADS", v.CheckGet($"STACKER3_THREADS").ToInt() }
                        };
                        trimParams.Add(c3);
                    }
                }

                if (trimParams.Count > 0)
                {
                    if ((trimParams.Count == 1) && trimParams[0]["TREADS"] > 1)
                    {
                        var c = trimParams[0];
                        trimParams.Add(c);
                    }
                    trimMin = GetMinTrim(trimParams);

                    TrimTMin.Text = trimMin.ToString();
                }
            }

            //Z-картон
            if (resume)
            {
                if (Stacker1!=null)
                {
                    if (Stacker1.CheckGet("Z_CARDBOARD").ToBool()==true)
                    {
                        var s = "На стекере 1 установлен Z-картон.\nПереместите заготовку на стекер 2 или 3";
                        ReportAdd(s);
                        taskComplete=false;
                    }
                }

                // Проверяем, что Z-картона суммарно не больше 2 ручьев
                int zThreads = 0;
                bool emptyStacker3 = true;
                if (Stacker2 != null)
                {
                    if (Stacker2.CheckGet("Z_CARDBOARD").ToBool() == true)
                    {
                        zThreads += v.CheckGet($"STACKER2_THREADS").ToInt();
                    }
                }

                if (Stacker3 != null)
                {
                    if (Stacker3.CheckGet("Z_CARDBOARD").ToBool() == true)
                    {
                        zThreads += v.CheckGet($"STACKER3_THREADS").ToInt();
                        emptyStacker3 = false;
                    }
                }

                if (zThreads > 2)
                {
                    var s = $"Ручьев Z-картона больше 2!";
                    ReportAdd(s);
                    taskComplete = false;
                }
                else if ((zThreads == 2) && emptyStacker3)
                {
                    // Делаем самокройный картон в 2 ручья. Поддонов должно быть чётное количество
                    double qty = v.CheckGet("STACKER2_QUANTITY_CALCULATED").ToDouble();
                    int productsInPallet = Stacker2.CheckGet("PRODUCTS_IN_PALLET").ToInt();
                    int palletQty = Math.Ceiling(qty / productsInPallet).ToInt();

                    if ((palletQty % 2) == 1)
                    {
                        ReportAdd("Для двух ручьев z-картона количество поддонов должно быть четным");
                        taskComplete = false;
                    }
                }
            }

            if (resume)
            {
                if (Stacker1!=null && Stacker2!=null)
                {
                    if (
                        Stacker1.CheckGet("MACHINE_ID").ToInt() != 0
                        && Stacker2.CheckGet("MACHINE_ID").ToInt() != 0
                        && Stacker1.CheckGet("MACHINE_ID").ToInt() != Stacker2.CheckGet("MACHINE_ID").ToInt() 
                    )
                    {
                        var s = "У заготовок заданы разные станки (в техкарте)";
                        ReportAdd(s);
                        taskComplete=false;
                    }
                }
            }

            if (resume)
            {
                if (Stacker1!=null && Stacker2!=null)
                {
                    if (
                        (
                            Stacker1.CheckGet("BLANK_ID") == Stacker2.CheckGet("BLANK_ID") 
                            && !string.IsNullOrEmpty(Stacker1.CheckGet("BLANK_ID"))
                        ) 
                        ||
                        (
                            Stacker1.CheckGet("GOOD_ID") == Stacker2.CheckGet("GOOD_ID") 
                            && !string.IsNullOrEmpty(Stacker1.CheckGet("GOOD_ID"))
                        )
                    )
                    {
                        var s = "На стекерах 1 и 2  установлены одинаковые заготовки.\nЗаготовки должны быть разными.";
                        ReportAdd(s);
                        taskComplete=false;
                    }
                }
            }

            // Все профили должны быть одинаковыми
            if (resume)
            {
                var checkProfile = CheckProfiles();
                taskComplete = taskComplete && checkProfile;
            }

            // Цвета внешних слоев должны быть одинаковыми
            if (resume)
            {
                var checkColors = CheckLayerColors();
                taskComplete = taskComplete && checkColors;
            }

            // Z-картон с печатью двумя потоками
            if (resume)
            {
                if (Stacker2.CheckGet("Z_CARDBOARD").ToBool() && Stacker3.CheckGet("Z_CARDBOARD").ToBool())
                {
                    var col2 = Stacker2.CheckGet("PRINTING_COLOR");
                    var col3 = Stacker3.CheckGet("PRINTING_COLOR");
                    var width2 = Stacker2.CheckGet("WIDTH").ToInt();
                    var width3 = Stacker3.CheckGet("WIDTH").ToInt();

                    // Если определены цвета на обоих листах, цвета должны быть одинаковыми и больший лист должен быть на 2 стекере (справа)
                    if (!string.IsNullOrEmpty(col2) && !string.IsNullOrEmpty(col3))
                    {
                        if (col2 != col3)
                        {
                            taskComplete = false;
                            ReportAdd("Цвета печати должны быть одинаковыми");
                        }

                        if (width2 < width3)
                        {
                            taskComplete = false;
                            ReportAdd("Больший лист должен быть на 2 стекере");
                        }
                    }
                    // Если печать на одном листе, то печать должна быть на 2 стекере (справа)
                    else if (!string.IsNullOrEmpty(col3))
                    {
                        taskComplete = false;
                        ReportAdd("Переместите лист с печатью на 2 стекер");
                    }

                    if (string.IsNullOrEmpty(col2) && string.IsNullOrEmpty(col3))
                    {
                        if (width2 < width3)
                        {
                            taskComplete = false;
                            ReportAdd("Больший лист должен быть на 2 стекере");
                        }
                    }
                }
            }

            // Проверка заявок на стекерах
            CheckApplication(Stacker1);
            CheckApplication(Stacker2);
            CheckApplication(Stacker3);

            //марка картона
            SetControlBorder(Stacker1Cardboard,0);
            SetControlBorder(Stacker2Cardboard,0);
            SetControlBorder(Stacker3Cardboard,0);
            if(resume)
            {
                if(StackerPrimary!=0 && StackerSecondary!=0)
                {
                    var c1=0;
                    var c2=0;

                    if(Stackers.ContainsKey(StackerPrimary))
                    {
                        c1=Stackers[StackerPrimary].CheckGet($"CARDBOARDID").ToInt();
                    }

                    if(Stackers.ContainsKey(StackerSecondary))
                    {
                        c2=Stackers[StackerSecondary].CheckGet($"CARDBOARDID").ToInt();
                    }

                    if(c1!=0 && c2!=0 && c1!=c2)
                    {

                        switch(ActiveStackerGroup)
                        {
                            case 1:
                                SetControlBorder(Stacker1Cardboard,2);
                                SetControlBorder(Stacker2Cardboard,2);
                                break;

                            case 2:
                                SetControlBorder(Stacker2Cardboard,2);
                                SetControlBorder(Stacker3Cardboard,2);
                                break;
                        }
                    }
                }
            }

            //рилевки
            SetControlBorder(Stacker1Crease,0);
            SetControlBorder(Stacker2Crease,0);
            SetControlBorder(Stacker3Crease,0);
            if(resume)
            {
                if(StackerPrimary!=0 && StackerSecondary!=0)
                {
                    var c1 = v.CheckGet($"STACKER{StackerPrimary}_CREASE").ToInt();
                    var c2 = v.CheckGet($"STACKER{StackerSecondary}_CREASE").ToInt();

                    if (c1!=0 && c2!=0 && c1!=3 && c2!=3 && c1!=c2)
                    {
                        switch(ActiveStackerGroup)
                        {
                            case 1:
                                SetControlBorder(Stacker1Crease,1);
                                SetControlBorder(Stacker2Crease,1);
                                break;

                            case 2:
                                SetControlBorder(Stacker2Crease,1);
                                SetControlBorder(Stacker3Crease,1);
                                break;
                        }

                        taskComplete = false;
                        var s = "Несовместимые типы рилевок";
                        ReportAdd(s);
                    }
                }
            }

            //ширина задания
            if(resume)
            {
                //главный стекер
                {
                    int stackerWidth=GetStackerWidth(StackerPrimary,v);
                    taskWidth=taskWidth+stackerWidth;
                }

                //дополнительный стекер
                {
                    int stackerWidth=GetStackerWidth(StackerSecondary,v);
                    taskWidth=taskWidth+stackerWidth;
                }  
                
                if(taskWidth>0)
                {
                    v.CheckAdd("CUTTING_WIDTH",taskWidth.ToString());
                }
                else
                {
                    resume=false;
                    ReportAdd("Общая ширина задания 0");
                }
            }

            //проверка ширины раскроя
            SetControlBorder(CuttingWidth,0);
            if(resume)
            {
                {
                    if(taskWidth>format)
                    {
                        var s="Ширина раскроя больше ширины формата";
                        SetControlBorder(CuttingWidth,1);

                        //SetStatus($"{s}", true);
                        resume=false;
                        ReportAdd($"{s}");
                        taskComplete = false;
                        goodCutting=false;
                        SchemeImage.Source=null;
                    }
                }
            }

            var layersParams = GetLayersParams();

            // Проверка на слабый гофрослой
            if (resume)
            {
                if (Stacker1 != null)
                {
                    var sw1 = Stacker1.CheckGet("WIDTH").ToInt();
                    if (sw1 > 0)
                    {
                        var st1 = v.CheckGet($"STACKER1_THREADS").ToInt();
                        if (sw1 * st1 < MinDurableWidth)
                        {
                            string s = "";
                            if (WeakCorrugatedLayerFlag)
                            {
                                s = "Слабый гофрослой. ";
                                // Запрещаем сохранять задания со слабым гофрослоем
                                //taskComplete = false;
                            }
                            s = $"{s}На 1 стекере суммарная ширина должна быть больше {MinDurableWidth} мм";
                            ReportAdd(s);
                        }
                    }
                }

                if (Stacker2 != null)
                {
                    var sw2 = Stacker2.CheckGet("WIDTH").ToInt();
                    if (sw2 > 0)
                    {
                        var st2 = v.CheckGet($"STACKER2_THREADS").ToInt();
                        if (sw2 * st2 < MinDurableWidth)
                        {
                            string s = "";
                            if (WeakCorrugatedLayerFlag)
                            {
                                s = "Слабый гофрослой. ";
                                // Запрещаем сохранять задания со слабым гофрослоем
                                //taskComplete = false;
                            }
                            s = $"{s}На 2 стекере суммарная ширина должна быть больше {MinDurableWidth} мм";
                            ReportAdd(s);
                        }
                    }
                }
            }

            //обрезь            
            int trimControlStatus=0;
            if(resume)
            {
                {
                    //обрезь в мм.
                    trim=format-taskWidth;
                    trimPercentage=((double)trim/(double)format);
                    trimPercentage=trimPercentage*100;
                    trimPercentage=Math.Round(trimPercentage,2);

                    v.CheckAdd("TRIM_PERCENTAGE",trimPercentage.ToString());
                    v.CheckAdd("TRIM", trim.ToString());
                    TrimTAbsolute.Text = trim.ToString();
                    //TrimAbsolute.Text = trim.ToString();

                    // Задача #2708
                    // Для формата больше 2500 запретить создавать задания с обрезью более 199 мм
                    // Последнее изменение 17.07.2023
                    int limitation = layersParams["LAYERS_QUANTITY"] == 3 ? 239 : 199;
                    if ((format > 2500) && (trim > limitation))
                    {
                        // 2023-12-26_F4 временно отключено по просьбе опп
                        //ReportAdd("Большая обрезь для Fosber");
                        //taskComplete = false;
                    }
                    else if (trimPercentage >= TrimPercentageBig)
                    {
                        trimControlStatus=2;
                        ReportAdd($"Большая обрезь");
                    }


                    // необрезная кромка разрешена для заготовок для Роды или если есть признак Необрезная кромка
                    // Пустые стекеры и изделия без заявок пропускаем
                    if (trim < trimMin)
                    {
                        // По умолчанию нулевая обрезь разрешена, чтобы не блокировать перестил
                        // и заготовки для образцов, которые идут без заявок
                        bool allowed = true;
                        foreach (var stacker in Stackers.Values)
                        {
                            if (stacker.CheckGet("POSITION_ID").ToInt() > 0)
                            {
                                int schemeId = stacker.CheckGet("PRODUCTION_SCHEME_ID").ToInt();
                                bool untrimmed = stacker.CheckGet("UNTRIMMED_EDGE").ToBool();
                                if (!schemeId.ContainsIn(42, 81, 122, 1470, 1546, 1549, 1550, 1561) && !untrimmed)
                                {
                                    allowed = false;
                                }
                            }
                        }

                        if (!allowed)
                        {
                            if (trim == 0)
                            {
                                ReportAdd("В этом случае необрезная кромка не допускается");
                            }
                            else
                            {
                                ReportAdd($"Слишком маленькая обрезь, допускается не меньше {trimMin}");
                            }
                            //Если нет спецправ, то блокируем сохранение
                            taskComplete = MasterRight;
                            resume = MasterRight;
                        }
                    }
                }
            }

            //предупреждение, если в подкрое изделие со сложной схемой производства
            if (resume)
            {
                var schemeSteps = Stackers[StackerSecondary].CheckGet("PRODUCTION_SCHEME_STEPS").ToInt();
                if (schemeSteps > 2)
                {
                    ReportAdd("В подкрое изделие со сложной схемой производства");
                }
            }

            //Предупреждение, если на 1 стекере лист с упаковкой
            if (resume)
            {
                bool packaging1 = Stacker1.CheckGet("PACKAGING").ToBool();
                if (packaging1)
                {
                    if (Stacker1.CheckGet("PRODUCTION_SCHEME_ID").ToInt() == 0)
                    {
                        ReportAdd("Лист с упаковкой должен быть на верхнем стекере");
                    }
                }
            }

            //схема раскроя
            {
                if(updateMap)
                {
                    var p = new Dictionary<string,string>();
                    p.Add("FORMAT",format.ToString());

                    var a=0;
                    var b=0;

                    switch(ActiveStackerGroup)
                    {
                        case 1:
                        {
                            a=1;
                            b=2;
                        }
                        break;

                        case 2:
                        {
                            a=3;
                            b=2;
                        }
                        break;

                    }

                    {
                        var x=a;
                        if(v.CheckGet($"STACKER{x}_WIDTH").ToInt()>0)
                        {
                            p.Add("STACKER1_WIDTH",     v.CheckGet($"STACKER{x}_WIDTH"));
                            p.Add("STACKER1_THREADS",   v.CheckGet($"STACKER{x}_THREADS"));
                            p.Add("STACKER1_CREASE_SYMMETRIC","");
                            p.Add("STACKER1_TITLE", a.ToString());
                        }
                    }

                    {
                        var x=b;
                        if(v.CheckGet($"STACKER{x}_WIDTH").ToInt()>0)
                        {
                            p.Add("STACKER2_WIDTH",     v.CheckGet($"STACKER{x}_WIDTH"));
                            p.Add("STACKER2_THREADS",   v.CheckGet($"STACKER{x}_THREADS"));
                            p.Add("STACKER2_CREASE_SYMMETRIC","");
                            p.Add("STACKER2_TITLE", b.ToString());
                        }
                    }

                    //линия допустимого края обрези (порог)
                    //заданный порог обрези в миллиметрах
                    p.Add("TRIM_SIZE","");

                    //порог обрези, %, в числовом выражении
                    //если задано, справа вверху появится тектсовая надпись:  обрезь, порог
                    p.Add("TRIM","");

                    //размер блока обрези
                    // фактическая обрезь, %, расчетная
                    p.Add("TRIM_RATED",trimPercentage.ToString());
                    // фактическая обрезь, мм, расчетная
                    p.Add("TRIM_SIZE_RATED",trim.ToString());
                
                    //годный раскрой
                    p.Add("MAP_STATUS",goodCutting.ToString());

                    //if(ProductionTaskId == 0)
                    {
                        var id1 = Stacker1.CheckGet("BLANK_ID").ToInt();
                        if (id1 != 0)
                        {
                            p.Add("STACKER1_ITEM_ID", id1.ToString());
                        }
                        else
                        {
                            id1 = Stacker3.CheckGet("BLANK_ID").ToInt();
                            if (id1 != 0)
                            {
                                p.Add("STACKER1_ITEM_ID", id1.ToString());
                            }
                        }

                        var id2 = Stacker2.CheckGet("BLANK_ID").ToInt();
                        if (id2 != 0)
                        {
                            p.Add("STACKER2_ITEM_ID", id2.ToString());
                        }
                    }

                    GetMap(p);
                }
            }

            //общая длина задания
            if(resume)
            {
                if(Mode!=2)
                {
                    //остальные режимы

                    if(resume)
                    {
                        var x=StackerPrimary;

                        int length  =v.CheckGet($"STACKER{x}_LENGTH").ToInt();
                        //расчетное число в раскрой
                        int quantity=v.CheckGet($"STACKER{x}_QUANTITY_CALCULATED").ToInt();
                        int threads =v.CheckGet($"STACKER{x}_THREADS").ToInt();

                        var quantity2=(int)((double)quantity/(threads));
                        //taskLenght=(int)Math.Ceiling((double)(length*quantity2)/(double)1000);
                        //мм
                        taskLenght=(int)(length*quantity2);
                    }

                    if(resume)
                    {
                        if(taskLenght==0)
                        {
                            resume=false;

                            {
                                var x=StackerPrimary;
                                int width  =v.CheckGet($"STACKER{x}_WIDTH").ToInt();
                                int quantity=v.CheckGet($"STACKER{x}_QUANTITY_CALCULATED").ToInt();
                                if (width>0)
                                {
                                    if (quantity==0)
                                    {
                                        ReportAdd("Общая длина задания равна нулю.");
                                        ReportAdd("Скорректируйте количество на стекерах.");
                                    
                                        var c = Stacker1QuantityCalculated;
                                        switch (StackerPrimary)
                                        {
                                            case 1:
                                                c = Stacker1QuantityCalculated;
                                                break;
                                            case 2:
                                                c = Stacker2QuantityCalculated;
                                                break;
                                            case 3:
                                                c = Stacker3QuantityCalculated;
                                                break;
                                        }
                                        SetControlBorderTooltip(c,1);
                                    }
                                }
                            }
                        }
                    }

                    if(resume)
                    {
                        var taskLenght2=(int)Math.Ceiling((double)(taskLenght)/(double)1000);
                        v.CheckAdd("CUTTING_LENGTH",taskLenght2.ToString());
                    }

                }
                else
                {
                    //только просмотр                    
                }

            }

            //расчет количества заготовок
            if(resume)
            {
                if(Mode!=2)
                {
                    //остальные режимы

                    //обратный расчет количества заготовок из общей длины задания
                    if(CalcQuantity){
                        //берем общую длину и вычисляем сколько поместится заготовок по длине
                        if(taskLenght>0)
                        {
                            var x=StackerSecondary;
                            int threads =v.CheckGet($"STACKER{x}_THREADS").ToInt();
                            int length  =v.CheckGet($"STACKER{x}_LENGTH").ToInt();

                            int quantity=0;
                            if(threads>0 && length>0)
                            {
                                int totalLength=taskLenght;
                                quantity=(int)((double)totalLength/(double)length);
                                if(threads>1)
                                {
                                    quantity=(int)Math.Ceiling((double)quantity*(double)threads);
                                }
                                if(quantity>0)
                                {
                                    v.CheckAdd($"STACKER{x}_QUANTITY_CALCULATED",quantity.ToString());
                                }
                            
                            }
                        }
                    }

                    //проверка отклонений количества
                    { 
                        var checkingResult = CheckCalculatedLimits(StackerPrimary,v);
                        if(!checkingResult)
                        {
                            var s=$"Указанное количество больше, чем в задании.";
                            ReportAdd(s);
                        }
                    }

                    { 
                        var checkingResult = CheckCalculatedLimits(StackerSecondary,v);
                        if(!checkingResult)
                        {
                            var s=$"Указанное количество больше, чем в задании.";
                            ReportAdd(s);
                        }
                    }
                }
            }
            
            //раскрой Z-картона
            //количество кратное количеству на поддоне
            {
                //стекер2
                {
                    if (Stacker2!=null)
                    {
                        if (Stacker2.CheckGet("Z_CARDBOARD").ToBool()==true)
                        {
                            //введенное количество
                            int q=v.CheckGet("STACKER2_QUANTITY_CALCULATED").ToInt();
                            //на поддоне
                            int p=v.CheckGet("STACKER2_PALLET").ToInt();

                            if(q>0)
                            {
                                bool error=false;
                                
                                if(p>0)
                                {
                                    if(q>p)
                                    {
                                        if( ((double)q % (double)p)!=0 )
                                        {
                                            error=true;
                                        }
                                    }
                                }

                                if(error)
                                {
                                    SetControlBorder(Stacker2QuantityCalculated,1);
                                    var s=$"Проверьте количество заготовок на стекере 2.\nОно должно быть кратным количеству заготовок на поддоне.";
                                    ReportAdd(s);
                                    SetStatus(Report,true);
                                    taskComplete=false;
                                    resume=false;
                                }
                            }
                        }
                    }
                }

                //стекер3
                {
                    if (Stacker3!=null)
                    {
                        if (Stacker3.CheckGet("Z_CARDBOARD").ToBool()==true)
                        {
                            //введенное количество
                            int q=v.CheckGet("STACKER3_QUANTITY_CALCULATED").ToInt();
                            //на поддоне
                            int p=v.CheckGet("STACKER3_PALLET").ToInt();

                            if(q>0)
                            {
                                bool error=false;
                                
                                if(p>0)
                                {
                                    if(q>p)
                                    {
                                        if( ((double)q % (double)p)!=0 )
                                        {
                                             error=true;
                                        }
                                    }                                    
                                }  

                                if(error)
                                {
                                    SetControlBorder(Stacker3QuantityCalculated,1);
                                    var s=$"Проверьте количество заготовок на стекере 3.\nОно должно быть кратным количеству заготовок на поддоне.";
                                    ReportAdd(s);
                                    SetStatus(Report,true);
                                    taskComplete=false;
                                    resume=false;
                                }
                            }
                        }
                    }
                }
            }

            if(resume)
            {
                var q1=v.CheckGet($"STACKER{StackerPrimary}_QUANTITY_CALCULATED").ToInt();
                var q2=v.CheckGet($"STACKER{StackerSecondary}_QUANTITY_CALCULATED").ToInt();

                if(q1>0 || q2>0)
                {
                    //SaveButton.IsEnabled=true;
                    //taskComplete=true;
                }
                else
                {
                    ReportAdd("На одном из стекеров нулевое количество заготовок");
                    taskComplete=false;
                }
            }

            //Проверка на подходящие гофроагрегаты
            if (resume)
            {
                var machineSetId = 0;
                if (FactoryId == 1)
                {
                    machineSetId = GetAvaliableMachineSetLp();
                }
                else if (FactoryId == 2)
                {
                    machineSetId = GetAvaliableMachineSetKsh();
                }

                if (machineSetId == 0)
                {
                    ReportAdd("Это задание нельзя выполнить ни на одном ГА");
                }
            }

            if(resume)
            {
                if(v.CheckGet("MACHINE_ERROR").ToBool())
                {
                    ReportAdd($"Ошибка ГА: {v.CheckGet("MACHINE_ERROR_CODE")}");
                    ErrorCodes.Visibility=Visibility.Visible;
                }
            }

            Form.SetValues(v);
            ChainUpdate = true;

            //материал, обновляем расходы материала
            if(resume)
            {
                bool virt=v.CheckGet("VIRTUAL").ToBool();
                
                var source=UpdateLayersWeights();

                //в режиме "виртуальный раскрой" ограничения по остаткам сырья не учитываются
                if(virt)
                {
                    source=true;
                }

                if(
                    !source 
                    && Mode!=2
                )
                {
                    ReportAdd("Недостаточно материала");
                    taskComplete=false;
                }
            }

            if(resume)
            {
                if(stackersUsed>0)
                {
                    //в режиме создания, разрешаем отслеживать смену сырья,
                    //только если один раз была постановка на стекер
                    if(Mode == 0 || Mode == 4)
                    {
                        SourceChangedChecking=true;
                    }
                }
            }

            //активация кнопки "сохранить"
            {
                if(taskComplete)
                {
                    SaveButton.IsEnabled=true;
                }
                else
                {
                    SaveButton.IsEnabled=false;
                }
            }


            // проверка и отображение предупреждения 
            // при изготовлении опытных образцов
            {
                UpdateSampleWarning();            
            }

            
            //сообщение в статус-баре
            if(taskComplete)
            {
                //complete
                SetStatus(Report,false);
            }
            else
            {
                //error
                SetStatus(Report,true);
            }

            SetControlBorder(TrimPercentage,trimControlStatus);

            Central.Logger.Debug(Report);
            Central.Logger.Debug($"..Calculate complete");            
        }


        /// <summary>
        /// проверка и отображение предупреждения 
        /// при изготовлении опытных образцов
        /// </summary>
        private void UpdateSampleWarning()
        {
            if(Mode == 0 || Mode == 4)
            {
                //если у заготовки стоит флаг "опытная партия"
                //и картон в заготовке отличается от выбранного картона
                //покажем предупреждение
                //  не совпадает ID картона или выбрана кастомная композиция cырья
                //
                bool warning=false;
                int blank=0;

                var v=Form.GetValues();

                var mainCardboardId=v.CheckGet($"CARDBOARD_ID").ToInt();

                if(mainCardboardId!=0)
                {
                    if(StackerPrimary!=0)
                    {
                        var sample=v.CheckGet($"STACKER{StackerPrimary}_SAMPLE").ToBool();
                        var cardboardId=Stackers[StackerPrimary].CheckGet($"CARDBOARD_ID").ToInt();
                        if(sample)
                        {
                            if(cardboardId!=mainCardboardId)
                            {
                                warning=true;
                                blank=StackerPrimary;
                            }
                        }
                    }

                    if(StackerSecondary!=0)
                    {
                        var sample=v.CheckGet($"STACKER{StackerSecondary}_SAMPLE").ToBool();
                        var cardboardId=Stackers[StackerSecondary].CheckGet($"CARDBOARD_ID").ToInt();
                        if(sample)
                        {
                            if(cardboardId!=mainCardboardId)
                            {
                                warning=true;
                                blank=StackerSecondary;
                            }
                        }
                    }
                }

                if(warning)
                {
                    ReportAdd($"Заготовка {blank} производится впервые.");
                    ReportAdd($"Картон в задании отличается от выбранного.");
                }
            }
        }

        /// <summary>
        /// проверка, не изменился ли состав сырьевой композиции,
        /// если изменился, поднимается флаг SourceChanged
        /// </summary>
        private void CheckSourceChanging()
        {
            if(SourceChangedChecking)
            {
                SourceChanged=true;
            }
        }

        private void ShowSourceChanging()
        {
            //транспарант "Сырье изменено" 
            //в отладочном режиме
            SourceChangedLabel.Visibility=Visibility.Collapsed;

            //кроме режима просмотра
            if(Mode!=2)
            {
                
                //индикация для отладки
                if(Central.DebugMode)
                {
                    if(SourceChanged)
                    {
                        SourceChangedLabel.Visibility=Visibility.Visible;
                    }
                    else
                    {
                        SourceChangedLabel.Visibility=Visibility.Collapsed;
                    }
                }
                
            }
        }

        /// <summary>
        /// определение главного и дополнительного стекеров
        /// </summary>
        private void CheckStackersPriority()
        {
            //главный стекер
            StackerPrimary=ActiveStacker;
            
            //дополнительный стекер                
            {
                switch(ActiveStackerGroup)
                {
                    case 1:
                    {
                        //стекеры 1-2
                        if(StackerPrimary==1)
                        {
                            StackerSecondary=2;
                        }
                        else if(StackerPrimary==2)
                        {
                            StackerSecondary=1;
                        }
                    }
                    break;

                    case 2:
                    { 
                        //стекеры 2-3
                        if(StackerPrimary==2)
                        {
                            StackerSecondary=3;
                        }
                        else if(StackerPrimary==3)
                        {
                            StackerSecondary=2;
                        }
                    }
                    break;
                }
            }
        }

        private async void GetProductReam(int stackerId)
        {
            var v = Form.GetValues();
            var p = new Dictionary<string, string>()
            {
                { "POSITION_ID", v.CheckGet($"STACKER{stackerId}_POSITION_ID") },
                { "QTY", v.CheckGet($"STACKER{stackerId}_QUANTITY_CALCULATED") },
            };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Position");
            q.Request.SetParam("Action", "GetReam");
            q.Request.SetParams(p);

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
                    if (ds.Items != null)
                    {
                        string stackCnt = ds.Items[0].CheckGet("STACK_CNT");
                        string stc = $"(стоп - {stackCnt})";

                        switch (stackerId)
                        {
                            case 1:
                                Stacker1StacksInPalletFact.Text = stc;
                                break;
                            case 2:
                                Stacker2StacksInPalletFact.Text = stc;
                                break;
                            case 3:
                                Stacker3StacksInPalletFact.Text = stc;
                                break;
                        }
                    }
                }
            }
            else if (q.Answer.Error.Code == 145)
            {
                Form.SetStatus(q.Answer.Error.Message);
            }
        }

        /// <summary>
        /// загрузка схемы раскроя с сервера
        /// </summary>
        private async void GetMap(Dictionary<string,string> p)
        {
            bool resume=true;
            p.Add("TASK_ID", ProductionTaskId.ToString());

            if (resume)
            {                
                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Production");
                q.Request.SetParam("Object","Cutter");
                q.Request.SetParam("Action","CutterGetMap");

                q.Request.SetParams(p);
                q.Request.RequiredAnswerType=LPackClientAnswer.AnswerTypeRef.Stream;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                SchemeImage.Source=null;

                if(q.Answer.Status == 0)                
                {
                    if(q.Answer.DataStream!=null)
                    {
                        try
                        {
                            BitmapImage image = new BitmapImage();
                            image.BeginInit();
                            image.StreamSource = q.Answer.DataStream;
                            image.EndInit();
                            SchemeImage.Source=image;
                        }
                        catch(Exception e)
                        {

                        }                        
                    }
                }
            }
        }

        /// <summary>
        /// получение ширины раскроя на стекере
        /// </summary>
        private int GetStackerWidth(int stackerId, Dictionary<string,string> v)
        {
            int result=0;

            if(stackerId!=0)
            {
                int stackerWidth=0;
                var x=stackerId;
                int width   =v.CheckGet($"STACKER{x}_WIDTH").ToInt();
                int threads =v.CheckGet($"STACKER{x}_THREADS").ToInt();
                stackerWidth=width*threads;
                if(stackerWidth>0)
                {
                    result=stackerWidth;
                }
            }

            return result;
        }

        /// <summary>
        /// установка данные в статус-бар
        /// </summary>
        private void SetStatus(string text, bool error=false)
        {
            StatusBarText.Text=text;

            if(error)
            {
                StatusBarIconInfo.Visibility=System.Windows.Visibility.Collapsed;
                StatusBarIconAlert.Visibility=System.Windows.Visibility.Visible;
            }
            else
            {
                StatusBarIconInfo.Visibility=System.Windows.Visibility.Visible;
                StatusBarIconAlert.Visibility=System.Windows.Visibility.Collapsed;
            }

            if(!string.IsNullOrEmpty(text))
            {
                StatusBar.Visibility=System.Windows.Visibility.Visible;
            }
            else
            {
                StatusBar.Visibility=System.Windows.Visibility.Hidden;
            }
            
        }

        /// <summary>
        /// добавление строки к репорту
        /// </summary>
        private void ReportAdd(string text)
        {
            if(!string.IsNullOrEmpty(text))
            {
                Report=$"{Report}\n{text}";
                Central.Dbg($"{text}");
                Central.Logger.Debug($"\nPT:{text}");
            }
        }

        /// <summary>
        /// установка цвета бордера контрола
        /// type:
        ///     0 -- нормальное сосотояние, серый цвет
        ///     1 -- ошибка, акцент внимания, красный цвет
        ///     2 -- предупреждение, акцент внимания, оранжевый цвет
        /// </summary>
        private void SetControlBorder(Control control, int type=0)
        {
            var color = "#ffcccccc";

            switch(type)
            {
                //red
                case 1:
                    color = "#ffff0000";
                    break;

                //orange
                case 2:
                    color = "#FFCA5100";
                    break;

                default:
                    color = "#ffcccccc";
                    break;
            }

            if(control!=null)
            {
                var bc = new BrushConverter();
                var brush = (Brush)bc.ConvertFrom(color);
                control.BorderBrush=brush;
            }
        }

        /// <summary>
        /// установка цвета бордера контрола, установка тега подсказки
        /// </summary>
        private void SetControlBorderTooltip(Control control, int type=0, string tooltip="")
        {
            /*
                type=
                    0 -- нормальное сосотояние, серый цвет
                    1 -- акцент внимания, красный цвет

             */

            var color = "#ffcccccc";

            switch(type)
            {
                case 1:
                    color = "#ffff0000";
                    break;

                default:
                    color = "#ffcccccc";
                    break;
            }

            if(control!=null)
            {
                var bc = new BrushConverter();
                var brush = (Brush)bc.ConvertFrom(color);
                control.BorderBrush=brush;

                if(!string.IsNullOrEmpty(tooltip))
                {
                    control.ToolTip=tooltip;
                }
                else
                {
                    control.ToolTip="";
                }
            }
        }

        /// <summary>
        /// проверка количества на стекере
        /// </summary>
        private void CheckCalculatedQuantity(int stackerId, Dictionary<string,string> v)
        {
            /*
                Если расчетное количество меньше требуемого по заданию,
                подсветим красным
            */
            var x=stackerId;
            int quantity =v.CheckGet($"STACKER{x}_QUANTITY").ToInt();
            int quantityCalculated  =v.CheckGet($"STACKER{x}_QUANTITY_CALCULATED").ToInt();

            var ctl=Stacker1QuantityCalculated;
            switch(stackerId)
            {
                case 1:
                    ctl=Stacker1QuantityCalculated;
                    break;

                case 2:
                    ctl=Stacker2QuantityCalculated;
                    break;

                case 3:
                    ctl=Stacker3QuantityCalculated;
                    break;
            }

            if(quantity>0)
            {
                if(quantityCalculated<quantity)
                {                        
                    //подкрасим красным
                    SetControlBorderTooltip(ctl,1,"Не соответствует требуемомоу количеству по заданию");
                }  
            }
        }

        /// <summary>
        /// проверка отклонений количества
        /// </summary>
        private bool CheckCalculatedLimits(int stackerId, Dictionary<string,string> v)
        {
            bool result=true;
            
            /*
                Если расчетное количество выходит за рамки LOWER_LIMIT-UPPER_LIMIT,
                подсветим красным                        
            */
            var x=stackerId;
            int quantity =v.CheckGet($"STACKER{x}_QUANTITY_TO_CUT").ToInt();
            // тип ограничения количества
            // 0 -- =
            // 1 -- >
            // 2 -- <
            // 3 -- *
            int quantityLimitType =v.CheckGet($"STACKER{x}_QUANTITY_LIMIT_TYPE").ToInt();
            int quantityCalculated  =v.CheckGet($"STACKER{x}_QUANTITY_CALCULATED").ToInt();
            int lower =v.CheckGet($"STACKER{x}_QUANTITY_LOWER").ToInt();
            int upper = Math.Ceiling(v.CheckGet($"STACKER{x}_QUANTITY_UPPER").ToDouble()).ToInt();

            var ctl=Stacker1QuantityCalculated;            
            switch(stackerId)
            {
                case 1:
                    ctl=Stacker1QuantityCalculated;
                    break;

                case 2:
                    ctl=Stacker2QuantityCalculated;
                    break;

                case 3:
                    ctl=Stacker3QuantityCalculated;
                    break;
            }

            if(quantity>0)
            {
                var error=false;

                //если расчетное количество более того, что указано
                //в задании (с учетом верхнего порога), тогда покажем примечание
                if(
                    quantityCalculated > (quantity + upper * 3)
                )
                {
                    error=true;
                }

                if(error)
                {
                    SetControlBorder(ctl,2);
                    result=false;
                }
            }

            return result;
        }


        /// <summary>
        /// проверка заполненности стекеров и установка группы
        /// </summary>
        public void ChangeActiveStackerGroup()
        {
            /*
                вызывается при холодной загрузке данных в форму
                если заполнены 1+2 -> группа 1
                если заполнены 2+3 -> группа 2
             */
            var v=Form.GetValues();
            int w1 =v.CheckGet($"STACKER1_WIDTH").ToInt();
            int w2 =v.CheckGet($"STACKER2_WIDTH").ToInt();
            int w3 =v.CheckGet($"STACKER3_WIDTH").ToInt();

            int activeGroup=0;

            if(
                w1!=0 && w2==0 && w3==0
                ||
                w1==0 && w2!=0 && w3==0
                ||
                w1!=0 && w2!=0 && w3==0
            )
            {
                activeGroup=1;
            }

            if(
                w2!=0 && w3==0 && w1==0
                ||
                w2==0 && w3!=0 && w1==0
                ||
                w2!=0 && w3!=0 && w1==0
            )
            {
                activeGroup=2;
            }

            /*
            if(
               (w3!=0 && w2!=0)
               ||
               (w2!=0 && w1==0)
            )
            {
                activeGroup=2;
            }
            */

            if(activeGroup!=0)
            {
                SetActiveStackerGroup(activeGroup);
            }            

        }

        /// <summary>
        /// авточейндж стекера
        /// устанавливается главный стекер
        /// </summary>
        public void ChangeActiveStacker(int stackerId=0)
        {
            var v=Form.GetValues();
            int w1 =v.CheckGet($"STACKER1_WIDTH").ToInt();
            int w2 =v.CheckGet($"STACKER2_WIDTH").ToInt();
            int w3 =v.CheckGet($"STACKER3_WIDTH").ToInt();

            int activeStacker=0;
                
            switch(ActiveStackerGroup)
            {
                //стекеры 1-2
                case 1:
                {
                    /*
                        если на втором есть заготовка, а на первом нет, сделаем главным второй
                        если на первом есть заготовка, а на втором нет, сделаем главным первый
                        если обе заготовки установлены, сделаем главным первый
                    */
                    
                    activeStacker=1;

                    if (w1==0 && w2!=0)
                    {
                        activeStacker=2;
                    }
                    else if (w1!=0 && w2==0)
                    {
                        activeStacker=1;
                    }


                    /*
                        в режиме ограниченного редактирования
                        если занят только один стекер, заблокируем возможность менять
                        главный стекер
                     */

                    if(Mode==1)
                    {
                        if(
                            (w1!=0 && w2==0)
                            || (w1==0 && w2!=0)
                        )
                        {
                            Stacker1Main.IsEnabled=false;
                            Stacker2Main.IsEnabled=false;
                            Stacker3Main.IsEnabled=false;
                        }
                    }
                   
                   
                }
                    break;

                //стекеры 2-3
                case 2:
                {
                    
                   /*
                        если на третьем есть заготовка а на втором нет, сделаем главным третий
                    */
                    
                    activeStacker=2;

                    if (w2==0 && w3!=0)
                    {
                        activeStacker=3;
                    }
                    else if (w2!=0 && w3==0)
                    {
                        activeStacker=2;
                    }

                    /*
                        в режиме ограниченного редактирования
                        если занят только один стекер, заблокируем возможность менять
                        главный стекер
                     */

                    if(Mode==1)
                    {
                        if(
                            (w2!=0 && w3==0)
                            || (w2==0 && w3!=0)
                        )
                        {
                            Stacker1Main.IsEnabled=false;
                            Stacker2Main.IsEnabled=false;
                            Stacker3Main.IsEnabled=false;
                        }
                    }

                }
                    break;
            }

            if(activeStacker!=0)
            {
                SetActiveStacker(activeStacker);
            }
        }

        /// <summary>
        /// автоматически назначает главный стекер по клику на поле "количество"
        /// </summary>
        public void AutochangeStacker(int stacker)
        {

            if(Mode!=2)
            {
                
                var v=Form.GetValues();
                int w1 =v.CheckGet($"STACKER1_WIDTH").ToInt();
                int w2 =v.CheckGet($"STACKER2_WIDTH").ToInt();
                int w3 =v.CheckGet($"STACKER3_WIDTH").ToInt();

                int j=0;
                if(w1!=0)
                {
                    j++;
                }
                if(w2!=0)
                {
                    j++;
                }
                if(w3!=0)
                {
                    j++;
                }

                if(j>1)
                {
                    switch(stacker)
                    {
                        case 1:
                            if(w1!=0)
                            {
                                //стекеры 1-2
                                if(ActiveStackerGroup==1)
                                { 
                                    SetActiveStacker(1);
                                    Calculate();
                                }
                            }                    
                            break;

                         case 2:
                            if(w2!=0)
                            {
                                //стекеры 1-2
                                if(ActiveStackerGroup==1 || ActiveStackerGroup==2)
                                { 
                                    SetActiveStacker(2);
                                    Calculate();
                                }
                            }                    
                            break;

                         case 3:
                            if(w3!=0)
                            {
                                //стекеры 1-2
                                if(ActiveStackerGroup==2)
                                { 
                                    SetActiveStacker(3);
                                    Calculate();
                                }
                            }                    
                            break;


                   
                    }
                }

                
            }
        }

        /// <summary>
        /// смена стекеров местами
        /// </summary>
        private void SwapStackers(int stackerGroup)
        {
            //можно свапнуть только стекеры в активной группе
            if(stackerGroup==ActiveStackerGroup)
            {
                var v=Form.GetValues();
                
                switch(stackerGroup)
                {
                    // 1 <-> 2
                    case 1:
                    {
                        int w1 =v.CheckGet($"STACKER1_WIDTH").ToInt();
                        int w2 =v.CheckGet($"STACKER2_WIDTH").ToInt();

                        Stacker1.CheckAdd("THREADS", v.CheckGet($"STACKER1_THREADS"));
                        Stacker2.CheckAdd("THREADS", v.CheckGet($"STACKER2_THREADS"));
                        
                        Stacker1.CheckAdd("QTY", v.CheckGet($"STACKER1_QUANTITY_CALCULATED"));
                        Stacker2.CheckAdd("QTY", v.CheckGet($"STACKER2_QUANTITY_CALCULATED"));

                        if(w1!=0 || w2!=0)
                        {
                            var s1=Stacker2;
                            var s2=Stacker1;
                            SetStackerBlank(s1,1,true);
                            SetStackerBlank(s2,2,true);
 
                            var application1DS = Stacker1ApplicationDS;
                            string application1 = Stacker1Application.Text;
                            var application2DS = Stacker2ApplicationDS;
                            string application2 = Stacker2Application.Text;
                            Stacker1ApplicationDS = application2DS;
                            Stacker1Application.Text = application2;
                            Stacker2ApplicationDS = application1DS;
                            Stacker2Application.Text = application1;
                        }

                        ChangeActiveStacker();
                        Calculate();
                    }
                    break;

                    // 2 <-> 3
                    case 2:
                    {
                        int w2 =v.CheckGet($"STACKER2_WIDTH").ToInt();
                        int w3 =v.CheckGet($"STACKER3_WIDTH").ToInt();

                        Stacker2.CheckAdd("THREADS", v.CheckGet($"STACKER2_THREADS"));
                        Stacker3.CheckAdd("THREADS", v.CheckGet($"STACKER3_THREADS"));

                        Stacker2.CheckAdd("QTY", v.CheckGet($"STACKER2_QUANTITY_CALCULATED"));
                        Stacker3.CheckAdd("QTY", v.CheckGet($"STACKER3_QUANTITY_CALCULATED"));
                        
                        if(w2!=0 || w3!=0) 
                        { 
                            var s2=Stacker3;
                            var s3=Stacker2;
                            SetStackerBlank(s2,2,true);
                            SetStackerBlank(s3,3,true);

                            var application3DS = Stacker2ApplicationDS;
                            string application3 = Stacker2Application.Text;
                            var application2DS = Stacker3ApplicationDS;
                            string application2 = Stacker3Application.Text;
                            Stacker2ApplicationDS = application3DS;
                            Stacker2Application.Text = application3;
                            Stacker3ApplicationDS = application2DS;
                            Stacker3Application.Text = application2;
                        }

                        ChangeActiveStacker();
                        Calculate();
                    }
                    break;
                }
            }            
            
        }

        /// <summary>
        /// фильтрация марок картона в поел выбора по профилю
        /// </summary>
        /// <param name="profileId"></param>
        private void FilterCardboard(int profileId=0)
        {
            /*
                если задан профиль, оставим в списке выбора марок картона
                только те, которые соответствуют этому профилю
             */
            
            if (CardboardDS!=null)
            {
                if(CardboardDS.Items.Count>0)
                {
                    var list=new Dictionary<string,string>();
                    list.Add("0","");
                        
                    if (profileId != 0)
                    {
                        foreach(Dictionary<string,string> row in CardboardDS.Items)
                        {
                            if(row.CheckGet("PROFILE_ID").ToInt()==profileId)
                            {
                                list.CheckAdd(row.CheckGet("ID"),row.CheckGet("NAME"));
                            }                        
                        }
                    }
                    else
                    {
                        list.AddRange<string,string>(CardboardDS.GetItemsList("ID","NAME"));    
                    }
                        
                    CardboardList.Items = list;
                    CardboardList.SelectedItem = list.FirstOrDefault((x) => x.Key == "0");
                }
            }
        }


        /// <summary>
        /// создание нового производственного задания
        /// </summary>
        public async void Create()
        {
            FormToolbar.IsEnabled = false;
            SetStatus("Загрузка данных");
            Mouse.OverrideCursor=Cursors.Wait;
            
            ProductionTaskId=0;
            ProductionTaskOldId=0;

            SourceChanged=false;
            SourceChangedChecking=false;

            //блокируем механизм, пока загружаются данные
            ChainUpdate = false;

            LoadRef();

            //подождем, пока загрузятся справочники
            await Task.Run(() =>
            {
                while(!RefSourcesLoaded)
                {
                    Task.Delay(100);
                }
            });

            Central.Logger.Debug($"..Loading task data");

            SetDefaults();

            Mode=0;
            CheckMode();  

            Show();

            FormToolbar.IsEnabled = true;
            SetStatus("");
            Mouse.OverrideCursor=null;
            
            ChainUpdate = true;
            CalcQuantity=true;            

            // Если вызвали из автораскроя, отправим сообщение, что форма загружена
            if (BackTabName == "CreatingTasks_cuttingAuto")
            {
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "ProductionTaskCutted",
                    ReceiverName = "PositionList",
                    SenderName = "ProductionTask",
                    Action = "GetPosition",
                });
            }
            
        }

        /// <summary>
        /// редактирование производственного задания
        /// </summary>
        public async void Edit(int productionTaskId, bool copy=false)
        {
            /*
                copy -- режим создания копии задания:
                    загрузим данные задания, но сбросим id, как будто это не редактирование,
                    а создание новго задания
                    будут установлены:
                        выбор слоев вручную
                        состав сырья будет скопирован
             */

            FormToolbar.IsEnabled = false;
            SetStatus("Загрузка данных");
            Mouse.OverrideCursor=Cursors.Wait;
            
            var resume=true;
            ProductionTaskId=productionTaskId;
            ProductionTaskOldId=ProductionTaskId;

            SourceChanged=false;

            //блокируем механизм, пока загружаются данные
            ChainUpdate = false;

           

            if(resume)
            {
                if(productionTaskId==0)
                {
                    resume=false;
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();

                {
                    p.Add("ID",productionTaskId.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module","Production");
                q.Request.SetParam("Object","ProductionTask");
                q.Request.SetParam("Action","Get");

                q.Request.SetParams(p);


                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                
                var f=new Dictionary<string,string>();
                var complete=false;
                
                if(q.Answer.Status == 0)                
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if(result!=null)
                    {
                        

                        /*
                            сначала прогрузим общие данные формы
                            затем  данные стекеров
                         */
                        var formDataLoaded = false;
                        {
                            var ds=ListDataSet.Create(result,"DATA");
                            if(ds.Items.Count>0)
                            {
                                var row = ds.Items.First();

                                ProductionTaskCreated=row.CheckGet("CREATED2").ToDateTime();

                                f.CheckAdd("ID", row.CheckGet("ID"));
                                f.CheckAdd("NUMBER", row.CheckGet("NUM"));
                                f.CheckAdd("FORMAT", row.CheckGet("FORMAT"));
                                f.CheckAdd("TRIM_PERCENTAGE", row.CheckGet("TRIM_PERCENTAGE"));
                                f.CheckAdd("CUTTING_WIDTH", "0");
                                f.CheckAdd("CUTTING_LENGTH", row.CheckGet("CUTTING_LENGTH"));
                                f.CheckAdd("COMMENT", row.CheckGet("NOTE"));
                                
                                f.CheckAdd("CARDBOARD_ID", "0");
                                f.CheckAdd("CARDBOARD_QUALITY", row.CheckGet("CARDBOARD_QUALITY"));
                                f.CheckAdd("CARDBOARD_PROFILE", row.CheckGet("CARDBOARD_PROFILE"));
                                f.CheckAdd("CARDBOARD_PROFILE_ID", row.CheckGet("CARDBOARD_PROFILE_ID"));

                                f.CheckAdd("LAYER1_ID", row.CheckGet("LAYER1_ID"));
                                f.CheckAdd("LAYER1_TASK_WEIGHT", row.CheckGet("LAYER1_TASK_WEIGHT"));
                                f.CheckAdd("LAYER1_RESIDUE_WEIGHT", "0");
                                f.CheckAdd("LAYER1_PT_WEIGHT", "0");
                                f.CheckAdd("LAYER1_GLUING", row.CheckGet("LAYER1_GLUING"));
                                f.CheckAdd("LAYER1_COLOR", row.CheckGet("LAYER1_COLOR"));

                                f.CheckAdd("LAYER2_ID", row.CheckGet("LAYER2_ID"));
                                f.CheckAdd("LAYER2_TASK_WEIGHT", row.CheckGet("LAYER2_TASK_WEIGHT"));
                                f.CheckAdd("LAYER2_RESIDUE_WEIGHT", "0");
                                f.CheckAdd("LAYER2_PT_WEIGHT", "0");
                                f.CheckAdd("LAYER2_GLUING", row.CheckGet("LAYER2_GLUING"));
                                f.CheckAdd("LAYER2_COLOR", row.CheckGet("LAYER2_COLOR"));

                                f.CheckAdd("LAYER3_ID", row.CheckGet("LAYER3_ID"));
                                f.CheckAdd("LAYER3_TASK_WEIGHT", row.CheckGet("LAYER3_TASK_WEIGHT"));
                                f.CheckAdd("LAYER3_RESIDUE_WEIGHT", "0");
                                f.CheckAdd("LAYER3_PT_WEIGHT", "0");
                                f.CheckAdd("LAYER3_GLUING", row.CheckGet("LAYER3_GLUING"));
                                f.CheckAdd("LAYER3_COLOR", row.CheckGet("LAYER3_COLOR"));

                                f.CheckAdd("LAYER4_ID", row.CheckGet("LAYER4_ID"));
                                f.CheckAdd("LAYER4_TASK_WEIGHT", row.CheckGet("LAYER4_TASK_WEIGHT"));
                                f.CheckAdd("LAYER4_RESIDUE_WEIGHT", "0");
                                f.CheckAdd("LAYER4_PT_WEIGHT", "0");
                                f.CheckAdd("LAYER4_GLUING", row.CheckGet("LAYER4_GLUING"));
                                f.CheckAdd("LAYER4_COLOR", row.CheckGet("LAYER4_COLOR"));

                                f.CheckAdd("LAYER5_ID", row.CheckGet("LAYER5_ID"));
                                f.CheckAdd("LAYER5_TASK_WEIGHT", row.CheckGet("LAYER5_TASK_WEIGHT"));
                                f.CheckAdd("LAYER5_RESIDUE_WEIGHT", "0");
                                f.CheckAdd("LAYER5_PT_WEIGHT", "0");
                                f.CheckAdd("LAYER5_GLUING", row.CheckGet("LAYER5_GLUING"));
                                f.CheckAdd("LAYER5_COLOR", row.CheckGet("LAYER5_COLOR"));

                                f.CheckAdd("MACHINE_ERROR", row.CheckGet("MACHINE_ERROR"));
                                f.CheckAdd("MACHINE_ERROR_CODE", row.CheckGet("MACHINE_ERROR_CODE"));

                                //if(Mode!=2)
                                {
                                    var profileId=row.CheckGet("CARDBOARD_PROFILE_ID").ToInt();
                                    if(profileId!=0)
                                    {
                                        FilterCardboard(profileId);
                                    }
                                    else
                                    {
                                        FilterCardboard(0);
                                    }
                                }

                                if(true)
                                {
                                                                        
                                    var format=row.CheckGet("FORMAT").ToInt();
                                    var profileId=row.CheckGet("CARDBOARD_PROFILE_ID").ToInt();
                                    LoadRef(true,format,profileId);
            
                                    await Task.Run(() =>
                                    {
                                        while(!RefSourcesLoaded)
                                        {
                                            Task.Delay(100);
                                        }
                                    });
                                }

                                // Определим код цвета слоев картона
                                int outerLayerColor = row.CheckGet("LAYER5_COLOR").ToInt();

                                int innerLayerColor = 0;
                                var lc1 = row.CheckGet("LAYER1_COLOR").ToInt();
                                var lc3 = row.CheckGet("LAYER3_COLOR").ToInt();
                                if (lc1 > 0)
                                {
                                    // крашенный = бурый
                                    if (lc1 == 3)
                                    {
                                        innerLayerColor = 2;
                                    }
                                    else
                                    {
                                        innerLayerColor = lc1;
                                    }

                                }
                                else if (lc3 > 0)
                                {
                                    if (lc3 == 3)
                                    {
                                        innerLayerColor = 2;
                                    }
                                    else
                                    {
                                        innerLayerColor = lc3;
                                    }
                                }

                                if (outerLayerColor > 1)
                                {
                                    outerLayerColor = 2;
                                }

                                string colorCode = $"{outerLayerColor}{innerLayerColor}";
                                int compositionOuterColorId = 0;
                                switch (colorCode)
                                {
                                    // белый / белый
                                    case "11":
                                        compositionOuterColorId = 3;
                                        break;

                                    // белый / бурый
                                    case "12":
                                        compositionOuterColorId = 1;
                                        break;

                                    // бурый / белый
                                    case "21":
                                        compositionOuterColorId = 4;
                                        break;

                                    // бурый / бурый
                                    case "22":
                                        compositionOuterColorId = 2;
                                        break;
                                }

                                f.CheckAdd("CARDBOARD_OUTER_COLOR_ID", compositionOuterColorId.ToString());


                                Form.SetValues(f);
                                
                                //определение режима работы
                                {                                 
      
                                    /*
                                        INPLANNINGQUEUE =0|1 -- в плане на ГА  (BHS_QUEUE)
                                        INPLANNING2QUEUE =0|1 -- в очереди на планирование (PROD_CONVRTNG_QUEUE)
                                        POSTING =0|1 -- признак выполнения, 1 --выполнено
                                        INSTACK =0|1 -- в очереди на ГА (PZ_LINE)
                                        WORK =1|0 -- в очередь, если 1, уйдет в очередь ГА

                                        если выполнено (POSTING=1), ничего нельзя (только смотреть)
                                        если в очереди на ГА (INSTACK=1), ничего нельзя (только смотреть)
                                        если не в плане  ГА (INPLANNINGQUEUE=0), можно редактировать (алгоритм2), можно удалить
                                        если в плане ГА (INPLANNINGQUEUE=1), и если заблокировано (WORK=0), можно редактировать (алгоритм1)
              
                                        алгоритм2 -- пересоздание ПЗГА
                                        алгоритм1 -- изменение количества, сырья
                                     */

                                    bool edit1=false;
                                    bool edit2=false;
                                    bool delete=false;
                                    bool view=true;

                                    if(row.CheckGet("POSTING").ToInt()==0)
                                    {
                                        //не выполнено

                                        if(row.CheckGet("INSTACK").ToInt()==0)
                                        {
                                            //не в очереди ГА

                                            if(row.CheckGet("INPLANNINGQUEUE").ToInt()==0)
                                            {
                                                //не в плане  ГА

                                                edit2=true;
                                                delete=true;
                                            }
                                            else
                                            {
                                                //в плане ГА

                                                if(row.CheckGet("WORK").ToInt()==0)
                                                {
                                                    //заблокировано
                                                    edit1=true;
                                                    
                                                }
                                                else
                                                {
                                                    //разблокировано
                                                    
                                                }
                                            }
                                        }
                                    }               

                                    //по умолчанию режим "только чтение"
                                    Mode=2;

                                    if(view)
                                    {
                                        Mode=2;
                                    }

                                    if(edit1)
                                    {
                                        Mode=1;
                                    }

                                    if(edit2)
                                    {
                                        Mode=3;
                                    }

                                    //проверка правил доступа
                                    {
                                        /*
                                            в режиме только для чтения отключается возможность 
                                            создавать записи и сохранять данные для открытых записей
                                            */
                                        var mode=Central.Navigator.GetRoleLevel("[erp]production_task_cm");
                                        switch(mode)
                                        {
                                            case Role.AccessMode.ReadOnly:
                                                Mode=2;
                                                break;

                                            case Role.AccessMode.FullAccess:
                        
                                                break;
                                        }
                                    }

                                    //режим "копия задания"
                                    //сбросим все идентифкаторы
                                    if(copy)
                                    {
                                        ProductionTaskId=0;
                                        ProductionTaskOldId=0;
                                        Mode=0;
                                    }
                                    
                                }
                                
                                formDataLoaded = true;
                            }
                        }
                        
                        var stackerDataLoaded = false;
                        {
                            var ds=ListDataSet.Create(result,"POSITION");
                            if(ds.Items.Count>0)
                            {
                                foreach(Dictionary<string,string> row in ds.Items)
                                {
                                    var v=new Dictionary<string,string>();
                                    
                                    v.CheckAdd("STACKER_ID", row.CheckGet("STACKER"));
                                    v.CheckAdd("POSITION_ID", row.CheckGet("POSITION_ID"));
                                    v.CheckAdd("_POSITION_ID", row.CheckGet("POSITION_ID"));
                                    v.CheckAdd("BLANK_ID", row.CheckGet("BLANK_ID"));
                                    v.CheckAdd("BLANK_NAME", row.CheckGet("BLANK_NAME"));
                                    v.CheckAdd("BLANK_CODE", row.CheckGet("BLANK_CODE"));
                                    v.CheckAdd("GOODS_ID", row.CheckGet("GOODS_ID"));
                                    v.CheckAdd("GOODS_CODE", row.CheckGet("GOODS_CODE"));
                                    v.CheckAdd("GOODS_NAME", row.CheckGet("GOODS_NAME"));
                                    v.CheckAdd("MIN_TRIM", row.CheckGet("MIN_TRIM"));
                                    v.CheckAdd("CARDBOARD_NAME", row.CheckGet("CARDBOARD_NAME"));
                                    v.CheckAdd("LENGTH", row.CheckGet("LENGTH"));
                                    v.CheckAdd("WIDTH", row.CheckGet("WIDTH"));
                                    v.CheckAdd("QTY", row.CheckGet("QTY"));
                                    v.CheckAdd("LOWER_LIMIT", row.CheckGet("LOWER_LIMIT"));
                                    v.CheckAdd("UPPER_LIMIT", row.CheckGet("UPPER_LIMIT"));
                                    v.CheckAdd("THREADS", row.CheckGet("THREADS"));
                                    v.CheckAdd("QTY_CALC", row.CheckGet("QTY"));
                                    v.CheckAdd("CARDBOARD_TYPE", row.CheckGet("CARDBOARD_TYPE"));
                                    v.CheckAdd("CARDBOARD_STANDART", row.CheckGet("CARDBOARD_STANDART"));
                                    v.CheckAdd("CARDBOARD_NAME", row.CheckGet("CARDBOARD_NAME"));
                                    v.CheckAdd("CARDBOARD_ID", row.CheckGet("CARDBOARD_ID"));
                                    v.CheckAdd("CARDBOARD_MARK_ID", row.CheckGet("CARDBOARD_MARK_ID"));
                                    v.CheckAdd("ID_PROF", row.CheckGet(""));
                                    v.CheckAdd("PRODUCTS_IN_PALLET", row.CheckGet("PRODUCTS_IN_PALLET"));
                                    v.CheckAdd("PRODUCTS_IN_APPLICATION", row.CheckGet("PRODUCTS_IN_APPLICATION"));
                                    v.CheckAdd("PRODUCTS_FROM_BLANK", row.CheckGet("PRODUCTS_FROM_BLANK"));
                                    v.CheckAdd("Z_CARDBOARD", row.CheckGet("Z_CARDBOARD"));
                                    v.CheckAdd("SAMPLE", row.CheckGet("SAMPLE"));
                                    v.CheckAdd("QUANTITY_LIMIT_TYPE", row.CheckGet("QUANTITY_LIMIT_TYPE"));
                                    v.CheckAdd("KIT_ID", row.CheckGet("KIT_ID"));
                                    v.CheckAdd("FULL_STAMP_RT", row.CheckGet("FULL_STAMP_RT"));
                                    v.CheckAdd("PRODUCTION_SCHEME_ID", row.CheckGet("PRODUCTION_SCHEME_ID"));
                                    v.CheckAdd("PRODUCTION_SCHEME_STEPS", row.CheckGet("PRODUCTION_SCHEME_STEPS"));
                                    v.CheckAdd("CREASE_COUNT", row.CheckGet("CREASE_COUNT"));
                                    v.CheckAdd("UNTRIMMED_EDGE", row.CheckGet("UNTRIMMED_EDGE_FLAG"));
                                    v.CheckAdd("PACKAGING", row.CheckGet("PACKAGING"));

                                    var creaseType = row.CheckGet("CREASE_TYPE");
                                    if (row.CheckGet("CREASE_COUNT").ToInt() == 0)
                                    {
                                        creaseType = "3";
                                    }
                                    v.CheckAdd("CREASE_TYPE", creaseType);

                                    //режим "копия задания"
                                    if (copy)
                                    {
                                        //сбросим привязку к позициям заявки
                                        v.CheckAdd("POSITION_ID", "0");
                                        v.CheckAdd("_POSITION_ID", "0");                                       
                                    }

                                    SetStackerBlank(v, 0, true,copy);
                                    GetApplication(v);
                                    stackerDataLoaded = true;
                                }
                            }
                        }

                        

                        if (formDataLoaded && stackerDataLoaded)
                        {
                            complete = true;
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }

                if(complete)
                {
                    CheckMode();

                    if(copy)
                    {
                        //выбор сырья вручную
                        UseManualComposition.IsEnabled=true;
                        UseManualComposition.IsChecked = true;
                        CheckManualComposition(true);                    
                        CardboardList.IsEnabled=false;
                        
                        //var v=new Dictionary<string,string>();
                        //v.CheckAdd("CARDBOARD_ID","0");
                        //Form.SetValues(v);
                    }

                    //активная группа стекеров
                    ChangeActiveStackerGroup();
                    //активный стекер
                    ChangeActiveStacker();
                    
                    CalcQuantity=false;
                    Calculate();                    

                    Show();
                }
                
                //просмотр
                if(Mode==2)
                {
                    CardboardList.IsEnabled=false;
                }
            }
            
            
            FormToolbar.IsEnabled = true;
            //SetStatus("");
            Mouse.OverrideCursor=null;
            
            ChainUpdate = true;
            CalcQuantity=false;
            SourceChangedChecking=true;
        }

        /// <summary>
        /// Вычисляет ID набора разрешенных ГА
        /// </summary>
        /// <param name="task"></param>
        /// <returns>ID набора разрешенных ГА</returns>
        public int GetAvaliableMachineSetLp()
        {
            int result = 1;
            var machineSet = new List<int>() { 1, 1, 1 };

            var v = Form.GetValues();
            //номера стекеров
            var x1 = 0;
            var x2 = 0;
            switch (ActiveStackerGroup)
            {
                case 1:
                    {
                        x1 = 1;
                        x2 = 2;
                    }
                    break;

                case 2:
                    {
                        x1 = 2;
                        x2 = 3;
                    }
                    break;
            }

            // Формат больше 2500 только на 3
            if (v.CheckGet("FORMAT").ToInt() > 2500)
            {
                machineSet[0] = 0;
                machineSet[1] = 0;
            }

            // Печать и фенфолд только на 1
            if (Stackers[x1].CheckGet("Z_CARDBOARD").ToBool()
                    || Stackers[x2].CheckGet("Z_CARDBOARD").ToBool()
                    || !string.IsNullOrEmpty(Stackers[x1].CheckGet("PRINTING_COLOR"))
                    || !string.IsNullOrEmpty(Stackers[x2].CheckGet("PRINTING_COLOR"))
            )
            {
                machineSet[1] = 0;
                machineSet[2] = 0;
            }

            // Пятислойки нет на 2
            if (Stackers[x1].CheckGet("LAYERS_QUANTITY").ToInt() > 3)
            {
                machineSet[1] = 0;
            }

            // Если обе заготовки длиннее 3600, то доступен только Фосбер
            if ((v.CheckGet($"STACKER{x1}_LENGTH").ToInt() > 3600) && (v.CheckGet($"STACKER{x2}_LENGTH").ToInt() > 3600))
            {
                machineSet[1] = 0;
                machineSet[2] = 0;
            }

            // Минимальная ширина заготовки для Фосбера - 270 мм
            /*
            if (
                (Stackers[x1].CheckGet("WIDTH").ToInt() > 0 && Stackers[x1].CheckGet("WIDTH").ToInt() < 270)
                || (Stackers[x2].CheckGet("WIDTH").ToInt() > 0 && Stackers[x2].CheckGet("WIDTH").ToInt() < 270)
            )
            {
                machineSet[2] = 0;
            }
            */

            int creaseSum = v.CheckGet($"STACKER{x1}_THREADS").ToInt() * Stackers[x1].CheckGet("CREASE_COUNT").ToInt() + v.CheckGet($"STACKER{x2}_THREADS").ToInt() * Stackers[x2].CheckGet("CREASE_COUNT").ToInt();
            // плоских рилевок на Фосбере не более 20, остальных не более 24
            if ((v.CheckGet($"STACKER{x1}_CREASE").ToInt() == 2) && (creaseSum > 20))
            {
                machineSet[2] = 0;
            }
            else if (creaseSum > 24)
            {
                machineSet[2] = 0;
            }

            // Определяем полученное SetId
            string key = string.Join(",", machineSet);
            switch (key)
            {
                // ничего
                case "0,0,0":
                    result = 0;
                    break;
                // только BHS1
                case "1,0,0":
                    result = 2;
                    break;
                // только BHS2
                case "0,1,0":
                    result = 3;
                    break;
                // только Fosber
                case "0,0,1":
                    result = 4;
                    break;
                // BHS1 и BHS2
                case "1,1,0":
                    result = 5;
                    break;
                // BHS1 и Fosber
                case "1,0,1":
                    result = 6;
                    break;
                // BHS2 и Fosber
                case "0,1,1":
                    result = 7;
                    break;
            }
            return result;
        }

        /// <summary>
        /// Вычисляет ID набора разрешенных ГА для Каширской площадки
        /// </summary>
        /// <param name="task"></param>
        /// <returns>ID набора разрешенных ГА</returns>
        public int GetAvaliableMachineSetKsh()
        {
            int result = 8;

            var v = Form.GetValues();
            //номера стекеров
            var x1 = 0;
            var x2 = 0;
            switch (ActiveStackerGroup)
            {
                case 1:
                    {
                        x1 = 1;
                        x2 = 2;
                    }
                    break;

                case 2:
                    {
                        x1 = 2;
                        x2 = 3;
                    }
                    break;
            }

            // Количество ручьев: если одна заготовка, то не более 14, если две, то не более 7 для каждой
            int t1 = v.CheckGet($"STACKER{x1}_THREADS").ToInt();
            int t2 = v.CheckGet($"STACKER{x2}_THREADS").ToInt();

            if (t1 == 0)
            {
                if (t2 > 14)
                {
                    result = 0;
                }
            }
            else if (t2 == 0)
            {
                if (t1 > 14)
                {
                    result = 0;
                }
            }
            else if ((t1 > 7) || (t2 > 7))
            {
                result = 0;
            }

            return result;
        }

        /// <summary>
        /// сохранение производственного задания
        /// </summary>
        private async void Save()
        {
            FormToolbar.IsEnabled = false;
            SetStatus("Сохранение задания");
            Mouse.OverrideCursor=Cursors.Wait;
            
            bool resume = true;

            var taskName="";
            var v=Form.GetValues();

            //ширина заготовок на стекерах
            int w1 =v.CheckGet($"STACKER1_LENGTH").ToInt();
            int w2 =v.CheckGet($"STACKER2_LENGTH").ToInt();
            int w3 =v.CheckGet($"STACKER3_LENGTH").ToInt();

            //количество занятых стекеров
            int stackersUsed=0;
            if(w1>0)
            {
                stackersUsed++;
            }
            if(w2>0)
            {
                stackersUsed++;
            }
            if(w3>0)
            {
                stackersUsed++;
            }

            //номера стекеров
            var x1=0;
            var x2=0;
            switch(ActiveStackerGroup)
            {
                case 1:
                {
                    x1=1;
                    x2=2;
                }
                break;

                case 2:
                {
                    x1=2;
                    x2=3;
                }
                break;
            }

            //заготовки должны быть
            if (resume)
            {
                if(stackersUsed==0)
                {
                    resume = false;
                    var s = "Нет заготовок для раскроя";
                    ReportAdd(s);
                }
            }

            //заготовки должны быть разные
            if (resume)
            {
                //заготовки
                int b1=v.CheckGet($"STACKER{x1}_BLANK_ID").ToInt();
                int b2=v.CheckGet($"STACKER{x2}_BLANK_ID").ToInt();

                if(b1!=0 && b1==b2)
                {
                    resume = false;
                    var s = "Заготовки должны быть разными";
                    ReportAdd(s);
                }
            }

            if (resume)
            {
                if(v.CheckGet($"FORMAT").ToInt()==0)
                {
                    resume = false;
                    var s = "Не задан формат";
                    ReportAdd(s);
                }
            }

            if (resume)
            {
                if(v.CheckGet($"CUTTING_LENGTH").ToInt()==0)
                {
                    resume = false;
                    var s = "Неверная длина задания";
                    ReportAdd(s);
                }
            }

            if (resume)
            {
                if(v.CheckGet($"TRIM_PERCENTAGE").ToInt()>TrimPercentageMax)
                {
                    resume = false;
                    var s=$"Обрезь слишком большая. Макс= {TrimPercentageMax}%";
                    ReportAdd(s);
                }
            }

            if (resume)
            {
                if(v.CheckGet($"CARDBOARD_PROFILE_ID").ToInt()==0)
                {
                    resume = false;
                    var s=$"Не задан профиль или качество.\nВыберите сырье повторно.";
                    ReportAdd(s);
                }
            }

            if (resume)
            {
                if(Mode==1 || Mode==3)
                {
                    if( string.IsNullOrEmpty(v.CheckGet($"CARDBOARD_QUALITY").ToString()) )
                    {
                        resume = false;
                        var s=$"Не задан профиль или качество.\nВыберите сырье повторно.";
                        ReportAdd(s);
                    }
                }
            }

            var comment = v.CheckGet("COMMENT");

            // Проверим обрезь. Если обрезь меньше базовой, добавим комментарий про необрезную кромку
            if (resume)
            {
                var trimParams = new List<Dictionary<string, int>>();
                int baseTrim1 = 0;
                int baseTrim2 = 0;
                if (Stackers[x1].CheckGet("WIDTH").ToInt() > 0)
                {
                    var c1 = new Dictionary<string, int>()
                    {
                        { "Z_CARDBOARD", Stackers[x1].CheckGet("Z_CARDBOARD").ToInt() },
                        { "CELLULOSE_LAYERS_QUANTITY", Stackers[x1].CheckGet("CELLULOSE_LAYERS_QUANTITY").ToInt() },
                        { "LAYERS_QUANTITY", Stackers[x1].CheckGet("LAYERS_QUANTITY").ToInt() },
                        { "FULL_STAMP_RT", Stackers[x1].CheckGet("FULL_STAMP_RT").ToInt() },
                    };
                    trimParams.Add(c1);
                    baseTrim1 = GetMinTrim(trimParams, false);
                }
                trimParams.Clear();

                if (Stackers[x2].CheckGet("WIDTH").ToInt() > 0)
                {
                    var c2 = new Dictionary<string, int>()
                    {
                        { "Z_CARDBOARD", Stackers[x2].CheckGet("Z_CARDBOARD").ToInt() },
                        { "CELLULOSE_LAYERS_QUANTITY", Stackers[x2].CheckGet("CELLULOSE_LAYERS_QUANTITY").ToInt() },
                        { "LAYERS_QUANTITY", Stackers[x2].CheckGet("LAYERS_QUANTITY").ToInt() },
                        { "FULL_STAMP_RT", Stackers[x2].CheckGet("FULL_STAMP_RT").ToInt() },
                    };
                    trimParams.Add(c2);
                    baseTrim2 = GetMinTrim(trimParams, false);
                }
                var baseMinTrim = Math.Max(baseTrim1, baseTrim2);

                // Признаки полный штамп для стекеров
                bool fullStampRt1 = false;
                if (Stackers[x1].CheckGet("FULL_STAMP_RT").ToInt() == 1)
                {
                    fullStampRt1 = true;
                }
                bool fullStampRt2 = false;
                if (Stackers[x2].CheckGet("FULL_STAMP_RT").ToInt() == 1)
                {
                    fullStampRt2 = true;
                }

                if (v.CheckGet("TRIM").ToInt() < baseMinTrim)
                {
                    //Добавляем, если надо, комментарий
                    if (fullStampRt1 || fullStampRt2)
                    {
                        string untrimComment = "Допускается необрезная кромка";
                        if (string.IsNullOrEmpty(comment))
                        {
                            comment = untrimComment;
                        }
                        else if (!comment.Contains(untrimComment))
                        {
                            comment = $"{comment}. {untrimComment}";
                        }
                    }

                    if (Stackers[x1].CheckGet("WIDTH").ToInt() > 0)
                    {
                        //У потоков с полным штампом ставим признак необрезной край
                        if (fullStampRt1)
                        {
                            Stackers[x1].CheckAdd("UNTRIMMED_EDGE_FLAG", "1");
                        }

                        // Для заготовок решеток ставим признак Необрезной край
                        int schemeId = Stackers[x1].CheckGet("PRODUCTION_SCHEME_ID").ToInt();
                        if (schemeId.ContainsIn(42, 81, 122, 1470, 1546, 1549, 1550, 1561))
                        {
                            Stackers[x1].CheckAdd("UNTRIMMED_EDGE_FLAG", "1");
                        }
                    }
                    if (Stackers[x2].CheckGet("WIDTH").ToInt() > 0)
                    {
                        //У потоков с полным штампом ставим признак необрезной край
                        if (fullStampRt2)
                        {
                            Stackers[x2].CheckAdd("UNTRIMMED_EDGE_FLAG", "1");
                        }

                        // Для заготовок решеток ставим признак Необрезной край
                        int schemeId = Stackers[x2].CheckGet("PRODUCTION_SCHEME_ID").ToInt();
                        if (schemeId.ContainsIn(42, 81, 122, 1470, 1546, 1549, 1550, 1561))
                        {
                            Stackers[x2].CheckAdd("UNTRIMMED_EDGE_FLAG", "1");
                        }
                    }
                }
            }

            // Проверка на признак Тандем

            int t = v.CheckGet($"STACKER{x1}_THREADS").ToInt() + v.CheckGet($"STACKER{x2}_THREADS").ToInt();
            int cc = v.CheckGet($"STACKER{x1}_THREADS").ToInt() * Stackers[x1].CheckGet("CREASE_COUNT").ToInt() + v.CheckGet($"STACKER{x2}_THREADS").ToInt() * Stackers[x2].CheckGet("CREASE_COUNT").ToInt();
            int profileId = v.CheckGet("CARDBOARD_PROFILE_ID").ToInt();
            int fmt = v.CheckGet("FORMAT").ToInt();

            var tandemFlag = CheckTandem(cc, t, fmt, profileId);

            //проверим расход сырья
            //должно быть не менее 3 заполненных строк
            if (resume)
            {
                var weightError = false;

                var j = 0;

                for (int i = 1; i <= 5; i++)
                {
                    if (v.CheckGet($"LAYER{i}_TASK_WEIGHT").ToInt()>0)
                    {
                        j++;
                    }
                }
                
                if (j<3)
                {
                    weightError = true;
                }
                
                if (weightError)
                {
                    resume = false;

                    var s = "Нет данных по расходу сырья. \nПроверьте выбор сырья и длину задания.";
                    ReportAdd(s);                    
                }
            }
            
            //проверим композицию сырья
            //должно быть не менее 3 заполненных строк
            if (resume)
            {
                var weightError = false;

                var j = 0;

                for (int i = 1; i <= 5; i++)
                {
                    if (v.CheckGet($"LAYER{i}_ID").ToInt()>0)
                    {
                        j++;
                    }
                }
                
                if (j<3)
                {
                    weightError = true;
                }
                
                if (weightError)
                {
                    resume = false;
                    var s = "Проверьте выбор сырья по слоям.";
                    ReportAdd(s);
                }
            }



            if(resume)
            {
                // перевыпуск. номер нового задания создаётся из номера старого
                if (Mode == 4)
                {
                    taskName = ReworkParams["NUM"];
                }
                //создание нового пз
                else if(Mode==0)
                {
                    if(string.IsNullOrEmpty(taskName))
                    {
                        var x=StackerPrimary;
                        if(x!=0)
                        {
                            var n=v.CheckGet($"STACKER{x}_BLANK");
                            if(!string.IsNullOrEmpty(n))
                            {
                                taskName=n;
                            }
                        }
                    }

                    if(string.IsNullOrEmpty(taskName))
                    {
                        var x=StackerSecondary;
                        if(x!=0)
                        {
                            var n=v.CheckGet($"STACKER{x}_BLANK");
                            if(!string.IsNullOrEmpty(n))
                            {
                                taskName=n;
                            }
                        }
                    }
                
                    if(!string.IsNullOrEmpty(taskName))
                    {
                        var num = DateTime.Now.ToString("dd");
                        if (taskName.Length<4)
                        {
                            taskName = $"{taskName}    ";
                        }
                        taskName= num + taskName.Trim().Substring(0, 3).ToLower();
                    }
                    else
                    {
                        var s = "Ошибка при формировании номера задания.";
                        ReportAdd(s);       
                        resume = false;
                    }
                    
                    // 25абк
                    
                }
                else if(Mode==3)
                {
                    // 2545/25абк -> 25абк
                    taskName = v.CheckGet("NUMBER");
                    if(taskName.Length>5)
                    {
                        taskName=taskName.CropAfter("/");
                    }                    
                }
                else
                {
                    taskName = v.CheckGet("NUMBER");
                }
            }

            // Определим доступные ГА
            var machineSetId = 0;
            if (resume)
            {
                if (FactoryId == 1)
                {
                    machineSetId = GetAvaliableMachineSetLp();
                }
                else if (FactoryId == 2)
                {
                    machineSetId = GetAvaliableMachineSetKsh();
                }

                if (machineSetId == 0)
                {
                    ReportAdd("Это задание нельзя выполнить ни на одном ГА");
                }
            }

            if (resume)
            {
                var p = new Dictionary<string, string>();

                {
                    p.CheckAdd("ID",                  ProductionTaskId.ToString());

                    //стекер 1
                    {
                        //Вычисляем количество для задания

                        //Для фанфолда и листов с упаковкой не корректируем количество
                        int q1 = v.CheckGet($"STACKER{x1}_QUANTITY_CALCULATED").ToInt();
                        if (!Stackers[x1].CheckGet("Z_CARDBOARD").ToBool() && !Stackers[x1].CheckGet("PACKAGING").ToBool())
                        {
                            double taskLength = v.CheckGet("CUTTING_LENGTH").ToDouble() * 1000;
                            double blankLength1 = v.CheckGet($"STACKER{x1}_LENGTH").ToDouble();
                            int threads1 = v.CheckGet($"STACKER{x1}_THREADS").ToInt();
                            q1 = (int)Math.Ceiling(taskLength / blankLength1) * threads1;
                        }

                        //ИД заявки изделия в ПЗ на 1 резке
                        p.CheckAdd("ID_ORDERDATES1",        v.CheckGet($"STACKER{x1}_POSITION_ID"));

                        //количество заготовок в ПЗ на 1 резке
                        p.CheckAdd("QTY1",                  q1.ToString());

                        //количество ручьев в ПЗ на 1 резке
                        p.CheckAdd("THREAD1",               v.CheckGet($"STACKER{x1}_THREADS"));

                        //образец в ПЗ на 1 резке
                        p.CheckAdd("O1",                    v.CheckGet($"STACKER{x1}_SAMPLE"));

                        //заготовка
                        p.CheckAdd("BLANK_ID1",             v.CheckGet($"STACKER{x1}_BLANK_ID"));
                        p.CheckAdd("LENGTH1",               v.CheckGet($"STACKER{x1}_LENGTH"));
                        p.CheckAdd("WIDTH1",                v.CheckGet($"STACKER{x1}_WIDTH"));

                        //изделие
                        p.CheckAdd("GOODS_ID1",             v.CheckGet($"STACKER{x1}_GOOD_ID"));

                        //стекер
                        p.CheckAdd("CUTOFF_ALLOCATION1",    x1.ToString());

                        //фанфолд (z-картон)
                        p.CheckAdd("FANFOLD_FLAG1", Stackers[x1].CheckGet("Z_CARDBOARD"));
                        // печать
                        p.CheckAdd("PRINTING_FLAG1", Stackers[x1].CheckGet("PRINTING"));
                        // количество рилевок на изделии
                        p.CheckAdd("CREASE_COUNT1", Stackers[x1].CheckGet("CREASE_COUNT"));
                        // конфигурация рилевок
                        p.CheckAdd("SCORERS1", Stackers[x1].CheckGet("CREASE_LIST"));
                        // признак необрезной край
                        p.CheckAdd("UNTRIMMED_EDGE_FLAG1", Stackers[x1].CheckGet("UNTRIMMED_EDGE_FLAG"));

                        { 
                            if(string.IsNullOrEmpty(p["QTY1"]))
                            {
                                p["QTY1"]="0";
                            }
                            if(string.IsNullOrEmpty(p["THREAD1"]))
                            {
                                p["THREAD1"]="0";
                            }
                            if(string.IsNullOrEmpty(p["BLANK_ID1"]))
                            {
                                p["BLANK_ID1"]="0";
                            }
                        }
                    }
                   
                    //стекер 2
                    {
                        //Вычисляем количество для задания
                        //Для фанфолда и листов с упаковкой не корректируем количество
                        int q2 = v.CheckGet($"STACKER{x2}_QUANTITY_CALCULATED").ToInt();
                        if (!Stackers[x2].CheckGet("Z_CARDBOARD").ToBool() && !Stackers[x2].CheckGet("PACKAGING").ToBool())
                        {
                            double taskLength = v.CheckGet("CUTTING_LENGTH").ToDouble() * 1000;
                            double blankLength2 = v.CheckGet($"STACKER{x2}_LENGTH").ToDouble();
                            int threads2 = v.CheckGet($"STACKER{x2}_THREADS").ToInt();
                            q2 = (int)Math.Ceiling(taskLength / blankLength2) * threads2;
                        }

                        //ИД заявки изделия в ПЗ на 1 резке
                        p.CheckAdd("ID_ORDERDATES2",        v.CheckGet($"STACKER{x2}_POSITION_ID"));
                        //количество заготовок в ПЗ на 1 резке
                        p.CheckAdd("QTY2",                  q2.ToString());
                        //количество ручьев в ПЗ на 1 резке
                        p.CheckAdd("THREAD2",               v.CheckGet($"STACKER{x2}_THREADS"));
                        //образец в ПЗ на 1 резке
                        p.CheckAdd("O2",                    v.CheckGet($"STACKER{x2}_SAMPLE"));
                        //заготовка
                        p.CheckAdd("BLANK_ID2",             v.CheckGet($"STACKER{x2}_BLANK_ID"));
                        p.CheckAdd("LENGTH2",               v.CheckGet($"STACKER{x2}_LENGTH"));
                        p.CheckAdd("WIDTH2",                v.CheckGet($"STACKER{x2}_WIDTH"));
                        //изделие
                        p.CheckAdd("GOODS_ID2",             v.CheckGet($"STACKER{x2}_GOOD_ID"));
                        //стекер
                        p.CheckAdd("CUTOFF_ALLOCATION2",    x2.ToString());
                        //фанфолд (z-картон)
                        p.CheckAdd("FANFOLD_FLAG2", Stackers[x2].CheckGet("Z_CARDBOARD"));
                        // печать
                        p.CheckAdd("PRINTING_FLAG2", Stackers[x2].CheckGet("PRINTING"));
                        // количество рилевок на изделии
                        p.CheckAdd("CREASE_COUNT2", Stackers[x2].CheckGet("CREASE_COUNT"));
                        // конфигурация рилевок
                        p.CheckAdd("SCORERS2", Stackers[x2].CheckGet("CREASE_LIST"));
                        // признак необрезной край
                        p.CheckAdd("UNTRIMMED_EDGE_FLAG2", Stackers[x2].CheckGet("UNTRIMMED_EDGE_FLAG"));

                        {
                            if (string.IsNullOrEmpty(p["QTY2"]))
                            {
                                p["QTY2"]="0";
                            }
                            if(string.IsNullOrEmpty(p["THREAD2"]))
                            {
                                p["THREAD2"]="0";
                            }
                            if(string.IsNullOrEmpty(p["BLANK_ID2"]))
                            {
                                p["BLANK_ID2"]="0";
                            }
                        }
                    }

                    //масса бумаги 
                    p.CheckAdd("WEIGHT1",                   v.CheckGet("LAYER1_TASK_WEIGHT"));
                    p.CheckAdd("WEIGHT2",                   v.CheckGet("LAYER2_TASK_WEIGHT"));
                    p.CheckAdd("WEIGHT3",                   v.CheckGet("LAYER3_TASK_WEIGHT"));
                    p.CheckAdd("WEIGHT4",                   v.CheckGet("LAYER4_TASK_WEIGHT"));
                    p.CheckAdd("WEIGHT5",                   v.CheckGet("LAYER5_TASK_WEIGHT"));

                    //raw group id
                    p.CheckAdd("ID_RAW_GROUP1",             v.CheckGet("LAYER1_ID"));
                    p.CheckAdd("ID_RAW_GROUP2",             v.CheckGet("LAYER2_ID"));
                    p.CheckAdd("ID_RAW_GROUP3",             v.CheckGet("LAYER3_ID"));
                    p.CheckAdd("ID_RAW_GROUP4",             v.CheckGet("LAYER4_ID"));
                    p.CheckAdd("ID_RAW_GROUP5",             v.CheckGet("LAYER5_ID"));

                    //признак склейки
                    p.CheckAdd("GLUED_FLAG1",               v.CheckGet("LAYER1_GLUING"));
                    p.CheckAdd("GLUED_FLAG2",               v.CheckGet("LAYER2_GLUING"));
                    p.CheckAdd("GLUED_FLAG3",               v.CheckGet("LAYER3_GLUING"));
                    p.CheckAdd("GLUED_FLAG4",               v.CheckGet("LAYER4_GLUING"));
                    p.CheckAdd("GLUED_FLAG5",               v.CheckGet("LAYER5_GLUING"));
                
                    //процент обрези
                    p.CheckAdd("TRIM_PERCENT",              v.CheckGet("TRIM_PERCENTAGE"));
                    //обрезь в мм
                    p.CheckAdd("TRIM",                      v.CheckGet("TRIM"));

                    //длина в м
                    p.CheckAdd("LENGTH",                    v.CheckGet("CUTTING_LENGTH"));

                    //номер ПЗ
                    //p.CheckAdd("NUM",                       v.CheckGet("NUMBER"));
                    p.CheckAdd("NUM",                       taskName);

                    //примечание ПЗ
                    p.CheckAdd("NOTE",                      comment);

                    //формат
                    p.CheckAdd("FORMAT",                    v.CheckGet("FORMAT"));

                    //ИД профиля
                    p.CheckAdd("ID_PROF",                   v.CheckGet("CARDBOARD_PROFILE_ID"));

                    //признак "фиксированный вес изделия"
                    p.CheckAdd("FIXED_WEIGHT_FLAG",    "0");
                    // Признак тандем
                    p.CheckAdd("TANDEM_FLAG", tandemFlag.ToInt().ToString());
                    // Производственная площадка: 1 - Липецк, 2 - Кашира
                    p.CheckAdd("FACTORY_ID", FactoryId.ToString());

                    //тип рилевки: 0 - нет, 1 - папа-мама, 2- плоская, 3 - папа-папа
                    int scorerType = 0;
                    int creaseType1 = 0;
                    if (Stackers[x1].CheckGet("CREASE_COUNT").ToInt() > 0)
                    {
                        creaseType1 = v.CheckGet($"STACKER{x1}_CREASE").ToInt();
                    }

                    int creaseType2 = 0;
                    if (Stackers[x2].CheckGet("CREASE_COUNT").ToInt() > 0)
                    {
                        creaseType2 = v.CheckGet($"STACKER{x2}_CREASE").ToInt();
                    }

                    if ((creaseType1 > 0) || (creaseType2 > 0))
                    {
                        if (creaseType1.ContainsIn(1, 2, 4))
                        {
                            scorerType = creaseType1;
                        }
                        else if (creaseType2.ContainsIn(1, 2, 4))
                        {
                            scorerType = creaseType2;
                        }
                        else
                        {
                            // если рилевки есть, но их тип не важен, делаем папа-мама
                            scorerType = 1;
                        }
                        //меняем 4 на 3
                        if (scorerType == 4)
                        {
                            scorerType = 3;
                        }
                    }

                    p.CheckAdd("SCORER_TYPE", scorerType.ToString());

                    p.CheckAdd("MACHINE_SET_ID", machineSetId.ToString());

                    if (Mode==3)
                    {
                        p.CheckAdd("_RECREATE","1");
                        p.CheckAdd("OLD_ID", ProductionTaskOldId.ToString());
                    }

                    p.CheckAdd("MODE",Mode.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ProductionTask");

                // Если перевыпуск, выполним экшен перевыпуска. В остальных случаях обычное сохранение
                if (Mode == 4)
                {
                    q.Request.SetParam("Action", "SaveRework");

                    p.CheckAdd("REWORK_REASON", ReworkParams.CheckGet("REWORK_REASON"));
                    p.CheckAdd("REWORK_COMMENT", ReworkParams.CheckGet("REWORK_COMMENT"));
                    p.CheckAdd("PRIMARY_ID_PZ", ReworkParams.CheckGet("PRIMARY_ID_PZ"));
                    p.CheckAdd("PRIMARY_ID2", ReworkParams.CheckGet("PRIMARY_ID2"));
                    string qty;
                    if (p["BLANK_ID1"] == p["PRIMARY_ID2"])
                    {
                        qty = p["QTY1"];
                    }
                    else
                    {
                        qty = p["QTY2"];
                    }

                    p.CheckAdd("QTY", qty);
                    p.CheckAdd("MODE", "1");
                }
                else
                {
                    q.Request.SetParam("Action", "Save");
                }

                q.Request.Timeout=120000;


                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });


                var complete=false;
                int taskId=0;

                if(q.Answer.Status == 0)                
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                    if(result!=null)
                    {
                        if(result.ContainsKey("ITEMS"))
                        {
                            var ds=(ListDataSet)result["ITEMS"];
                            ds.Init();

                            taskId=ds.GetFirstItemValueByKey("ID").ToInt();
                            complete=true;
                        }
                    }
                   
                }
                else
                {
                    q.ProcessError();
                }


                {
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "ProductionTask",
                        ReceiverName = "TaskList",
                        SenderName = "CuttingManual",
                        Action = "Refresh",
                        Message= $"{taskId}",
                    });
                }

                if(complete)
                {
                    SetDefaults();

                    if(!string.IsNullOrEmpty(BackTabName))
                    {
                        Close();
                    }
                }
                    
            }




            if (!resume)
            {
                Central.Logger.Debug(Report);
                SetStatus(Report,true);
            }
            else
            {
                SetStatus("");
            }

            FormToolbar.IsEnabled = true;
            Mouse.OverrideCursor=null;
        }

        //public Window Window { get; set; }
        public void Show()
        {
            string windowTitle=$"";
            string tabTitle=$"";
            string tabTitleDebug="";
            
            switch(Mode)
            {
                default:
                {
                    //создание
                    windowTitle=$"Новое ПЗГА";         
                    tabTitle=$"Новое ПЗГА";         
                }
                break;

                //частичное
                case 1:
                //полное
                case 3:
                {
                    //реадктирование
                    windowTitle=$"ПЗГА: {Number.Text}    ИД: {ProductionTaskId}    Создано: {ProductionTaskCreated.ToString("dd.MM.yyyy HH:mm")}";         
                    tabTitle=$"ПЗГА: {Number.Text}";   
                }
                break;

                //просмотр
                case 2:
                {
                    //просмотр
                    windowTitle=$"ПЗГА: {Number.Text}    ИД: {ProductionTaskId}    Создано: {ProductionTaskCreated.ToString("dd.MM.yyyy HH:mm")} (просмотр)";         
                    tabTitle=$"ПЗГА: {Number.Text}";   
                }
                break;
            }

            tabTitleDebug=$"{tabTitleDebug} Mode=[{Mode}]";

            Title.Text=windowTitle;
            Central.WM.AddTab($"manual_cutting",tabTitle,true,"add",this);

            if(Central.DebugMode)
            {
                TitleDebug.Text=tabTitleDebug;
            }
            else
            {
                TitleDebug.Text="";
            }

            
        }

        public void Close()
        {
            GoBack();
            Central.WM.RemoveTab($"manual_cutting");
            Destroy();
        }
      
        public string BackTabName { get; set; }
        public void GoBack()
        {
            if(!string.IsNullOrEmpty(BackTabName))
            {
                Central.WM.SetActive(BackTabName,true);
                BackTabName="";
            }
        }
        
        private void Save_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Save();
        }

        private void Reset_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            SetDefaults();
        }
        
        private void Stacker12Group_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            SetActiveStackerGroup(1);
        }

        private void Stacker23Group_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            SetActiveStackerGroup(2);
        }

        private void Stacker1Main_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            SetActiveStacker(1);
            ChangeQuantity=false;
            Calculate(true);
        }

        private void Stacker2Main_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            SetActiveStacker(2);
            ChangeQuantity=false;
            Calculate(true);
        }

        private void Stacker3Main_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            SetActiveStacker(3);
            ChangeQuantity=false;
            Calculate(true);
        }

        private void UseManualComposition_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            var v=(bool)UseManualComposition.IsChecked;
            CheckManualComposition(v);
        }

        private void Stacker1SelectButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            SelectBlank(1);
        }

        private void Stacker1ClearButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            ClearStackerBlank(1);
        }

        private void Stacker2SelectButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            SelectBlank(2);
        }

        private void Stacker2ClearButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            ClearStackerBlank(2);
        }

        private void Stacker3SelectButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            SelectBlank(3);
        }

        private void Stacker3ClearButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            ClearStackerBlank(3);
        }

        private void Stacker1Blank_MouseDoubleClick(object sender,System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectBlank(1);
        }

        private void Stacker2Blank_MouseDoubleClick(object sender,System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectBlank(2);
        }

        private void Stacker3Blank_MouseDoubleClick(object sender,System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectBlank(3);
        }

        private void SelectBlank(int stackerId)
        {
            if(Mode!=2)
            {
                var s=new SelectBlank();
                s.BackTabName="manual_cutting";
                s.StackerId=stackerId;
                s.FactoryId = FactoryId;
                s.Show();
            }            
        }

        private void Stacker1Threads_TextChanged(object sender,TextChangedEventArgs e)
        {
            EnableCalcQuantity();
            Calculate();
        }

        private void Stacker2Threads_TextChanged(object sender,TextChangedEventArgs e)
        {
            EnableCalcQuantity();
            Calculate();
        }

        private void Stacker3Threads_TextChanged(object sender,TextChangedEventArgs e)
        {
            EnableCalcQuantity();
            Calculate();
        }

        private void Stacker1QuantityCalculated_TextChanged(object sender,TextChangedEventArgs e)
        {
            //ChangeQuantity=false;  
            EnableCalcQuantity();
            Calculate(false,false);
            GetProductReam(1);
        }

        private void Stacker2QuantityCalculated_TextChanged(object sender,TextChangedEventArgs e)
        {
            //ChangeQuantity=false;
            EnableCalcQuantity();
            Calculate(false,false);
            GetProductReam(2);
        }

        private void Stacker3QuantityCalculated_TextChanged(object sender,TextChangedEventArgs e)
        {
            //ChangeQuantity=false;          
            EnableCalcQuantity();
            Calculate(false,false);
            GetProductReam(3);
        }

        private void CalculateButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Calculate();
        }

        private void Calculate2Button_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            Calculate(true);
        }

        private void Stacker12Swap_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            SwapStackers(1);
        }

        private void Stacker23Swap_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            SwapStackers(2);
        }

        private void HelpButton_Click(object sender,System.Windows.RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void Close_Click(object sender,RoutedEventArgs e)
        {
            Close();
        }

        private void Layer1PaperListSelectButton_Click(object sender,RoutedEventArgs e)
        {
           SelectPaper(1);
        }

        private void Layer2PaperListSelectButton_Click(object sender,RoutedEventArgs e)
        {
           SelectPaper(2);
        }

        private void Layer3PaperListSelectButton_Click(object sender,RoutedEventArgs e)
        {
           SelectPaper(3);
        }

        private void Layer4PaperListSelectButton_Click(object sender,RoutedEventArgs e)
        {
           SelectPaper(4);
        }

        private void Layer5PaperListSelectButton_Click(object sender,RoutedEventArgs e)
        {
           SelectPaper(5);
        }

        private void Layer1Paper_MouseDoubleClick(object sender,MouseButtonEventArgs e)
        {
            SelectPaper(1);
        }

        private void Layer2Paper_MouseDoubleClick(object sender,MouseButtonEventArgs e)
        {
            SelectPaper(2);
        }

        private void Layer3Paper_MouseDoubleClick(object sender,MouseButtonEventArgs e)
        {
            SelectPaper(3);
        }

        private void Layer4Paper_MouseDoubleClick(object sender,MouseButtonEventArgs e)
        {
            SelectPaper(4);
        }

        private void Layer5Paper_MouseDoubleClick(object sender,MouseButtonEventArgs e)
        {
            SelectPaper(5);
        }

        private void Stacker1QuantityCalculated_PreviewMouseUp(object sender,MouseButtonEventArgs e)
        {
            AutochangeStacker(1);
        }

        private void Stacker2QuantityCalculated_PreviewMouseUp(object sender,MouseButtonEventArgs e)
        {
            AutochangeStacker(2);
        }

        private void Stacker3QuantityCalculated_PreviewMouseUp(object sender,MouseButtonEventArgs e)
        {
            AutochangeStacker(3);
        }

        private void CalculateBUtton_Click_1(object sender,RoutedEventArgs e)
        {
            Calculate();
        }

        private void Stacker1ThreadsInc_Click(object sender,RoutedEventArgs e)
        {
            UIUtil.ChangeIntValue(Stacker1Threads,1);
        }

        private void Stacker1ThreadsDec_Click(object sender,RoutedEventArgs e)
        {
            UIUtil.ChangeIntValue(Stacker1Threads,2);
        }

        private void Stacker2ThreadsInc_Click(object sender,RoutedEventArgs e)
        {
            UIUtil.ChangeIntValue(Stacker2Threads,1);
        }

        private void Stacker2ThreadsDec_Click(object sender,RoutedEventArgs e)
        {
            UIUtil.ChangeIntValue(Stacker2Threads,2);
        }

        private void Stacker3ThreadsInc_Click(object sender,RoutedEventArgs e)
        {
            UIUtil.ChangeIntValue(Stacker3Threads,1);
        }

        private void Stacker3ThreadsDec_Click(object sender,RoutedEventArgs e)
        {
            UIUtil.ChangeIntValue(Stacker3Threads,2);
        }

        private void LoadRefButton_Click(object sender,RoutedEventArgs e)
        {
            LoadRef();
        }

        private void Virtual_Click(object sender,RoutedEventArgs e)
        {
            Calculate();
        }

        private void Layer1Gluing_Click(object sender,RoutedEventArgs e)
        {
            CheckSourceChanging();
        }

        private void Stacker1Sample_Click(object sender,RoutedEventArgs e)
        {
            Calculate();
        }

        private void ErrorCodes_Click(object sender, RoutedEventArgs e)
        {
            ShowHelpErrors();
        }

        private void Rework_Click(object sender, RoutedEventArgs e)
        {
            var reworkTab = new ProductionTaskReworkFromTask();
            reworkTab.BackTabName = "manual_cutting";
            reworkTab.FactoryId = FactoryId;
            reworkTab.ShowTab();
        }

        private void Stacker1SelectApplicationButton_Click(object sender, RoutedEventArgs e)
        {
            var selectApplication = new ProductionTaskSelectApplication();
            selectApplication.ReceiverName = "CuttingManualView";
            selectApplication.ApplicationDS = Stacker1ApplicationDS;
            selectApplication.Edit(new Dictionary<string, string>()
            {
                { "STACKER_ID", "1" },
                { "POSITION_ID", Stacker1ApplicationId.Text },
            });
        }

        private void Stacker2SelectApplicationButton_Click(object sender, RoutedEventArgs e)
        {
            var selectApplication = new ProductionTaskSelectApplication();
            selectApplication.ReceiverName = "CuttingManualView";
            selectApplication.ApplicationDS = Stacker2ApplicationDS;
            selectApplication.Edit(new Dictionary<string, string>()
            {
                { "STACKER_ID", "2" },
                { "POSITION_ID", Stacker2ApplicationId.Text },
            });
        }

        private void Stacker3SelectApplicationButton_Click(object sender, RoutedEventArgs e)
        {
            var selectApplication = new ProductionTaskSelectApplication();
            selectApplication.ReceiverName = "CuttingManualView";
            selectApplication.ApplicationDS = Stacker3ApplicationDS;
            selectApplication.Edit(new Dictionary<string, string>()
            {
                { "STACKER_ID", "3" },
                { "POSITION_ID", Stacker3ApplicationId.Text },
            });
        }

        private void CheckRawGroupResidue_Click(object sender, RoutedEventArgs e)
        {
            var obj = (MenuItem)sender;
            if (obj != null)
            {
                var s = obj.Name.Substring(obj.Name.Length - 1);
                if (!s.IsNullOrEmpty())
                {
                    var v = Form.GetValues();
                    int rawGroupId = v.CheckGet($"LAYER{s}_ID").ToInt();
                    int reelWidth = v.CheckGet("FORMAT").ToInt();

                    var rawGroupStockWin = new RawGroupStockResidue();
                    rawGroupStockWin.FactoryId = FactoryId;
                    rawGroupStockWin.RawGroupId = rawGroupId;
                    rawGroupStockWin.ReelWidth = reelWidth;
                    rawGroupStockWin.ShowWin();
                }
            }

        }
    }
}
