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
    /// форма редактирования
    /// </summary>
    /// <author>motenko_ek</author>
    public partial class ProductionSchemeForm : ControlBase
    {
        public ProductionSchemeForm(int id = 0)
        {
            InitializeComponent();

            Id = id;

            RoleName = "[erp]production_catalog";
            DocumentationUrl = "/doc/l-pack-erp/production/production_catalog/production_scheme";
            FrameMode = 0;
            FrameTitle = id > 0 ? $"Схема #{id}" : "Новая схема";
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
                    Path="ARCHIVE_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=ArchiveFlagCheckBox,
                    ControlType="CheckBox",
                },
                new FormHelperField()
                {
                    Path="CONTAINER_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=ContainerFlagCheckBox,
                    ControlType="CheckBox",
                },
                new FormHelperField()
                {
                    Path="COREBOARD_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=CoreboardFlagCheckBox,
                    ControlType="CheckBox",
                },
                new FormHelperField()
                {
                    Path="CORRUGATED_CARDBOARD_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=CorrugatedCardboardFlagCheckBox,
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
            if (Id == 0) return;

            Form.SetBusy(true);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionCatalog");
            q.Request.SetParam("Object", "ProductionScheme");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("PRSC_ID", Id.ToString());

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
                    Form.SetValues(ds);
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
            q.Request.SetParam("Object", "ProductionScheme");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParams(Form.GetValues());
            q.Request.SetParam("PRSC_ID", Id.ToString());

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
                    var id = ds.GetFirstItemValueByKey("PRSC_ID").ToInt();

                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverName = "ProductionCatalogSchemeTab",
                        SenderName = "ProductionSchemeForm",
                        Action = "production_catalog_scheme_refresh",
                        Message = $"{id}",
                    });
                    Close();
                }
                else
                {
                    DialogWindow.ShowDialog("Неверный ответ сервера", "Добавление", "");
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
