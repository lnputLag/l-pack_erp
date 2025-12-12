using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.DeliveryAddresses;
using Client.Interfaces.Main;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.XtraReports.Native;
using DevExpress.XtraReports.UI;
using DevExpress.XtraSpellChecker;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Interfaces.Main.SelectBox;
using static iTextSharp.text.pdf.qrcode.Version;

namespace Client.Interfaces.Delivery.Shippings
{
    /// <summary>
    /// Заявка на загрузку
    /// </summary>
    /// <author>motenko_ek</author>
    public partial class ShippingForm : ControlBase
    {
        public ShippingForm(ItemMessage message, int id = 0)
        {
            InitializeComponent();

            Message = message;
            Id = id;

            FactIdSelectBox.SelectedItemChanged += (DependencyObject d, DependencyPropertyChangedEventArgs e) => {
                FillIdFactoryIdAdres();
            };

            IdAdresAddButton.Click += (object sender, RoutedEventArgs e) => {
                new ShippingAddressForm(new ItemMessage()
                {
                    ReceiverName = ControlName,
                    Action = "id_adres_refresh",
                }, IdMoGrid.SelectedItem.CheckGet("ID_POST").ToInt(), true, 0, 2);
            };
            WeightTextBox.TextChanged += TextChanged;

            DocumentationUrl = "/doc/l-pack-erp/production/shipping_form";
            FrameMode = 0;
            FrameTitle = id > 0 ? $"Загрузка #{id}" : "Новая загрузка";
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
                IdMoGridInit();
                Get();
            };
            OnUnload = () =>
            {
                IdMoGrid.Destruct();
            };

            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path = "SHIPPING_TYPE",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = ShippingTypeSelectBox,
                    ControlType = "SelectBox",
                    Description = "Тип доставки",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    OnCreate = (FormHelperField field) =>
                    {
                        var c=(SelectBox)field.Control;

                        c.SetItems(new Dictionary<string, string>{
                            {"1", "Доставка в Л-Пак"},
                            {"2", "Доставка из Л-Пак"},
                            //{"3", "Возврат продукции в Л-Пак"},
                            //{"4", "Доставка из Л-Пак покупателям"},
                        });

                        c.SetSelectedItemFirst();
                    }
                },
                new FormHelperField()
                {
                    Path="FACT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=FactIdSelectBox,
                    ControlType="SelectBox",
                    Description = "Завод",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="FACTORY_ID_ADRES",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=FactoryIdAdresSelectBox,
                    ControlType="SelectBox",
                    Description = "Адрес площадки",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "ID_ADRES",
                    //Description = "Адрес",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = IdAdresSelectBox,
                    ControlType = "SelectBox",
                    Description = "Адрес контрагента",
                    //Width = 450,
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="CARGO",
                    Control=CargoTextBox,
                    Description = "Груз",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="WEIGHT",
                    FieldType = FormHelperField.FieldTypeRef.Double,
                    Control=WeightTextBox,
                    Description = "Вес",
                    //Format="N3",
                    Format = "0.###",
                },
                new FormHelperField()
                {
                    Path="CARGO_LENGTH",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control=CargoLengthTextBox,
                    Description = "Длина",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 5 },
                    },
                },
                new FormHelperField()
                {
                    Path="CARGO_WIDTH",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control=CargoWidthTextBox,
                    Description = "Ширина",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
                },
                new FormHelperField()
                {
                    Path="CARGO_HEIGHT",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control=CargoHeightTextBox,
                    Description = "Высота",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.MaxLen, 4 },
                    },
                },
                new FormHelperField()
                {
                    Path="BEGIN_DT",
                    Control=FromDtPicker,
                    Description = "Начало периода погрузки/выгрузки у контрагента",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="END_DT",
                    Control=EndDtPicker,
                    Description = "Окончание периода погрузки/выгрузки у контрагента",
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    Control=NoteTextBox,
                    Description = "Комментарий",
                },
                new FormHelperField()
                {
                    Path = "ID_PROD",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = IdProdSelectBox,
                    ControlType = "SelectBox",
                    Description = "Плательщик",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path = "PROXY_FLAG",
                    FieldType = FormHelperField.FieldTypeRef.Boolean,
                    Control = ProxyFlagCheckBox,
                    ControlType = "CheckBox",
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
            Commander.Add(new CommandItem()
            {
                Name = "id_adres_refresh",
                Enabled = true,
                Title = "Обновить адреса",
                ActionMessage = (ItemMessage message) =>
                {
                    IdAdres = message.Message.ToInt();
                    LoadIdAdres();
                },
            });
            Commander.Init(this);

            Show();
        }

        private ItemMessage Message;
        private int Id = 0;
        private int FactoryIdAdres = 0;
        private int IdMo = 0;
        private int IdAdres = 0;
        private List<Dictionary<string, string>> FactoryIdAdresDict;
        private int RecBlock;

        /// <summary>
        /// процессор форм
        /// </summary>
        private FormHelper Form { get; set; }
        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            if (RecBlock > 0) return;

            var tb = sender as TextBox;

            var newStr = new StringBuilder(tb.Text.Length);
            var selectionStart = tb.SelectionStart;
            var before = 0;
            var after = 0;
            var points = 0;
            foreach (var ch in tb.Text)
            {
                if (ch >= '0' && ch <= '9'
                    && ((before < 3 && points == 0)
                        || (after < 3 && points != 0)
                    ))
                {
                    newStr.Append(ch);
                    if (points > 0) after++;
                    else before++;
                }
                else if ((ch == '.' || ch == ',') && before > 0 && points == 0)
                {
                    newStr.Append(',');
                    points++;
                }
                else if(newStr.Length< selectionStart) selectionStart--;
            }
            RecBlock++;
            tb.Text = newStr.ToString();
            RecBlock--;
            tb.SelectionStart = selectionStart;
        }

        private void IdMoGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="IDMO",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                },
                new DataGridHelperColumn
                {
                    Header="ИНН",
                    Path="INN",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="ИД поставщика",
                    Path="ID_POST",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=6,
                    Visible=false,
                },
            };
            IdMoGrid.SetColumns(columns);
            IdMoGrid.SetPrimaryKey("IDMO");
            IdMoGrid.SearchText = IdMoGridSearch;
            IdMoGrid.Toolbar = IdMoGridToolbar;
            IdMoGrid.Commands = Commander;
            IdMoGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            IdMoGrid.AutoUpdateInterval = 0;
            IdMoGrid.ItemsAutoUpdate = false;
            IdMoGrid.Commands = Commander;
            IdMoGrid.OnSelectItem = (row) =>
            {
                IdMo = row.CheckGet("IDMO").ToInt();
                IdAdresSelectBox.Clear();
                if(IdMo>0) LoadIdAdres();
            };
            IdMoGrid.Init();
        }
        /// <summary>
        /// получение данных с сервера
        /// </summary>
        private async void Get()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Delivery");
            q.Request.SetParam("Object", "Shippings");
            q.Request.SetParam("Action", "GetRecord");
            q.Request.SetParam("ID", Id.ToString());

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

            FactoryIdAdresDict = ListDataSet.Create(result, "FACTORY_ID_ADRES").Items;

            FactIdSelectBox.SetItems(ListDataSet.Create(result, "FACT_ID"), "ID", "NAME");

            IdMoGrid.UpdateItems(ListDataSet.Create(result, "IDMO"));

            IdProdSelectBox.SetItems(ListDataSet.Create(result, "ID_PROD"), "ID", "NAME");
            IdProdSelectBox.SetSelectedItemFirst();

            if (Id == 0)
            {
                FactIdSelectBox.SetSelectedItemFirst();
                IdProdSelectBox.SetSelectedItemFirst();
                var now = DateTime.Now;
                FromDtPicker.EditValue = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
            }
            else
            {
                var ds = ListDataSet.Create(result, "ITEMS");

                Form.SetValues(ds);

                IdMoGridSearch.Text = ds.GetFirstItemValueByKey("IDMO");
                IdMo = IdMoGridSearch.Text.ToInt();

                FactoryIdAdres = ds.GetFirstItemValueByKey("FACTORY_ID_ADRES").ToInt();
                IdAdres = ds.GetFirstItemValueByKey("ID_ADRES").ToInt();

                if (ds.GetFirstItemValueByKey("ID_TS").ToInt() != 0)
                {
                    SaveButton.IsEnabled = false;
                    Grid.IsEnabled = false;
                }
            }
        }
        private void FillIdFactoryIdAdres()
        {
            FactoryIdAdresSelectBox.Clear();
            var id = FactIdSelectBox.SelectedItem.Key.ToInt();
            FactoryIdAdresSelectBox.SetItems(FactoryIdAdresDict
                .Where(g => id == g["FACT_ID"].ToInt())
                .ToDictionary(g => g["ID_ADRES"], g => g["FACT_ADDRESS"]));

            if (FactoryIdAdresSelectBox.Items.ContainsKey(FactoryIdAdres.ToString())) FactoryIdAdresSelectBox.SetSelectedItemByKey(FactoryIdAdres.ToString());
            else FactoryIdAdresSelectBox.SetSelectedItemFirst();
        }

        private async void LoadIdAdres()
        {
            var idPost = IdMoGrid.SelectedItem.CheckGet("ID_POST").ToInt();
            if (idPost == 0) return;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Delivery");
            q.Request.SetParam("Object", "ShippingAddress");
            q.Request.SetParam("Action", "List");
            q.Request.SetParam("ID_POST", idPost.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    IdAdresSelectBox.SetItems(ListDataSet.Create(result, "ITEMS"), "ID_ADRES", "ADDRESS");
                    if(IdAdresSelectBox.Items.ContainsKey(IdAdres.ToString())) IdAdresSelectBox.SetSelectedItemByKey(IdAdres.ToString());
                    else IdAdresSelectBox.SetSelectedItemFirst();
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
        }

        /// <summary>
        /// подготовка данных
        /// </summary>
        private async void Save()
        {
            IdMoGrid.BorderBrush = (IdMo == 0 ? HColor.Red: HColor.FieldBorderNormal).ToBrush();

            if (!Form.Validate() || IdMo == 0)
            {
                Form.SetStatus("Не все обязательные поля заполнены верно", 1);
                return;
            }
            if (EndDtPicker.EditValue != null
                && (DateTime)FromDtPicker.EditValue > (DateTime)EndDtPicker.EditValue)
            {
                var bc = new BrushConverter();
                EndDtPicker.BorderBrush = HColor.Red.ToBrush();

                var errorMessage = "Время окончания не может быть меньше начала";
                EndDtPicker.ToolTip = errorMessage;
                Form.SetStatus(errorMessage, 1);
                return;
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Delivery");
            q.Request.SetParam("Object", "Shippings");
            q.Request.SetParam("Action", "SaveRecord");
            q.Request.SetParams(Form.GetValues());
            q.Request.SetParam("ID", Id.ToString());
            q.Request.SetParam("IDMO", IdMo.ToString());
            q.Request.SetParam("ACCO_ID", Central.User.AccountId.ToString());

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

            Message.SenderName = "ShippingForm";
            Message.Message = $"{id}";
            Central.Msg.SendMessage(Message);
            Close();
        }
    }
}
