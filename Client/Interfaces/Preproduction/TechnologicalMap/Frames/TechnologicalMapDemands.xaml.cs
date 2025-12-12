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

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма для редактирования дополнительных требований к тех карте.
    /// Для решёток в сборе ограниченый набор требований, прочие требования скрыты.
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class TechnologicalMapDemands : UserControl
    {
        public TechnologicalMapDemands()
        {

            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            ProcessPermissions();
            Init();
            SetDefaults();
        }

        public string RoleName = "[erp]partition_technological_map";

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
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        public string ParentFrame { get; set; }

        /// <summary>
        /// Идентификатор типа продукции, для которой устанавливаются доп требования.
        /// tk.id_pclass
        /// </summary>
        public int TypeProduct { get; set; }

        /// <summary>
        /// ИД тех карты (первой решётки)
        /// </summary>
        public int FirstTechnologicalMapId { get; set; }

        /// <summary>
        /// ИД тех карты (второй решётки)
        /// </summary>
        public int SecondTechnologicalMapId { get; set; }

        /// <summary>
        /// Датасет с данными по текущим доп требованиям к тех карте
        /// </summary>
        public ListDataSet TechnologicalMapDemandsDataSet { get; set; }

        /// <summary>
        /// Датасет с данными по дополнительным требованиям
        /// Нужен для заполнения выпадающих списков дополнительных требований
        /// </summary>
        public ListDataSet DemandsDataSet { get; set; }

        /// <summary>
        /// Датасет с данными по рамкам
        /// Нужен для заполнения выпадающего списка Рамка
        /// </summary>
        public ListDataSet FrameDataSet { get; set; }

        /// <summary>
        /// Датасет с данными по кодировке по классификатору FEFCO для нестандартных изделий
        /// Нужен для заполнения выпадающего списка FEFCO
        /// </summary>
        public ListDataSet FEFCODataSet { get; set; }

        /// <summary>
        /// Датасет с данными по станку ГА, на котором следует ехать
        /// Нужен для заполнения выпадающего списка ГА
        /// </summary>
        public ListDataSet CorrugatingMachineDataSet { get; set; }

        /// <summary>
        /// Пустой словарь
        /// Нужен для добавления в выпадающие списки пустой строки
        /// KEY -- (-1), VALUE -- ("")
        /// </summary>
        public Dictionary<string, string> EmptyDictionary { get; set; }

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
        }

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
                    Path="DEMAND_FIRST",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=DemandFirstSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DEMAND_SECOND",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=DemandSecondSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FEFCO",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=FEFCOSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FRAME",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=FrameSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QTY_LABEL",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QtyLabelSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SPECIAL_LABEL",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=SpecialLabelCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QTY_SAMPLE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QtySampleTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRINT_DATE",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=PrintDateCreatedCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="REFERENCE_SAMPLE",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=ReferenceSampleCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="OLD_PRODUCT",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=OldProductCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ARHIVE",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=ArhiveCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TARGET_BTC",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TargetBTCTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },



                new FormHelperField()
                {
                    Path="SCORING",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ScoringSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CORRUGATING_MACHINE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CorrugatingMachineSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="HOT",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=HotCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="REVERSE_CUTTING",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=ReverseCuttingCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="AUTOMATIC_ASSEMBLY",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=AutomaticAssemblyCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FULL_STAMP",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=FullStampCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CORRUGATED_ACROSS",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=CorrugatedAcrossCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SUPPLY_ONE",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=SupplyOneCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TWO_SIDE_PRINT",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=TwoSidePrintCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="UNCUT_EDGE",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=UncutEdgeCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STRICTLY_QTY_ON_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=StrictlyQtyOnPalletCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ON_EDGE",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=OnEdgeCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },



                new FormHelperField()
                {
                    Path="PRODUCTION_NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductionNoteTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CORRUGATING_MACHINE_NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CorrugatingMachineNoteTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },






                new FormHelperField()
                {
                    Path="DEMAND_FIRST2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=DemandFirst2SelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DEMAND_SECOND2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=DemandSecond2SelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FEFCO2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=FEFCO2SelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FRAME2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Frame2SelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QTY_LABEL2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QtyLabel2SelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SPECIAL_LABEL2",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=SpecialLabel2CheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QTY_SAMPLE2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QtySample2TextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRINT_DATE2",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=PrintDateCreated2CheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="REFERENCE_SAMPLE2",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=ReferenceSample2CheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="OLD_PRODUCT2",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=OldProduct2CheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ARHIVE2",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=Arhive2CheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TARGET_BTC2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TargetBTC2TextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },



                new FormHelperField()
                {
                    Path="SCORING2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Scoring2SelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CORRUGATING_MACHINE2",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=CorrugatingMachine2SelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="HOT2",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=Hot2CheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="REVERSE_CUTTING2",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=ReverseCutting2CheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="AUTOMATIC_ASSEMBLY2",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=AutomaticAssembly2CheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FULL_STAMP2",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=FullStamp2CheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CORRUGATED_ACROSS2",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=CorrugatedAcross2CheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SUPPLY_ONE2",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=SupplyOne2CheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TWO_SIDE_PRINT2",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=TwoSidePrint2CheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="UNCUT_EDGE2",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=UncutEdge2CheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STRICTLY_QTY_ON_PALLET2",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=StrictlyQtyOnPallet2CheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ON_EDGE2",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=OnEdge2CheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },



                new FormHelperField()
                {
                    Path="PRODUCTION_NOTE2",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductionNote2TextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CORRUGATING_MACHINE_NOTE2",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CorrugatingMachineNote2TextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            TechnologicalMapDemandsDataSet = new ListDataSet();

            EmptyDictionary = new Dictionary<string, string>();
            EmptyDictionary.Add("-1", "");
                
            DemandsDataSet = new ListDataSet();
            FrameDataSet = new ListDataSet();
            FEFCODataSet = new ListDataSet();
            CorrugatingMachineDataSet = new ListDataSet();

            LoadDemandDataForSelectBoxes();
            LoadFrameDataForSelectBox();
            LoadFEFCODataForSelectBox();
            LoadCorrugatingMachineDataForSelectBox();
            FillingScoringSelectBox();
            FillingQtyLabelSelectBox();
        }

        /// <summary>
        /// Получаем данные для заполненя выпадающих списков Дополнительных требований
        /// </summary>
        public void LoadDemandDataForSelectBoxes()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PartitionTechnologicalMap");
            q.Request.SetParam("Action", "ListDemand");
            q.Request.SetParams(p);
            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    DemandsDataSet = ListDataSet.Create(result, "ITEMS");

                    {
                        DemandFirstSelectBox.SetItems(DemandsDataSet, "ID", "NAME");
                        DemandFirstSelectBox.Items.AddRange(EmptyDictionary);
                        if (DemandFirstSelectBox.Items.ContainsKey("-1"))
                        {
                            DemandFirstSelectBox.SelectedItem = DemandFirstSelectBox.Items.FirstOrDefault(x => x.Key == "-1");
                        }
                    }

                    {
                        DemandSecondSelectBox.SetItems(DemandsDataSet, "ID", "NAME");
                        DemandSecondSelectBox.Items.AddRange(EmptyDictionary);
                        if (DemandSecondSelectBox.Items.ContainsKey("-1"))
                        {
                            DemandSecondSelectBox.SelectedItem = DemandSecondSelectBox.Items.FirstOrDefault(x => x.Key == "-1");
                        }
                    }

                    {
                        DemandFirst2SelectBox.SetItems(DemandsDataSet, "ID", "NAME");
                        DemandFirst2SelectBox.Items.AddRange(EmptyDictionary);
                        if (DemandFirst2SelectBox.Items.ContainsKey("-1"))
                        {
                            DemandFirst2SelectBox.SelectedItem = DemandFirst2SelectBox.Items.FirstOrDefault(x => x.Key == "-1");
                        }
                    }

                    {
                        DemandSecond2SelectBox.SetItems(DemandsDataSet, "ID", "NAME");
                        DemandSecond2SelectBox.Items.AddRange(EmptyDictionary);
                        if (DemandSecond2SelectBox.Items.ContainsKey("-1"))
                        {
                            DemandSecond2SelectBox.SelectedItem = DemandSecond2SelectBox.Items.FirstOrDefault(x => x.Key == "-1");
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
        /// Получаем данные для заполнения выпадающего списка Рамка
        /// </summary>
        public void LoadFrameDataForSelectBox()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PartitionTechnologicalMap");
            q.Request.SetParam("Action", "ListPalletFrame");
            q.Request.SetParams(p);
            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    FrameDataSet = ListDataSet.Create(result, "ITEMS");

                    FrameSelectBox.SetItems(FrameDataSet, "ID", "NAME");
                    FrameSelectBox.Items.AddRange(EmptyDictionary);
                    if (FrameSelectBox.Items.ContainsKey("-1"))
                    {
                        FrameSelectBox.SelectedItem = FrameSelectBox.Items.FirstOrDefault(x => x.Key == "-1");
                    }

                    Frame2SelectBox.SetItems(FrameDataSet, "ID", "NAME");
                    Frame2SelectBox.Items.AddRange(EmptyDictionary);
                    if (Frame2SelectBox.Items.ContainsKey("-1"))
                    {
                        Frame2SelectBox.SelectedItem = Frame2SelectBox.Items.FirstOrDefault(x => x.Key == "-1");
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Получаем данные для заполнения выпадающего списка FEFCO
        /// </summary>
        public void LoadFEFCODataForSelectBox()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PartitionTechnologicalMap");
            q.Request.SetParam("Action", "ListFEFCO");
            q.Request.SetParams(p);
            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    FEFCODataSet = ListDataSet.Create(result, "ITEMS");

                    FEFCOSelectBox.SetItems(FEFCODataSet, "ID", "NAME");
                    FEFCOSelectBox.Items.AddRange(EmptyDictionary);
                    if (FEFCOSelectBox.Items.ContainsKey("-1"))
                    {
                        FEFCOSelectBox.SelectedItem = FEFCOSelectBox.Items.FirstOrDefault(x => x.Key == "-1");
                    }

                    FEFCO2SelectBox.SetItems(FEFCODataSet, "ID", "NAME");
                    FEFCO2SelectBox.Items.AddRange(EmptyDictionary);
                    if (FEFCO2SelectBox.Items.ContainsKey("-1"))
                    {
                        FEFCO2SelectBox.SelectedItem = FEFCO2SelectBox.Items.FirstOrDefault(x => x.Key == "-1");
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Получаем данные для заполнения выпадающего списка ГА
        /// </summary>
        public void LoadCorrugatingMachineDataForSelectBox()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PartitionTechnologicalMap");
            q.Request.SetParam("Action", "ListCorrugatingMachine");
            q.Request.SetParams(p);
            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    CorrugatingMachineDataSet = ListDataSet.Create(result, "ITEMS");

                    CorrugatingMachineSelectBox.SetItems(CorrugatingMachineDataSet, "ID", "NAME");
                    CorrugatingMachineSelectBox.Items.Add("-1", "Любой");
                    if (CorrugatingMachineSelectBox.Items.ContainsKey("-1"))
                    {
                        CorrugatingMachineSelectBox.SelectedItem = CorrugatingMachineSelectBox.Items.FirstOrDefault(x => x.Key == "-1");
                    }

                    CorrugatingMachine2SelectBox.SetItems(CorrugatingMachineDataSet, "ID", "NAME");
                    CorrugatingMachine2SelectBox.Items.Add("-1", "Любой");
                    if (CorrugatingMachine2SelectBox.Items.ContainsKey("-1"))
                    {
                        CorrugatingMachine2SelectBox.SelectedItem = CorrugatingMachine2SelectBox.Items.FirstOrDefault(x => x.Key == "-1");
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Заполняем данные для выпадающего списка Рилёвки
        /// </summary>
        public void FillingScoringSelectBox()
        {
            ScoringSelectBox.Items.Add("1", "Рилевка с ответной частью (папа-мама)");
            ScoringSelectBox.Items.Add("2", "Заливная печать (плоская рилевка)");
            ScoringSelectBox.Items.Add("3", "Любая");
            ScoringSelectBox.Items.Add("4", "Рилевка с ответной частью (папа-папа)");

            if (ScoringSelectBox.Items.ContainsKey("3"))
            {
                ScoringSelectBox.SelectedItem = ScoringSelectBox.Items.FirstOrDefault(x => x.Key == "3");
            }

            Scoring2SelectBox.Items.Add("1", "Рилевка с ответной частью (папа-мама)");
            Scoring2SelectBox.Items.Add("2", "Заливная печать (плоская рилевка)");
            Scoring2SelectBox.Items.Add("3", "Любая");
            Scoring2SelectBox.Items.Add("4", "Рилевка с ответной частью (папа-папа)");

            if (Scoring2SelectBox.Items.ContainsKey("3"))
            {
                Scoring2SelectBox.SelectedItem = Scoring2SelectBox.Items.FirstOrDefault(x => x.Key == "3");
            }
        }

        /// <summary>
        /// Заполняем данные для выпадающего списка Кол-во ярлыков
        /// </summary>
        public void FillingQtyLabelSelectBox()
        {
            QtyLabelSelectBox.Items.Add("1", "1");
            QtyLabelSelectBox.Items.Add("2", "2");
            QtyLabelSelectBox.Items.Add("3", "3");
            QtyLabelSelectBox.Items.Add("4", "4");
            QtyLabelSelectBox.Items.Add("5", "5");
            QtyLabelSelectBox.Items.Add("6", "6");
            QtyLabelSelectBox.Items.Add("7", "7");
            QtyLabelSelectBox.Items.Add("8", "8");
            QtyLabelSelectBox.Items.Add("9", "9");
            QtyLabelSelectBox.Items.Add("10", "10");

            QtyLabel2SelectBox.Items.Add("1", "1");
            QtyLabel2SelectBox.Items.Add("2", "2");
            QtyLabel2SelectBox.Items.Add("3", "3");
            QtyLabel2SelectBox.Items.Add("4", "4");
            QtyLabel2SelectBox.Items.Add("5", "5");
            QtyLabel2SelectBox.Items.Add("6", "6");
            QtyLabel2SelectBox.Items.Add("7", "7");
            QtyLabel2SelectBox.Items.Add("8", "8");
            QtyLabel2SelectBox.Items.Add("9", "9");
            QtyLabel2SelectBox.Items.Add("10", "10");
        }

        /// <summary>
        /// Получаем данные по текущим доп требованиям для заполнения полей формы
        /// </summary>
        public async void LoadTechnologicalMapData()
        {
            if (TypeProduct == 100 || TypeProduct == 12)
            {
                var p = new Dictionary<string, string>();
                p.Add("ID_TK_FIRST", FirstTechnologicalMapId.ToString());
                p.Add("ID_TK_SECOND", SecondTechnologicalMapId.ToString());
                p.Add("TYPE_PRODUCT", TypeProduct.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "PartitionTechnologicalMap");
                q.Request.SetParam("Action", "GetDemands");
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
                        TechnologicalMapDemandsDataSet = ListDataSet.Create(result, "ITEMS");
                        Form.SetValues(TechnologicalMapDemandsDataSet);

                        {
                            var fefcoName = TechnologicalMapDemandsDataSet.Items.First().CheckGet("FEFCO");
                            var fefcoKey = FEFCOSelectBox.Items.FirstOrDefault(x => x.Value == fefcoName).Key;
                            if (fefcoKey != null)
                            {
                                FEFCOSelectBox.SetSelectedItemByKey(fefcoKey);
                            }
                        }

                        {
                            var fefcoName2 = TechnologicalMapDemandsDataSet.Items.First().CheckGet("FEFCO2");
                            var fefcoKey2 = FEFCO2SelectBox.Items.FirstOrDefault(x => x.Value == fefcoName2).Key;
                            if (fefcoKey2 != null)
                            {
                                FEFCO2SelectBox.SetSelectedItemByKey(fefcoKey2);
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
            else if (TypeProduct == 14 || TypeProduct == 15 || TypeProduct == 226)
            {
                var p = new Dictionary<string, string>();
                p.Add("ID_TK_FIRST", FirstTechnologicalMapId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "GasketTechnologicalMap");
                q.Request.SetParam("Action", "GetDemands");
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
                        TechnologicalMapDemandsDataSet = ListDataSet.Create(result, "ITEMS");
                        Form.SetValues(TechnologicalMapDemandsDataSet);

                        {
                            var fefcoName = TechnologicalMapDemandsDataSet.Items.First().CheckGet("FEFCO");
                            var fefcoKey = FEFCOSelectBox.Items.FirstOrDefault(x => x.Value == fefcoName).Key;
                            if (fefcoKey != null)
                            {
                                FEFCOSelectBox.SetSelectedItemByKey(fefcoKey);
                            }
                        }

                        {
                            var fefcoName2 = TechnologicalMapDemandsDataSet.Items.First().CheckGet("FEFCO2");
                            var fefcoKey2 = FEFCO2SelectBox.Items.FirstOrDefault(x => x.Value == fefcoName2).Key;
                            if (fefcoKey2 != null)
                            {
                                FEFCO2SelectBox.SetSelectedItemByKey(fefcoKey2);
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Получаем данные формы для отправки запроса на сохранение
        /// </summary>
        public Dictionary<string, string> GetDataForSave()
        {
            Dictionary<string, string> formValues = new Dictionary<string, string>();

            if (Form != null)
            {
                if (Form.Validate())
                {
                    formValues = Form.GetValues();
                    formValues.CheckAdd("ID_TK_FIRST", FirstTechnologicalMapId.ToString());
                    formValues.CheckAdd("FEFCO", FEFCOSelectBox.SelectedItem.Value);
                    formValues.CheckAdd("TYPE_PRODUCT", TypeProduct.ToString());
                    formValues.CheckAdd("ID_TK_SECOND", SecondTechnologicalMapId.ToString());
                    formValues.CheckAdd("FEFCO2", FEFCO2SelectBox.SelectedItem.Value);
                }
            }

            return formValues;
        }

        /// <summary>
        /// Сохраняем изменения в доп требованиях
        /// </summary>
        public async void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "PartitionTechnologicalMap");
            q.Request.SetParam("Action", "UpdateDemands");

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
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds.Items.First().CheckGet("ID_TK_FIRST").ToInt() > 0)
                    {
                        // Отправляем сообщение вкладке редактирования Решётки о новом сохранённом значении поля Укладка на ребро
                        {
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "Preproduction",
                                ReceiverName = "TechnologicalMap",
                                SenderName = FrameName,
                                Action = "UpdateOnEdge",
                                Message = "",
                                ContextObject = Form.GetValues(),
                            }
                            );
                        }

                        var msg = "Дополнительные требования успешно сохранены.";
                        var d = new DialogWindow($"{msg}", "Дополнительные требования", "", DialogWindowButtons.OK);
                        d.ShowDialog();

                        Close();
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public void Show(string parentFrame = "add")
        {
            ParentFrame = parentFrame;
            var dt = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
            FrameName = $"PartitionTechnologicalMapDemands_{FirstTechnologicalMapId}_{dt}";

            if (TypeProduct == 100)
            {
                GridOfDemandsSecond.Visibility = Visibility.Visible;
            }
            else
            {
                GridOfDemandsSecond.Visibility = Visibility.Collapsed;
            }

            LoadTechnologicalMapData();

            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;
            Central.WM.Show(FrameName, $"Дополнительные требования ТК №{FirstTechnologicalMapId}", true, "add", this);
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.SetActive(ParentFrame, true);

            Central.WM.Close(FrameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Preproduction",
                ReceiverName = "TechnologicalMap",
                SenderName = "TechnologicalMapDemands",
                Action = "Closed",
                Message = $"{ParentFrame}",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }


        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        /// <summary>
        /// обработка ввода с клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Документация
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/preproduction/tk_grid/TkGrid/demands");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string msg = "Сохранить доп требования?";
            var d = new DialogWindow($"{msg}", "Дополнительные требования", "", DialogWindowButtons.NoYes);
            if (d.ShowDialog() == true)
            {
                var values = GetDataForSave();
                if (values.Count > 0)
                {
                    SaveData(values);
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
    }
}
