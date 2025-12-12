using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Рекомендация технолога для операторов БДМ
    /// </summary>
    /// <author>greshnyh_ni</author>
    public partial class OperatorsLogDecision : ControlBase
    {

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        // номер станка
        public int MachineId { get; set; }
        // ИД записи
        public int IdLogbook { get; set; }


        public OperatorsLogDecision()
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    ProcessMessage(msg);
                }
            };


            OnLoad = () =>
            {

                DataGet();

                // получение прав пользователя
                ProcessPermissions();
            };

            double nScale = 1.5;
            GridParent.LayoutTransform = new ScaleTransform(nScale, nScale);

        }

        /// <summary>
        /// обработка правил доступа
        /// </summary>
        private void ProcessPermissions(string roleCode = "")
        {
            string role = "";
            // Проверяем уровень доступа
            if (MachineId == 716)
            {
                role = "[erp]bdm1_operators_log";
            }
            else
            {
                role = "[erp]bdm2_operators_log";
            }

            var mode = Central.Navigator.GetRoleLevel(role);
            var userAccessMode = mode;

            switch (mode)
            {
                case Role.AccessMode.Special:
                    {
                        if (MachineId == 716)
                            EditButton.IsEnabled = false;
                        else
                            // для БДМ2 все пользователи вносят изменения
                            EditButton.IsEnabled = true;
                    }
                    break;

                case Role.AccessMode.FullAccess:
                    EditButton.IsEnabled = true;
                    break;

                case Role.AccessMode.ReadOnly:
                    EditButton.IsEnabled = false;
                    break;
            }
        }

        /// <summary>
        /// Обработка общих сообщений
        /// </summary>
        /// <param name="action"></param>
        /// <param name="obj"></param>
        private void ProcessMessage(ItemMessage obj = null)
        {
            string action = obj.Action;
            switch (action)
            {
                case "RefreshDecision":
                    DataGet();
                    break;
            }
        }

        /// <summary>
        /// получаем данные по рекомендациям от технологов
        /// </summary>
        private void DataGet()
        {
            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ST", MachineId.ToString());
                p.CheckAdd("IS_DIRECTION", "1");
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "Monitoring");
            q.Request.SetParam("Action", "DecisionGet");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var res = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (res != null)
                {
                    var ds = ListDataSet.Create(res, "ITEMS");
                    DecisionTxt.Text = ds.Items[0].CheckGet("DECISION").ToString();
                    IdLogbook = ds.Items[0].CheckGet("ID").ToInt();
                }
            }
            else
            {
                q.ProcessError();
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку справки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void ShowHelp()
        {
            Central.ShowHelp("/doc/l-pack-erp/production/MoldedContainer_report");
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = "OperatorsLogDecision",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
        }

        /// <summary>
        /// обработка ввода с клавиатуры (роли)
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
            }
        }

        // редактируем запись
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            DataGet();
            var logbookDecisionRecord = new LogbookDecisionRecord(IdLogbook, DecisionTxt.Text);
            logbookDecisionRecord.ReceiverName = ControlName;
            logbookDecisionRecord.Edit();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            DataGet();
        }
    }
}
