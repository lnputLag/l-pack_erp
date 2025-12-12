using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Ручная отбраковка изделий оператором
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class ManuallyReject : ControlBase
    {
        /// <summary>
        /// Обязательные к заполнению переменные:
        /// ProductionTaskId;
        /// Не обязательные к заполнению переменные:
        /// ParentFrame;
        /// ProductionTaskNumber.
        /// ParentContainer
        /// </summary>
        public ManuallyReject()
        {
            ControlTitle = "Ручная отбраковка изделий";
            FrameName = "ManuallyReject";
            LogTableName = "corrugator_label";

            OnMessage = (ItemMessage message) => {
                DebugLog($"message=[{message.Message}]");
            };

            OnLoad = () =>
            {
                InitializeComponent();

                SecondProductGrid.Visibility = Visibility.Collapsed;

                ProcessPermissions();
                FormInit();
                SetDefaults();
                LoadItems();

                Messenger.Default.Register<ItemMessage>(this, _ProcessMessages);
                Central.Msg.Register(ProcessMessages);
            };

            OnUnload = () =>
            {
                Messenger.Default.Unregister<ItemMessage>(this);
                Central.Msg.UnRegister(ProcessMessages);
            };

            OnFocusGot = () =>
            {
            };

            OnFocusLost = () =>
            {
            };
        }

        /// <summary>
        /// Наименование таба, который вызвал этот таб
        /// </summary>
        public string ParentFrame { get; set; }

        public string ParentContainer { get; set; }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// форма с полями для фильтрации данных
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Датасет с данными для заполнения полей формы
        /// </summary>
        public ListDataSet FormDataSet { get; set; }

        /// <summary>
        /// Идентификатор производственного задания, для которого бракуется продукция
        /// </summary>
        public int ProductionTaskId { get; set; }

        /// <summary>
        /// Номер производственного задания
        /// </summary>
        public string ProductionTaskNumber { get; set; }

        /// <summary>
        /// Имя папки верхнего уровня, в которой хранятся лог файлы по работе стекера
        /// </summary>
        public string LogTableName { get; set; }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            // Проверяем уровень доступа
            var mode = Central.Navigator.GetRoleLevel(this.RoleName);
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(this.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.IsEnabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void FormInit()
        {
            //инициализация формы
            {
                Form = new FormHelper();

                //колонки формы
                var fields = new List<FormHelperField>()
                {
                    new FormHelperField()
                    {
                        Path="FIRST_PRODUCT_NAME",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=FirstProductNameTextBox,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="FIRST_QUANTITY_OF_REJECTED",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=FirstQuantityOfRejectedTextBox,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="FIRST_QUANTITY_BY_CORRUGATOR_MACHINE",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=FirstQuantityByCorrugatorMachineTextBox,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="FIRST_QUANTITY_BY_LABEL",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=FirstQuantityByLabelTextBox,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="FIRST_QUANTITY_TO_LAST_PALLET",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=FirstQuantityToLastPalletTextBox,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="FIRST_REJECTED_HISTORY",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=FirstRejectedHistoryTextBox,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="FIRST_BLOCKED_LAST_LABEL_PRINT_FLAG",
                        FieldType=FormHelperField.FieldTypeRef.Boolean,
                        Control=FirstBlockedLastLabelPrintFlagCheckBox,
                        ControlType="CheckBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="FIRST_QUANTITY_OF_REJECTED_OLD",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=null,
                        ControlType="void",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="FIRST_PRODUCT_ID",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=null,
                        ControlType="void",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },

                    new FormHelperField()
                    {
                        Path="SECOND_PRODUCT_NAME",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=SecondProductNameTextBox,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="SECOND_QUANTITY_OF_REJECTED",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=SecondQuantityOfRejectedTextBox,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="SECOND_QUANTITY_BY_CORRUGATOR_MACHINE",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=SecondQuantityByCorrugatorMachineTextBox,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="SECOND_QUANTITY_BY_LABEL",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=SecondQuantityByLabelTextBox,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="SECOND_QUANTITY_TO_LAST_PALLET",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=SecondQuantityToLastPalletTextBox,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="SECOND_REJECTED_HISTORY",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=SecondRejectedHistoryTextBox,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="SECOND_BLOCKED_LAST_LABEL_PRINT_FLAG",
                        FieldType=FormHelperField.FieldTypeRef.Boolean,
                        Control=SecondBlockedLastLabelPrintFlagCheckBox,
                        ControlType="CheckBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="SECOND_QUANTITY_OF_REJECTED_OLD",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=null,
                        ControlType="void",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="SECOND_PRODUCT_ID",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=null,
                        ControlType="void",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },

                    new FormHelperField()
                    {
                        Path="PRODUCTION_TASK_NUMBER",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=ProductionTaskNumbetTextBox,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                };
                Form.SetFields(fields);

                //после установки значений
                Form.AfterSet = (Dictionary<string, string> v) =>
                {
                    //фокус на кнопку обновления
                   // RefreshAccountsButton.Focus();
                };
            }
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            FormDataSet = new ListDataSet();
            Form.SetDefaults();
        }

        public void Refresh()
        {
            FormDataSet = new ListDataSet();
            Form.SetDefaults();
            LoadItems();
        }

        /// <summary>
        /// Получаем данные для заполнения полей формы
        /// </summary>
        public void LoadItems()
        {
            if (ProductionTaskId > 0)
            {
                DisableControls();

                SecondProductGrid.Visibility = Visibility.Collapsed;

                var p = new Dictionary<string, string>();
                p.Add("PRODUCTION_TASK_ID", ProductionTaskId.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ManuallyPrint");
                q.Request.SetParam("Action", "GetManuallyReject");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var dataSet = ListDataSet.Create(result, "ITEMS");
                        if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                        {

                            Dictionary<string, string> dictionary = new Dictionary<string, string>();

                            if (dataSet.Items.Count == 2)
                            {
                                var firstDictionary = dataSet.Items[0];
                                foreach (var item in firstDictionary)
                                {
                                    dictionary.Add($"FIRST_{item.Key}", item.Value);
                                }
                                firstDictionary.CheckAdd("FIRST_QUANTITY_OF_REJECTED_OLD", firstDictionary.CheckGet("FIRST_QUANTITY_OF_REJECTED"));

                                var secondDictionary = dataSet.Items[1];
                                foreach (var item in secondDictionary)
                                {
                                    dictionary.Add($"SECOND_{item.Key}", item.Value);
                                }
                                secondDictionary.CheckAdd("SECOND_QUANTITY_OF_REJECTED_OLD", secondDictionary.CheckGet("SECOND_QUANTITY_OF_REJECTED"));

                                SecondProductGrid.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                var firstDictionary = dataSet.Items[0];
                                foreach (var item in firstDictionary)
                                {
                                    dictionary.Add($"FIRST_{item.Key}", item.Value);
                                }
                                firstDictionary.CheckAdd("FIRST_QUANTITY_OF_REJECTED_OLD", firstDictionary.CheckGet("FIRST_QUANTITY_OF_REJECTED"));
                            }

                            dataSet.Items[0] = dictionary;
                            FormDataSet = dataSet;
                        }
                        else
                        {
                            FormDataSet = dataSet;
                        }

                        Form.SetValues(FormDataSet);
                        Form.SetValueByPath("PRODUCTION_TASK_NUMBER", ProductionTaskNumber);

                        LoadRejectHistory(Form.GetValueByPath("FIRST_PRODUCT_ID"), 1);
                        if (!string.IsNullOrEmpty(Form.GetValueByPath("SECOND_PRODUCT_ID")))
                        {
                            LoadRejectHistory(Form.GetValueByPath("SECOND_PRODUCT_ID"), 2);
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }

                EnableControls();
            }
            else
            {
                var d = new DialogWindow($"Не выбрано производственное задание", "Ручная отбраковка изделий", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Загружаем данные по истории отбраковки через кнопку
        /// </summary>
        public async void LoadRejectHistory(string productId, int formNumber)
        {
            var p = new Dictionary<string, string>();
            // 1=global,2=local,3=net
            p.Add("STORAGE_TYPE", "3");
            p.Add("TABLE_NAME", LogTableName);
            p.Add("TABLE_DIRECTORY", $"{ProductionTaskId}_{productId}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Service");
            q.Request.SetParam("Object", "LiteBase");
            q.Request.SetParam("Action", "List");
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
                    var ds = ListDataSet.Create(result, LogTableName);
                    if (ds != null && ds.Items != null && ds.Items.Count > 0)
                    {
                        List<Dictionary<string, string>> logList = new List<Dictionary<string, string>>();
                        logList = ds.Items.Where(x => x.CheckGet("PRODUCTION_TASK_ID_ID2").Contains("manually_reject") || x.CheckGet("PRODUCTION_TASK_ID_ID2").Contains("manually_block") || x.CheckGet("PRODUCTION_TASK_ID_ID2").Contains("manually_drop")).OrderBy(x => x.CheckGet("ON_DATE").ToDateTime()).ToList();

                        string logMsg = "";
                        foreach (var logItem in logList)
                        {
                            if (logItem.CheckGet("PRODUCTION_TASK_ID_ID2").Contains("manually_reject"))
                            {
                                logMsg = $"{logMsg}" +
                                $"[{logItem.CheckGet("ON_DATE")}] {logItem.CheckGet("MESSAGE").Substring(logItem.CheckGet("MESSAGE").IndexOf("Количество отбракованной продукции:"))}{Environment.NewLine}";
                            }
                            else if (logItem.CheckGet("PRODUCTION_TASK_ID_ID2").Contains("manually_drop"))
                            {
                                logMsg = $"{logMsg}" +
                               $"[{logItem.CheckGet("ON_DATE")}] {logItem.CheckGet("MESSAGE").Substring(logItem.CheckGet("MESSAGE").IndexOf("Количество сброшенной продукции:"))}{Environment.NewLine}";

                            }
                            else if (logItem.CheckGet("PRODUCTION_TASK_ID_ID2").Contains("manually_block"))
                            {
                                logMsg = $"{logMsg}" +
                                $"[{logItem.CheckGet("ON_DATE")}] Ручная блокировка по кнопке всех неотсканированных поддонов по текущему заданию.{Environment.NewLine}";
                            }
                            else
                            {
                                logMsg = $"{logMsg}" +
                                $"[{logItem.CheckGet("ON_DATE")}] {logItem.CheckGet("MESSAGE")}{Environment.NewLine}";
                            }
                        }

                        if (formNumber == 1)
                        {
                            Form.SetValueByPath("FIRST_REJECTED_HISTORY", logMsg);
                        }
                        else if (formNumber == 2)
                        {
                            Form.SetValueByPath("SECOND_REJECTED_HISTORY", logMsg);
                        }
                    }
                }
            }
            else
            {
                if (q.Answer.Error.Code == 7)
                {
                    var d = new DialogWindow($"Не удалось получить историю ручной отбраковки по кнопке. Пожалуйста, закройте и повторно откройте окно ручной отбраковки.", "Ручная отбраковка изделий", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
                else
                {
                    q.ProcessError();
                }
            }
        }

        /// <summary>
        /// Сохранение данных по ручной отбраковке продукции выбранного производственного задания
        /// </summary>
        public void Save()
        {
            if (ProductionTaskId > 0)
            {
                DisableControls();

                var values = Form.GetValues();
                if (values != null)
                {
                    var listOfParametrs = new List<Dictionary<string, string>>();
                    if (values.CheckGet("SECOND_PRODUCT_ID").ToInt() > 0)
                    {
                        {
                            var p = new Dictionary<string, string>();
                            p.Add("PRODUCTION_TASK_ID", ProductionTaskId.ToString());
                            p.Add("PRODUCT_ID", values.CheckGet("FIRST_PRODUCT_ID"));
                            p.Add("QUANTITY_OF_REJECTED", values.CheckGet("FIRST_QUANTITY_OF_REJECTED"));

                            if (values.CheckGet("FIRST_QUANTITY_OF_REJECTED").ToInt() > values.CheckGet("FIRST_QUANTITY_OF_REJECTED_OLD").ToInt())
                            {
                                p.Add("UNBLOCK_LAST_LABEL_PRINT", "1");
                            }

                            listOfParametrs.Add(p);
                        }

                        {
                            var p = new Dictionary<string, string>();
                            p.Add("PRODUCTION_TASK_ID", ProductionTaskId.ToString());
                            p.Add("PRODUCT_ID", values.CheckGet("SECOND_PRODUCT_ID"));
                            p.Add("QUANTITY_OF_REJECTED", values.CheckGet("SECOND_QUANTITY_OF_REJECTED"));

                            if (values.CheckGet("SECOND_QUANTITY_OF_REJECTED").ToInt() > values.CheckGet("SECOND_QUANTITY_OF_REJECTED_OLD").ToInt())
                            {
                                p.Add("UNBLOCK_LAST_LABEL_PRINT", "1");
                            }

                            listOfParametrs.Add(p);
                        }
                    }
                    else
                    {
                        var p = new Dictionary<string, string>();
                        p.Add("PRODUCTION_TASK_ID", ProductionTaskId.ToString());
                        p.Add("PRODUCT_ID", values.CheckGet("FIRST_PRODUCT_ID"));
                        p.Add("QUANTITY_OF_REJECTED", values.CheckGet("FIRST_QUANTITY_OF_REJECTED"));

                        if (values.CheckGet("FIRST_QUANTITY_OF_REJECTED").ToInt() < values.CheckGet("FIRST_QUANTITY_OF_REJECTED_OLD").ToInt())
                        {
                            p.Add("UNBLOCK_LAST_LABEL_PRINT", "1");
                        }

                        listOfParametrs.Add(p);
                    }

                    if (listOfParametrs != null && listOfParametrs.Count > 0)
                    {
                        foreach (var p in listOfParametrs)
                        {
                            {
                                var q = new LPackClientQuery();
                                q.Request.SetParam("Module", "Production");
                                q.Request.SetParam("Object", "ManuallyPrint");
                                q.Request.SetParam("Action", "UpdateManuallyReject");
                                q.Request.SetParams(p);

                                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                                q.DoQuery();

                                if (q.Answer.Status != 0)
                                {
                                    q.ProcessError();
                                }
                            }

                            if (p.CheckGet("UNBLOCK_LAST_LABEL_PRINT").ToInt() == 1)
                            {
                                p.CheckAdd("LAST_PALLET_BLOCKED_PRINT_FLAG", "0");

                                var q = new LPackClientQuery();
                                q.Request.SetParam("Module", "Production");
                                q.Request.SetParam("Object", "ManuallyPrint");
                                q.Request.SetParam("Action", "UpdateLastPalletBlockedPrintFlag");
                                q.Request.SetParams(p);

                                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                                q.DoQuery();

                                if (q.Answer.Status != 0)
                                {
                                    q.ProcessError();
                                }
                            }
                        }

                        Refresh();
                    }                  
                }

                EnableControls();
            }
            else
            {
                var d = new DialogWindow($"Не выбрано производственное задание", "Ручная отбраковка изделий", "", DialogWindowButtons.OK);
                d.ShowDialog();
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
            Central.WM.FrameMode = 1;

            var dt = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
            FrameName = $"{FrameName}_new_{dt}";

            if (Central.WM.TabItems.ContainsKey(ParentContainer))
            {
                Central.WM.Show(FrameName, $"{ControlTitle}", true, ParentContainer, this);
            }
            else
            {
                Central.WM.Show(FrameName, $"{ControlTitle}", true, "main", this);
            }
        }

        /// <summary>
        /// Деактивация контроллов
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
            MainGrid.IsEnabled = false;
        }

        /// <summary>
        /// Активация контроллов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
            MainGrid.IsEnabled = true;
        }

        /// <summary>
        /// обработка сообщений
        /// </summary>
        /// <param name="message"></param>
        public void ProcessMessages(ItemMessage message)
        {
            if (message != null)
            {
                if (message.SenderName == "WindowManager")
                {
                    switch (message.Action)
                    {
                        case "FocusGot":
                            break;

                        case "FocusLost":
                            break;
                    }
                }
            }
        }

        public void Close()
        {
            if (!string.IsNullOrEmpty(ParentFrame))
            {
                Central.WM.SetActive(ParentFrame, true);
            }

            Central.WM.Close(FrameName);
        }

        /// <summary>
        /// отображение справочной статьи
        /// (относительный путь)
        /// </summary>
        public void ShowHelp()
        {
            //FIXME: Нужно сделать документацию
            Central.ShowHelp("/doc/l-pack-erp/");
        }


        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void _ProcessMessages(ItemMessage m)
        {
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        private void FirstBlockedLastLabelPrintFlagCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (FirstBlockedLastLabelPrintFlagLabel != null)
            {
                FirstBlockedLastLabelPrintFlagLabel.Foreground = HColor.RedFG.ToBrush();
            }
        }

        private void FirstBlockedLastLabelPrintFlagCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (FirstBlockedLastLabelPrintFlagLabel != null)
            {
                FirstBlockedLastLabelPrintFlagLabel.Foreground = "#000000".ToBrush();
            }
        }

        private void SecondBlockedLastLabelPrintFlagCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (SecondBlockedLastLabelPrintFlagLabel != null)
            {
                SecondBlockedLastLabelPrintFlagLabel.Foreground = HColor.RedFG.ToBrush();
            }
        }

        private void SecondBlockedLastLabelPrintFlagCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (SecondBlockedLastLabelPrintFlagLabel != null)
            {
                SecondBlockedLastLabelPrintFlagLabel.Foreground = "#000000".ToBrush();
            }
        }

        private void ResreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }
    }
}
