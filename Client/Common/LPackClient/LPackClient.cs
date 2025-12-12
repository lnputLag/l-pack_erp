using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Xml.Linq;

namespace Client.Common
{
    /// <summary>
    /// клиентская библиотека для отправки запросов к серверу
    /// (документация в конце файла)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>3</version>
    public class LPackClient
    {
        public LPackClient()
        {
            SystemThreadResume = true;
            Connections = new List<LPackClientConnection>();
            CurrentConnection = null;
            Session = new LPackClientSession();
            Timeout = 30000;
            var rnd = new Random();
            ClientId = $"{rnd.Next(111111, 999999)}";
            ConnectingHopping = false;
            ConnectingTimeout = false;
            ConnectingInProgress = false;
            OnlineMode = false;
            Parameters = new LPackClientParameters();
        }

        /// <summary>
        /// Список подключений. Берется из конфиг-файла. Клиент может переключиться на
        /// другое подключение, когда получает несколько ошибок подряд на текущем подключении.
        /// </summary>
        public List<LPackClientConnection> Connections { get; set; }

        /// <summary>
        /// Текущее подключение.
        /// </summary>
        public LPackClientConnection CurrentConnection { get; set; }

        /// <summary>
        /// Данные текущей сессии.
        /// </summary>
        public LPackClientSession Session { get; set; }

        /// <summary>
        /// Таймаут ожидания ответа.
        /// Перекрывает значение Request.Timeout
        /// При необходимости поднять таймауты всех запросов, нужно установить это значение.
        /// </summary>
        public int Timeout { get; set; }

        public LPackClientQuery LoginQuery { get;set;}

        public string ClientId { get; set; }

        public bool ConnectingHopping { get; set; }

        /// <summary>
        /// флаг "нет связи"
        /// Когда поднят, все прикладные запросы переходят в цикл ожидания.
        /// Цикл ожидания длится, пока не опустится флаг.
        /// </summary>
        public bool ConnectingTimeout { get; set; }

        public bool ConnectingInProgress { get; set; }

        /// <summary>
        /// глобальный флаг разрешения запросов
        /// при старте false, поднимается, после всех инициализаций
        /// (если что-то будет отправлять запрос до инициализации, он не пройдет)
        /// </summary>
        public bool OnlineMode { get; set; }

        public LPackClientParameters Parameters { get; set; }

        /// <summary>
        /// Инициализация. Создается список подключений из данных конфигурации.
        /// </summary>
        /// <param name="config"></param>
        public void Init(LPackConfig config)
        {
            if(config != null)
            {
                if(config.ServerAddresses.Count > 0)
                {
                    Connections.Clear();

                    int connectionsCount = 0;
                    foreach(string s in config.ServerAddresses)
                    {
                        var c = new LPackClientConnection();
                        if(!string.IsNullOrEmpty(s))
                        {
                            var uri = new Uri(s);
                            c.Host=uri.Host;
                            c.Port=uri.Port;
                            c.Login=config.Login;
                            c.Password=config.Password;
                            c.Enabled=false;
                            c.Enabled=true;
                        }

                        //добавляем в список подключений
                        Connections.Add(c);
                        connectionsCount++;

                        //первое подключение ставим текущим
                        string a = "";
                        if(CurrentConnection == null)
                        {
                            CurrentConnection=c;
                            //Timeout=c.Timeout;
                            a="*";
                        }

                        Central.Dbg($"Connection [{c.Host}:{c.Port}] {a}");
                    }
                }

                if(!string.IsNullOrEmpty(config.Login) && !string.IsNullOrEmpty(config.Password))
                {
                    Session.Login=config.Login;
                    Session.Password=config.Password;
                }

                InitSystemRequest();
            }

            ServicePointManager.DefaultConnectionLimit = 5;
        }

        public void UpdateStatusString(LPackClientQuery q, string s, int mode=1)
        {
            var today = DateTime.Now.ToString("mm:ss");
            var system = "";
            if(q.SystemQuery)
            {
                system = "s";
            }
            s = $"{today} <{q.RequestId}{system}> {s}";

            if(mode == 1)
            {
                CurrentConnection.DebugStatusString = s;
                CurrentConnection.DebugStatusStringLog = CurrentConnection.DebugStatusStringLog.Append(s, true);
                CurrentConnection.DebugStatusStringLog = CurrentConnection.DebugStatusStringLog.Crop(1000);
            }

            if(mode == 2)
            {
                CurrentConnection.DebugStatusString2 = s;
                CurrentConnection.DebugStatusString2Log = CurrentConnection.DebugStatusString2Log.Append(s, true);
                CurrentConnection.DebugStatusString2Log = CurrentConnection.DebugStatusString2Log.Crop(1000);
            }

        }

        public void ProcessTimeout(bool flag)
        {
            ConnectingTimeout = flag;
            //if(flag == false)
            //{
            //    DoSystemRequest();
            //}
        }

        private Thread SystemThread { get; set; }
        public void InitSystemRequest()
        {
            if(SystemThread == null)
            {
                SystemThread = new Thread(SystemThreadWorker);
                SystemThread.Start();
            }
        }

        public void Destroy()
        {
            SystemThreadResume = false;
            try
            {
                if(SystemThread != null)
                {
                    SystemThread.Join();
                    SystemThread.Abort();
                }
            }
            catch(Exception e)
            {

            }
        }

        private bool SystemThreadResume { get; set; }
        private void SystemThreadWorker()
        {
            DoSystemRequestFaultCounter = 0;
            DoSystemRequestFaultLimit = Central.Parameters.DoSystemRequestFaultLimit;

            int runInterval = 5000;
            int sleepInterval = 1000;
            var r1 = DateTime.Now;
            var r0 = r1;
            SystemThreadResume = true;
            var stopCounter = 0;
            var stopLimit = 10;
            while(SystemThreadResume)
            {
                try
                {
                    var doWork = true;
                    //if(Central.MainWindow == null)
                    //{
                    //    //resume = false;
                    //    doWork = false;
                    //    stopCounter++;
                    //}

                    //if(stopCounter > stopLimit)
                    //{
                    //    resume = false;
                    //}

                    if(doWork)
                    {
                        runInterval = Central.Parameters.HopControlIntervalSlow;
                        if(ConnectingTimeout)
                        {
                            runInterval = Central.Parameters.HopControlIntervalFast;
                        }
                        Central.Parameters.HopControlInterval = runInterval;

                        var runFlag = false;
                        {
                            r1 = DateTime.Now;
                            var dt = (int)(((TimeSpan)(r1 - r0)).TotalMilliseconds);
                            if(dt > runInterval)
                            {
                                r0 = r1;
                                runFlag = true;
                            }
                        }

                        if(runFlag)
                        {
                            if(Central.Parameters.HopMode == 2)
                            {
                                DoSystemRequest();
                            }
                        }
                        else
                        {
                            Thread.Sleep(sleepInterval);
                        }
                    }
                }
                catch(Exception e)
                {

                }
            }
        }

        private int DoSystemRequestFaultCounter {  get; set; }
        private int DoSystemRequestFaultLimit { get; set; }
        private LPackClientQuery SystemRequest { get; set; }
        private void DoSystemRequest()
        {
            SystemRequest = new LPackClientQuery();
            
            var q = SystemRequest;
            q.SystemQuery = true;
            q.Request.SetParam("Module", "Session");
            q.Request.SetParam("Object", "Info");
            q.Request.SetParam("Action", "GetVersion");
            q.Request.Timeout = Central.Parameters.RequestTimeoutSystem; 

            q.DoRawQuery(false);
            UpdateStatusString(q, $"пинг =>[{q.Answer.Status}]({q.Answer.Time})  f=[{DoSystemRequestFaultCounter}]", 2);
            if(q.Answer.Status == 0)
            {
                ConnectingTimeout = false;

                var result = JObject.Parse(q.Answer.Data);

                var statusFlag = (string)result["TechnicalWorkStatusFlag"];

                if (statusFlag.ToInt() == 1)
                {
                    GalaSoft.MvvmLight.Messaging.Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "All",
                        ReceiverName = "",
                        SenderName = "LPackClient",
                        Action = "TechnicalWorkStatusOn",
                        Message = (string)result["TechnicalWorkStatusFlagInfo"] ?? string.Empty
                    });
                } 
                else if (statusFlag.ToInt() == 0)
                {
                    GalaSoft.MvvmLight.Messaging.Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "All",
                        ReceiverName = "",
                        SenderName = "LPackClient",
                        Action = "TechnicalWorkStatusOff",
                        Message = "0"
                    });
                }

            }
            else
            {
                ConnectingTimeout = true;
                DoSystemRequestFaultCounter++;
                if(DoSystemRequestFaultCounter >= DoSystemRequestFaultLimit)
                {
                    DoSystemHop2();
                }
            }
        }

        [Obsolete]
        private void DoSystemHop1()
        {
            var connection = Central.LPackClient.CurrentConnection;
            if(connection != null)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Session");
                q.Request.SetParam("Object", "Info");
                q.Request.SetParam("Action", "GetVersion");

                q.Request.Timeout = Central.Parameters.RequestTimeoutSystem;
                q.Request.Attempts = 10;

                DoHop(true, q);
            }
        }

        private void DoSystemHop2()
        {
            //смена сервера
            var q = SystemRequest;
            var hopComplete = false;
            var selectThis = false;
            if(Connections.Count > 0)
            {
                //выбор следующего коннекта
                if(!hopComplete)
                {
                    foreach(LPackClientConnection c in Connections)
                    {
                        if(c.Enabled)
                        {
                            if(selectThis)
                            {
                                CurrentConnection = c;
                                DoSystemRequestFaultCounter = 0;
                                hopComplete = true;
                                selectThis = false;
                                UpdateStatusString(q, $"хоп => [{CurrentConnection.Host}]", 2);
                                break;
                            }

                            //флаг, следующее соединение будет выбрано
                            if(c.Host == CurrentConnection.Host)
                            {
                                selectThis = true;
                            }
                        }
                    }
                }

                //если до сих пор не выбрано, выберем первое активное
                if(!hopComplete)
                {
                    foreach(LPackClientConnection c in Connections)
                    {
                        if(c.Enabled)
                        {
                            CurrentConnection = c;
                            DoSystemRequestFaultCounter = 0;
                            hopComplete = true;
                            selectThis = false;
                            UpdateStatusString(q, $"хоп => [{CurrentConnection.Host}]", 2);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Отправляет запрос на авторизацию.
        /// Возвращает true, если авторизация прошла успешно
        /// </summary>
        /// <returns></returns>
        public bool DoLogin(string login="", string password="", bool? saveLogin = false)
        {
            bool result = false;
            bool resume=true;

            if(resume)
            {
                if(!login.IsNullOrEmpty())
                {
                    Session.Login=login;
                }
                if(!password.IsNullOrEmpty())
                {
                    Session.Password=password;
                }
            }

            if(resume)
            {
                if(Session.Login.IsNullOrEmpty())
                {
                    resume=false;
                }
            }

            if(resume)
            {
                var p=new Dictionary<string,string>();
                {
                    p.CheckAdd("LOGIN",     Session.Login);
                    p.CheckAdd("PASSWORD",  Session.Password);
                    p.CheckAdd("PASSWORD",  Session.Password);
                    p.CheckAdd("APPLICATION_NAME", "l-pack_erp");
                    p.CheckAdd("FACTORY_ID", Central.Config.FactoryId.ToString());

                    p.CheckAdd("TODAY", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                    p.CheckAdd("UPTIME", Central.GetUptime().ToString());
                    p.CheckAdd("VERSION", Central.Version.ToString());
                    p.CheckAdd("COUNTER", "1");

                    var systemInfo=Central.GetSystemInfo();
                    p.AddRange(systemInfo);

                    p.CheckAdd("CLIENT_CONFIG_CONTENT",Central.ConfigContent.ToString());
                }

                LoginQuery = new LPackClientQuery();
                LoginQuery.Request.SetParam("Module","Session");
                LoginQuery.Request.SetParam("Object","Auth");
                LoginQuery.Request.SetParam("Action","Login3");
                LoginQuery.Request.SetParams(p);
                
                LoginQuery.Request.Attempts = 3;
                LoginQuery.Request.Timeout=Central.Parameters.RequestTimeoutSystem;

                LoginQuery.DoRawQuery(false);

                if(LoginQuery.Answer.Status == 0)
                {
                    var data = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(LoginQuery.Answer.Data);
                    if (data != null)
                    {
                        {
                            var ds = ListDataSet.Create(data, "ITEMS");
                            var row=ds.GetFirstItem();
                            var token=row.CheckGet("TOKEN");
                            if(!token.IsNullOrEmpty())
                            {
                                Session.Token=token;
                                Session.Sid=row.CheckGet("SID");

                                if (saveLogin == true)
                                {
                                    CheckAndCreateTextLogin(Session.Login);
                                }

                                result =true;
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Записывает/перезаписывает логин в конфигурацию если был поставлен флаг (Сохранить логин)
        /// </summary>
        /// <param name="login">Логин пользователя</param>
        private void CheckAndCreateTextLogin(string login)
        {
            string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "application.config");

            try
            {
                XDocument doc = XDocument.Load(configFilePath);
                XElement configElement = doc.Element("Config");

                if (configElement != null)
                {
                    configElement.SetElementValue("Login", login);

                    doc.Save(configFilePath);
                }
                else
                {
                    Central.Dbg($"Элемент Config не найден");
                }
            } 
            catch (Exception ex)
            {
                Central.Dbg($"Ошибка при загрузке файла конфигурации: {ex.Message}");
            }
        }

        /// <summary>
        /// элементарный запрос, чтобы проверить работоспособность сервера
        /// применяется перед хопом
        /// </summary>
        /// <param name="login"></param>
        /// <param name="password"></param>
        /// <returns></returns>        
        [Obsolete]
        public bool DoCheckServer()
        {
            bool result = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module","Session");
            q.Request.SetParam("Object","Info");
            q.Request.SetParam("Action","GetVersion");
            q.Request.Attempts = 1;
            q.Request.Timeout= Central.Parameters.RequestTimeoutSystem;
            q.DoRawQuery(false);
            if(q.Answer.Status == 0)
            {
                var serverInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(q.Answer.Data);

                if(serverInfo.Count>0)
                {
                    result=true;
                }

                if( serverInfo.CheckGet("ServerActivityLabel").ToString() == "closed" )
                {
                    result=false;
                }                
            }
            return result;
        }

        /// <summary>
        /// переход на другой сервер
        /// </summary>
        public void ChangeServer()
        {
            if(Central.Parameters.HopMode == 2)
            {
                DoSystemHop2();
            }
            else
            {
                DoSystemHop1();                
            }

            DoSystemRequest();
            Central.LoadServerInfo();
            
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "All",
                ReceiverName = "",
                SenderName = "MainWindow",
                Action = "PollUser",
            });

            /*
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "All",
                ReceiverName = "",
                SenderName = "MainWindow",
                Action = "UpdateTitle",
            });

            GalaSoft.MvvmLight.Messaging.Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "All",
                ReceiverName = "",
                SenderName = "MainWindow",
                Action = "UpdateServerLabel",
            });
            */
        }

        [Obsolete]
        public bool DoHop(bool forceHop, LPackClientQuery q)
        {
            q.DoHopLog = "";
            q.DoHopLog = q.DoHopLog.Append($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss ffffff")} CLIENT_DO_HOP: force=[{forceHop}] Request=[{q.Module}>{q.Object}>{q.Action}] Rid=[{q.RequestId}] Status=[{q.Answer.Status}] Time=[{q.Time}] Timeout=[{q.Timeout}] Attempts=[{q.RepeatCounter}/{q.Request.Attempts}]", true);

            bool result = false;
            int hopAttemptsProcessed = 0;

            {
                bool doHop = false;
                bool checkResult = false;
                ConnectingHopping = true;

                /*
                    так вот неудачным считается, запрос с ошибкой транспорта
                    когда сервер в принципе не ответил
                    этот факт мы проверяем элементарным запросом
                    если элементарный запрос прошел, значит сервер работает,
                    (он не выполняет конкретный запрос, а не в принципе сломан,
                    это не является неудачной попыткой)
                */

                if(!doHop)
                {
                    if(forceHop)
                    {
                        doHop = true;
                    }
                }

                if(!doHop)
                {
                    bool resume = true;
                    while(resume)
                    {
                        ConnectingInProgress = true;
                        checkResult = DoCheckServer();
                        LogQueryInfo(q);
                        q.DoHopLog = q.DoHopLog.Append($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss ffffff")} CLIENT_DO_HOP: {DoCheckServerLog}");

                        {
                            q.DoHopLog = q.DoHopLog.Append($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss ffffff")} CLIENT_DO_HOP: DoCheckServer ({CurrentConnection.AttemptsFailed}/{CurrentConnection.AttemptsFailedMax}) result=[{checkResult}]", true);

                            var s = "Client:DoHop:CheckServer ";
                            s = $"{s} [{CurrentConnection.Host}:{CurrentConnection.Port}]";
                            s = $"{s} attempts=[{CurrentConnection.AttemptsFailed}]";
                            s = $"{s} max=[{CurrentConnection.AttemptsFailedMax}]";
                            s = $"{s} checkResult=[{checkResult}]";
                            
                        }

                        if(resume)
                        {
                            if(!checkResult)
                            {
                                CurrentConnection.AttemptsFailed++;
                            }
                            else
                            {
                                resume = false;
                                doHop = false;
                                result = true;
                                q.DoHopLog = q.DoHopLog.Append($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss ffffff")} CLIENT_DO_HOP: server ready, no hop, ret", true);
                            }
                        }

                        if(resume)
                        {
                            if(CurrentConnection.AttemptsFailed >= CurrentConnection.AttemptsFailedMax)
                            {
                                resume = false;
                                doHop = true;
                                q.DoHopLog = q.DoHopLog.Append($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss ffffff")} CLIENT_DO_HOP: it will be hop", true);
                            }
                        }
                    }
                    ConnectingInProgress = false;
                }

                if(doHop)
                {
                    CurrentConnection.AttemptsFailed = 0;
                }

                if(doHop)
                {
                    var resume = true;
                    var hopAttempts = 0;
                    while(resume)
                    {
                        hopAttempts++;
                        hopAttemptsProcessed = hopAttempts;
                        bool selectThis = false;
                        bool hopComplete = false;

                        q.UpdateStatusString($"хоп: {hopAttempts}");
                        q.DoHopLog = q.DoHopLog.Append($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss ffffff")} CLIENT_DO_HOP: attempt {hopAttempts}", true);

                        if(Connections.Count > 0)
                        {
                            //выбираем следующее соединение
                            foreach(LPackClientConnection c in Connections)
                            {
                                if(c.Enabled)
                                {
                                    if(selectThis && !hopComplete)
                                    {
                                        CurrentConnection = c;
                                        hopComplete = true;
                                        selectThis = false;
                                        break;
                                    }

                                    //взводим флаг, следующее соединение будет выбрано
                                    if(c.Host == CurrentConnection.Host)
                                    {
                                        selectThis = true;
                                    }
                                }
                            }

                            //если до сих пор не выбрано, выберем первое активное
                            if(!hopComplete)
                            {
                                foreach(LPackClientConnection c in Connections)
                                {
                                    if(c.Enabled)
                                    {
                                        CurrentConnection = c;
                                        hopComplete = true;
                                        selectThis = false;
                                        break;
                                    }
                                }
                            }
                        }

                        if(hopComplete)
                        {
                            result = true;
                            {
                                var txt = $"{CurrentConnection.Host}:{CurrentConnection.Port}";
                                q.DoHopLog = q.DoHopLog.Append($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss ffffff")} CLIENT_DO_HOP: selected [{txt}]", true);
                            }
                        }

                        {
                            q.DoHopLog = q.DoHopLog.Append($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss ffffff")} CLIENT_DO_HOP: DoCheckServer [{CurrentConnection.Host}:{CurrentConnection.Port}]", true);
                            var checkResult2 = DoCheckServer();
                            LogQueryInfo(q);
                            q.DoHopLog = q.DoHopLog.Append($"{DoCheckServerLog}");

                            if(checkResult2)
                            {
                                //сервер ответил, выход
                                resume = false;
                                result = true;
                                q.DoHopLog = q.DoHopLog.Append($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss ffffff")} CLIENT_DO_HOP: server ready, hop complete, ret", true);
                                q.UpdateStatusString($"хоп: {CurrentConnection.Host}");
                            }
                            else
                            {
                                q.DoHopLog = q.DoHopLog.Append($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss ffffff")} CLIENT_DO_HOP: server is not ready, select next", true);

                                if(hopAttempts >= 1)
                                {
                                    //check others
                                    //DoCheckServer2("192.168.3.60", q);
                                    //DoCheckServer2("192.168.3.49", q);
                                    //DoCheckServer2("192.168.3.184", q);
                                    //DoCheckServer2("192.168.3.204", q);
                                }
                            }
                        }

                        if(resume)
                        {
                            //там большие проблемы, не теребим сервер часто, пусть разгребется с запросами
                            if(hopAttempts >= 10 && hopAttempts < 100)
                            {
                                Thread.Sleep(3000);
                            }

                            if(hopAttempts >= 100)
                            {
                                Thread.Sleep(10000);
                            }
                        }

                        if(resume)
                        {
                            if(hopAttempts > 20)
                            {
                                resume = false;
                            }
                        }
                    }
                }
                ConnectingHopping = false;
            }
            return result;
        }

        private string DoCheckServerLog { get; set; }

        public void LogQueryInfo(LPackClientQuery q)
        {
            var s = "";
            s = s.Append($" Module=[{q.Request.Params.CheckGet("Module")}]");
            s = s.Append($" Object=[{q.Request.Params.CheckGet("Object")}]");
            s = s.Append($" Action=[{q.Request.Params.CheckGet("Action")}]");
            s = s.Append($" Status=[{q.Answer.Status}]");
            s = s.Append($" Timeout=[{q.Timeout}]");
            s = s.Append($" Time=[{q.Time}]");
            s = s.Append($" Rid=[{q.RequestId}]");
            s = s.Append($" Oid=[{q.Oid}]");

            if(q.Answer.Status != 0)
            {
                s = s.Append($" Code=[{q.Answer.Error.Code}]");
                s = s.Append($" Message=[{q.Answer.Error.Message}]");
                s = s.Append($" Description=[{q.Answer.Error.Description}]");
            }

            LogMsg($"LOG_QUERY_INFO {s}");
        }

        public void LogMsg(string text, bool addCr = false, int offset = 0)
        {
            Central.Logger.Trace(text);
            //Central.Dbg(text);
        }

        /// <summary>
        /// Отображение информации об ошибке.
        /// </summary>
        /// <param name="q"></param>
        public void ProcessError(LPackClientQuery q)
        {
            if(q.Answer.Status!=0)
            {
                var error = new Error();
                error.Code=q.Answer.Error.Code;
                error.Message=q.Answer.Error.Message;
                error.Description=q.Answer.Error.Description;
                Central.ProcError(error,"",true,q);
            }
        }

        public static void SendErrorReport(string message, string description = "", LPackClientQuery q = null)
        {
            var error = new Error();
            error.Code = 145;
            error.Message = message;
            error.Description = description;

            if (q == null)
            {
                q = new LPackClientQuery();
            }
            q.SilentErrorProcess = true;

            Central.ProcError(error, "", true, q);
        }

        public string GetError(LPackClientQuery q)
        {
            var result = "";
            if(q.Answer.Status != 0)
            {
                var error = new Error();
                error.Code = q.Answer.Error.Code;
                error.Message = q.Answer.Error.Message;
                error.Description = q.Answer.Error.Description;

                result = $"({error.Code}) {error.Message} {error.Description}";
            }
            return result;
        }
    }


    /*     
        Архитектура клиентской библиотеки.

        Клиентская библиотека состоит из следущих объектов:
        (1) LPackClient -- клиент
        (2) LPackClientQuery -- запрос (и его вспомогательные структуры)
        (3) LPackClientDataProvider -- хелпер для создания шаблонных запросов

        1.  LPackClient
            Центральная часть библиотеки. LPackClient является синглтоном,
            он является членом Central.
            LPackClient содержит реестр подключений и балансирует между ними.
            
            Алгоритм балансировки следующий.
            Выбирается первое подключение из списка и используется для отправки запросов.
            Одно подключение -- один сервер (все серверы равнозначны).
            Если несколько раз подряд возникает ошибка с даннам сервером,
            выбирается следующее подключение из списка и так далее по кругу.
            LPackClientConnection.AttemptsFailedMax -- лимит ошибок по подключению

            Клиент содержит сервисные методы:
            DoLogin -- осуществляет инициализацию сессии
            DoHop -- осуществляет смену сервера


        2.  LPackClientQuery
            Объект запроса и методы работы с данными запроса.
            Запрос содержит параметры запроса и данные ответа.
            Все что используется для выполнения зпроса сконцентрировано в объекте запроса
            (это позволяет выполнять запрос внутри await).

            Мы выделяем 3 типа ответа:
            LPackClientAnswer.AnswerTypeRef:
                Data -- обычный ответ application/json, парсим обрабатываем
                    в переменной Data возвращается структура ответа

                File -- загрузка файла, сохраняем массив байтов в файл
                    данные пишутся бинарно во временный файл:
                    downloadFilePath -- путь ко временному файлу (данные сохраняются в него)
                    downloadFileName -- оригинальное имя файла
                                    
                Stream -- поток данных
                    данные пишутся в поток:

            Тип ответа определяется автоматически путем анализа заголовков ответа (Data, File)
            Также есть возможность установить тип ответа вручную, установив Request.RequiredAnswerType.
            Например, мы отправляем запрос на скачивание файла, но  хотим сохранить его
            содержимое в оперативной памяти (Stream)

            

        3.  LPackClientDataProvider
            Хелпер содержит типовые методы для отправки запросов:
            DoQueryRawResult -- запрос с получением сырого результата в виде строки
            DoQueryDeserialize -- запрос и десериализация результата в объект указанного типа

        Примеры использования.
        
        Вручную:

            var q = new LPackClientQuery();
            q.Request.SetParam("Module","Session");
            q.Request.SetParam("Object","Info");
            q.Request.SetParam("Action","GetVersion");
            
            await Task.Run(()=>{ 
                q.DoQuery();
            });

            if(q.Answer.Status==0)
            {
                result=q.Answer.Data;
            }
            else
            {
                q.ProcessError();
            }
     */
}
