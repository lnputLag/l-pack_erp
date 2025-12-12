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
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Interfaces.Main.SelectBox;

namespace Client.Interfaces.Sources.ProductionScheme2
{
    /// <summary>
    /// форма редактирования станка
    /// </summary>
    /// <author>motenko_ek</author>
    public partial class ProductionStageInputOutputForm : ControlBase
    {
        public ProductionStageInputOutputForm(ItemMessage message, bool isOutput, Dictionary<string, string> good, int prspId, Dictionary<string, string> stage, int id = 0)
        {
            InitializeComponent();

            Message = message;
            IsOutput = isOutput;
            Good = good;
            PrspId = prspId;
            Stage = stage;
            Id = id;

            GoodsGridSearch.TextChanged += GoodsGridSearchTextChanged;
            GoodsGridSearchButton.IsEnabled = false;

            DocumentationUrl = "/doc/l-pack-erp/sources/production_scheme2/production_stage_input_output_form";
            FrameMode = 0;
            FrameTitle = id > 0 ? $"Изменение #{id}" : isOutput ? "Добавление на выход этапа" : "Добавление на вход этапа";
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
                GoodsGridInit();
                FillData();
            };
            OnUnload = () => 
            {
                GoodsGrid.Destruct();
            };

            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=QtyTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
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
            Commander.SetCurrentGridName("GoodsGrid");
            Commander.Add(new CommandItem()
            {
                Name = "goods_refresh",
                Group = "grid_base",
                Enabled = true,
                Title = "Показать",
                Description = "Загрузить данные",
                ButtonUse = true,
                ButtonName = "GoodsGridSearchButton",
                Action = () =>
                {
                    GoodsGridLoadItems();
                },
                CheckEnabled = () =>
                {
                    return Good["ID2"] != Id2;
                },
            });
            Commander.Init(this);

            Show();
        }


        private ItemMessage Message;
        private readonly bool IsOutput;
        private readonly Dictionary<string, string> Good;
        private int PrspId;
        private readonly Dictionary<string, string> Stage;

        private int Id = 0;
        private string Id2 = "0";


        /// <summary>
        /// процессор форм
        /// </summary>
        private FormHelper Form { get; set; }

        private void GoodsGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД изделия",
                    Path="ID2",
                    Width2=10,
                    ColumnType=ColumnTypeRef.Integer,

                },
                new DataGridHelperColumn
                {
                    Header="idk1",
                    Path="IDK1",
                    Width2=6,
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="ARTIKUL",
                    ColumnType=ColumnTypeRef.String,
                    Width2=18,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    Doc="Наименование товара",
                    ColumnType=ColumnTypeRef.String,
                    Width2=48,
                },
            };
            GoodsGrid.SetColumns(columns);
            GoodsGrid.SetPrimaryKey("ID2");
            GoodsGrid.Toolbar = GoodsGridToolbar;
            GoodsGrid.Commands = Commander;
            GoodsGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            GoodsGrid.AutoUpdateInterval = 0;
            GoodsGrid.ItemsAutoUpdate = false;
            GoodsGrid.Commands = Commander;
            //GoodsGrid.QueryLoadItems = new RequestData()
            //{
            //    Module = "Sources/ProductionScheme2",
            //    Object = "Goods",
            //    Action = "ListWoExist",
            //    AnswerSectionKey = "ITEMS",
            //    BeforeRequest = (RequestData rd) =>
            //    {
            //        var idk1 = Good["IDK1"].ToInt();//5||6||16
            //        string list;
            //        switch(idk1)
            //        {
            //            case 5: case 6:
            //                list = "4";
            //                if (IsOutput && Stage["NEXT_PRST_ID"].ToInt() == 0) list += ",5,6";
            //                break;
            //            case 16:
            //                list = "26";
            //                break;
            //            default:
            //                list = "";
            //                break;
            //        }
            //        rd.Params = new Dictionary<string, string>()
            //                {
            //                    { "idk1", list },
            //                    { "TEXT", Good["ID2"] == Id2 ? "" : "%" + GoodsGridSearch.Text + "%" },
            //                    { "ID2", Good["ID2"] },
            //                };
            //    },
            //    AfterRequest = (RequestData rd, ListDataSet ds) =>
            //    {
            //        GoodsGridSearchButton.Style = (Style)GoodsGridSearch.TryFindResource("Button");

            //        return ds;
            //    },
            //};
            GoodsGrid.OnSelectItem = (row) =>
            {
                Id2 = row.CheckGet("ID2");
            };
            GoodsGrid.Init();
        }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        private void FillData()
        {
            if (Id == 0)
            {
                QtyTextBox.Text = "1";
            }
            else
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Sources/ProductionScheme2");
                q.Request.SetParam("Object", IsOutput ? "ProductionStageOutput" : "ProductionStageInput");
                q.Request.SetParam("Action", "Get");
                q.Request.SetParam("ID", Id.ToString());

                Form.SetBusy(true);

                //await Task.Run(() =>
                //{
                    q.DoQuery();
                //});

                Form.SetBusy(false);

                if (q.Answer.Status != 0)
                {
                    q.ProcessError();
                    return;
                }

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result == null)
                {
                    DialogWindow.ShowDialog("Неверный ответ", "Получение данных с сервера", "");
                    return;
                }

                var ds = ListDataSet.Create(result, "ITEMS");
                Form.SetValues(ds);

                Id2 = ds.GetFirstItemValueByKey("ID2");
                GoodsGridSearch.Text = Id2;
                if (Good["ID2"] == Id2)
                {
                    GoodsGridSearch.IsEnabled = false;
                    GoodsGridSearchButton.IsEnabled = false;
                }
            }
            Commander.Process("goods_refresh");
        }
        private void GoodsGridLoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sources/ProductionScheme2");
            q.Request.SetParam("Object", "Goods");
            if (Good["ID2"] != Id2)
            {
                q.Request.SetParam("Action", "ListForProductionStage");
                string list;
                switch (Good["IDK1"].ToInt())
                {
                    case 5:
                    case 6:
                        if (Good["CARDBOARD_SLEEVES_FLAG"].ToBool())
                        {
                            list = "17";
                        }
                        else
                        {
                            list = "4";
                        }
                        break;
                    case 16:
                        list = "26";
                        break;
                    default:
                        list = "";
                        break;
                }
                q.Request.SetParam("IDK1", list);
                q.Request.SetParam("TEXT", "%" + GoodsGridSearch.Text + "%");
                q.Request.SetParam("PRSP_ID", PrspId.ToString());
                q.Request.SetParam("PRST_ID", Stage["PRST_ID"]);
                q.Request.SetParam("ISOUTPUT", IsOutput.ToInt().ToString());
            }
            else
            {
                q.Request.SetParam("Action", "Get");
            }
            q.Request.SetParam("ID2", Id2);

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
                DialogWindow.ShowDialog("Неверный ответ", "Получение данных с сервера", "");
                return;
            }

            var ds = ListDataSet.Create(result, "ITEMS");
            GoodsGrid.UpdateItems(ds);
            GoodsGridSearchButton.Style = (Style)GoodsGridSearch.TryFindResource("Button");
        }

        /// <summary>
        /// подготовка данных
        /// </summary>
        private async void Save()
        {
            var color = "#ffcccccc";
            if (Id2.IsNullOrEmpty())
            {
                color = "#ffee0000";
            }
            var bc = new BrushConverter();
            var brush = (Brush)bc.ConvertFrom(color);
            GoodsGrid.BorderBrush = brush;

            if (!Form.Validate() || Id2.IsNullOrEmpty())
            {

                Form.SetStatus("Не все обязательные поля заполнены верно", 1);
                return;
            }

            Form.SetBusy(true);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Sources/ProductionScheme2");
            q.Request.SetParam("Object", IsOutput ? "ProductionStageOutput" : "ProductionStageInput");
            q.Request.SetParam("Action", "Save");
            q.Request.SetParam("ID", Id.ToString());
            q.Request.SetParam("PRSP_ID", PrspId.ToString());
            q.Request.SetParam("PRST_ID", Stage["PRST_ID"]);
            q.Request.SetParam("ID2", Id2);
            q.Request.SetParams(Form.GetValues());

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    var id = ds.GetFirstItemValueByKey("ID").ToInt();

                    Message.SenderName = ControlName;
                    Message.Message = $"{id}";
                    Central.Msg.SendMessage(Message);
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
        private void GoodsGridSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            GoodsGridSearchButton.Style = (Style)GoodsGridSearch.TryFindResource("FButtonPrimary");
        }
    }
}
