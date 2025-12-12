using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Чат по образцу
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class SampleChat : UserControl
    {
        public SampleChat()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);
            Central.Msg.Register(ProcessNewMessages);

            InitForm();
            SetDefaults();
        }

        /// <summary>
        /// ИД образца 
        /// </summary>
        public int ObjectId;

        /// <summary>
        /// Имя вкладки, откуда открыт чат 
        /// </summary>
        public string ReceiverName;

        public ListDataSet MessageDS { get; set; }

        private Dictionary<string, string> LastMessage { get; set; }
        /// <summary>
        /// Код пользователя как получателя сообщений для чата с клиентом
        /// </summary>
        public int Recipient;

        /// <summary>
        /// Список идентификаторов пользователей получателей вводимого сообщения
        /// </summary>
        public string RecipientIdList;
        /// <summary>
        /// Имя вкладки
        /// </summary>
        private string TabName;
        /// <summary>
        /// Флаг закрытия формы. Если false, после сохранения сообщения перезагружаем все сообщения чата
        /// </summary>
        private bool CloseForm;
        /// <summary>
        /// Признак чата по образцу, у которого стоит признак отсутствия сырья. Или чат при приемке образца.
        /// Если признака нет, кнопка альтернатив не отображается
        /// </summary>
        public int RawMissingFlag;

        /// <summary>
        /// Тип чата: 0 - чат с клиентом, 1 - чат с коллегами
        /// </summary>
        public int ChatType;
        /// <summary>
        /// Объект, по которому ведется чат: TechMap - техкарта, PriceCalc - расчет цены, Tender - тендер, Sample - образец
        /// </summary>
        public string ChatObject;
        /// <summary>
        /// ID чата
        /// </summary>
        public int ChatId;
        /// <summary>
        /// Признак отправки нового сообщения чата на почту клиенту
        /// </summary>
        private bool ChatEmailFlag = false;

        /// <summary>
        /// Форма редактирования
        /// </summary>
        FormHelper Form { get; set; }

        public Dictionary<string, string> RecipientList;

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
                    Path="MESSAGE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=Message,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        
                    },
                },
                new FormHelperField()
                {
                    Path="EMAIL",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=SendToEmailCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{

                    },
                },
                new FormHelperField()
                {
                    Path="SET_UNREAD",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=UnReadCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{

                    },
                },
                new FormHelperField()
                {
                    Path="MESSAGE_RECIPIENT",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=RecipientSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{

                    },
                },
                new FormHelperField()
                {
                    Path="RECIPIENT_LIST",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=RecipientListTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{

                    },
                },
            };
            Form.SetFields(fields);
        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        private void SetDefaults()
        {
            ObjectId = 0;
            ChatId = 0;
            ChatObject = "Sample";
            ReceiverName = "";
            RecipientList = new Dictionary<string, string>()
            {
                { "0", "" },
                { "6",  "технолог" },
                { "23", "конструктор" },
                { "32", "менеджер" },
            };

            RecipientSelectBox.Items = RecipientList;
            RecipientSelectBox.SetSelectedItemByKey("0");
            LastMessage = new Dictionary<string, string>();
            CloseForm = false;
            RawMissingFlag = 0;
            ChatType = 0;
        }

        /// <summary>
        /// деструктор
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Sample",
                ReceiverName = "",
                SenderName = "SampleChat",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);

            if (!ReceiverName.IsNullOrEmpty())
            {
                Central.WM.SetActive(ReceiverName, true);
                ReceiverName = "";
            }
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CloseForm = false;

                if (
                    Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)
                    ||
                    Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)
                )
                {
                    // [Ctrl] + [Enter] или  [Shift] + [Enter]
                    var caretIndex = Message.CaretIndex;
                    Message.Text = Message.Text.Insert(caretIndex, Environment.NewLine);

                    Message.CaretIndex = caretIndex + 1;
                    e.Handled = true;
                }
                else
                {
                    // Enter
                    SendMessage();
                }
            }
        }

        /// <summary>
        /// Обработка сообщений
        /// </summary>
        /// <param name="obj"></param>
        private void ProcessMessages(ItemMessage obj)
        {
            if (obj.ReceiverGroup.IndexOf("PreproductionSample") > -1)
            {
                if (obj.ReceiverName.IndexOf(TabName) > -1)
                {
                    if (obj.Action == "CardboardSelected")
                    {
                        var v = (Dictionary<string, string>)obj.ContextObject;
                        SetAlternatives(v);
                    }
                }
            }
        }

        /// <summary>
        /// Обработка сообщений во встроенной шине
        /// </summary>
        /// <param name="obj"></param>
        private void ProcessNewMessages(ItemMessage obj)
        {
            if (obj.ReceiverGroup.IndexOf("ChatMessage") > -1)
            {
                if (obj.ReceiverName.IndexOf(TabName) > -1)
                {
                    if (obj.Action == "SetRecipients")
                    {
                        var v = (Dictionary<string, string>)obj.ContextObject;
                        SetRecipients(v);
                    }
                }
            }
        }

        /// <summary>
        /// Заполнение блока сообщений чата
        /// </summary>
        /// <param name="ds">датасет с сообщениями</param>
        private void FillChatBlock(ListDataSet ds)
        {
            int i = 0;
            // очистка содержимого
            ChatMessageContainer.Children.Clear();
            ChatMessageContainer.RowDefinitions.Clear();

            foreach (var mes in ds.Items)
            {
                ChatMessageContainer.RowDefinitions.Add(new RowDefinition());
                // "DTTM" - дата
                // "SENDER" - отправитель
                // "SENDER_TYPE" - тип отправителя: 0 - Л-ПАК, 1 - клиент
                // "TXT" - сообщение
                // "RECIPIENT" - получатель: 0 - все, 1 - технолог, 2 - конструктор, 3 - менеджер
                // "READ" - сообщение прочитано
                // "WOGR_ID" - код группы пользователей: 0 - все, 6 - технологи, 23 - конструкторы, 32 - все менеджеры
                int col = mes.CheckGet("SENDER_TYPE").ToInt() == 0 ? 1 : 0;
                var msg = new SampleChatRow();
                string header = $"{mes.CheckGet("DTTM")} {mes.CheckGet("SENDER")}";
                var emplName = mes.CheckGet("FULL_NAME");
                if ((col == 1) && (!string.IsNullOrEmpty(emplName)))
                {
                    header = $"{header} ({emplName})";
                }
                msg.User.Text = header;
                if (ChatType == 1)
                {
                    msg.Recipients.Text = $"Кому: {mes.CheckGet("RECIPIENT")}";
                }
                else
                {
                    msg.RecipientBlock.Visibility = Visibility.Collapsed;
                }
                msg.Message.Text = mes.CheckGet("TXT");

                ChatMessageContainer.Children.Add(msg);
                Grid.SetRow(msg, i);
                Grid.SetColumn(msg, col);
                Grid.SetColumnSpan(msg, 2);
                i++;

                // После выхода из цикла будем знать, информацию по последнему сообщению
                LastMessage = mes;
            }

            // Настройки контролов в зависимости от последнего сообщения в чате с клиентом
            if (ChatType == 0)
            {
                // Если отправитель последнего сообщения Л-ПАК, блокируем чекбокс Оставить непрочитанным
                if (LastMessage.CheckGet("SENDER_TYPE").ToInt() == 0)
                {
                    UnReadCheckBox.IsEnabled = false;
                }
                else
                {
                    //Если сообщение от клиента не прочитано, ставим чекбокс Оставить непрочитанным
                    UnReadCheckBox.IsChecked = !LastMessage.CheckGet("READ").ToBool();
                }

                // Если заполнен получатель, выберем его в селектбоксе
                int recipient = LastMessage.CheckGet("WOGR_ID").ToInt();
                int recipientCode = LastMessage.CheckGet("RECIPIENT").ToInt();
                if (recipient > 0 || recipientCode > 0)
                {
                    if (recipient == 0)
                    {
                        // Ключ списка выбора получателей - ID группы
                        switch (recipientCode)
                        {
                            // технологи
                            case 1:
                                recipient = 6;
                                break;
                            // конструкторы
                            case 2:
                                recipient = 23;
                                break;
                            // менеджеры
                            case 3:
                                recipient = 32;
                                break;
                        }
                    }
                    RecipientSelectBox.SetSelectedItemByKey(recipient.ToString());
                }
            }
        }

        /// <summary>
        /// Получение данных о сообщениях чата
        /// </summary>
        /// <param name="p"></param>
        private async void GetMessageList(Dictionary<string, string> p)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Messages");
            q.Request.SetParam("Object", "ChatMessage");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    // Специальные настройки чата:
                    // CHAT_EMAIL_FLAG - предустановленный флаг отправки клиенту на email
                    var refDict = ListDataSet.Create(result, "REF");
                    if (refDict != null)
                    {
                        if (refDict.Items.Count > 0)
                        {
                            ChatEmailFlag = refDict.Items[0].CheckGet("CHAT_EMAIL_FLAG").ToBool();
                            SendToEmailCheckBox.IsChecked = ChatEmailFlag;
                        }
                    }

                    MessageDS = ListDataSet.Create(result, "MESSAGES");
                    FillChatBlock(MessageDS);
                }

            }
        }

        /// <summary>
        /// Привязка к образцу нового внутреннего чата
        /// </summary>
        private async void CreateNewChat()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Messages");
            q.Request.SetParam("Object", "ChatMessage");
            q.Request.SetParam("Action", "CreateChat");
            q.Request.SetParam("CHAT_OBJECT", ChatObject);
            q.Request.SetParam("OBJECT_ID", ObjectId.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    if (ds.Items.Count > 0)
                    {
                        ChatId = ds.Items[0].CheckGet("ChatId").ToInt();
                    }
                }

            }
        }

        /// <summary>
        /// Получение данных из БД
        /// </summary>
        private async void GetData()
        {
            var p = new Dictionary<string, string>()
            {
                { "CHAT_ID", ChatId.ToString() },
                { "CHAT_TYPE", ChatType.ToString() },
                { "CHAT_OBJECT", ChatObject },
            };

            if (ChatType == 0)
            {
                if (ChatObject == "TechMap")
                {
                    if (ChatId > 0)
                    {
                        GetMessageList(p);
                    }
                    else
                    {
                        CreateNewChat();
                    }
                }
                else
                {
                    if (ObjectId > 0)
                    {
                        p["CHAT_ID"] = ObjectId.ToString();
                        GetMessageList(p);
                    }
                }
            }
            else
            {
                if (ChatId > 0)
                {
                    GetMessageList(p);
                }
                else
                {
                    CreateNewChat();
                }
            }
        }

        public void Edit()
        {
            GetData();
            Show();
        }

        /// <summary>
        /// Добавление нового сообщения в чат.
        /// Отправляет сообщение на сервер, в ответе получает новый список сообщений
        /// </summary>
        private async void SendMessage()
        {
            // Если сообщение состоит только из переносов, не отправляем его
            string clearMessage = Message.Text;
            clearMessage = clearMessage.Replace("\n", "").Replace("\r", "");
            if (string.IsNullOrEmpty(clearMessage))
            {
                Message.Text = "";
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Messages");
            q.Request.SetParam("Object", "ChatMessage");
            q.Request.SetParam("Action", "Save");

            var p = Form.GetValues();
            p.Add("CHAT_ID", ChatId.ToString());
            p.Add("CHAT_OBJECT", ChatObject);
            p.Add("CHAT_TYPE", ChatType.ToString());
            if (ChatType == 0 && ChatObject!="TechMap")
            {
                p["CHAT_ID"] = ObjectId.ToString();
            }
            p["RECIPIENT_LIST"] = RecipientIdList;
            
            if(ChatObject == "TechMap")
            {
                p["SENDER_TYPE"]="1";
                p["RECIPIENT_TYPE"] = "2";
            }
            
            // Вместо фамилий передаем список идентификаторов

            q.Request.SetParams(p);

            q.Request.SetParams(LastMessage);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Message.Text = "";
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (!CloseForm)
                    {
                        RecipientListTextBox.Text = "";
                        RecipientIdList = "";
                        var ds = ListDataSet.Create(result, "MESSAGES");
                        FillChatBlock(ds);
                    }
                }
            }
        }

        /// <summary>
        /// Обновляет флаг прочитанных сообщений для ТК ЛТ
        /// </summary>
        private async void UpdateUnread()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Messages");
            q.Request.SetParam("Object", "ChatMessage");
            q.Request.SetParam("Action", "UpdateUnread");

            q.Request.SetParam("ID", ChatId.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Message.Text = "";
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
            }
        }

        public void Show()
        {
            TabName = $"{ChatObject}Chat_{ObjectId}";
            // Показывать кнопку Альтернатива
            if (RawMissingFlag == 0)
            {
                AlternativeButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                AlternativeButton.Visibility = Visibility.Visible;
            }

            // Тип чата. Если чат с клиентом, то скрывать блок с получателями.
            // Если чат с коллегами, то скрывать нижний блок

            if (ChatType == 0)
            {
                InnerRecipientBlock.Visibility = Visibility.Collapsed;
            }
            else if (ChatType == 1)
            {
                RecipientLabel.Visibility = Visibility.Collapsed;
                RecipientSelectBox.Visibility = Visibility.Collapsed;
            }

            if (ChatObject == "TechMap")
            {
                RecipientLabel.Visibility = Visibility.Collapsed;
                RecipientSelectBox.Visibility = Visibility.Collapsed;
            }

            Central.WM.AddTab(TabName, $"Чат по образцу {ObjectId}", true, "add", this);
        }

        /// <summary>
        /// Отправка данных на сервер и закрытие вкладки
        /// </summary>
        public void Close()
        {
            CloseForm = true;
            // Если в последнем сообщении ничего не содержится, значит никаких сообщений не было
            bool filledMessageBlock = LastMessage.Count > 0;
            bool newMessage = true;

            // Проверяем поле сообщения перед закрытием. Если поле не пустое, предлагаем сохранить собщение
            string clearMessage = Message.Text;
            clearMessage = clearMessage.Replace("\n", "").Replace("\r", "");
            if (!string.IsNullOrEmpty(clearMessage))
            {
                var dw = new DialogWindow("Поле с текстом сообщения не пустое. Отправить сообщение?", "Чат по образцу", "", DialogWindowButtons.NoYes);
                if (!(bool)dw.ShowDialog())
                {
                    Message.Text = "";
                    newMessage = false;
                }
            }
            else
            {
                Message.Text = "";
                newMessage = false;
            }

            if (filledMessageBlock || newMessage)
            {
                SendMessage();
            }

            if (ChatObject == "TechMap" && UnReadCheckBox.IsChecked == false)
            {
                UpdateUnread();
                Messenger.Default.Send(new ItemMessage()
                {
                    ReceiverGroup = "MoldedContainerTechCard",
                    ReceiverName = ReceiverName,
                    SenderName = "SampleChat",
                    Action = "Refresh",
                });
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "MoldedContainerTechCard",
                    ReceiverName = ReceiverName,
                    SenderName = "SampleChat",
                    Action = "Refresh",
                });
            }

            Central.WM.RemoveTab(TabName);
            //Отправляем сообщение на обновление таблицы, из которой вызван чат, чтобы обновить отметки непрочитанных сообщений
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "PreproductionSample",
                ReceiverName = ReceiverName,
                SenderName = "SampleChat",
                Action = "Refresh",
            });
            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverGroup = "PreproductionSample",
                ReceiverName = ReceiverName,
                SenderName = "SampleChat",
                Action = "Refresh",
            });
            Destroy();
        }

        /// <summary>
        /// Получает необходимые данные и вызывает форму выбора картона для образца
        /// </summary>
        private async void GetAlternatives()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "GetRecord");
            q.Request.SetParam("ID", ObjectId.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "SampleRec");
                    if (ds.Items.Count > 0)
                    {
                        var rec = ds.Items[0];
                        int profileId = rec.CheckGet("PROFILE_ID").ToInt();
                        int markId = rec.CheckGet("MARK_ID").ToInt();
                        int colorId = rec.CheckGet("COLOR_OUTER_ID").ToInt();

                        if ((profileId > 0) && (markId > 0) && (colorId > 0))
                        {
                            var selectCardboard = new SampleSelectCardboard();
                            var p = new Dictionary<string, string>()
                            {
                                { "PROFILE",  profileId.ToString() },
                                { "MARK",  markId.ToString() },
                                { "COLOR", colorId.ToString() },
                                { "CARDBOARD", "0" },
                            };
                            selectCardboard.ReceiverName = TabName;
                            selectCardboard.AnswerType = 1;
                            selectCardboard.Edit(p);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Добавляет полученный текст в окно ввода сообщения
        /// </summary>
        /// <param name="v"></param>
        private void SetAlternatives(Dictionary<string, string>  v)
        {
            Message.Text = $"{Message.Text}\n{v.CheckGet("ALTERNATIVES")}";
        }

        private void SetRecipients(Dictionary<string, string> recipients)
        {
            RecipientListTextBox.Text = recipients["FULL_NAMES"];
            RecipientIdList = recipients["ID_LIST"];
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void ReceipientSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            int newRecipient = RecipientSelectBox.SelectedItem.Key.ToInt();
            if ((newRecipient != 0) && (newRecipient != Recipient) && (ChatObject != "TechMap"))
            {
                UnReadCheckBox.IsChecked = true;
                UnReadCheckBox.IsEnabled = false;
            }
            else
            {
                UnReadCheckBox.IsEnabled = true;
            }
        }

        private bool _autoScroll = true;
        /// <summary>
        /// автоматическая прокрутка списка к низу
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange == 0)
            {
                _autoScroll = SampleChatContainer.VerticalOffset == SampleChatContainer.ScrollableHeight;
            }

            if (_autoScroll && e.ExtentHeightChange != 0)
            {
                SampleChatContainer.ScrollToVerticalOffset(SampleChatContainer.ExtentHeight);
            }
        }

        private void Alternative_Click(object sender, RoutedEventArgs e)
        {
            GetAlternatives();
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            var printChat = new SampleChatPrint();
            printChat.MessageList = MessageDS.Items;
            printChat.SampleName = ObjectId.ToString();
            printChat.Make();
        }

        private void RecipientButton_Click(object sender, RoutedEventArgs e)
        {
            var recipientListForm = new SampleChatRecipient();
            recipientListForm.ReturnTabName = TabName;
            recipientListForm.ChatObject = ChatObject;
            recipientListForm.Show();

        }
    }
}
