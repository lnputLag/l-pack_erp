using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.XtraReports.Native;
using DevExpress.XtraReports.UI;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Interfaces.Main.SelectBox;

namespace Client.Interfaces.ProductionCatalog
{
    /// <summary>
    /// форма редактирования станка
    /// </summary>
    /// <author>motenko_ek</author>
    public partial class ProductionMachineForm : ControlBase
    {
        public ProductionMachineForm(int id = 0)
        {
            InitializeComponent();

            Id = id;

            RoleName = "[erp]production_catalog";
            DocumentationUrl = "/doc/l-pack-erp/production/production_catalog/production_machine";
            FrameMode = 0;
            FrameTitle = id > 0 ? $"Станок #{id}" : "Новый станок";
            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };
            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };
            OnLoad = () =>
            {
                FillData();
            };

            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NameTextBox,
                },
                new FormHelperField()
                {
                    Path="SHORT_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ShortNameTextBox,
                },
                new FormHelperField()
                {
                    Path="ID_ST",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=IdStSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRMG_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PrmgIdSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PROD_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProdIdSelectBox,
                    ControlType="SelectBox",
                },
                new FormHelperField()
                {
                    Path="COLOR_CNT_MAX",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ColorCntMaxTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_LENGTH_MIN",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductLengthMinTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_LENGTH_MAX",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductLengthMaxTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_WIDTH_MIN",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductWidthMinTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_WIDTH_MAX",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductWidthMaxTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_HEIGHT_MIN",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductHeightMinTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_HEIGHT_MAX",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductHeightMaxTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="BLANK_LENGTH_MIN",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=BlankLengthMinTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="BLANK_LENGTH_MAX",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=BlankLengthMaxTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="BLANK_WIDTH_MIN",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=BlankWidthMinTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="BLANK_WIDTH_MAX",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=BlankWidthMaxTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PUNCHING_DEPTH_MAX",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PunchingDepthMaxTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_LENGTH_WIDTH_MIN",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductLengthWidthMinTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_LENGTH_WIDTH_MAX",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductLengthWidthMaxTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_LENGTH_TRIM_MIN",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ProductLengthTrimMinTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="BLANK_WIDTH_VIAONE_MAX",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=BlankWidthViaoneMaxTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },
                new FormHelperField()
                {
                    Path="EFFICIENCY_PCT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=EfficiencyPctTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                    },
                },

            };
            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = StatusBar;
            Form.SetDefaults();

            Commander.Add(new CommandItem()
            {
                Name = "save",
                Enabled = true,
                Title = "Сохранить",
                Description = "",
                ButtonUse = true,
                ButtonName = "SaveButton",
                HotKey = "Ctrl+Return",
                Action = () =>
                {
                    Save();
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "cancel",
                Enabled = true,
                Title = "Отмена",
                Description = "",
                ButtonUse = true,
                ButtonName = "CancelButton",
                HotKey = "Escape",
                Action = () =>
                {
                    Close();
                },
            });
            Commander.Init(this);

            base.Show();
        }

        private int Id = 0;

        /// <summary>
        /// процессор форм
        /// </summary>
        private FormHelper Form { get; set; }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        private async void FillData()
        {
            Form.SetBusy(true);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionCatalog");
            q.Request.SetParam("Object", "ProductionMachine");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("PRMA_ID", Id.ToString());

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
                        var ds = ListDataSet.Create(result, "ID_ST");
                        IdStSelectBox.SetItems(ds, "ID_ST", "NAME");
                    }

                    {
                        var ds = ListDataSet.Create(result, "PRMG_ID");
                        PrmgIdSelectBox.SetItems(ds, "PRMG_ID", "NAME");
                    }

                    {
                        var ds = ListDataSet.Create(result, "PROD_ID");
                        ProdIdSelectBox.SetItems(ds, "PROD_ID", "PRODUCTION");
                    }

                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        Form.SetValues(ds);
                    }
                }
                else
                {
                    DialogWindow.ShowDialog("Неверный ответ", "Получение данных с сервера", "");
                }
            }
            else
            {
                q.ProcessError();
            }

            Form.SetBusy(false);
        }

        /// <summary>
        /// подготовка данных
        /// </summary>
        private async void Save()
        {
            if (!Form.Validate())
            {
                Form.SetStatus("Не все обязательные поля заполнены верно", 1);
                return;
            }

            Form.SetBusy(true);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionCatalog");
            q.Request.SetParam("Object", "ProductionMachine");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParams(Form.GetValues());
            q.Request.SetParam("PRMA_ID", Id.ToString());

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
                    var id = ds.GetFirstItemValueByKey("PRMA_ID").ToInt();

                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverName = "ProductionCatalogMachineTab",
                        SenderName = "ProductionMachineForm",
                        Action = "production_catalog_machine_refresh",
                        Message = $"{id}",
                    });
                    Close();
                }
                else
                {
                    DialogWindow.ShowDialog("Неверный ответ сервера", "Добавление станка", "");
                }
            }
            else
            {
                q.ProcessError();
            }

            Form.SetBusy(false);
        }
    }
}
