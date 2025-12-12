using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Accounts;
using Client.Interfaces.Main;
using Client.Interfaces.Sales;
using DevExpress.DirectX.StandardInterop.Direct2D;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using SharpVectors.Dom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма редактирования контакта покупателя
    /// Страница веб-техкарты.
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class WebTechnologicalMapAddPokupatelContact : ControlBase
    {
        public WebTechnologicalMapAddPokupatelContact()
        {

            InitializeComponent();
            FrameMode = 1;
            FrameName = "WebTechnologicalMapAddPokupatelContact";
            OnGetFrameTitle = () =>
            {
                if (IdPoco > 0)
                {
                    return $"Контакты покупателя {IdPoco}";
                }
                else
                {
                    return $"Добавление контакта";
                }
            };
            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }

            };
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
                        SaveOrUpdate();

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
            }
            Commander.Init(this);
            OnLoad = () =>
            {
                FormInit();
                if (IdPoco > 0)
                {
                    LoadContact();
                }
                //LoadLastCallComment();
            };
        }

        public FormHelper Form { get; set; }
        public int IdPoco { get; set; }
        public int IdPok { get; set; }
        public int IdTk { get; set; }
        public string ReceiverName { get; set; }


        public void FormInit()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="POKUPATEL_CONTACT_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PokupatelName,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="POKUPATEL_PHONE_TB",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PhoneTextBox,
                    ControlType="TextEdit",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="EXTENSION_PHONE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ExtensionPhone,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="POKUPATEL_EMAIL",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PokupatelEmail,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CALL_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=CallFlagCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="POKUPATEL_NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PokupatelNote,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="POKUPATEL_PHONE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PokupatelPhone,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.ToolbarControl = null;
            Form.SetFields(fields);
        }

        /// <summary>
        /// Загрузка контактов покупателя
        /// </summary>
        public async void LoadContact()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "GetContactData");
            q.Request.SetParam("POCO_ID", IdPoco.ToString());
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
            }
            else
            {
                q.ProcessError();
            }
        }

        public async void SaveOrUpdate()
        {
            var q = new LPackClientQuery();
            MaskedTextBox_Parsed();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            if (IdPoco > 0)
            {
                q.Request.SetParam("Action", "UpdatePokupatelContact");
            }
            else
            {
                q.Request.SetParam("Action", "InsertPokupatelContact");
            }
            var p = Form.GetValues();
            p.CheckAdd("ID_POK", IdPok.ToString());
            p.CheckAdd("POKUPATEL_POCO_ID", IdPoco.ToString());

            q.Request.SetParams(p);


            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "WebTechnologicalMap",
                    ReceiverName = ReceiverName,
                    SenderName = ControlName,
                    Action = "Refresh",
                });
                Close();
            }
        }
        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
        }

        #region Функции парсинга номера 

        private void MaskedTextBox_Parsed()
        {
            var text = PhoneTextBox.Text;
            text = text.Replace("(", "");
            text = text.Replace("+", "");
            text = text.Replace(")", "");
            text = text.Replace(" ", "");
            text = text.Replace("-", "");
            if (text.Length == 11)
            {
                PokupatelPhone.Text = text;
            }
        }

        #endregion
    }
}
