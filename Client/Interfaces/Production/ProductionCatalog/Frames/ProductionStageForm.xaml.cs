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
    /// форма редактирования этапа
    /// </summary>
    /// <author>motenko_ek</author>
    public partial class ProductionStageForm : ControlBase
    {
        public ProductionStageForm(Dictionary<string, string> parent, int id = 0)
        {
            InitializeComponent();

            Parent = parent;

            Id = id;

            RoleName = "[erp]production_catalog";
            DocumentationUrl = "/doc/l-pack-erp/production/production_catalog/production_stage";
            FrameMode = 0;
            FrameTitle = id > 0 ? $"Этап #{id} для схемы\"{Parent.CheckGet("NAME")}\"" : $"Новый этап для схемы\"{Parent.CheckGet("NAME")}\"";
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
                //new FormHelperField()
                //{
                //    Path="PRSC_ID",
                //    FieldType=FormHelperField.FieldTypeRef.Integer,
                //    Control=PrscIdSelectBox,
                //    ControlType="SelectBox",
                //    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                //        { FormHelperField.FieldFilterRef.Required, null },
                //    },
                //},
                new FormHelperField()
                {
                    Path="PRWO_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PrwoIdSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="NEXT_PRST_ID",
                    AutoloadItems = false,
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=NextPrstIdSelectBox,
                    ControlType="SelectBox",
                    OnCreate = (FormHelperField field) =>
                    {
                        var c = (SelectBox)field.Control;

                        var columns = new List<DataGridHelperColumn>()
                        {
                            new DataGridHelperColumn
                            {
                                Header="ИД",
                                Path="PRST_ID",
                                ColumnType=ColumnTypeRef.Integer,
                                Width2=6,
                            },
                            //new DataGridHelperColumn
                            //{
                            //    Header="Схема производства",
                            //    Path="PRSC_NAME",
                            //    ColumnType=ColumnTypeRef.String,
                            //    Width2=15,
                            //},
                            new DataGridHelperColumn
                            {
                                Header="Рабочий центр",
                                Path="PRWO_NAME",
                                ColumnType=ColumnTypeRef.String,
                                Width2=15,
                            },
                        };
                        c.GridColumns = columns;
                        c.SelectedItemValue = "PRST_ID";

                        c.ListBoxMinWidth = 600;
                        c.ListBoxMinHeight = 200;
                        //c.Style = FindResource("CustomFormField");
                        c.DataType = SelectBox.DataTypeRef.Grid;
                        c.GridPrimaryKey = "PRST_ID";
                    },
                    OnChange = (FormHelperField f, string v) =>
                    {
                        f = f;
                    },
                    OnTextChange = (FormHelperField f, string v) =>
                    {
                        f = f;
                    }
                    //QueryLoadItems = new RequestData()
                    //{
                    //    Module = "ProductionCatalog",
                    //    Object = "ProductionStage",
                    //    Action = "ListForStage",
                    //    AnswerSectionKey = "ITEMS",
                    //    OnComplete = (FormHelperField field, ListDataSet ds) =>
                    //    {
                    //        var c = (SelectBox)field.Control;
                    //        c.GridDataSet = ds;
                    //    },
                    //},
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

        private Dictionary<string, string> Parent;

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
            q.Request.SetParam("Object", "ProductionStage");
            q.Request.SetParam("Action", "Get");
            q.Request.SetParam("PRST_ID", Id.ToString());
            q.Request.SetParam("PRSC_ID", Parent["PRSC_ID"]);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    //{
                    //    var ds = ListDataSet.Create(result, "PRSC_ID");
                    //    PrscIdSelectBox.SetItems(ds, "PRSC_ID", "NAME");
                    //}

                    {
                        var ds = ListDataSet.Create(result, "PRWO_ID");
                        PrwoIdSelectBox.SetItems(ds, "PRWO_ID", "NAME");
                    }

                    {
                        var ds = ListDataSet.Create(result, "NEXT_PRST_ID");
                        NextPrstIdSelectBox.SetItems(ds, "NEXT_PRST_ID", "PRODUCTION");
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
            q.Request.SetParam("Object", "ProductionStage");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParams(Form.GetValues());
            q.Request.SetParam("PRST_ID", Id.ToString());
            q.Request.SetParam("PRSC_ID", Parent["PRSC_ID"]);

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
                    var id = ds.GetFirstItemValueByKey("PRST_ID").ToInt();

                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverName = "ProductionCatalogSchemeTab",
                        SenderName = "ProductionStageForm",
                        Action = "production_catalog_stage_refresh",
                        Message = $"{id}",
                    });
                    Close();
                }
                else
                {
                    DialogWindow.ShowDialog("Неверный ответ сервера", "Добавление этапа", "");
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
