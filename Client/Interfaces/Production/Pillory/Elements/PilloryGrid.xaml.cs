using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Mvvm.UI.ModuleInjection;
using DevExpress.Utils.About;
using Newtonsoft.Json;
using NPOI.OpenXmlFormats.Vml;
using NPOI.SS.UserModel.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static DevExpress.Data.Filtering.Helpers.SubExprHelper.ThreadHoppingFiltering;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Элемент "Грид" для монитора позорного столба
    /// </summary>
    public partial class PilloryGrid : UserControl
    {
        public PilloryGrid(int machineId = 0, string machineName = "")
        {
            FrameName = "PilloryGrid";
            InitializeComponent();

            MachineId = machineId;
            MachineName = machineName;

            FormInit();
            BarChartInit();
            GridInit();
            SetDefaults();
        }

        public string RoleName { get; set; }

        public int FactoryId { get; set; }

        public string ParentFrame { get; set; }

        private string ControlName = "Монитор мастера";

        private string FrameName { get; set; }

        /// <summary>
        /// процессор форм
        /// </summary>
        private FormHelper Form { get; set; }

        /// <summary>
        /// Ид станка
        /// </summary>
        public int MachineId { get; set; }

        /// <summary>
        /// Наименование станка
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// Номер страницы, на которой быдем размещать грид
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Данные верхнего блока сводки
        /// </summary>
        public Dictionary<string, string> FormData { get; set; }

        /// <summary>
        /// Данные грида
        /// </summary>
        public ListDataSet GridDataSet { get; set; }

        /// <summary>
        /// Данные для столбчатой диаграммы
        /// </summary>
        public Dictionary<string, string> BarChartData { get; set; }

        /// <summary>
        /// Столбчатая диаграмма
        /// </summary>
        public BarChart BarChart { get; set; }

        /// <summary>
        /// Спидометр
        /// </summary>
        public SpeedBar2 SpeedBar { get; set; }

        private void SetDefaults()
        {
            FormData = new Dictionary<string, string>();
            GridDataSet = new ListDataSet();
            BarChartData = new Dictionary<string, string>();

            Form.SetDefaults();

            MachineIdTextBox.Text = $"{MachineId}";
            MachineNameTextBox.Text = MachineName;
        }

        private void FormInit()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="MACHINE_SPEED",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=MachineSpeedTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DEFECT_PERCENTAGE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=DefectPercentageTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="MACHINE_DATA",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=MachineDataTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="MACHINE_DATA_2",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=MachineData2TextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="IDLES_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    AfterSet = (FormHelperField f, string v) => {
                        MachineNameTextBox.Foreground = GetColorByMachineStatus(Form.GetValueByPath("IDLES_TYPE").ToInt()).ToBrush();
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_PRODUCT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NEXT_PRODUCT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENT_SPEED",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Format = "N2",
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PLAN_SPEED",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Format = "N2",
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="MAX_SPEED",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Format = "N2",
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);

            Form.AfterSet = (Dictionary<string, string> v) =>
            {
                MachineNameTextBox.Foreground = GetColorByMachineStatus(Form.GetValueByPath("IDLES_TYPE").ToInt()).ToBrush();
            };
        }

        private void FormLoadItems()
        {
            if (FormData != null && FormData.Count > 0)
            {
                Form.SetValues(FormData);
                SetMachineStatus(FormData);
            }
            else
            {
                //Form.SetValues(new Dictionary<string, string>());
                Form.SetDefaults();
            }
        }

        private void GridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Description = "",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=0,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Символы",
                        Description = "",
                        Path="LABEL",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                        Labels = new List<DataGridHelperColumnLabel>()
                        {
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("М",HColor.GreenFG,HColor.Yellow, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(row.CheckGet("MASTER_FLAG").ToInt() > 0)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("Б",HColor.GreenFG,HColor.Yellow, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(row.CheckGet("BRAKER_FLAG").ToInt() > 0)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("О",HColor.GreenFG,HColor.Yellow, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(row.CheckGet("SAMPLE_FLAG").ToInt() > 0)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("НШ",HColor.GreenFG,HColor.Yellow, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(row.CheckGet("SHTANZ_STATUS").ToInt() == 1)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("НК",HColor.GreenFG,HColor.Yellow, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(row.CheckGet("KLISHE_STATUS").ToInt() == 1)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("ОБ",HColor.GreenFG,HColor.Yellow, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(row.CheckGet("MODULEX_FLAG").ToInt() > 0)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("2С",HColor.GreenFG,HColor.Yellow, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(row.CheckGet("TWICE_SLOTTOR_FLAG").ToInt() > 0)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("Т",HColor.GreenFG,HColor.Yellow, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        //Если задание выполняется "строго" или "не менее"
                                        if(row.CheckGet("STRICLE_STATUS").ToInt() == 1 
                                            || row.CheckGet("STRICLE_STATUS").ToInt() == 3)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("Э",HColor.GreenFG,HColor.Yellow, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(row.CheckGet("REFERENCE_SAMPLE_FLAG").ToInt() > 0)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("А",HColor.GreenFG,HColor.Yellow, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(row.CheckGet("CAR_ARRIVAL_FLAG").ToInt() > 0)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("1Ц",HColor.GreenFG,HColor.Yellow, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(row.CheckGet("COLOR_QUANTITY").ToInt() == 1)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("2Ц",HColor.GreenFG,HColor.Yellow, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(row.CheckGet("COLOR_QUANTITY").ToInt() == 2)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("3Ц",HColor.GreenFG,HColor.Yellow, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(row.CheckGet("COLOR_QUANTITY").ToInt() == 3)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("4Ц",HColor.GreenFG,HColor.Yellow, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(row.CheckGet("COLOR_QUANTITY").ToInt() == 4)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("5Ц",HColor.GreenFG,HColor.Yellow, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(row.CheckGet("COLOR_QUANTITY").ToInt() == 5)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("Ц",HColor.GreenFG,HColor.Yellow, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(row.CheckGet("COLOR_QUANTITY").ToInt() > 5)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("П",HColor.GreenFG,HColor.Yellow, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(row.CheckGet("SAMPLE_GET_QUANTITY").ToInt() > 0)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("Л",HColor.BlackFG,HColor.White, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(row.CheckGet("TESTING_FLAG").ToInt() > 0)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("!",HColor.GreenFG,HColor.Yellow, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(!string.IsNullOrEmpty(row.CheckGet("NOTE")) && row.CheckGet("HOT_FLAG").ToInt() == 0)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("!",HColor.Yellow,HColor.BlackFG, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(!string.IsNullOrEmpty(row.CheckGet("NOTE")) && row.CheckGet("HOT_FLAG").ToInt() > 0)
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                            new DataGridHelperColumnLabel()
                            {
                                Construct=()=>
                                {
                                    var block=DataGridHelperColumnLabel.MakeElement("!",HColor.RedAccented,HColor.Yellow, 16, 16);
                                    return block;
                                },
                                Update = (Dictionary<string, string> row) =>
                                {
                                    var result = DependencyProperty.UnsetValue;
                                    {
                                        result=Visibility.Collapsed;
                                        if(row.CheckGet("HOT_FLAG").ToInt() > 0 && string.IsNullOrEmpty(row.CheckGet("NOTE")))
                                        {
                                            result=Visibility.Visible;
                                        }
                                    }
                                    return result;
                                },
                            },
                        },
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    color = HColor.White;

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
                        Header="Начать до",
                        Description = "",
                        Path="DT_START2",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Продукция",
                        Description = "Наименование продукции",
                        Path="PRODUCT_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Количество",
                        Description = "Количество заготовок",
                        Path="QUANTITY",
                        ColumnType=ColumnTypeRef.Double,
                        Format="N0",
                        Width2=6,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Профиль",
                        Description = "",
                        Path="PROFILE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=4,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // можно перенести на альтернативную схему производства
                                    if (row.CheckGet("SECOND_SCHEME_FLAG").ToInt() > 0)
                                    {
                                        // желтый
                                        color = HColor.Yellow;
                                    }

                                    if (!string.IsNullOrEmpty(color))
                                    {
                                        result=color.ToBrush();
                                    }

                                    return result;
                                }
                            },
                            {
                                StylerTypeRef.ForegroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // можно перенести на альтернативную схему производства
                                    if (row.CheckGet("SECOND_SCHEME_FLAG").ToInt() > 0)
                                    {
                                        // зелёный
                                        color = HColor.GreenFG;
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
                        Header="Статус",
                        Description = "",
                        Path="STATUS",
                        ColumnType=ColumnTypeRef.String,
                        Width2=4,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.FontWeight,
                                row =>
                                {
                                    var fontWeight= new FontWeight();
                                    fontWeight=FontWeights.Normal;

                                    if(row.CheckGet("STATUS") != "Г")
                                    {
                                        fontWeight=FontWeights.Bold;
                                    }

                                    return fontWeight;
                                }
                            },
                        },
                    },
                    new DataGridHelperColumn
                    {
                        Header="Отгрузка",
                        Description = "Дата отгрузки",
                        Path="SHIPMENT_DTTM",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // отгрузка с признаком самовывоз
                                    if (row.CheckGet("SELFSHIP_FLAG").ToInt() == 1)
                                    {
                                        // светоло-фиолетовый
                                        color = "#E6A0FC";
                                    }
                                    // отгрузка с нашей доставкой
                                    else if (row.CheckGet("SELFSHIP_FLAG").ToInt() == 0)
                                    {
                                        // светоло-зелёный
                                        color = HColor.Green;
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
                        Header="Задание",
                        Description = "Номер производственного задания",
                        Path="PRODUCTION_TASK_NUMBER",
                        ColumnType=ColumnTypeRef.String,
                        Width2=4,
                        Stylers=new Dictionary<StylerTypeRef,StylerDelegate>()
                        {
                            {
                                StylerTypeRef.BackgroundColor,
                                row =>
                                {
                                    var result=DependencyProperty.UnsetValue;
                                    var color = "";

                                    // задание в очереди ГА
                                    if (row.CheckGet("IN_CORRUGATOR_QUEUE").ToInt() > 0)
                                    {
                                        color = HColor.Yellow;
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
                        Header="В очереди ГА",
                        Description = "Признак того, что задание находится в очереди гофроагрегата",
                        Path="IN_CORRUGATOR_QUEUE",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },

                    new DataGridHelperColumn
                    {
                        Header="DT_EXIT",
                        Description = "Дата выхода продукции со станка ?",
                        Path="DT_EXIT",
                        ColumnType=ColumnTypeRef.String,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="DATETS",
                        Description = "Дата отгрузки ?",
                        Path="DATETS",
                        ColumnType=ColumnTypeRef.String,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Альт. схема",
                        Description = "Флаг возможности переноса производства данной продукции по данной заявке на альтернативную линию производства",
                        Path="SECOND_SCHEME_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Самовывоз",
                        Description = "Флаг того, что отгрузка с самовывозом",
                        Path="SELFSHIP_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Мастер",
                        Description = "Флаг того, что задание нужно делать под присмотром мастера",
                        Path="MASTER_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="На горячую",
                        Description = "Флаг того, что задание нужно ехать на горячую",
                        Path="HOT_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Примечание",
                        Description = "Примечание по заданию",
                        Path="NOTE",
                        ColumnType=ColumnTypeRef.String,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Брекер",
                        Description = "Флаг того, что используется Брекер",
                        Path="BRAKER_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Опытная партия",
                        Description = "Флаг того, что это опытная партия",
                        Path="SAMPLE_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Лаборатория",
                        Description = "Флаг того, что нужно отправить образцы в лабораторию",
                        Path="TESTING_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус штанцформы",
                        Description = "2 - есть на заводе, 1 - нет на заводе, 0 - не требуется",
                        Path="SHTANZ_STATUS",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус клише",
                        Description = "1 - клише отсутствует, 0 - клише присутствует",
                        Path="KLISHE_STATUS",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Обвязчик",
                        Description = "Признак того, будем ли мы обвязывать лентами ИСВ",
                        Path="MODULEX_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Двойной слоттор",
                        Description = "Признак того, можно ли проехать на двойном слотторе",
                        Path="TWICE_SLOTTOR_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ограничение",
                        Description = "Ограничение количества. 0 без ограничения, 1 не менее, 2 не более, 3 точное количество",
                        Path="STRICLE_STATUS",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Эталонный образец",
                        Description = "Присутствие эталлонного образца 1 - Да, 0 - Нет",
                        Path="REFERENCE_SAMPLE_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Транспорт",
                        Description = "Признак того, что машина приехала под отгрузку. 1 - Да, 0 - Нет",
                        Path="CAR_ARRIVAL_FLAG",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Цветов",
                        Description = "Количество используемых цветов печати",
                        Path="COLOR_QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Образцов",
                        Description = "Количество образцов, которое нужно сохранить",
                        Path="SAMPLE_GET_QUANTITY",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="MAXDT",
                        Description = "MAXDT",
                        Path="MAXDT",
                        ColumnType=ColumnTypeRef.String,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид заготовки",
                        Description = "Идентификатор заготовки",
                        Path="BLANK_PRODUCT_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид ПЗ ГА",
                        Description = "Идентификатор производственного задания на гофроагрегат",
                        Path="BLANK_PRODUCTION_TASK_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Ид позиции заявки",
                        Description = "Идентификатор позиции заявки",
                        Path="ORDER_POSITION_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=1,
                        Visible=false,
                    },
                };
                Grid.SetColumns(columns);
                Grid.SetPrimaryKey("_ROWNUMBER");
                Grid.EnableSortingGrid = false;
                Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                Grid.AutoUpdateInterval = 0;
                Grid.ItemsAutoUpdate = false;
                Grid.UseProgressSplashAuto = false;
                Grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            // указана дата отгрузки
                            if(!string.IsNullOrEmpty(row.CheckGet("DATETS")))
                            {
                                // указана дата выхода продукции
                                if (!string.IsNullOrEmpty(row.CheckGet("DT_EXIT")))
                                {
                                    var dateTs = row.CheckGet("DATETS").ToDateTime("dd.MM.yyyy HH:mm:ss");
                                    // до отгрузки осталось не более 12 часов
                                    if (dateTs < DateTime.Now.AddHours(12))
                                    {
                                        var d = dateTs - row.CheckGet("DT_EXIT").ToDateTime("dd.MM.yyyy HH:mm:ss");
                                        if (d.TotalHours < -2)
                                        {
                                            color = HColor.Red;
                                        }
                                        else if (d.TotalHours < 0)
                                        {
                                            color = HColor.Pink;
                                        }
                                    }
                                }

                                if (row.CheckGet("MAXDT").ToDateTime("dd.MM.yyyy HH:mm:ss") > DateTime.Now)
                                {
                                    color = HColor.Blue;
                                }
                            }


                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                    
                    // определение выделения црифта строк
                    {
                        StylerTypeRef.FontWeight,
                        row =>
                        {
                            var fontWeight= new FontWeight();
                            fontWeight=FontWeights.Normal;

                            // Если это та же продукция, что сейчас стоит первой с очередени выполнения ПЗ на станке
                            if(row.CheckGet("BLANK_PRODUCT_ID").ToInt() == Form.GetValueByPath("CURRENT_PRODUCT_ID").ToInt())
                            {
                                fontWeight=FontWeights.Black;    //Black     //Bold
                            }

                            // Если это та же продукция, что сейчас стоит второй с очередени выполнения ПЗ на станке
                            if(row.CheckGet("BLANK_PRODUCT_ID").ToInt() == Form.GetValueByPath("NEXT_PRODUCT_ID").ToInt())
                            {
                                fontWeight=FontWeights.DemiBold;   //Bold   //DemiBold
                            }

                            return fontWeight;
                        }
                    },
                };
                // контекстное меню
                Grid.Menu = new Dictionary<string, DataGridContextMenuItem>()
                {
                    {
                        "ShowTechnologicalMap",
                        new DataGridContextMenuItem()
                        {
                            Header="Техкарта",
                            ToolTip = "Открыть техкарту по выбранному заданию",
                            Action=()=>
                            {
                                ShowTechnologicalMap(Grid.SelectedItem);
                            }
                        }
                    },
                    {
                        "ShowPalletByTask",
                        new DataGridContextMenuItem()
                        {
                            Header="Поддоны",
                            ToolTip = "Список поддонов по выбранному заданию",
                            Action=()=>
                            {
                                ShowPalletByTask(Grid.SelectedItem);
                            }
                        }
                    },
                    {
                        "ShowNoteByTask",
                        new DataGridContextMenuItem()
                        {
                            Header="Примечание",
                            ToolTip = "Посмотреть примечание по выбранному заданию",
                            Action=()=>
                            {
                                ShowNoteByTask(Grid.SelectedItem);
                            }
                        }
                    },
                    {
                        "SubProductOrder",
                        new DataGridContextMenuItem()
                        {
                            Header="Перестил",
                            ToolTip = "Заказать перестил",
                            Action=()=>
                            {
                                SubProductOrder();
                            }
                        }
                    },
                    {
                        "ChangeProductionTaskMachine",
                        new DataGridContextMenuItem()
                        {
                            Header = "Сменить станок",
                            ToolTip = "Сменить схему производства по выбранному заданию",
                            Action = () =>
                            {
                                ChangeProductionTaskMachine(Grid.SelectedItem);
                            }
                        }
                    },
                };
                Grid.Init();
                Grid.Run();
            }
        }

        private void GridLoadItems()
        {
            if (GridDataSet != null && GridDataSet.Items != null && GridDataSet.Items.Count > 0)
            {
                Grid.UpdateItems(GridDataSet);
            }
            else
            {
                Grid.UpdateItems(new ListDataSet());
            }
        }

        private void BarChartInit()
        {
            BarChart = new BarChart(MachineId, MachineName);
        }

        private void BarChartLoadItems()
        {
            if (BarChartData != null && BarChartData.Count > 0)
            {
                BarChart.SetValues(
                    BarChartData.CheckGet("HEIGHT").ToDouble(),
                    $"{BarChartData.CheckGet("WORKLOAD").ToDouble().ToString("#0")} %", 
                    $"{BarChartData.CheckGet("HOUR").ToDouble().ToString("#0.0")} ч.", 
                    BarChartData.CheckGet("WORKLOAD2").ToDouble().ToString("#0.0"),
                    BarChartData.CheckGet("COLOR"));
            }
            else
            {
                BarChart.SetValues(0, "0", "0.0", "0.0");
            }
        }

        private void SpeedBarLoadItems()
        {
            if (FormData != null && FormData.Count > 0)
            {
                MachineSpeedBar.SetCurrentValue(Form.GetValueByPath("CURRENT_SPEED").ToDouble(), Form.GetValueByPath("PLAN_SPEED").ToDouble());
            }
            else
            {
                MachineSpeedBar.SetCurrentValue(0, 0);
            }
        }

        /// <summary>
        /// Установка значенией из FormData в форму и GridDataSet в грид
        /// </summary>
        public void LoadItems()
        {
            FormLoadItems();
            SpeedBarLoadItems();
            GridLoadItems();
            BarChartLoadItems();

            // При установке данных через FormHelper слетают ToolTip.
            SetToolTipList();
        }

        /// <summary>
        /// Устанавливаем ToolTip контроллам
        /// </summary>
        private void SetToolTipList()
        {
            MachineIdTextBox.ToolTip = "Идентификатор станка";
            DefectPercentageTextBox.ToolTip = "Процент брака";
            MachineDataTextBox.ToolTip = "Проехали / средняя скорость";
            MachineNameTextBox.ToolTip = $"" +
                $"Красный - Технический простой" +
                $"{Environment.NewLine}Зелёный - Едет" +
                $"{Environment.NewLine}Синий - Простой" +
                $"{Environment.NewLine}Желтый - ППР" +
                $"{Environment.NewLine}Чёрный - Нет данных";
            MachineSpeedTextBox.ToolTip = "Текущая скорость / плановая скорость";
        }

        /// <summary>
        /// Возвращяет цвет для выделения станка в зависимости от текущего статуса станка
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        private string GetColorByMachineStatus(int status)
        {
            string color = HColor.BlackFG;

            switch (status)
            {
                // Технический простой
                case 1:
                    color = HColor.RedFG;
                    break;

                // Едет
                case 2:
                    color = HColor.GreenFG;
                    break;

                // Простой
                case 3:
                    color = HColor.BlueFG;
                    break;

                // ХЗ (ППР?)
                case 4:
                    color = HColor.YellowOrange;
                    break;

                // Нет данных
                case 0:
                default:
                    color = HColor.BlackFG;
                    break;
            }

            return color;
        }

        private void SetMachineStatus(Dictionary<string, string> formData)
        {
            MachineStatusStackPanel.Children.Clear();

            if (formData != null  && formData.Count > 0)
            {
                var machineInstanceData = formData.CheckGet("MACHINE_INSTANCE").Split(';');
                if (machineInstanceData != null && machineInstanceData.Length > 0)
                {
                    foreach (var machineInstance in machineInstanceData)
                    {
                        var machineData = machineInstance.Split(':');
                        if (machineData != null && machineData.Length >= 3)
                        {
                            Border border = new Border();
                            border.VerticalAlignment = VerticalAlignment.Center;
                            border.Width = 10;
                            border.Height = 15;
                            border.Margin = new Thickness(3, 0, 0, 0);
                            border.BorderBrush = HColor.BlackFG.ToBrush();
                            border.BorderThickness = new Thickness(2);
                            border.Background = GetColorByMachineStatus(machineData[2].ToInt()).ToBrush();

                            string statusName = "";
                            switch (machineData[2].ToInt())
                            {
                                // Технический простой
                                case 1:
                                    statusName = "Технический простой";
                                    break;

                                // Едет
                                case 2:
                                    statusName = "Едет";
                                    break;

                                // Простой
                                case 3:
                                    statusName = "Простой";
                                    break;

                                // ХЗ (ППР?)
                                case 4:
                                    statusName = "ППР";
                                    break;

                                // Нет данных
                                case 0:
                                default:
                                    statusName = "Нет данных";
                                    break;
                            }
                            border.ToolTip = $"{machineData[0]} {machineData[1]} {statusName}";

                            MachineStatusStackPanel.Children.Add(border);
                        }
                    }
                }
            }
        }

        private void ChangeProductionTaskMachine(Dictionary<string, string> selectedItem)
        {
            if (selectedItem.CheckGet("SECOND_SCHEME_FLAG").ToInt() > 0)
            {
                var i = new ChangeMachine();
                i.RoleName = this.RoleName;
                i.ParentFrame = this.ParentFrame;
                i.FactoryId = this.FactoryId;
                i.ProductName = selectedItem.CheckGet("PRODUCT_NAME");
                i.OrderPositionId = selectedItem.CheckGet("ORDER_POSITION_ID").ToInt();
                i.BlankProductionTaskId = selectedItem.CheckGet("BLANK_PRODUCTION_TASK_ID").ToInt();
                i.BlankProductId = selectedItem.CheckGet("BLANK_PRODUCT_ID").ToInt();
                i.Show();
            }
            else
            {
                var msg = "Нет альтернативных схем производства";
                var d = new DialogWindow(msg, this.ControlName, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void ShowTechnologicalMap(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null && selectedItem.Count > 0)
            {
                var p = new Dictionary<string, string>();
                p.Add("BLANK_ID", selectedItem.CheckGet("BLANK_PRODUCT_ID"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production/Pillory");
                q.Request.SetParam("Object", "ProductionTask");
                q.Request.SetParam("Action", "GetTechnologicalMap");

                q.Request.SetParams(p);

                q.Request.Timeout = 10000;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    string technologicalMapPath = "";
                    int technologicalMapPage = 0;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            technologicalMapPath = ds.Items[0].CheckGet("TECHNOLOGICAL_MAP_PATH");
                            technologicalMapPage = ds.Items[0].CheckGet("TECHNOLOGICAL_MAP_PAGE").ToInt();
                        }
                    }

                    if (!string.IsNullOrEmpty(technologicalMapPath) && technologicalMapPage > 0)
                    {
                        if (System.IO.File.Exists(technologicalMapPath))
                        {
                            Central.OpenFile(technologicalMapPath);
                        }
                        else
                        {
                            var msg = $"Файл {technologicalMapPath} не найден по указанному пути";
                            var d = new DialogWindow($"{msg}", this.ControlName, "", DialogWindowButtons.OK);
                            d.ShowDialog();
                        }
                    }
                    else
                    {
                        var msg = "Не найден путь к Excel файлу тех карты";
                        var d = new DialogWindow($"{msg}", this.ControlName, "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }
            else 
            {
                var msg = "Не выбрана позиция для открытия тех карты";
                var d = new DialogWindow(msg, this.ControlName, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void ShowPalletByTask(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null && selectedItem.Count > 0)
            {
                var i = new Client.Interfaces.Production.Pillory.PalletListByTask();
                i.RoleName = this.RoleName;
                i.ParentFrame = this.FrameName;
                i.BlankProductId = selectedItem.CheckGet("BLANK_PRODUCT_ID").ToInt();
                i.MachineId = MachineId;
                i.OrderPositionId = selectedItem.CheckGet("ORDER_POSITION_ID").ToInt();
                i.BlankProductionTaskId = selectedItem.CheckGet("BLANK_PRODUCTION_TASK_ID").ToInt();
                i.Show();
            }
            else
            {
                var msg = "Не выбрана позиция для просмотра поддонов";
                var d = new DialogWindow(msg, this.ControlName, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void ShowNoteByTask(Dictionary<string, string> selectedItem)
        {
            if (selectedItem != null && selectedItem.Count > 0)
            {
                string note = selectedItem.CheckGet("NOTE");
                if (!string.IsNullOrEmpty(note))
                {
                    var d = new DialogWindow(note, this.ControlName, "Примечание по заданию", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    var msg = "По выбранному заданию не задано примечание";
                    var d = new DialogWindow(msg, this.ControlName, "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
            else
            {
                var msg = "Не выбрана позиция для просмотра примечания";
                var d = new DialogWindow(msg, this.ControlName, "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        private void SubProductOrder()
        {
            var i = new Client.Interfaces.Production.Pillory.SubProductList();
            i.RoleName = this.RoleName;
            i.ParentFrame = this.FrameName;
            i.FactoryId = this.FactoryId;
            i.MachineId = this.MachineId;
            i.Show();
        }
    }
}
