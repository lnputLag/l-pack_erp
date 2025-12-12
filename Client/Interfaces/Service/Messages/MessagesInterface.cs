using Client.Common;

namespace Client.Interfaces.Messages
{
    /// <summary>
    /// интерфейс "сообщения"
    /// </summary>
    public class MessagesInterface
    {
        public MessagesInterface()
        {
            Central.WM.AddTab("messages", "Сообщения");

            Central.WM.AddTab<EmailTab>("messages");          
            Central.WM.AddTab<SmsTab>("messages");

            //var notificationView = new NotificationView();
            //Central.WM.AddTab("Messages_NotificationAdd", "Уведомление", false, "messages", notificationView);

            Central.WM.SetActive("EmailTab");
        }
    }
}
