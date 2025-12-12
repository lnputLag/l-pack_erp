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
    /// Форма создания запроса на получение файла.
    /// Страница веб техкарт.
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class WebTechnologicalMapRequestFileForm : ControlBase
    {
        public WebTechnologicalMapRequestFileForm()
        {

            InitializeComponent();
            FrameMode = 2;
            FrameName = "WebTechnologicalMapRequestFileForm";
            OnGetFrameTitle = () =>
            {
                return "Запрос на файл";
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
            }
            Commander.Init(this);
            OnLoad = () =>
            {
                DesignersDS = new Dictionary<string, string>();
                ConstructorsDS = new Dictionary<string, string>();
                FormInit();
                LoadResp();
                LoadRef();
            };
        }

        public FormHelper Form { get; set; }
        Dictionary<string,string> DesignersDS { get; set; }
        Dictionary<string,string> ConstructorsDS { get; set; }

        public int IdTk { get; set; }
        public int IdDes { get; set; }
        public int IdCon { get; set; }

        public string ResiverName { get; set; }

        #region "Загрузка справочников"
        public async void LoadRef()
        {
            var types = new Dictionary<string, string>()
            {
                {"2", "Запрос чертежа"},
                {"1", "Запрос дизайна"},
                {"3", "Другое"},

            };
            TypeSelectBox.Items = types;
            TypeSelectBox.SetSelectedItemByKey("2");

            var recipients = new Dictionary<string, string>()
            {
                {"1", "Конструкторы"},
                {"2", "Дизайнеры"},

            };
            RecipientSelectBox.Items = recipients;
            RecipientSelectBox.SetSelectedItemByKey("1");
        }

        public async void LoadResp()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "ListResponse");

            q.DoQuery();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {

                    var des = ListDataSet.Create(result, "LIST_DESIGNERS");
                    foreach (var item in des.Items)
                    {
                        if (item["ID"].ToInt() != 0)
                        {
                            DesignersDS.Add(item["ID"].ToInt().ToString(), item.CheckGet("FULL_NAME"));

                        }
                    }

                    var con = ListDataSet.Create(result, "LIST_CONSTRUCTORS");
                    foreach (var item in con.Items)
                    {
                        if (item["ID"].ToInt() != 0)
                        {
                            ConstructorsDS.Add(item["ID"].ToInt().ToString(), item.CheckGet("FULL_NAME"));

                        }
                    }
                }
            }
        }
        #endregion

        #region "Форма"

        public void FormInit()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="TYPE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=TypeSelectBox,
                    ControlType = "SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        switch (TypeSelectBox.SelectedItem.Key.ToInt())
                        {
                            case 1:
                                RecipientSelectBox.SetSelectedItemByKey("2");
                                break;
                            case 2:
                                RecipientSelectBox.SetSelectedItemByKey("1");
                                break;
                            case 3:
                                RecipientSelectBox.SetSelectedItemByKey("3");
                                break;
                            default:
                                break;
                        }
                    },
                    Validate = (f, v) =>
                    {
                        if (TypeSelectBox.SelectedItem.Key.ToInt() <= 0)
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Выберите тип запроса";
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="RCPT_TYPE_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=RecipientSelectBox,
                    ControlType = "SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnChange = (FormHelperField field, string value) =>
                    {
                        switch (RecipientSelectBox.SelectedItem.Key.ToInt())
                        {
                            case 1:
                                
                                ResponsibleSelectBox.Items = ConstructorsDS;
                                if (IdCon>0 && ResponsibleSelectBox!=null && ResponsibleSelectBox.Items!=null && ResponsibleSelectBox.Items.ContainsKey(IdCon.ToString()))
                                {
                                    ResponsibleSelectBox.SetSelectedItemByKey(IdCon.ToString());
                                }
                                else
                                {
                                    ResponsibleSelectBox.SetSelectedItemFirst();
                                }
                                break;
                            case 2:
                                ResponsibleSelectBox.Items = DesignersDS;
                                if (IdDes>0 && ResponsibleSelectBox!=null && ResponsibleSelectBox.Items!=null && ResponsibleSelectBox.Items.ContainsKey(IdDes.ToString()))
                                {
                                    ResponsibleSelectBox.SetSelectedItemByKey(IdDes.ToString());
                                }
                                else
                                {
                                    ResponsibleSelectBox.SetSelectedItemFirst();
                                }
                                break;
                            default:
                                break;
                        }
                    },
                    Validate = (f, v) =>
                    {
                        if (RecipientSelectBox.SelectedItem.Key.ToInt() <= 0)
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Выберите направление";
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="RESP_EMPL_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ResponsibleSelectBox,
                    ControlType = "SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Validate = (f, v) =>
                    {
                        if (ResponsibleSelectBox.SelectedItem.Key.ToInt() <= 0)
                        {
                            f.ValidateResult = false;
                            f.ValidateProcessed = true;
                            f.ValidateMessage = "Выберите ответственного";
                        }
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CommentTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },

            };
            Form.ToolbarControl = null;
            Form.SetFields(fields);

        }
        #endregion


        public async void Save()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "AddTkFileRequest");

            q.Request.SetParam("ID_TK", IdTk.ToString());
            q.Request.SetParam("NOTE", CommentTextBox.Text.ToString());
            q.Request.SetParam("TYPE", TypeSelectBox.SelectedItem.Key.ToString());
            q.Request.SetParam("RCPT_TYPE_ID", RecipientSelectBox.SelectedItem.Key.ToString());
            q.Request.SetParam("RESP_EMPL_ID", ResponsibleSelectBox.SelectedItem.Key.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEM");
                    if (ds.Items[0].CheckGet("SUCCESS").ToInt() == 3)
                    {
                        string msg = $"Ошибка создания запроса. Запрос на получение файла уже существует.";
                        var d = new DialogWindow($"{msg}", "Получение файла", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                        Close();
                    }
                    else if (ds.Items[0].CheckGet("SUCCESS").ToInt() == 1)
                    {
                        string msg = $"Запрос на получение файла сохранен";
                        var d = new DialogWindow($"{msg}", "Получение файла", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                        Close();
                    }
                    else
                    {
                        string msg = $"Ошибка создания запроса на получение файла";
                        var d = new DialogWindow($"{msg}", "Получение файла", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
            }
        }
        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
        }

    }
}
