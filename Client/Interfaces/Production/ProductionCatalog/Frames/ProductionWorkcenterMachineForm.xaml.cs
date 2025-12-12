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
    public partial class ProductionWorkcenterMachineForm : ControlBase
    {
        public ProductionWorkcenterMachineForm(Dictionary<string, string> parent)
        {
            InitializeComponent();

            Parent = parent;

            RoleName = "[erp]delivery_addresses";
            DocumentationUrl = "/doc/l-pack-erp/production/production_catalog/production_workcenter_machine";
            FrameMode = 0;
            FrameTitle = $"Добавить станок в {Parent.CheckGet("NAME")}";
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
                    Path="PRMA_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PrmaIdSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
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
            q.Request.SetParam("Action", "ListForWorkcenter");
            q.Request.SetParam("PRWO_ID", Parent.CheckGet("PRWO_ID"));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "PRMA_ID");
                    PrmaIdSelectBox.SetItems(ds, "PRMA_ID", "NAME");
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
            q.Request.SetParam("Object", "ProductionWorkcenterMachine");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParams(Form.GetValues());
            q.Request.SetParam("PRWO_ID", Parent.CheckGet("PRWO_ID"));

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
                    var id = ds.GetFirstItemValueByKey("PRWM_ID").ToInt();

                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverName = "ProductionCatalogWorkcenterTab",
                        SenderName = "ProductionWorkcenterMachineForm",
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
