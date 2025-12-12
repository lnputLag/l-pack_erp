using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Client.Common
{
    public class Msg
    {
        public Msg()
        {
            MessageProcessList=new List<ProcessMessageDelegate>();
            Label="Msg";
        }


        private string Label {get;set;}
        public delegate void ProcessMessageDelegate(ItemMessage message);
        public ProcessMessageDelegate ProcessMessage;
        public virtual void ProcessMessageAction(ItemMessage message)
        {

        }

        private List<ProcessMessageDelegate> MessageProcessList {get;set;}

        public void AddProcessor(ProcessMessageDelegate processor)
        {
            lock (MessageProcessList)
            {
                MessageProcessList.Add(processor);
            }
        }
        
        public void RemoveProcessor(ProcessMessageDelegate processor)
        {
            lock (MessageProcessList)
            {
                while (MessageProcessList.Remove(processor)) ;
            }
        }

        public void Register(ProcessMessageDelegate processor)
        {
            AddProcessor(processor);
        }
        
        public void UnRegister(ProcessMessageDelegate processor)
        {
            RemoveProcessor(processor);
        }

        public void SendMessage(ItemMessage message)
        {
            Central.Dbg($"{Label}: SendMessage action=[{message.Action}] message=[{message.Message}] sender=[{message.SenderName}] receiver=[{message.ReceiverName}] ");
            if(MessageProcessList.Count > 0)
            {
                lock (MessageProcessList)
                {
                    var list=new List<ProcessMessageDelegate>(MessageProcessList);
                    foreach (ProcessMessageDelegate processor in list)
                    {              
                        processor.Invoke(message);
                    }
                }
            }
        }
    }
}
