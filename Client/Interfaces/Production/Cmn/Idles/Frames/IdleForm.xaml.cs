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

namespace Client.Interfaces.Production.Cmn.Idles
{
    /// <summary>
    /// форма редактирования станка
    /// </summary>
    /// <author>motenko_ek</author>
    public partial class IdleForm : ControlBase
    {
        public IdleForm(ItemMessage message, int factId, int prodType, int id = 0)
        {
            InitializeComponent();

            Message = message;
            FactId = factId;
            ProdType = prodType;
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
            IdReasonSelectBox.SelectedItemChanged += (DependencyObject d, DependencyPropertyChangedEventArgs e) => {
                FillIdReasonDetail();
            };
            IdReasonDetailSelectBox.SelectedItemChanged += (DependencyObject d, DependencyPropertyChangedEventArgs e) => {
                if (CommentedFlag.Contains(IdReasonDetailSelectBox.SelectedItem.Key.ToInt()))
                {
                    ReasonLable.Content = (ReasonLable.Content as string).Remove((ReasonLable.Content as string).Length - 1, 1) + "*";
                }
                else
                {
                    ReasonLable.Content = (ReasonLable.Content as string).Remove((ReasonLable.Content as string).Length - 1, 1) + " ";
                }
            };

            DocumentationUrl = "/doc/l-pack-erp/production/idle_form";
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
                },
                new FormHelperField()
                {
                    Path="FROMDT",
                    Control=FromDtPicker,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="TODT",
                    Control=ToDtPicker,
                },
                new FormHelperField()
                {
                    Path="IDREASON",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=IdReasonSelectBox,
                    ControlType="SelectBox",
                },
                new FormHelperField()
                {
                    Path="ID_REASON_DETAIL",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=IdReasonDetailSelectBox,
                    ControlType="SelectBox",
                },
                new FormHelperField()
                {
                    Path="REASON",
                    Control=ReasonTextBox,
                },
                new FormHelperField()
                {
                    Path="DODT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=DodtIdSelectBox,
                    ControlType="SelectBox",
                },
                new FormHelperField()
                {
                    Path="MEASURES_TAKEN",
                    Control=MeasuresTakenTextBox,
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
        private readonly int ProdType;
        private int Id = 0;
        private List<Dictionary<string, string>> IdReasonDetail;
        private HashSet<int> CommentedFlag;
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
            q.Request.SetParam("Module", "ProdIdle");
            q.Request.SetParam("Object", "Idle");
            q.Request.SetParam("Action", "GetRecord");
            q.Request.SetParam("IDIDLES", Id.ToString());
            q.Request.SetParam("FACT_ID", FactId.ToString());
            q.Request.SetParam("PROD_TYPE", ProdType.ToString());

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

            IdReasonDetail = ListDataSet.Create(result, "ID_REASON_DETAIL").Items;
            CommentedFlag = IdReasonDetail
                .Where(g => g["COMMENTED_FLAG"].ToBool())
                .Select(g => g["ID_IDLE_DETAILS"].ToInt())
                .ToHashSet();
            IdReasonSelectBox.SetItems(ListDataSet.Create(result, "IDREASON"), "ID", "NAME");
            DodtIdSelectBox.SetItems(ListDataSet.Create(result, "DODT_ID"), "DODT_ID", "DEFECT_TYPE");

            IdUnit = ListDataSet.Create(result, "ID_UNIT").Items;
            IdStSelectBox.SetItems(ListDataSet.Create(result, "ID_ST"), "ID_ST", "NAME");

            Form.SetValues(ListDataSet.Create(result, "ITEMS"));
            if (Id == 0)
            {
                FromDtPicker.EditValue = DateTime.Now;
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
        private void FillIdReasonDetail()
        {
            IdReasonDetailSelectBox.Clear();
            var id = IdReasonSelectBox.SelectedItem.Key.ToInt();
            IdReasonDetailSelectBox.SetItems(IdReasonDetail
                .Where(g => id == g["ID_IDLE_REASON"].ToInt())
                .ToDictionary(g => g["ID_IDLE_DETAILS"], g => g["DESCRIPTION"]));
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
            if(ToDtPicker.EditValue != null
                && (DateTime)FromDtPicker.EditValue > (DateTime)ToDtPicker.EditValue)
            {
                var bc = new BrushConverter();
                ToDtPicker.BorderBrush = (Brush)bc.ConvertFrom("#ffee0000");

                var errorMessage = "Время окончания не может быть меньше начала";
                ToDtPicker.ToolTip = errorMessage;
                Form.SetStatus(errorMessage, 1);
                return;
            }
            if(ReasonTextBox.Text.IsNullOrEmpty()
                && CommentedFlag.Contains(IdReasonDetailSelectBox.SelectedItem.Key.ToInt()))
            {
                var bc = new BrushConverter();
                ReasonTextBox.BorderBrush = (Brush)bc.ConvertFrom("#ffee0000");

                var errorMessage = "Для этой причины необходимо описание";
                ReasonTextBox.ToolTip = errorMessage;
                Form.SetStatus(errorMessage, 1);
                return;
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProdIdle");
            q.Request.SetParam("Object", "Idle");
            q.Request.SetParam("Action", "SaveRecord");
            q.Request.SetParams(Form.GetValues());
            q.Request.SetParam("IDIDLES", Id.ToString());

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

            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
            if (result == null)
            {
                DialogWindow.ShowDialog("Неверный ответ сервера", "Добавление простоя", "");
                return;
            }

            var ds = ListDataSet.Create(result, "ITEMS");
            var id = ds.GetFirstItemValueByKey("IDIDLES").ToInt();

            Message.SenderName = "IdleForm";
            Message.Message = $"{id}";
            Central.Msg.SendMessage(Message);
            Close();
        }
    }
}
