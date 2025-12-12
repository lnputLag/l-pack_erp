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
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Окно чата с клиентом по заявке
    /// </summary>
    public partial class PreproductionConfirmOrderChat : ControlBase
    {
        /// <summary>
        /// Конструктор окна чата с клиентом по заявке
        /// Обязательные к заполнению переменные:
        /// OrderId;
        /// CustomerEmail;
        /// OrderNumber.
        /// </summary>
        public PreproductionConfirmOrderChat()
        {
            ControlTitle = "Чат по заявке";
            InitializeComponent();

            OnMessage = (ItemMessage message) => {
                DebugLog($"message=[{message.Message}]");
            };

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                FormInit();
                SetDefaults();
                ChatGridLoadItems();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
            };
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        /// <summary>
        /// Ид заявки на гофропроизводство.
        /// naklrashodz.nsthet
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Номер заявки
        /// </summary>
        public string OrderNumber { get; set; }

        /// <summary>
        /// Электронная почта покупателя
        /// </summary>
        public string CustomerEmail { get; set; }

        /// <summary>
        /// Датасет с данными чата с клиентом по выбранной заявке
        /// </summary>
        public ListDataSet ChatGridDataSet { get; set; }

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
                        Path = "EMAIL_FLAG",
                        FieldType = FormHelperField.FieldTypeRef.Boolean,
                        Control = EmailCheckBox,
                        ControlType = "CheckBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "UNREAD_FLAG",
                        FieldType = FormHelperField.FieldTypeRef.Boolean,
                        Control = UnreadCheckBox,
                        ControlType = "CheckBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                    new FormHelperField()
                    {
                        Path = "MESSAGE",
                        FieldType = FormHelperField.FieldTypeRef.String,
                        Control = MessageTextBox,
                        ControlType = "TextBox",
                        Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
                        },
                    },
                };

                Form.SetFields(fields);
            }
        }

        public void SetDefaults()
        {
            TitleLabel.Content = $"{TitleLabel.Content}{OrderNumber}";
            MessageTextBox.Focus();
        }

        /// <summary>
        /// Заполняем грид перепиской
        /// </summary>
        public void ChatGridFillItems()
        {
            int i = 0;
            // очистка содержимого
            ChatGrid.Children.Clear();
            ChatGrid.RowDefinitions.Clear();

            if (ChatGridDataSet != null && ChatGridDataSet.Items != null && ChatGridDataSet.Items.Count > 0)
            {
                foreach (var chatGridItem in ChatGridDataSet.Items)
                {
                    ChatGrid.RowDefinitions.Add(new RowDefinition());
                    int messageColumn = chatGridItem.CheckGet("MESSAGE_SENDER").ToInt() == 0 ? 1 : 0;
                    var messageRow = new SampleChatRow();
                    messageRow.User.Text = $"{chatGridItem.CheckGet("MESSAGE_DTTM")} {chatGridItem.CheckGet("MESSAGE_SENDER_NAME")}";
                    messageRow.Message.Text = chatGridItem.CheckGet("MESSAGE_TEXT");

                    ChatGrid.Children.Add(messageRow);
                    Grid.SetRow(messageRow, i);
                    Grid.SetColumn(messageRow, messageColumn);
                    Grid.SetColumnSpan(messageRow, 2);
                    i++;
                }
            }
        }

        /// <summary>
        /// Поулчаем переписку
        /// </summary>
        public async void ChatGridLoadItems()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.Add("ORDER_ID", OrderId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ConfirmOrder");
            q.Request.SetParam("Action", "GetChat");
            q.Request.SetParams(p);
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
                    ChatGridDataSet = ListDataSet.Create(result, "ITEMS");
                    ChatGridFillItems();
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        public async void Save()
        {
            if (!string.IsNullOrEmpty(Form.GetValueByPath("MESSAGE")))
            {
                DisableControls();

                var p = new Dictionary<string, string>();
                p.Add("ORDER_ID", OrderId.ToString());
                p.Add("MESSAGE", Form.GetValueByPath("MESSAGE"));
                p.Add("EMAIL_FLAG", Form.GetValueByPath("EMAIL_FLAG").ToInt().ToString());
                p.Add("ORDER_NUMBER", OrderNumber);
                p.Add("CUSTOMER_EMAIL", CustomerEmail);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "ConfirmOrder");
                q.Request.SetParam("Action", "SendChatMessage");
                q.Request.SetParams(p);
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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        if (ds != null && ds.Items != null && ds.Items.Count > 0)
                        {
                            if (ds.Items.First().CheckGet("ORDER_ID").ToInt() > 0)
                            {
                                Form.SetValueByPath("MESSAGE", "");
                                ChatGridLoadItems();
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
                string msg = $"Нельзя отправить пустое сообщение.";
                var d = new DialogWindow($"{msg}", $"Чат по заявке {OrderId}", "", DialogWindowButtons.OK);
                d.ShowDialog();
            }
        }

        public void ReadChatMessage()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.Add("ORDER_ID", OrderId.ToString());
            p.Add("READ_FLAG", "1");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "ConfirmOrder");
            q.Request.SetParam("Action", "UpdateChatOrderRead");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status != 0)
            {
                q.ProcessError();
            }

            EnableControls();
        }

        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
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
            var frameName = $"{ControlName}_{OrderId}";
            Central.WM.Show(frameName, $"Чат по заявке {OrderId}", true, "main", this);
        }

        public void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "help":
                        {
                            Central.ShowHelp("/doc/l-pack-erp/preproduction/preproduction_confirm_order/");
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            if (!string.IsNullOrEmpty(Form.GetValueByPath("MESSAGE")))
            {
                var message = $"Поле с текстом сообщения не пустое." +
                            $"{Environment.NewLine}Отправить сообщение?";
                var d = new DialogWindow($"{message}", $"Чат по заявке {OrderId}", "", DialogWindowButtons.YesNo);
                if (d.ShowDialog() == true)
                {
                    Save();
                }
            }

            if (Form.GetValueByPath("UNREAD_FLAG").ToInt() == 0)
            {
                ReadChatMessage();
            }

            // Отправляем сообщение табу список заявок на гофропроизводство для подтверждения о необходимости обновить грид
            {
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "Preproduction",
                    ReceiverName = "PreproductionConfirmOrderList",
                    SenderName = "PreproductionConfirmOrderChat",
                    Action = "Refresh",
                });
            }

            var frameName = $"{ControlName}_{OrderId}";
            Central.WM.Close(frameName);
        }

        /// <summary>
        /// Проверяем текущее набираемое сообщение, если оно не пустое, то разрешаем отправку сообщения
        /// </summary>
        public void CheckCurrentMessage()
        {
            if (!string.IsNullOrEmpty(Form.GetValueByPath("MESSAGE")) && FormToolbar.IsEnabled)
            {
                SaveButton.IsEnabled = true;
            }
            else
            {
                SaveButton.IsEnabled = false;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessCommand("help");
        }

        private void MessageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckCurrentMessage();
        }

        private void СhatContainer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Автоматическая прокрутка в конец чата

            bool autoScroll = true;
            if (e.ExtentHeightChange == 0)
            {
                autoScroll = СhatContainer.VerticalOffset == СhatContainer.ScrollableHeight;
            }

            if (autoScroll && e.ExtentHeightChange != 0)
            {
                СhatContainer.ScrollToVerticalOffset(СhatContainer.ExtentHeight);
            }
        }
    }
}
