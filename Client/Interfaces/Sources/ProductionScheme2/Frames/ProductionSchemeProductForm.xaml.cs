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

namespace Client.Interfaces.Sources.ProductionScheme2
{
    /// <summary>
    /// форма редактирования станка
    /// </summary>
    /// <author>motenko_ek</author>
    public partial class ProductionSchemeProductForm : ControlBase
    {
        public ProductionSchemeProductForm(ItemMessage message, Dictionary<string, string> parent, int id = 0)
        {
            InitializeComponent();

            Message = message;
            Parent = parent;
            Id = id;

            if(id > 0) PrscIdSelectBox.IsEnabled = false;

            DocumentationUrl = "/doc/l-pack-erp/sources/production_scheme/production_scheme_product_form";
            FrameMode = 0;
            FrameTitle = id > 0 ? $"Схема продукт #{id}" : $"Добавить схему в {Parent.CheckGet("NAME")}";
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
                    Path="PRSC_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PrscIdSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRIMARY_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=PrimaryFlagCheckBox,
                    ControlType="CheckBox",
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
                Title = "Добавить",
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

        private Dictionary<string, string> Parent;
        private readonly int Id;
        private ItemMessage Message;

        /// <summary>
        /// процессор форм
        /// </summary>
        private FormHelper Form { get; set; }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        private async void FillData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sources/ProductionScheme2");
            q.Request.SetParam("Object", "ProductionSchemeProduct");
            q.Request.SetParam("Action", "GetRecord");
            q.Request.SetParam("ID", Id.ToString());
            q.Request.SetParam("ID2", Parent.CheckGet("ID2"));

            Form.SetBusy(true);

            q.DoQuery();

            Form.SetBusy(false);

            if (q.Answer.Status != 0)
            {
                q.ProcessError();
                Close();
                return;
            }

            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
            if (result == null)
            {
                DialogWindow.ShowDialog("Неверный ответ", "Получение данных с сервера", "");
                Close();
                return;
            }

            var ds = ListDataSet.Create(result, "PRSC_ID");

            if (ds.Items.Count == 0)
            {
                DialogWindow.ShowDialog("Невозможно добавить", "Добавление схемы", "");
                Close();
                return;
            }

            PrscIdSelectBox.SetItems(ds, "PRSC_ID", "NAME");

            Form.SetValues(ListDataSet.Create(result, "ITEMS"));
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

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sources/ProductionScheme2");
            q.Request.SetParam("Object", "ProductionSchemeProduct");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParam("ID", Id.ToString());
            q.Request.SetParams(Form.GetValues());
            q.Request.SetParam("ID2", Parent.CheckGet("ID2"));

            Form.SetBusy(true);

            q.DoQuery();

            Form.SetBusy(false);

            if (q.Answer.Status != 0)
            {
                q.ProcessError();
                return;
            }

            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
            if (result == null)
            {
                DialogWindow.ShowDialog("Неверный ответ сервера", "Добавление станка", "");
                return;
            }

            var id = ListDataSet.Create(result, "ITEMS").GetFirstItemValueByKey("PRSP_ID").ToInt();

            Message.SenderName = ControlName;
            Message.Message = $"{id}";
            Central.Msg.SendMessage(Message);
            Close();
        }
    }
}
