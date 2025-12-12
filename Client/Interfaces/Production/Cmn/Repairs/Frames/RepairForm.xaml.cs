using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.XtraReports.Native;
using DevExpress.XtraReports.UI;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Interfaces.Main.SelectBox;

namespace Client.Interfaces.Production.Cmn.Repairs
{
    /// <summary>
    /// форма редактирования станка
    /// </summary>
    /// <author>motenko_ek</author>
    public partial class RepairForm : ControlBase
    {
        public RepairForm(ItemMessage message, int factId, int id = 0)
        {
            InitializeComponent();

            Message = message;
            FactId = factId;
            Id = id;

            //if (ProdType == 1)
            //{
            //    IdUnitLabel.Visibility = Visibility.Collapsed;
            //    IdUnitValue.Visibility = Visibility.Collapsed;
            //}

            IdStSelectBox.IsEnabled = Id == 0;
            IdStSelectBox.SelectedItemChanged += (DependencyObject d, DependencyPropertyChangedEventArgs e) => {
                FillIdUnit();
            };

            StatusSelectBox.SetItems(new Dictionary<string, string>()
            {
                {"0",  "Создан"},
                {"10",  "Выполняется"},
                {"20",  "Закончен"},
            });
            StatusSelectBox.SelectedItem = StatusSelectBox.Items.First();

            DocumentationUrl = "/doc/l-pack-erp/production/repair_form";
            FrameMode = 0;
            FrameTitle = id > 0 ? $"Простой #{id}" : "Новый простой";
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
                Get();
            };

            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
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
                    Path="ID_UNIT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=IdUnitSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="START_DTTM",
                    Control=StartDttmPicker,
                    //Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    //    { FormHelperField.FieldFilterRef.Required, null },
                    //},
                },
                new FormHelperField()
                {
                    Path="FINISH_DTTM",
                    Control=FinishDttmPicker,
                },
                new FormHelperField()
                {
                    Path="TASK",
                    Control=TaskTextBox,
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    Control=NoteTextBox,
                },
                new FormHelperField()
                {
                    Path="MEH",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=MehCheckBox,
                    ControlType="CheckBox",
                },
                new FormHelperField()
                {
                    Path="EL",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=ElCheckBox,
                    ControlType="CheckBox",
                },
                new FormHelperField()
                {
                    Path="TEH",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=TehCheckBox,
                    ControlType="CheckBox",
                },
                new FormHelperField()
                {
                    Path="LONG_TIME",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=LongTimeCheckBox,
                    ControlType="CheckBox",
                },
                new FormHelperField()
                {
                    Path="REPAIR_STATUS",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=StatusSelectBox,
                    ControlType="SelectBox",
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

            Show();
        }

        private ItemMessage Message;
        private readonly int FactId;
        private int Id = 0;
        private List<Dictionary<string, string>> IdUnit;

        /// <summary>
        /// процессор форм
        /// </summary>
        private FormHelper Form { get; set; }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        private async void Get()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProdRepairs");
            q.Request.SetParam("Object", "Repairs");
            q.Request.SetParam("Action", "GetRecord");
            q.Request.SetParam("RESC_ID", Id.ToString());
            q.Request.SetParam("FACT_ID", FactId.ToString());

            Form.SetBusy(true);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

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

            IdUnit = ListDataSet.Create(result, "ID_UNIT").Items;
            IdStSelectBox.SetItems(ListDataSet.Create(result, "ID_ST"), "ID_ST", "NAME");

            Form.SetValues(ListDataSet.Create(result, "ITEMS"));
            if (Id == 0)
            {
                StartDttmPicker.EditValue = DateTime.Now;
                //ToDtPicker.EditValue = DateTime.Now + TimeSpan.FromHours(1);
            }

        }
        private void FillIdUnit()
        {
            IdUnitSelectBox.Clear();
            var id = IdStSelectBox.SelectedItem.Key.ToInt();
            IdUnitSelectBox.SetItems(IdUnit
                .Where(g => id == g["ID_ST"].ToInt())
                .ToDictionary(g => g["ID_UNIT"], g => g["NAME_UNIT"]));
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
            if(FinishDttmPicker.EditValue != null
                && (DateTime)StartDttmPicker.EditValue > (DateTime)FinishDttmPicker.EditValue)
            {
                var bc = new BrushConverter();
                FinishDttmPicker.BorderBrush = (Brush)bc.ConvertFrom("#ffee0000");

                var errorMessage = "Время окончания не может быть меньше начала";
                FinishDttmPicker.ToolTip = errorMessage;
                Form.SetStatus(errorMessage, 1);
                return;
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProdRepairs");
            q.Request.SetParam("Object", "Repairs");
            q.Request.SetParam("Action", "SaveRecord");
            q.Request.SetParams(Form.GetValues());
            q.Request.SetParam("RESC_ID", Id.ToString());

            Form.SetBusy(true);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            Form.SetBusy(false);

            if (q.Answer.Status != 0)
            {
                q.ProcessError();
                return;
            }

            var id = JsonConvert.DeserializeObject<int>(q.Answer.Data);

            Message.SenderName = "RepairForm";
            Message.Message = $"{id}";
            Central.Msg.SendMessage(Message);
            Close();
        }
    }
}
