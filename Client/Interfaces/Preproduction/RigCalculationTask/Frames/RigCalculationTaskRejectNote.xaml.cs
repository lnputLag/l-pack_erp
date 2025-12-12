using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Окно редактирования примечания
    /// </summary>
    public partial class RigCalculationTaskRejectNote : UserControl
    {
        public RigCalculationTaskRejectNote()
        {
            InitializeComponent();
            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// форма
        /// </summary>
        public FormHelper Form { get; set; }
        /// <summary>
        /// Название получателя сообщения
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// ID расчета оснастки
        /// </summary>
        int RigCalcTaskId;
        /// <summary>
        /// Имя вкладки с комментарием
        /// </summary>
        string TabName;

        public Dictionary<string, string> TaskValues { get; set; }

        /// <summary>
        /// инициализация формы
        /// </summary>
        private void InitForm()
        {
            Form = new FormHelper();
            //список полей формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ID",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Note,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ROLE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=null,
                    ControlType="void",
                },
            };
            Form.SetFields(fields);
            Form.StatusControl = FormStatus;
        }

        /// <summary>
        /// установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            Form.SetDefaults();
            ReceiverName = "";
            TabName = "";
            FormStatus.Text = "";
            TaskValues = new Dictionary<string, string>();
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverGroup = "Preproduction",
                ReceiverName = "",
                SenderName = "RejectNote",
                Action = "Closed",
            });

            if (!ReceiverName.IsNullOrEmpty())
            {
                Central.WM.SetActive(ReceiverName, true);
                ReceiverName = "";
            }
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;

                case Key.Enter:
                    Save();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// редактирование
        /// </summary>
        public void Edit(Dictionary<string, string> values)
        {
            TaskValues = values;
            Form.SetValues(values);
            RigCalcTaskId = values.CheckGet("ID").ToInt();
            TabName = $"RigCalcTaskNote_{RigCalcTaskId}";
            Show();
        }

        /// <summary>
        /// Показывает окно
        /// </summary>
        private void Show()
        {
            Central.WM.AddTab(TabName, $"Отмена расчета {RigCalcTaskId}", true, "add", this);
        }

        /// <summary>
        /// Создаем сообщение в чате с причиной отказа в расчете оснастки
        /// </summary>
        private async void SendChatMessage()
        {
            bool error = false;

            int chatId = TaskValues.CheckGet("CHAT_ID").ToInt();
            if (chatId > 0)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Messages");
                q.Request.SetParam("Object", "ChatMessage");
                q.Request.SetParam("Action", "Save");

                var p = new Dictionary<string, string>();
                p.Add("CHAT_ID", chatId.ToString());
                p.Add("CHAT_OBJECT", "PriceCalc");
                p.Add("CHAT_TYPE", "1");
                p.Add("MESSAGE", Note.Text);
                p.Add("EMAIL", "1");
                p.Add("RECIPIENT_LIST", TaskValues.CheckGet("MANAGER_EMPL_ID"));

                q.Request.SetParams(p);

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var dataAnswer = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (dataAnswer != null)
                    {
                        if (!dataAnswer.ContainsKey("MESSAGES"))
                        {
                            error = true;
                        }
                    }
                    else
                    {
                        error = true;
                    }
                }
                else
                {
                    error = true;
                }

                if (error)
                {
                    var dw = new DialogWindow("Не удалось создать сообщение в чате.\nСоздайте, пожалуйста, сообщение с причиной отмены расчета", "Отмена расчета оснастки");
                    dw.ShowDialog();
                }
            }
        }

        /// <summary>
        /// Передаем комментарий с причиной отмены оснастки в основное окно
        /// </summary>
        public void Save()
        {
            if (!Note.Text.IsNullOrEmpty())
            {
                SendChatMessage();
                //отправляем сообщение о закрытии окна
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "Preproduction",
                    ReceiverName = ReceiverName,
                    SenderName = "RejectNote",
                    Action = "SetReject",
                    ContextObject = Form.GetValues(),
                });
                Close();
            }
            else
            {
                Form.SetStatus("Заполните причину отказа", 1);
            }
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Close()
        {
            Central.WM.RemoveTab(TabName);

            Destroy();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }
    }
}
