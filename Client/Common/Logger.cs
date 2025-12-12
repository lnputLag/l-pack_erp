using System;

namespace Client.Common
{
    /// <summary>
    /// логгер отладочных сообщений
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>    
    public class Logger
    {
        public Logger()
        {
            Log="";
            MaxLen=1000000;
        }

        public string Log { get; set;}
        public int MaxLen { get;set;}

        public enum LoggerMessageTypeRef
        {
            Trace=1,
            Info=2,
            Debug=3,            
            Error=4,
            Fatal=5,
        }
        
        public void Message(string text, LoggerMessageTypeRef type=LoggerMessageTypeRef.Debug)
        {
            if(Central.Parameters.GlobalLogging)
            {
                var typeString="";
                switch(type)
                {
                    case LoggerMessageTypeRef.Trace:
                        typeString="TRACE";
                        break;
                    case LoggerMessageTypeRef.Info:
                        typeString="INFO";
                        break;
                    case LoggerMessageTypeRef.Debug:
                        typeString="DEBUG";
                        break;
                    case LoggerMessageTypeRef.Error:
                        typeString="ERROR";
                        break;
                    case LoggerMessageTypeRef.Fatal:
                        typeString="FATAL";
                        break;
                }

                typeString=typeString.PadLeft(5);
            
                var today=DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss_ffffff");

                Log=$"{Log}\n{today}|{typeString}|{text}";

                if(MaxLen>0)
                {
                    if(Log.Length>MaxLen)
                    {
                        Log="";
                    }
                }
            }

            Central.Dbg(text);
        }

            
        public void Trace(string text)
        {
            Message(text,LoggerMessageTypeRef.Trace);
        }
        public void Info(string text)
        {
            Message(text,LoggerMessageTypeRef.Info);
        }
        public void Debug(string text)
        {
            Message(text,LoggerMessageTypeRef.Debug);
        }
        public void Error(string text)
        {
            Message(text,LoggerMessageTypeRef.Error);
        }
        public void Fatal(string text)
        {
            Message(text,LoggerMessageTypeRef.Fatal);
        }

    }
}
