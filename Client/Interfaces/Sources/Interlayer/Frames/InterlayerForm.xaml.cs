using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.DirectX.StandardInterop.Direct2D;
using DevExpress.Xpf.Core.Internal;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Crypto;
using SharpVectors.Dom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Sources
{
    /// <summary>
    /// Форма редактирования перестила
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class InterlayerForm : ControlBase
    {
        public InterlayerForm()
        {
            ControlTitle = "Редактирование перестила";
            DocumentationUrl = "/doc/l-pack-erp-new/products_materials/interlayer";
            RoleName = "[erp]interlayer";

            InitializeComponent();

            FormInit();
            FrameName = "InterlayerForm";
            FrameMode = 1;
            OnGetFrameTitle = () =>
            {
                var result = "";
                var id2 = Id2.ToInt();
                result = $"Изменение перестила #{id2}";
                return result;
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
                MinRemainderTextBox.Focus();
            };
            Commander.SetCurrentGridName("main");
            Commander.SetCurrentGroup("item");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Сохранить",
                    Description = "Сохранить",
                    ButtonUse = true,
                    ButtonName = "SaveButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Save();

                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "close",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Отмена",
                    Description = "Закрыть форму без сохранения",
                    ButtonUse = true,
                    ButtonName = "CancelButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Close();
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "help",
                    Enabled = true,
                    Title = "Справка",
                    Description = "Показать справочную информацию",
                    ButtonUse = true,
                    ButtonName = "HelpButton",
                    AccessLevel = Role.AccessMode.ReadOnly,
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
            }


            Commander.Init(this);

        }
        public void Init()
        {
            Form.SetValues(Values);
        }
        public int Id2 { get; set; }
        
        public Dictionary<string, string> Values { get; set; }
        public FormHelper Form { get; set; }

        private void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NameTextBox,
                    ControlType="TextBox",
                    Enabled=false,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 32 },
                    },
                },
                new FormHelperField()
                {
                    Path="ARTIKUL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ArtikulTextBox,
                    ControlType="TextBox",
                    Enabled=false,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 20 },
                    },
                },
                new FormHelperField()
                {
                    Path="MIN_REMAINDER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    First=true,
                    Control=MinRemainderTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 10 },
                    },
                },
                new FormHelperField()
                {
                    Path="TASK_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    First=true,
                    Control=TaskQtyTextBox,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 6 },
                    },
                },
                new FormHelperField()
                {
                    Path="MAX_REMAINDER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=MaxRemainderTextBox,
                    ControlType="TextBox",
                    Enabled=false,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 10 },
                    },
                },
                new FormHelperField()
                {
                    Path="PLANNED_PRODUCTION_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PlannedProductTextBox,
                    ControlType="TextBox",
                    Enabled=false,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.DigitOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 10 },
                    },
                },
                new FormHelperField()
                {
                    Path="SIGNODE_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=SignodeFlag,
                    ControlType="CheckBox",
                    Enabled=false,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="RECYCLING_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=RecyclingFlag,
                    ControlType="CheckBox",
                    Enabled=false,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);
        }
        private void Save()
        {
            var p = new Dictionary<string, string>();

            var v = Form.GetValues();

            int min = v.CheckGet("MIN_REMAINDER").ToInt();
            int taskQty = v.CheckGet("TASK_QTY").ToInt();
            p.CheckAdd("ID2", Id2.ToString());
            p.CheckAdd("MIN", min.ToString());
            p.CheckAdd("QTY", taskQty.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sources/Interlayer");
            q.Request.SetParam("Object", "Interlayer");
            q.Request.SetParam("Action", "UpdateMinReminder");
            q.Request.SetParams(p);

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverName = "InterlayerTab",
                    SenderName = ControlName,
                    Action = "RefreshGrid",
                    Message = "",
                });
                Close();
            }
            else {
                q.ProcessError();
            }
        }
        private void HelpButtonClick(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp-new/products_materials/interlayer");
        }
    }
}
