using System;

namespace Client.Common
{
    /// <summary>
    /// структура сообщения
    /// (шина передачи сообщений)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public class ItemMessage
    {
        /// <summary>
        /// Группа получателей: "All" -- для всех или имя группы (например: "Tasks")
        /// </summary>
        public string ReceiverGroup { get; set; }
        /// <summary>
        /// Имя объекта-получателя
        /// </summary>
        public string ReceiverName { get; set; }
        /// <summary>
        /// Имя объекта-отпаравителя
        /// </summary>
        public string SenderName { get; set; }
        /// <summary>
        /// Текст сообщения
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Текст сообщения
        /// </summary>
        public string Action { get; set; }

        public object ContextObject { get; set; }
        
        public DateTime OnDate { get; set; }

        public ItemMessage()
        {
            ReceiverGroup = "All";
            ReceiverName = "";
            SenderName = "";
            Message = "";
            Action = "";
            ContextObject=null;
            OnDate = DateTime.Today;

            //Central.Dbg($"MESSAGE: Construct RcvGroup={ReceiverGroup} RcvName={ReceiverName} SenderName={SenderName} Action={Action} Msg={Message}");

        }
    }
}
