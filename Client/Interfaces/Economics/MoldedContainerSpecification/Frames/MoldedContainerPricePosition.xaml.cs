using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.Interfaces.Economics.MoldedContainer
{
    /// <summary>
    /// Форма редактирования цены за товарную позицию в спецификации
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class MoldedContainerPricePosition : ControlBase
    {
        public MoldedContainerPricePosition()
        {
            DocumentationUrl = "/doc/l-pack-erp/service/agent/agents";
            InitializeComponent();
            InitForm();
            SetDefaults();

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessMessage(msg);
                }
            };

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Сохранить",
                    Description = "Сохранение данных",
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
                    Group = "main_form",
                    Enabled = true,
                    Description = "Справка",
                    ButtonUse = true,
                    ButtonName = "HelpButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "product_select",
                    Group = "main_form",
                    Enabled = true,
                    Description = "Выбрать изделие",
                    ButtonUse = true,
                    ButtonName = "ProductSelectButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        ProductSelect();
                    },
                });
            }
            Commander.Init(this);
            
        }

        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// Форма редактирования спецификации
        /// </summary>
        FormHelper Form { get; set; }
        /// <summary>
        /// Идентификатор спецификации
        /// </summary>
        public int SpecificationId { get; set; }

        public int PricePositionId { get; set; }

        public double Vat;

        private void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="PRODUCT_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="PRODUCT_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ProductId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CATEGORY_ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=CategoryId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PRICE_VAT_EXCLUDED",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=PriceVatExcluded,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    Format="N2",
                },
                new FormHelperField()
                {
                    Path="PRICE_WITH_VAT",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=PriceWithVat,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Format="N2",
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Note,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        public void SetDefaults()
        {
            Form.SetDefaults();
            SpecificationId = 0;
            PricePositionId = 0;
            Vat = 0;
        }

        /// <summary>
        /// Обработка сообщений
        /// </summary>
        /// <param name="obj"></param>
        private void ProcessMessage(ItemMessage obj)
        {
            string action = obj.Action;
            if (!action.IsNullOrEmpty())
            {
                switch (action)
                {
                    case "ProductSelect":
                        var v = (Dictionary<string, string>)obj.ContextObject;
                        if (v.ContainsKey("PRODUCT_ID") && v.ContainsKey("CATEGORY_ID"))
                        {
                            v["PRODUCT_NAME"] = $"{v["SKU_CODE"]} {v["PRODUCT_NAME"]}";
                            Form.SetValues(v);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Получение данных
        /// </summary>
        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Economics");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "GetPricePosition");
            q.Request.SetParam("POSITION_ID", PricePositionId.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "POSITION");
                    Form.SetValues(ds);
                }
            }
        }

        /// <summary>
        /// Вызов редактирования позиции спецификации
        /// </summary>
        /// <param name="positionId"></param>
        public void Edit(int positionId = 0)
        {
            PricePositionId = positionId;
            if (PricePositionId > 0)
            {
                GetData();
                ControlTitle = $"Позиция спецификации {PricePositionId}";
            }
            else
            {
                ControlTitle = "Новая позиция спецификации";
            }
            ControlName = $"PricePosition_{PricePositionId}";
            
            Show();
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void Show()
        {
            Central.WM.Show(ControlName, ControlTitle, true, "add", this);
        }

        /// <summary>
        /// Вызов вкладки выбора изделия для назначения цены
        /// </summary>
        private void ProductSelect()
        {
            var productFrame = new MoldedContainerProductSelect();
            productFrame.SpecificationId = SpecificationId;
            productFrame.ReceiverName = ControlName;
            productFrame.Show();
        }

        /// <summary>
        /// Вычисление цены с НДС
        /// </summary>
        private void CalculatePriceWithVat()
        {
            if (!PriceVatExcluded.Text.IsNullOrEmpty())
            {
                var price = PriceVatExcluded.Text.ToDouble();
                if (price > 0)
                {
                    var priceVat = Math.Round(price * Vat, 2);
                    PriceWithVat.Text = priceVat.ToString();
                }
            }
            else
            {
                PriceWithVat.Text = "";
            }
        }

        /// <summary>
        /// Сохранение
        /// </summary>
        public void Save()
        {
            if (Form.Validate())
            {
                var v = Form.GetValues();
                bool resume = true;
                string errorMsg = "Не все поля заполнены верно";

                if (PricePositionId == 0 && SpecificationId == 0)
                {
                    resume = false;
                    errorMsg = "Не удалось определить спецификацию для добавления позиции";
                }

                if (resume)
                {
                    SaveData(v);
                }
                else
                {
                    Form.SetStatus(errorMsg, 1);
                }
            }
        }

        /// <summary>
        /// Запись данных в БД
        /// </summary>
        /// <param name="p"></param>
        private async void SaveData(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Economics");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "SavePricePosition");
            q.Request.SetParam("POSITION_ID", PricePositionId.ToString());
            q.Request.SetParam("SPECIFICATION_ID", SpecificationId.ToString());
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (result.ContainsKey("ITEMS"))
                    {
                        Central.Msg.SendMessage(new ItemMessage()
                        {
                            ReceiverGroup = "Economics",
                            ReceiverName = ReceiverName,
                            SenderName = ControlName,
                            Action = "RefreshPosition",
                        });

                        Close();
                    }
                }
            }
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            Central.WM.SetActive(ReceiverName);
        }

        /// <summary>
        /// Обработка изменения значения в поле цены
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PriceVatExcluded_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculatePriceWithVat();
        }
    }
}
