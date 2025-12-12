using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Управление отгрузками. Настройка
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class ShipmentSettings : UserControl
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="settingsType">
        /// Тип настроек, которые необходимо отобразить пользователю.
        /// 0 -- ShipmentSettings;
        /// 1 -- RollSettings;
        /// 2 -- ShipmentKshSettings.
        /// </param>
        public ShipmentSettings(int settingsType)
        {
            InitializeComponent();

            TabName = "ShipmentSettings";
            SettingsType = settingsType;
            InitForm();
        }

        public FormHelper Form { get; set; }
        /// <summary>
        /// Имя вкладки
        /// </summary>
        public string TabName;
        /// <summary>
        /// Имя вкладки, из которой вызвана форма, для передачи активности после закрытия
        /// </summary>
        public string ReceiverTabName;
        /// <summary>
        /// Тип настроек, которые необходимо отобразить пользователю.
        /// 0 -- ShipmentSettings;
        /// 1 -- RollSettings;
        /// 2 -- ShipmentKshSettings.
        /// </summary>
        public int SettingsType { get; set; }

        public void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>();

            // Найтройки для рулонов
            if (SettingsType == 1)
            {
                ShipmentSettingFields.Visibility = Visibility.Collapsed;
                RollSettingFields.Visibility = Visibility.Visible;
                ShipmentKshSettingFields.Visibility= Visibility.Collapsed;

                //список колонок формы
                fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="MAX_LENGTH_GLUED",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=MaxLengthGlued,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="MIN_LENGTH_ROLL_BEFORE_GLUED",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=MinLengthRollBeforeGlued,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="MIN_LENGTH_ROLL_AFTER_GROUP",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=MinLengthRollAfterGroup,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="MIN_LENGTH_WITH_LAST_ROLL",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=MinLengthWithLastRoll,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="PAPER_LEFTOVER",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=PaperLeftover,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="PAPER_LEFTOVER_ALLOWABLE",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=PaperLeftoverAllowable,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="ROLL_CALCULATION_ERROR_1",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=RollCalculationError1,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="ROLL_CALCULATION_ERROR_2",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=RollCalculationError2,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="ROLL_CALCULATION_ERROR_3",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=RollCalculationError3,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="BLOCK_SELECTION_RANGE_1",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=BlockSelectionRange1,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="BLOCK_SELECTION_RANGE_3",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=BlockSelectionRange3,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="ROLL_STORAGE_PERIOD_MAX",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=RollStoragePeriodMax,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="ROLL_STORAGE_PERIOD_MIN",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=RollStoragePeriodMin,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                };
            }
            // Настройки для отгрузок
            else if (SettingsType == 0)
            {
                ShipmentSettingFields.Visibility = Visibility.Visible;
                RollSettingFields.Visibility = Visibility.Collapsed;
                ShipmentKshSettingFields.Visibility = Visibility.Collapsed;

                //список колонок формы
                fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="ALLOW_PLACING_PALLET_FLAG",
                        FieldType=FormHelperField.FieldTypeRef.Boolean,
                        Control=AllowPalletPlacing,
                        ControlType="CheckBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="BLOCK_TOGETHER_BLANK_GP",
                        FieldType=FormHelperField.FieldTypeRef.Boolean,
                        Control=BanBlankAndGoodsToOneCellCheckBox,
                        ControlType="CheckBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="BAN_STANDARD_PALLET_TO_NONSTANDARD_CELL",
                        FieldType=FormHelperField.FieldTypeRef.Boolean,
                        Control=BanStandardPalletToNonStandardCellCheckBox,
                        ControlType="CheckBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="HOUR_BEFORE_SHIP",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=HourBeforeShipment,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="BLANK_BUFFER_SIZE",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=BufferSizeFirst,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="BLANK_BUFFER_SIZE_2",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=BufferSizeSecond,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="SGP_SIZE",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=StockSize,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="ROLL_STOCK_SIZE",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=RollStockSize,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="DIFF_CAR_LENGTH",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=DifCarLength,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="DIFF_CAR_WIDTH",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=DifCarWidth,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="DIFF_CAR_HEIGHT",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=DifCarHeight,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="COUNT_POSITION_ALG_PLACE_PALLET",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=CountPositionAlgPlacePallet,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="COUNT_PALLET_NOT_EXISTS_WAREHOUSE",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=CountPalletNotExists,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="SGP_QTY_MINUTE_DIFF_COND",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=SgpQtyMinuteDiffCond,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="IN_RACK_SHIPMENT_HOUR",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=InRackShipmentHour,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="BAN_IN_RACK_SHIPMENT_HOUR",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=BanInRackShipmentHour,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },

                    new FormHelperField()
                    {
                        Path="MAX_LENGTH_PALLET_IN_RACK",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=MaxLengthPalletInRack,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="MAX_WIDTH_PALLET_IN_RACK",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=MaxWidthPalletInRack,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },

                    new FormHelperField()
                    {
                        Path="MAX_LENGTH_STANDART_PALLET",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=MaxLengthStandartPallet,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="MAX_WIDTH_STANDART_PALLET",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=MaxWidthStandartPallet,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                };
            }
            // Настройки для отгрузок Кашира
            else if (SettingsType == 2)
            {
                ShipmentSettingFields.Visibility = Visibility.Collapsed;
                RollSettingFields.Visibility = Visibility.Collapsed;
                ShipmentKshSettingFields.Visibility = Visibility.Visible;

                //список колонок формы
                fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="BLANK_BUFFER_SIZE_KSH",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=BufferSizeKsh,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="SGP_SIZE_KSH",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=StockSizeKsh,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField()
                    {
                        Path="SGP_SIZE_CNT_KSH",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=StockSizeCntKsh,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                };
            }       

            Form.SetFields(fields);
            Form.OnValidate = (bool valid, string message) =>
            {
                if (valid)
                {
                    FormStatus.Text = "";
                }
                else
                {
                    FormStatus.Text = "Не все поля заполнены верно";
                }
            };
        }

        public void Edit()
        {
            GetData();
        }

        public void Save()
        {
            if (Form.Validate())
            {
                var formValues = Form.GetValues();
                Dictionary<string, string> param = new Dictionary<string, string>();
                param.Add("PARAMETER_LIST", JsonConvert.SerializeObject(formValues));
                SaveData(param);
            }
        }

        public async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Settings");
            q.Request.SetParam("Action", "Get");

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (result.ContainsKey("Items"))
                    {
                        var formDS = (ListDataSet)result["Items"];
                        formDS?.Init();

                        var p = new Dictionary<string, string>();
                        if (formDS.Items.Count > 0)
                        {
                            foreach (Dictionary<string, string> i in formDS.Items)
                            {
                                var k = i.CheckGet("NAME");
                                var v = i.CheckGet("VALUE");
                                p.CheckAdd(k, v);
                            }
                        }

                        Form.SetValues(p);
                    }

                    Show();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public async void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Shipments");
            q.Request.SetParam("Object", "Settings");
            q.Request.SetParam("Action", "Save");

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
                    if (result.ContainsKey("Items"))
                    {
                        var formDS = result["Items"];
                        formDS?.Init();
                        var operationResult = formDS.GetFirstItemValueByKey("RESULT").ToInt();

                        if (operationResult == 1)
                        {
                            //отправляем сообщение о закрытии окна
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "Main",
                                ReceiverName = "",
                                SenderName = "SettingsView",
                                Action = "UpdateStatus",
                            });

                            Close();
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        public void Show()
        {
            if (SettingsType == 1)
            {
                Central.WM.AddTab(TabName, "Настройки управления рулонами", true, "add", this);
            }
            else if (SettingsType == 0) 
            {
                Central.WM.AddTab(TabName, "Настройки управления отгрузками", true, "add", this);
            }
            else if (SettingsType == 2)
            {
                Central.WM.AddTab(TabName, "Настройки управления отгрузками КШ", true, "add", this);
            }
        }

        /// <summary>
        /// Закрытие вкладки редактирования образца
        /// </summary>
        private void Close()
        {
            Central.WM.RemoveTab(TabName);
            if (!ReceiverTabName.IsNullOrEmpty())
            {
                Central.WM.SetActive(ReceiverTabName);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
