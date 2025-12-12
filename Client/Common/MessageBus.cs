using System.Collections.Generic;

namespace Client.Common
{
    /// <summary>
    /// шина сообщений
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-01-16</released>
    /// <changed>2023-01-19</changed>
    public class MessageBus
    {
        public MessageBus()
        {
            Messages=new Queue<MessageItem>();
            Processors=new List<MessageProcessor>();

            //ProcessorTimer = new ActionTimer();
            //ProcessorTimer.Interval = 100;
            //ProcessorTimer.OnTimerTick = () =>
            //{
            //    ProcessMessages();
            //};
            //ProcessorTimer.Start();
        }

        private Queue<MessageItem> Messages {get;set;}
        private List<MessageProcessor> Processors {get;set;}
        private ActionTimer ProcessorTimer { get;set;}

        public void Init()
        {
            Messages.Clear();
            Processors.Clear();
        }

        public void AddProcessor(MessageProcessor p)
        {
             Processors.Add(p);
        }

        public void SendMessage(MessageItem m)
        {
            lock(Messages)
            {
                Messages.Enqueue(m);
            }
            ProcessMessages();
        }

        private void ProcessMessages()
        {
            if(Messages.Count > 0)
            {
                if(Processors.Count > 0)
                {
                    lock(Messages)
                    {
                        var m=Messages.Dequeue();
                        foreach(MessageProcessor p in Processors)
                        {
                            if(p.Enabled)
                            {
                                p.OnMessage?.Invoke(m);
                            }                            
                        }
                    }
                }
            }
        }
    }

    public class MessageProcessor
    {
        public MessageProcessor(OnMessageDelegate onMessage, bool timer=false)
        {
            Enabled=true;
            Timer = timer;
            OnMessage =onMessage;
        }

        public bool Enabled {get;set;}
        public bool Timer { get; set; }

        public delegate void OnMessageDelegate(MessageItem m);
        public OnMessageDelegate OnMessage;
        public virtual void OnMessageAction(MessageItem m)
        {
        }
    }


    /// <summary>
    /// структура сообщения
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2019-11-06</released>
    /// <changed>2023-01-16</changed>
    public class MessageItem
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
        /// Имя объекта-отправителя 
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
        /// <summary>
        /// передаваемый объект
        /// </summary>
        public object ContextObject { get; set; }

        public MessageItem()
        {
            ReceiverGroup = "All";
            ReceiverName = "";
            SenderName = "";
            Message = "";
            Action = "";
            ContextObject=null;
        }

        public Dictionary<string,string> GetDict()
        {
            var result=new Dictionary<string,string>() 
            { 
                {"RECEIVER_GROUP",   ReceiverGroup.ToString()},
                {"RECEIVER_NAME",    ReceiverName.ToString()},
                {"SENDER_NAME",      SenderName.ToString()},
                {"MESSAGE",         Message.ToString()},
                {"ACTION",          Action.ToString()},
            };

            return result;
        }
    }
}
