using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Подробная информация по заявке производственного задания
    /// </summary>
    public partial class TaskDetails : UserControl
    {
        /// <summary>
        /// Обязательные к заполнению переменные:
        /// ProductionTaskId;
        /// ProductId.
        /// Не обязательные к заполнению переменные:
        /// ParentFrame.
        /// </summary>
        public TaskDetails()
        {
            InitializeComponent();
            FrameName = "TaskDetails";

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();
            SetDefaults();
        }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        public string ParentFrame { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Идентификатор производственного задания. Обязательное поле
        /// </summary>
        public int ProductionTaskId { get; set; }

        /// <summary>
        /// Идентификатор продукции. Обязательное поле
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Датасет с данными для заполнения формы
        /// </summary>
        public ListDataSet FormDataSet { get; set; }

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
                    Path="PRODUCTION_TASK_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCTION_TASK_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductionTaskNumberTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TASK_CREATED_DTTM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TaskCreatedDttmTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TASK_PLAN_DTTM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TaskPlanDttmTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCTION_TASK_NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductionTaskNoteTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PROFIL_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProfilNameTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=QIDTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PROFIL_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="PRODUCT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_IDK1",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductNameTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_CODE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductCodeTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_KOD",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductKodTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductWidthTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductLengthTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_HEIGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductHeigthTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductNoteTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_VOLUM",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=ProductVolumTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_TURN",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductTurnTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SCORING",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ScoringTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FANFOLD_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=FanfoldFlagCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SEQUENC",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=SequencTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SHRAN",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ShranTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CARDBOARD_DESCRIPTION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CardboardDescriptionTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="OUTER_COLOR_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=OuterColorNameTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_STACK_ON_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QuantityStackOnPalletTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKING_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=StackingNameTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CARDBOARD_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_PROFIL_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="PACKAGE_WIDTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PackageWidthTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PACKAGE_LENGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PackageLengthTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PACKAGE_HEIGTH",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PackageHeigthTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FIXED_WEIGHT_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=FixedWeightFlagCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="FILL_PRINTING_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=FillPrintingFlagCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CORRUGATED_NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CorrugatedNoteTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

                new FormHelperField()
                {
                    Path="ORDER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=OrderIdTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CROSSCUT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CrosscutTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CUTOFF_ALLOCATION",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CutoffAllocationTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_STREAM",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QuantityStreamTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_PRODUCT_BY_TASK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QuantityProductByTaskTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_PALLET_BY_TASK",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QuantityPalletByTaskTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="STACKING_ON_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=StackingOnPalletTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_IN_REAM",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QuantityInReamTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="QUANTITY_ON_PALLET",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QuantityOnPalletTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="MASTER_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=MasterFlagCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SAMPLE_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=SampleFlagCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TESTING_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=TestingFlagCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="REWORK_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=ReworkFlagCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="UNDERCUT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=UndercutTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NEXT_PLACE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NextPlaceTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NEXT_TASK_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NextTaskNumberTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PALLET_STANDART_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=PalletStandartFlagCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PALLET_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PalletNameTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PALLET_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PalletTypeTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="TASK_END_DTTM_BY_SHIPMENT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TaskEndDttmByShipmentTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="SHIPMENT_PLAN_DTTM",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ShipmentPlanDttmTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NEXT_TASK_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
        }

        /// <summary>
        /// Деактивация контроллов
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
            MainGrid.IsEnabled = false;
        }

        /// <summary>
        /// Активация контроллов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
            MainGrid.IsEnabled = true;
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FormDataSet = new ListDataSet();
        }

        /// <summary>
        /// Получение данных для заполнения полей
        /// </summary>
        public void LoadData()
        {
            if (Form != null)
            {
                Form.SetDefaults();
            }

            if (ProductionTaskId > 0 && ProductId > 0)
            {
                DisableControls();

                var p = new Dictionary<string, string>();
                p.Add("PRODUCTION_TASK_ID", ProductionTaskId.ToString());
                p.Add("PRODUCT_ID", ProductId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ManuallyPrint");
                q.Request.SetParam("Action", "GetPositionDetails");
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
                        FormDataSet = dataSet;
                        if (FormDataSet != null && FormDataSet.Items != null && FormDataSet.Items.Count > 0)
                        {
                            var first = FormDataSet.Items.First();

                            // Считаем рилёвки
                            {
                                int creaseCount = 0;
                                if (first.CheckGet("PRODUCT_HEIGTH").ToInt() > 0)
                                {
                                    creaseCount = 2;
                                }
                                else
                                {
                                    for (int i = 1; i <= 24; i++)
                                    {
                                        if (first.CheckGet($"P{i}").ToInt() > 0)
                                        {
                                            creaseCount++;
                                        }
                                    }
                                }

                                if (creaseCount > 0)
                                {
                                    first.CheckAdd("SCORING", creaseCount.ToString());
                                }
                                else
                                {
                                    first.CheckAdd("SCORING", "Нет");
                                }
                            }

                            // Рисуем картинку укладки
                            {
                                byte[] bytes = Convert.FromBase64String(first.CheckGet("STACKING_IMAGE"));
                                var mem = new MemoryStream(bytes) { Position = 0 };
                                var image = new BitmapImage();
                                image.BeginInit();
                                image.StreamSource = mem;
                                image.EndInit();
                                StackingImage.Source = image;
                            }
                        }

                        Form.SetValues(FormDataSet);
                    }
                }
                else
                {
                    q.ProcessError();
                }

                EnableControls();
            }
            else
            {
                var msg = "Не выбраны производственное задание или продукция.";
                var d = new DialogWindow($"{msg}", "Детализация задания", "", DialogWindowButtons.OK);
                d.ShowDialog();
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
            Central.WM.FrameMode = 1;
            FrameName = $"{FrameName}_{ProductionTaskId}_{ProductId}";
            Central.WM.Show(FrameName, $"Детализация задания {ProductionTaskId}", true, "add", this);

            LoadData();
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
            if (!string.IsNullOrEmpty(ParentFrame))
            {
                Central.WM.SetActive(ParentFrame, true);
            }

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
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = "TaskDetails",
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


        private void ResreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
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
