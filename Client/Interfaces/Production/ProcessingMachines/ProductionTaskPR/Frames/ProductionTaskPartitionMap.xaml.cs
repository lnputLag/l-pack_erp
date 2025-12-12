using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production.ProcessingMachines
{
    /// <summary>
    /// Логика взаимодействия для ProductionTaskPartitionMap.xaml
    /// </summary>
    public partial class ProductionTaskPartitionMap : UserControl
    {
        public ProductionTaskPartitionMap()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// Имя вкладки, куда происходит возврат при закрытии этой вкладки
        /// </summary>
        public string ReceiverName;

        /// <summary>
        /// Имя этой вкладки
        /// </summary>
        public string TabName;

        /// <summary>
        /// Данные по сырью для производства
        /// </summary>
        private Dictionary<string, string> RawData;
        /// <summary>
        /// Данные по изделию для производства
        /// </summary>
        private Dictionary<string, string> ProductData;

        /// <summary>
        /// Форма задания на решетки
        /// </summary>
        FormHelper Form { get; set; }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.F1:
                    ShowHelp();
                    e.Handled = true;
                    break;

                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Обработка сообщений
        /// </summary>
        /// <param name="obj"></param>
        private void ProcessMessages(ItemMessage obj)
        {
            if (obj.ReceiverGroup.IndexOf("ProductionTaskProcessing") > -1)
            {
                if (obj.ReceiverName.IndexOf(TabName) > -1)
                {
                    switch (obj.Action)
                    {
                        case "PartitionalSetSelected":
                            if (obj.ContextObject != null)
                            {
                                var v = (Dictionary<string, string>)obj.ContextObject;
                                SetProduct(v);
                            }
                            break;

                        case "PartitionalRawSelected":
                            if (obj.ContextObject != null)
                            {
                                var v = (Dictionary<string, string>)obj.ContextObject;
                                SetRaw(v);
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>
            {
                new FormHelperField()
                {
                    Path="PRODUCT_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=PartitionalSetName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    Description="Название изделия",
                },
                new FormHelperField()
                {
                    Path="ORDER_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=PartitionalSetQty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    Description="Количество в заявке",
                },
                new FormHelperField()
                {
                    Path="ORDER_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=OrderId,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    Description="ID заявки",
                },
                new FormHelperField()
                {
                    Path="SHIPMENT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    Description="ID отгрузки",
                },
                new FormHelperField()
                {
                    Path="PRODUCT_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    Description="ID изделия (ID2)",
                },
                new FormHelperField()
                {
                    Path="PRODUCT_CATEGORY_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    Description="ID категории изделия (IDK1)",
                },
                new FormHelperField()
                {
                    Path="RAW_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=RawName,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    Description="Название сырья (картона)",
                },
                new FormHelperField()
                {
                    Path="RAW_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    Description="ID картона (ID2)",
                },
                new FormHelperField()
                {
                    Path="RAW_CATEGORY_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=null,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    Description="ID категории картона (IDK1)",
                },
                new FormHelperField()
                {
                    Path="RAW_TASK_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=RawTaskQty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    Description="Количество сырья, необходимое для задания",
                },
                new FormHelperField()
                {
                    Path="RAW_STOCK_QTY",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=RawStockQty,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Description="Остаток сырья на складе",
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;

        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            RawData = new Dictionary<string, string>();
            ProductData = new Dictionary<string, string>();
            // Количество комплектов можно менять только если заполнена заявка
            PartitionalSetQty.IsReadOnly = true;
        }

        public void Edit()
        {
            Show();
        }

        /// <summary>
        /// Вызов вкладки выбора заявки
        /// </summary>
        private void SelectPartitionalSet()
        {
            var setSelectForm = new ProductionTaskPartitionalSetSelect();
            setSelectForm.ReceiverName = TabName;
            setSelectForm.Show();
        }

        /// <summary>
        /// Вызов вкладки выбора сырья
        /// </summary>
        private void SelectRaw()
        {
            var rawSelectForm = new ProductionTaskPrRawSelect();
            rawSelectForm.ReceiverName = TabName;
            rawSelectForm.Show();

        }

        /// <summary>
        /// Показ окна
        /// </summary>
        public void Show()
        {
            TabName = "ProcesingPartitionSet";
            Central.WM.AddTab(TabName, "Решетки из гильзового картона", true, "add", this);
            Central.WM.SetActive(TabName);
        }

        /// <summary>
        /// Получение схемы производства и заполнение полей
        /// </summary>
        /// <param name="v"></param>
        private async void SetProduct(Dictionary<string, string> v)
        {
            int productId = v.CheckGet("PRODUCT_ID").ToInt();
            int categoryId = v.CheckGet("PRODUCT_CATEGORY_ID").ToInt();

            // Если задание частично выполнено, корректируем количество из заявки
            int completeQty = v.CheckGet("TASK_QTY").ToInt() + v.CheckGet("COMPLETE_QTY").ToInt();
            if (completeQty > 0)
            {
                int taskQty =  v["ORDER_QTY"].ToInt() - completeQty;
                if (taskQty > 0)
                {
                    v["ORDER_QTY"] = taskQty.ToString();
                }
                else
                {
                    v["ORDER_QTY"] = "0";
                }
            }

            if (productId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPr");
                q.Request.SetParam("Object", "ProductionTaskPr");
                q.Request.SetParam("Action", "GetScheme");
                q.Request.SetParam("PRODUCT_ID", productId.ToString());
                q.Request.SetParam("PRODUCT_CATEGORY_ID", categoryId.ToString());

                q.Request.Timeout = Central.Parameters.RequestTimeoutMax;
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
                        var schemeDS = ListDataSet.Create(result, "SCHEME");
                        // список доступного сырья
                        var rawDS = ListDataSet.Create(result, "RAW_LIST");

                        // В схеме должно быть 2 строки: первая для сырья, вторая для продукции
                        if (schemeDS.Items.Count > 1)
                        {
                            // Для сырья заполняем только те поля, которых нет
                            int rawId = 0;
                            foreach (var item in schemeDS.Items[0])
                            {
                                if (!RawData.ContainsKey(item.Key))
                                {
                                    RawData.Add(item.Key, item.Value);
                                    v.CheckAdd($"RAW_{item.Key}", item.Value);
                                    if (item.Key == "ID")
                                    {
                                        rawId = schemeDS.Items[0].CheckGet("ID").ToInt();
                                    }
                                }
                            }
                            // Если сырье не было заполнено, найдем для него остаток на складе
                            if ((rawId > 0) && (rawDS.Items.Count > 0))
                            {
                                foreach (var row in rawDS.Items)
                                {
                                    if (row.CheckGet("ID").ToInt() == rawId)
                                    {
                                        RawStockQty.Text = row.CheckGet("QTY").ToInt().ToString();
                                    }
                                }
                            }

                            ProductData = schemeDS.Items[1];
                        }
                    }
                }

                Form.SetValues(v);
                if (v.CheckGet("ORDER_ID").ToInt() > 0)
                {
                    PartitionalSetQty.IsReadOnly = false;
                }
                CalcRawReqirements();
            }
        }

        /// <summary>
        /// Заполнение данных по сырью
        /// </summary>
        /// <param name="v"></param>
        private void SetRaw(Dictionary<string, string> raw)
        {
            var v = new Dictionary<string, string>();
            foreach(var item in raw)
            {
                RawData.CheckAdd(item.Key, item.Value);
                v.CheckAdd($"RAW_{item.Key}", item.Value);
            }

            v.CheckAdd("RAW_STOCK_QTY", raw.CheckGet("QTY").ToInt().ToString());
            Form.SetValues(v);
            CalcRawReqirements();
        }

        /// <summary>
        /// Вычисление необходимого количества сырья для выполнения заявки
        /// </summary>
        private void CalcRawReqirements()
        {
            // order_qty * (set_square / raw_square) * density * 1.1
            var order_qty = PartitionalSetQty.Text.ToInt();
            var set_square = ProductData.CheckGet("SQUARE").ToDouble();
            var raw_square = RawData.CheckGet("SQUARE").ToDouble();
            var density = RawData.CheckGet("DENSITY").ToDouble();

            bool resume = true;
            string errorMsg = "";
            // Все значения должны быть больше нуля
            if (resume)
            {
                if (order_qty == 0)
                {
                    errorMsg = "Нечего изготавливать";
                    resume = false;
                }
            }

            if (resume)
            {
                if (set_square == 0)
                {
                    errorMsg = "Не заполнена площадь комплекта";
                    resume = false;
                }
            }

            if (resume)
            {
                if (raw_square == 0)
                {
                    errorMsg = "Не заполнен формат сырья";
                    resume = false;
                }
            }

            if (resume)
            {
                if (raw_square == 0)
                {
                    errorMsg = "Не заполнена плотность сырья";
                    resume = false;
                }
            }

            if (resume)
            {
                int req = (int)Math.Ceiling(order_qty * set_square * density * 1.1 / raw_square);
                RawTaskQty.Text = req.ToString();
            }
            else
            {
                RawTaskQty.Text = "";
                Form.SetStatus(errorMsg, 1);
            }
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        private void Close()
        {
            Central.WM.RemoveTab(TabName);
            Central.WM.SetActive(ReceiverName);
            ReceiverName = "";

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// Сохранение задания
        /// </summary>
        private async void Save()
        {
            if (Form.Validate())
            {
                var p = Form.GetValues();
                p.Add("MACHINE_ID", ProductData.CheckGet("MACHINE_ID"));
                p.Add("PLACE", RawData.CheckGet("MACHINE_NAME"));
                p.Add("SCHEME_ID", RawData.CheckGet("IDTSCHEME"));
                p.Add("ID_TLS", RawData.CheckGet("IDTSTREE"));
                p.Add("IDTSTREE", RawData.CheckGet("ID_TLS"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "ProductionPr");
                q.Request.SetParam("Object", "ProductionTaskPr");
                q.Request.SetParam("Action", "SavePartitionalSet");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestTimeoutMax;
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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds.Items.Count > 0)
                        {
                            var rec = ds.Items[0];
                            string pzNum = rec.CheckGet("NUM");
                            if (!string.IsNullOrEmpty(pzNum))
                            {
                                var dw = new DialogWindow($"Успешно создано задание {pzNum}", "Задания на решетки");
                                dw.ShowDialog();
                            }

                            //отправляем сообщение о необходимости обновить грид
                            Messenger.Default.Send(new ItemMessage()
                            {
                                ReceiverGroup = "ProductionTaskProcessing",
                                ReceiverName = ReceiverName,
                                SenderName = TabName,
                                Action = "Refresh",
                            });
                        }
                    }

                    Close();
                }
                else if (q.Answer.Error.Code == 145)
                {
                    Form.SetStatus(q.Answer.Error.Message, 1);
                }
            }
            else
            {
                Form.SetStatus("Не все поля заполнены верно", 1);
            }
        }

        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/production_tasks_pr/grating_sleeve_cardboard");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PartitionalSetSelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectPartitionalSet();
        }

        private void PartitionalSetClearButton_Click(object sender, RoutedEventArgs e)
        {
            var clearSet = new Dictionary<string, string>();
            clearSet.CheckAdd("PRODUCT_NAME", "");
            clearSet.CheckAdd("ORDER_QTY", "");
            clearSet.CheckAdd("ORDER_ID", "");
            clearSet.CheckAdd("SHIPMENT_ID", "");
            clearSet.CheckAdd("PRODUCT_ID", "");
            clearSet.CheckAdd("PRODUCT_CATEGORY_ID", "");

            Form.SetValues(clearSet);

            PartitionalSetQty.IsReadOnly = true;
            ProductData.Clear();
        }

        private void RawSelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectRaw();
        }

        private void RawClearButton_Click(object sender, RoutedEventArgs e)
        {
            var clearRaw = new Dictionary<string, string>();
            clearRaw.CheckAdd("RAW_ID", "");
            clearRaw.CheckAdd("RAW_NAME", "");
            clearRaw.CheckAdd("RAW_CATEGORY_ID", "");
            clearRaw.CheckAdd("RAW_TASK_QTY", "");
            clearRaw.CheckAdd("RAW_STOCK_QTY", "");

            Form.SetValues(clearRaw);
            RawData.Clear();
        }

        private void PartitionalSetName_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectPartitionalSet();
        }

        private void RawName_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectRaw();
        }

        private void PartitionalSetQty_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcRawReqirements();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }
    }
}
