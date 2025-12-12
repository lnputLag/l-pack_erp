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
    /// Форма редактирования договора на литую тару
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class MoldedContainerContract : ControlBase
    {
        public MoldedContainerContract()
        {
            DocumentationUrl = "/doc/l-pack-erp/service/agent/agents";
            InitializeComponent();

            InitForm();
            SetDefaults();

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
            }
            Commander.Init(this);
        }

        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// Форма редактирования техкарты
        /// </summary>
        FormHelper Form { get; set; }
        /// <summary>
        /// Идентификатор договора
        /// </summary>
        public int ContractId { get; set; }
        /// <summary>
        /// Идентификатор контрагента. Нужен при создании договора
        /// </summary>
        public int ContractorId;

        /// <summary>
        /// Инициализация формы
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();

            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="CONTRACT_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=ContractType,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="CONTRACT_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ContractName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="STANDARD_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=StandardFlag,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CONTRACT_NUMBER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ContractNumber,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="START_DATE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=StartDate,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="SELLER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Seller,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="CURRENCY_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=Currency,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="DOG_ORIGINAL_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=OriginalFlag,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="DOG_COPY_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=CopyFlag,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="COMMENTS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Comments,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="PAYMENT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PaymentTerms,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CHAIRMAN_POST",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Chairman,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CHAIRMAN_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PersonName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;

            // Блокируем кнопку сохранения, пока не выполнена загрузка данных
            SaveButton.IsEnabled = false;
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            ContractId = 0;
            ContractorId = 0;
            Form.SetDefaults();

            var contractTypeList = new Dictionary<string, string>
            {
                { "1", "Договор с покупателем" },
                { "6", "Договор с покупателем литой тары" },
            };
            ContractType.Items = contractTypeList;
            ContractType.SetSelectedItemByKey("6");
        }

        /// <summary>
        /// Получение данных из БД
        /// </summary>
        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Economics");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "GetContract");
            q.Request.SetParam("CONTRACT_ID", ContractId.ToString());

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
                    // Продавец
                    var sellerDS = ListDataSet.Create(result, "SELLERS");
                    Seller.Items = sellerDS.GetItemsList("ID", "NAME");
                    Seller.SetSelectedItemByKey("1");

                    // Валюта
                    var currencyDS = ListDataSet.Create(result, "CURRENCY");
                    Currency.Items = currencyDS.GetItemsList("ID", "NAME");
                    Currency.SetSelectedItemByKey("1");

                    // Данные контракта
                    if (ContractId > 0)
                    {
                        var valueDS = ListDataSet.Create(result, "CONTRACT");
                        Form.SetValues(valueDS);
                        
                        if (ContractNumber.Text.IsNullOrEmpty())
                        {
                            ControlTitle = $"Договор {ContractId}";
                        }
                        else
                        {
                            ControlTitle = $"Договор №{ContractNumber.Text}";
                        }
                    }
                    else
                    {
                        ControlTitle = "Новый договор";
                        StartDate.Text = DateTime.Now.ToString("dd.MM.yyyy");
                    }

                    // После загрузки разблокируем кнопку сохранения
                    SaveButton.IsEnabled = true;

                    Show();
                }
            }
        }

        /// <summary>
        /// Вход для отображения вкладки с данными
        /// </summary>
        /// <param name="contractId"></param>
        public void Edit(int contractId = 0)
        {
            ContractId = contractId;
            ControlName = $"MoldedContainerContract_{contractId}";
            GetData();
        }

        /// <summary>
        /// Показывает вкладку
        /// </summary>
        public void Show()
        {
            Central.WM.Show(ControlName, ControlTitle, true, "add", this);
        }

        /// <summary>
        /// Закрытие вкладки с формой
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            Central.WM.SetActive(ReceiverName);
        }

        /// <summary>
        /// Проверки перед записью данных в БД
        /// </summary>
        private void Save()
        {
            if (Form.Validate())
            {
                bool resume = true;
                var v = Form.GetValues();
                string errorMsg = "Не все поля заполнены верно";

                if (ContractId == 0 && ContractorId == 0)
                {
                    resume = false;
                    errorMsg = "Не удалось определить покупателя для создания договора";
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
            q.Request.SetParam("Action", "SaveContract");
            q.Request.SetParam("CONTRACT_ID", ContractId.ToString());
            q.Request.SetParam("CONTRACTOR_ID", ContractorId.ToString());
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
                        Central.Msg.SendMessage(new ItemMessage() {
                            ReceiverGroup = "Economics",
                            ReceiverName = ReceiverName,
                            SenderName = ControlName,
                            Action = "RefreshContracts",
                        });
                        
                        Close();
                    }
                }
            }

        }
    }
}
