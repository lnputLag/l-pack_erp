using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Форма изменения укладки на поддон для выбранной заявки выбранного производственного задания.
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class StackerManuallyPrintStackingEditor : UserControl
    {
        public StackerManuallyPrintStackingEditor(string productionTask, string productName, int quantityStackOnPallet, int quantityOnStack, int quantityOnPallet, int productionTaskId, int orderId, int productId)
        {
            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitializeComponent();
            FrameName = "StackerManuallyPrintStackingEditor";

            ProductionTask = productionTask;
            ProductName = productName;
            QuantityStackOnPallet = quantityStackOnPallet;
            QuantityOnStack = quantityOnStack;
            QuantityOnPallet = quantityOnPallet;
            ProductionTaskId = productionTaskId;
            OrderId = orderId;
            ProductId = productId;

            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// Техническое имя фрейма
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// Процессор форм
        /// </summary>
        public FormHelper FormHelper { get; set; }

        /// <summary>
        /// Номер производственного задания
        /// </summary>
        public string ProductionTask { get; set; }

        /// <summary>
        /// Наименование продукции
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// Количество стоп на поддоне
        /// </summary>
        public int QuantityStackOnPallet { get; set; }

        /// <summary>
        /// Количество продукции в стопе
        /// </summary>
        public int QuantityOnStack { get; set; }

        /// <summary>
        /// Количество продукции на поддоне
        /// </summary>
        public int QuantityOnPallet { get; set; }

        /// <summary>
        /// ид производственного задания (proiz_zad.id_pz)
        /// </summary>
        public int ProductionTaskId { get; set; }

        /// <summary>
        /// Ид заявки (orderdates.idorderdates)
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Ид продукции (tover.id2)
        /// </summary>
        public int ProductId { get; set; }

        public string ParemtFrameName { get; set; }

        public void InitForm()
        {
            FormHelper = new FormHelper();

            var fields = new List<FormHelperField>
                {
                    new FormHelperField
                    {
                        Path = "PRODUCTION_TASK",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = ProductionTaskTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },
                    new FormHelperField
                    {
                        Path = "PRODUCT_NAME",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = ProductNameTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        },
                    },

                    new FormHelperField
                    {
                        Path = "QUANTITY_STACK_ON_PALLET",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = QuantityStackOnPalletTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                            { FormHelperField.FieldFilterRef.DigitOnly, null },
                            { FormHelperField.FieldFilterRef.IsNotZero, null },
                        },
                    },
                    new FormHelperField
                    {
                        Path = "QUANTITY_ON_STACK",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = QuantityOnStackTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                            { FormHelperField.FieldFilterRef.DigitOnly, null },
                            { FormHelperField.FieldFilterRef.IsNotZero, null },
                        },
                    },
                    new FormHelperField
                    {
                        Path = "QUANTITY_ON_PALLET",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = QuantityOnPalletTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                            { FormHelperField.FieldFilterRef.DigitOnly, null },
                            { FormHelperField.FieldFilterRef.IsNotZero, null },
                        },
                    },

                    new FormHelperField
                    {
                        Path = "PRODUCTION_TASK_ID",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = null,
                        ControlType = "void",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                            { FormHelperField.FieldFilterRef.DigitOnly, null },
                            { FormHelperField.FieldFilterRef.IsNotZero, null },
                        },
                    },
                    new FormHelperField
                    {
                        Path = "ORDER_ID",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = null,
                        ControlType = "void",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                            { FormHelperField.FieldFilterRef.DigitOnly, null },
                            { FormHelperField.FieldFilterRef.IsNotZero, null },
                        },
                    },
                     new FormHelperField
                    {
                        Path = "PRODUCT_ID",
                        FieldType = FormHelperField.FieldTypeRef.Integer,
                        Control = null,
                        ControlType = "void",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                            { FormHelperField.FieldFilterRef.DigitOnly, null },
                            { FormHelperField.FieldFilterRef.IsNotZero, null },
                        },
                    },
                };

            FormHelper.SetFields(fields);
        }

        public void SetDefaults()
        {
            FormHelper.SetValueByPath("PRODUCTION_TASK", $"{ProductionTask}");
            FormHelper.SetValueByPath("PRODUCT_NAME", $"{ProductName}");
            FormHelper.SetValueByPath("QUANTITY_STACK_ON_PALLET", $"{QuantityStackOnPallet}");
            FormHelper.SetValueByPath("QUANTITY_ON_PALLET", $"{QuantityOnPallet}");
            FormHelper.SetValueByPath("QUANTITY_ON_STACK", $"{QuantityOnStack}");
            FormHelper.SetValueByPath("PRODUCTION_TASK_ID", $"{ProductionTaskId}");
            FormHelper.SetValueByPath("ORDER_ID", $"{OrderId}");
            FormHelper.SetValueByPath("PRODUCT_ID", $"{ProductId}");
        }

        public void Save()
        {
            if (FormHelper.Validate())
            {
                var p = FormHelper.GetValues();

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ManuallyPrint");
                q.Request.SetParam("Action", "UpdateStackingOnPallet");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;
                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    bool succesfullFlag = false;

                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            var orderId = ds.Items.First().CheckGet("ORDER_ID").ToInt();
                            if (orderId > 0)
                            {
                                succesfullFlag = true;
                            }
                        }
                    }

                    if (succesfullFlag)
                    {
                        // отправляем сообщение гриду производственных заданий обновиться
                        {
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "Production",
                                ReceiverName = ParemtFrameName,
                                SenderName = "StackerManuallyPrintStackingEditor",
                                Action = "Refresh",
                                Message = "",
                            }
                            );
                        }

                        var msg = "Успешное обновление укладки на поддон.";
                        var d = new DialogWindow($"{msg}", "Изменение укладки", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                    else
                    {
                        var msg = "Ошибка обновления укладки на поддон.";
                        var d = new DialogWindow($"{msg}", "Изменение укладки", "", DialogWindowButtons.OK);
                        d.ShowDialog();
                    }
                }
                else
                {
                    q.ProcessError();
                }

                Close();
            }
            else
            {
                var msg = "Не все поля заполнены корректно.";
                var d = new DialogWindow($"{msg}", "Изменение укладки", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Расчитываем количество продукции в стопе (Количество продукции на поддоне / количество стоп на поддоне)
        /// </summary>
        public void CalculateQuantityOnStack()
        {
            if (QuantityOnPalletTextBox != null && QuantityStackOnPalletTextBox != null && QuantityOnStackTextBox != null)
            {
                if (QuantityOnPalletTextBox.Text.ToInt() > 0 && QuantityStackOnPalletTextBox.Text.ToInt() > 0)
                {
                    QuantityOnStackTextBox.Text = Math.Ceiling(QuantityOnPalletTextBox.Text.ToDouble() / QuantityStackOnPalletTextBox.Text.ToDouble()).ToString();
                }
            }
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 2;

            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            Central.WM.Show(FrameName, "Укладка на поддон", true, "add", this, "top", windowParametrs);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m">Сообщение</param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = "StackerManuallyPrintStackingEditor",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close(FrameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void QuantityOnPalletTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateQuantityOnStack();
        }

        private void QuantityStackOnPalletTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateQuantityOnStack();
        }
    }
}
