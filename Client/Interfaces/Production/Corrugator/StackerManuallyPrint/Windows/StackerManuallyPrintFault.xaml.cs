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
    /// Отбраковка поддона для создание его перемещения в К0
    /// </summary>
    public partial class StackerManuallyPrintFault : ControlBase
    {
        /// <summary>
        /// Обязательные к заполнению поля:
        /// PalletId.
        /// Не обязательные к заполнению поля:
        /// PalletFullName.
        /// </summary>
        public StackerManuallyPrintFault()
        {
            ControlTitle = "Отбраковка поддона";
            FrameName = "StackerManuallyPrintFault";

            OnMessage = (ItemMessage message) => {
                DebugLog($"message=[{message.Message}]");
            };

            OnLoad = () =>
            {
                InitializeComponent();

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
        /// Техническое имя фрейма
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// Процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Полное наименование поддона (номер ПЗ + / + номер поддона).
        /// Приходит извне.
        /// </summary>
        public string PalletFullName { get; set; }

        /// <summary>
        /// Идентификатор поддона.
        /// Приходит извне.
        /// </summary>
        public int PalletId { get; set; }

        /// <summary>
        /// Этап производства, на котором был обнаружен брак.
        /// </summary>
        public int FaultStage { get; set; }

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
                        Path="PALLET_FULL_NUMBER",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=PalletFullNumberTextBox,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                    new FormHelperField()
                    {
                        Path="FAULT_TYPE",
                        FieldType=FormHelperField.FieldTypeRef.Integer,
                        Control=FaultTypeSelectBox,
                        ControlType="SelectBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                            { FormHelperField.FieldFilterRef.Required, null },
                        }
                    },
                    new FormHelperField()
                    {
                        Path="NOTE",
                        FieldType=FormHelperField.FieldTypeRef.String,
                        Control=NoteTextBox,
                        ControlType="TextBox",
                        Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        }
                    },
                };

                Form.SetFields(fields);
            }
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            Form.SetDefaults();
            Form.SetValueByPath("PALLET_FULL_NUMBER", PalletFullName);
        }

        /// <summary>
        /// Получаем список типов брака для этого этапа производства
        /// </summary>
        public void LoadItems()
        {
            if (FaultStage > 0)
            {
                DisableControls();

                var p = new Dictionary<string, string>();
                p.Add("STAGE", FaultStage.ToString());

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ManuallyPrint");
                q.Request.SetParam("Action", "ListFaultType");
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
                            FaultTypeSelectBox.SetItems(dataSet, "ID", "NAME");

                            if (FaultTypeSelectBox != null && FaultTypeSelectBox.Items != null && FaultTypeSelectBox.Items.Count > 0)
                            {
                                FaultTypeSelectBox.SelectedItem = FaultTypeSelectBox.Items.FirstOrDefault(x => x.Key.ToInt() == 10003);
                            }
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
                var d = new DialogWindow($"Не определён этап производства, на котором обнаружили брак. Пожалуйста, закройте и повторно откройте это окно.", "Отбраковка поддона", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        /// <summary>
        /// Сохраняем данные по отбраковке поддона
        /// </summary>
        public void Save()
        {
            if (Form.Validate())
            {
                DisableControls();

                var p = new Dictionary<string, string>();
                p.Add("PALLET_ID", PalletId.ToString());
                p.Add("STAGE", FaultStage.ToString());               
                p.Add("TYPE", Form.GetValueByPath("FAULT_TYPE"));
                p.Add("NOTE", Form.GetValueByPath("NOTE"));

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "ManuallyPrint");
                q.Request.SetParam("Action", "MoveToComplectation");
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
                            if (dataSet.Items.First().CheckGet("PALLET_ID").ToInt() > 0)
                            {
                                Close();
                            }
                        }
                    }
                }
                else
                {
                    q.ProcessError();
                }

                EnableControls();
            }
        }

        /// <summary>
        /// Деактивация контроллов
        /// </summary>
        public void DisableControls()
        {
            if (FormToolbar != null)
            {
                FormToolbar.IsEnabled = false;
            }
        }

        /// <summary>
        /// Активация контроллов
        /// </summary>
        public void EnableControls()
        {
            if (FormToolbar != null)
            {
                FormToolbar.IsEnabled = true;
            }
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

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void _ProcessMessages(ItemMessage m)
        {
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
            this.MinHeight = 150;
            this.MinWidth = 400;

            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            Central.WM.Show(FrameName, ControlTitle, true, "add", this, "top", windowParametrs);
        }

        public void Close()
        {
            Central.WM.Close(FrameName);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
