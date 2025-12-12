using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Client.Common
{
    /// <summary>
    /// Объект запроса. Основной рабочий объект.
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>3</version>
    public class LPackClientQuery
    {
        /// <summary>
        /// Структура запроса
        /// </summary>
        public LPackClientRequest Request { get; set; }

        /// <summary>
        /// Структура ответа
        /// </summary>
        public LPackClientAnswer Answer { get; set; }

        /// <summary>
        /// идентификатор запроса
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// время работы запроса, мс
        /// </summary>
        public int Time { get; set; }

        /// <summary>
        /// таймаут ожидания ответа, мс
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// ip адрес текущего запроса
        /// </summary>
        public string ServerIp { get; set; }
        
        /// <summary>
        /// url текущего запроса
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// имя модуля
        /// </summary>
        public string Module { get; set; }

        /// <summary>
        /// имя объекта
        /// </summary>
        public string Object { get; set; }

        /// <summary>
        /// имя экшна
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// тихая обработка ошибок
        /// (не выводится окно с описанием ошибки)
        /// </summary>
        public bool SilentErrorProcess { get; set; }

        /// <summary>
        /// внутренний отладочный лог
        /// </summary>
        public string InnerLog { get; set; }

        /// <summary>
        /// отступ сообщений лога
        /// </summary>
        public int DebugOffset { get; set; }

        /// <summary>
        /// внутренний отладочный лог процесса хопа
        /// </summary>
        public string DoHopLog { get; set; }

        /// <summary>
        /// флаг системного запроса
        /// </summary>
        public bool SystemQuery { get; set; }

        /// <summary>
        /// уникальный идентификатор запроса
        /// </summary>
        public string Oid { get; set; }

        private HttpWebRequest WebRequest { get; set; }
        private int WorkingTimeout { get; set; }
        private Profiler Profiler { get; set; }
        private int Code { get; set; }
        private int TimeTotal { get; set; }
        private int Attempts { get; set; }
        private string Log { get; set; }
        private string Report { get; set; }
        private bool InProgress { get; set; }
        private string Hash { get; set; }
        private string Label { get; set; }
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }

        public LPackClientQuery()
        {
            Label = "QUERY";
            Request = new LPackClientRequest();
            Answer = new LPackClientAnswer();

            Code = -1;
            Start = default(DateTime);
            Finish = default(DateTime);
            Time = 0;
            TimeTotal = 0;
            Attempts = 0;
            Log = "";
            Report = "";
            ServerIp = "";
            InProgress = false;
            WorkingTimeout = 10000;

            Module = "";
            Object = "";
            Action = "";

            Hash = "";
            SilentErrorProcess = false;
            InnerLog = "";
            DebugOffset = 0;
            DoHopLog = "";
            SystemQuery = false;

            RequestId = Cryptor.MakeRandom().ToString();

            if(Central.Parameters.UseRequestLog)
            {
                lock(Central.Queries)
                {
                    if(!Central.Queries.ContainsKey(RequestId))
                    {
                        Central.Queries.Add(RequestId, this);
                    }
                }
            }

            Request.Timeout = Central.Parameters.RequestTimeoutDefault;
        }

        public Dictionary<string, string> GetDict()
        {
            var result = new Dictionary<string, string>()
            {
                {"ID", RequestId.ToString()},
                {"MODULE", Module.ToString()},
                {"OBJECT", Object.ToString()},
                {"ACTION", Action.ToString()},
                {"DATE_START", Start.ToString("dd.MM.yyyy HH:mm:ss")},
                {"DATE_FINISH", Finish.ToString("dd.MM.yyyy HH:mm:ss")},
                {"TIMEOUT", WorkingTimeout.ToString()},
                {"TIME", Time.ToString()},
                {"TIME_TOTAL", TimeTotal.ToString()},
                {"IN_PROGRESS", InProgress.ToString().ToInt().ToString()},
                {"CODE", Code.ToString() },
                {"REPORT", Report.ToString()},
                {"LOG", Report.ToString()},
                {"ATTEMPTS", Attempts.ToString()},
                {"SERVER_IP", ServerIp.ToString()},
            };

            return result;
        }

        /// <summary>
        /// Асинхронная операция выполняющая рутинные действия по выполнению запроса к серверу
        /// При успешном завершении Answer.Status == 0, распаковывает данные в Answer.QueryResult
        /// Возможности для улучшения, сделать возможность передачи массива ключей для распаковки 
        /// Нескольких ключей при такой необходимости
        /// </summary>
        /// <param name="m">модуль</param>
        /// <param name="o">объект</param>
        /// <param name="a">экшен</param>
        /// <param name="key">ключ распаковки</param>
        /// <param name="p"></param>
        /// <returns>Возвращает таск с выполняемой операцией обращения к серверу</returns>
        [Obsolete]
        public static Task<LPackClientQuery> DoQueryAsync(string m, string o, string a, string key, Dictionary<string, string> p = null, int Timeout = -1)
        {
            return Task.Run(() =>
                    {
                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", m);
                        q.Request.SetParam("Object", o);
                        q.Request.SetParam("Action", a);
                        q.Request.SetParams(p);

                        if(Timeout == -1)
                        {
                            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                        }
                        else
                        {
                            q.Request.Timeout = Timeout;
                        }

                        q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                        q.DoQuery();

                        if(key != string.Empty)
                        {
                            if(q.Answer.Status == 0)
                            {
                                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                                if(result != null)
                                {
                                    q.Answer.QueryResult = ListDataSet.Create(result, key);
                                }
                            }
                        }

                        return q;
                    }
                );
        }

        public int RepeatCounter { get; set; } = 0;

        public void UpdateStatusString(string s)
        {
            LogMsg(s, true, 0);
            Central.LPackClient.UpdateStatusString(this, s);
        }



        /// <summary>
        /// Выполнение запроса к серверу.
        /// Производится отработка ошибок протокола.
        /// </summary>
        public void DoQuery()
        {
            if(Central.Parameters.HopMode == 2)
            {
                DoQuery2();
            }
            else
            {
                DoQuery1();
            }
        }

        [Obsolete]
        public void DoQuery1()
        {
            var client = Central.LPackClient;
            var connection = Central.LPackClient.CurrentConnection;

            bool resume = true;
            int repeatCounter = 0;
            int repeatMax = 10;

            while(resume)
            {
                repeatCounter++;
                RepeatCounter = repeatCounter;
                //Request.SetParam("Token", client.Session.Token);

                DoRawQuery(true);

                {
                    var s = "";
                    s = s.Append($"{Request.Params.CheckGet("Module")}>");
                    s = s.Append($"{Request.Params.CheckGet("Object")}>");
                    s = s.Append($"{Request.Params.CheckGet("Action")} ");
                    s = s.Append($"{Answer.Status} ");
                    connection.DebugStatusString = s;
                }

                resume = false;

                if(Answer.Status != 0)
                {
                    var checkAttempts = true;
                    var doLogin = false;
                    var doHop = false;

                    switch(Answer.Status)
                    {
                        // нет сессии, авторизуемся и повторяем запрос
                        case 31:
                        doLogin = true;
                        break;

                        // таймаут ответа
                        case 7:
                        //прочая ошибка уровня HTTP
                        case 9:
                        //оффлайн
                        case 6:
                        doHop = true;
                        break;
                    }

                    if(doLogin)
                    {
                        var loginResult = client.DoLogin();
                        if(loginResult)
                        {
                            resume = true;
                            checkAttempts = false;
                        }
                        else
                        {
                            doHop = true;
                        }
                    }

                    if(doHop)
                    {
                        if(Request.Attempts > 1)
                        {
                            var hopResult = client.DoHop(false, this);
                            if(hopResult)
                            {
                                resume = true;
                                checkAttempts = false;
                            }
                            LogMsg($"CLIENT_QUERY_DO_QUERY HOPPING: {DoHopLog}", true, DebugOffset);
                        }
                    }

                    if(checkAttempts)
                    {
                        if(repeatCounter >= Request.Attempts)
                        {
                            var s = "DoQuery repeat attempts limit1 reached, aborting.";
                            s = $"{s} repeatCounter=[{repeatCounter}]";
                            s = $"{s} repeatMax=[{repeatMax}]";
                            LogMsg(s, true);
                            resume = false;
                        }
                    }
                }

                //защита от зацикливания
                //это никак не влияет на код ошибки
                //будет установлен статус от последней ошибки
                if(repeatCounter >= repeatMax)
                {
                    var s = "DoQuery repeat attempts limit2 reached, aborting.";
                    s = $"{s} repeatCounter=[{repeatCounter}]";
                    s = $"{s} repeatMax=[{repeatMax}]";
                    LogMsg(s, true);
                    resume = false;
                }
            }
        }

        public void DoQuery2()
        {
            /*
                отправка запроса на сервер
             */
            bool resumeQuery = true;

            int queryRepeatCounter = 0;
            int queryRepeatLimitTime = Central.Parameters.QueryRepeatLimitTime;
            int queryRepeatDelay = Central.Parameters.QueryRepeatDelay;

            int waitRepeatLimitTime = Central.Parameters.WaitRepeatLimitTime;
            int waitRepeatDelay = Central.Parameters.WaitRepeatDelay;

            var requestString = "";
            {
                var s = "";
                s = s.Append($"{Request.Params.CheckGet("Module")}>");
                s = s.Append($"{Request.Params.CheckGet("Object")}>");
                s = s.Append($"{Request.Params.CheckGet("Action")} ");
                requestString = s;
            }

            var queryMomentStart = Tools.GetToday();
            while(resumeQuery)
            {
                queryRepeatCounter++;
                RepeatCounter = queryRepeatCounter;

                if(resumeQuery)
                {
                    //если нет связи, выполняется цикл ожидания
                    if(Central.LPackClient.ConnectingTimeout)
                    {
                        var resumeWait = true;
                        var waitRepeatCounter = 0;
                        var waitMomentStart = Tools.GetToday();
                        while(resumeWait)
                        {
                            waitRepeatCounter++;
                            if(waitRepeatCounter == 1)
                            {
                                UpdateStatusString($"ожидание: {requestString}");
                            }

                            if(!Central.LPackClient.ConnectingTimeout)
                            {
                                resumeWait = false;
                            }

                            if(resumeWait)
                            {
                                var dt = Tools.TimeOffset(waitMomentStart);
                                if(dt > waitRepeatLimitTime)
                                {
                                    resumeWait = false;
                                    UpdateStatusString($"лимит ожидания");
                                }
                            }

                            if(resumeWait)
                            {
                                if(waitRepeatDelay > 0)
                                {
                                    System.Threading.Thread.Sleep(waitRepeatDelay);
                                }
                            }
                        }
                    }
                }

                if(resumeQuery)
                {
                    DoRawQuery(true);
                    UpdateStatusString($"запрос: {requestString} =>[{Answer.Status}]{Answer.Time}");
                }

                if(resumeQuery)
                {
                    if(Answer.Status == 0)
                    {
                        Central.LPackClient.ProcessTimeout(false);
                        resumeQuery = false;
                    }
                    else
                    {
                        var doLogin = false;
                        switch(Answer.Status)
                        {
                            // нет сессии, авторизация, повтор запроса
                            case 31:
                            doLogin = true;
                            break;

                            // таймаут ответа
                            case 7:
                            //прочая ошибка уровня HTTP
                            case 9:
                            //оффлайн
                            case 6:
                            break;
                        }

                        if(resumeQuery)
                        {
                            if(doLogin)
                            {
                                var loginResult = Central.LPackClient.DoLogin();
                                UpdateStatusString($"релогин");
                            }
                        }

                        if(resumeQuery)
                        {
                            var dt = Tools.TimeOffset(queryMomentStart);
                            if(dt > queryRepeatLimitTime)
                            {
                                resumeQuery = false;
                                UpdateStatusString($"лимит повторов");
                            }
                        }

                        if(resumeQuery)
                        {
                            var attempts = Request.Attempts;

                            if(Central.Parameters.RequestAttemptsFixMode == 1)
                            {
                                if(attempts == 0)
                                {
                                    attempts = 1;
                                }
                            }

                            if(Central.Parameters.RequestAttemptsFixMode == 2)
                            {
                                if(attempts == 0)
                                {
                                    attempts = 1;
                                }
                                if(attempts >= 3)
                                {
                                    attempts = 100;
                                }
                            }

                            if(RepeatCounter >= attempts)
                            {
                                resumeQuery = false;
                                UpdateStatusString($"предел повторов");
                            }

                        }

                        if(resumeQuery)
                        {
                            if(queryRepeatDelay > 0)
                            {
                                System.Threading.Thread.Sleep(queryRepeatDelay);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// проверка статуса онлайн
        /// </summary>
        //public void CheckOnline()
        //{
        //    bool repeat = false;
        //    int repeatCounter = 0;
        //    int repeatDelay = 1000;
        //    int repeatMax = 60;

        //    //если текущий статус офлайн
        //    //и какой-то запрос в процессе ожидания
        //    if(Central.ConnectingInProgress && !Central.IsOnline)
        //    {
        //        repeat = true;
        //    }
        //    else
        //    {
        //        repeat = false;
        //    }

        //    while(repeat)
        //    {
        //        {
        //            var s = "Check online.";

        //            s = $"{s} ConnectingInProgress=[{Central.ConnectingInProgress}]";
        //            s = $"{s} IsOnline=[{Central.IsOnline}]";
        //            s = $"{s} repeatCounter=[{repeatCounter}]";
        //            s = $"{s} repeatMax=[{repeatMax}]";

        //            Central.Logger.Trace(s);
        //            Central.Dbg(s);
        //        }

        //        if(Central.ConnectingInProgress && !Central.IsOnline)
        //        {
        //            repeat = true;
        //        }
        //        else
        //        {
        //            repeat = false;
        //        }

        //        repeatCounter++;


        //        //await Task.Run(() =>
        //        //{
        //        //    Task.Delay(repeatDelay);
        //        //});

        //        Task.Delay(repeatDelay);


        //        //защита от зацикливания
        //        if(repeatCounter >= repeatMax)
        //        {
        //            {
        //                var s = "CheckOnline attempts limit reached, resume";

        //                s = $"{s} repeatCounter=[{repeatCounter}]";
        //                s = $"{s} repeatMax=[{repeatMax}]";

        //                Central.Logger.Trace(s);
        //                Central.Dbg(s);
        //            }

        //            repeat = false;
        //        }
        //    }

        //}

        /// <summary>
        /// Выполнение сырого запроса к серверу.
        /// Ядро отработки запроса.
        /// </summary>
        public void DoRawQuery(bool checkOnline = true)
        {
            /*
                коды ответа
                http://192.168.3.237/developer/l-pack-erp/client/development/concepts/protocol
                http://192.168.3.237/developer/erp2/server/struct/protocol

                0
                ок (0)
                все хорошо, ответ получен и успешно интерпретирован

                6
                Оффлайн (6)
                Нет соединения с сервером
                сервер оффлайн, будет сделан хоп на другой сервер

                8
                Ошибка получения ответа (8)
                Невозможно разобрать ответ сервера
                ответ от сервера пришел в неверном формате, ошибка разработчиков
  
                7
                Ошибка получения ответа (7)
                Время ожидания ответа истекло
                таймаут, сервер онлайн, ожидание ответа на данный запрос истекло

                9
                Ошибка получения ответа (9)
                прочая ошибка уровня HTTP
             
             */

            var connection = Central.LPackClient.CurrentConnection;
            var client = Central.LPackClient;
            var clientId = Central.LPackClient.ClientId;

            Start = DateTime.Now;
            InProgress = true;
            Attempts++;
            Time = 0;
            Report = "";

            Profiler = new Profiler();

            //системные
            var systemParams = new JObject();
            //пользовательские
            var userParams = new JObject();
            //строка для вычисления хэша
            var userParamsString = "";

            //имена системных параметров
            var systemParamsKeys = new List<string>(){
                "Module",
                "Object",
                "Action",
                "Token",
                "ClientId",
                "RequestId"
            };

            var p = new Dictionary<string, string>();
            Central.SetServerIP(connection.Host);
            string url = $"http://{connection.Host}:{connection.Port}{connection.Path}";
            Url = url;
            ServerIp = $"{connection.Host}";

            if(Request.Params != null)
            {
                if(Request.Params.Count > 0)
                {
                    Request.SetParam("Token", Central.LPackClient.Session.Token);

                    var requestParams = new Dictionary<string, string>(Request.Params);
                    foreach(KeyValuePair<string, string> item in requestParams)
                    {
                        var k = item.Key;
                        var v = item.Value;
                        if(!string.IsNullOrEmpty(k))
                        {
                            p.CheckAdd(k, v);
                        }
                    }

                    p.CheckAdd("ClientId", clientId);
                    p.CheckAdd("RequestId", RequestId);
                }

                foreach(KeyValuePair<string, string> i in p)
                {
                    if(!string.IsNullOrEmpty(i.Key))
                    {
                        if(systemParamsKeys.Contains(i.Key))
                        {
                            systemParams[i.Key] = i.Value;
                        }
                        else
                        {
                            userParams[i.Key] = i.Value;
                            userParamsString = $"{userParamsString}|{i.Key}={i.Value}";
                        }
                    }
                }

                //кастомные системные пераметры
                {
                    //дата создания запроса
                    systemParams["OnDate"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    //хэш запроса
                    systemParams["Hash"] = Cryptor.MakeMD5(userParamsString);
                }

                Module = systemParams["Module"].ToString();
                Object = systemParams["Object"].ToString();
                Action = systemParams["Action"].ToString();

                WorkingTimeout = Request.Timeout;
            }

            systemParams["Params"] = userParams;

            string requestString = JsonConvert.SerializeObject(systemParams);
            byte[] requestBytes = Encoding.UTF8.GetBytes(requestString);

            bool doRequest = true;
            Answer.Clear();

            if(checkOnline)
            {
                //если в данный момент офллайн режим, не выполняем запрос вообще
                {
                    if(!Central.LPackClient.OnlineMode)
                    {
                        doRequest = false;
                        Answer.Status = 6;
                        Answer.Error.Code = Answer.Status;
                        Answer.Error.Message = "Оффлайн";
                        Answer.Error.Description = "Нет соединения с сервером";
                    }
                }
            }

            if(doRequest)
            {
                try
                {
                    WebRequest = (HttpWebRequest)System.Net.WebRequest.Create(url);
                    WebRequest.Method = "POST";
                    WebRequest.KeepAlive = false;
                    WebRequest.Credentials = System.Net.CredentialCache.DefaultCredentials;
                    WebRequest.Timeout = WorkingTimeout;

                    bool queryComplete = false;

                    switch(Request.Type)
                    {
                        case LPackClientRequest.RequestTypeRef.FormUrlencoded:
                            {
                                WebRequest.ContentType = "text/plain";
                                WebRequest.ContentLength = requestBytes.Length;

                                var requestStream = WebRequest.GetRequestStream();
                                requestStream.Write(requestBytes, 0, requestBytes.Length);
                                requestStream.Close();
                                queryComplete = true;
                            }
                            break;

                        case LPackClientRequest.RequestTypeRef.MultipartForm:
                            {
                                if(!string.IsNullOrEmpty(Request.UploadFilePath))
                                {
                                    var file = new[] { Request.UploadFilePath };
                                    var paramName = new[] { "File" };
                                    var contentType = new[] { "application/octet-stream" };

                                    string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
                                    byte[] boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");


                                    WebRequest.ContentType = "multipart/form-data; boundary=" + boundary;
                                    var RequestStream = WebRequest.GetRequestStream();

                                    string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";

                                    if(systemParams != null)
                                    {
                                        foreach(var o in (JObject)systemParams)
                                        {
                                            string k = o.Key;
                                            JToken v = o.Value;

                                            if(k == "Data" || k == "Params")
                                            {
                                                foreach(var o2 in (JObject)o.Value)
                                                {
                                                    string k2 = o2.Key;
                                                    JToken v2 = o2.Value;

                                                    RequestStream.Write(boundarybytes, 0, boundarybytes.Length);
                                                    string formitem = string.Format(formdataTemplate, $"{k}[{k2}]", v2);
                                                    byte[] formitembytes = Encoding.UTF8.GetBytes(formitem);
                                                    RequestStream.Write(formitembytes, 0, formitembytes.Length);
                                                }
                                            }
                                            else
                                            {
                                                RequestStream.Write(boundarybytes, 0, boundarybytes.Length);
                                                string formitem = string.Format(formdataTemplate, $"{k}", v);
                                                byte[] formitembytes = Encoding.UTF8.GetBytes(formitem);
                                                RequestStream.Write(formitembytes, 0, formitembytes.Length);
                                            }
                                        }
                                    }
                                    RequestStream.Write(boundarybytes, 0, boundarybytes.Length);

                                    string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
                                    string header;

                                    for(int i = 0; i < file.Length; i++)
                                    {
                                        header = string.Format(headerTemplate, paramName[i], System.IO.Path.GetFileName(file[i]), contentType[i]);
                                        byte[] headerbytes = Encoding.UTF8.GetBytes(header);
                                        RequestStream.Write(headerbytes, 0, headerbytes.Length);

                                        FileStream fileStream = new FileStream(file[i], FileMode.Open, FileAccess.Read);
                                        byte[] buffer = new byte[8192];
                                        int bytesRead;
                                        while((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                                        {
                                            RequestStream.Write(buffer, 0, bytesRead);
                                        }
                                        fileStream.Close();
                                        RequestStream.Write(boundarybytes, 0, boundarybytes.Length);
                                    }
                                    RequestStream.Close();
                                    queryComplete = true;
                                }
                            }
                            break;
                    }

                    if(queryComplete)
                    {
                        var webResponse = WebRequest.GetResponse();

                        if(webResponse != null)
                        {
                            string contentType = webResponse.ContentType;
                            long contentLength = webResponse.ContentLength;
                            Stream answerStream = webResponse.GetResponseStream();

                            string downloadFileName = "";
                            string downloadFilePath = "";

                            /*
                                мы выделяем 3 типа ответа:
                                type:
                                    data -- обычный ответ application/json, парсим обрабатываем
                                        в переменной Data возвращается структура ответа

                                    file -- загрузка файла, сохраняем массив байтов в файл
                                        данные пишутся бинарно во временный файл:
                                        downloadFilePath -- путь ко временному файлу (данные сохраняются в него)
                                        downloadFileName -- оригинальное имя файла
                                    
                                    stream -- поток данных
                                        данные пишутся в поток:
                            
                                для определения типа, возьмем заголовок Content-Disposition
                                если Content-Disposition=attachment;filename=etc
                                    то тип данных file
                                        вытащим имя файла после "filename=" 

                                */

                            //Читаем заголовки ответа, если есть Content-Disposition, значит нам прислали файл
                            var headers = webResponse.Headers;
                            if(headers != null)
                            {
                                for(int i = 0; i < webResponse.Headers.Count; i++)
                                {
                                    var k=webResponse.Headers.Keys[i];
                                    k=k.ToLower();
                                    if( k == "content-disposition")
                                    {
                                        string what = "filename=";
                                        int pos = webResponse.Headers[i].IndexOf(what);
                                        int len = webResponse.Headers[i].Length;
                                        int len0 = what.Length;
                                        if(pos != -1)
                                        {
                                            downloadFileName = webResponse.Headers[i].Substring(pos + len0, len - (pos + len0));
                                            downloadFileName = downloadFileName.Trim();
                                        }
                                    }

                                    if(k == "l-pack-erp-oid")
                                    {
                                        Answer.ObjectId = webResponse.Headers[i];
                                    }
                                }
                            }

                            //определяем тип ответа
                            var answerType = LPackClientAnswer.AnswerTypeRef.Default;

                            //если имя файла установлено, то тип контента -- File
                            if(!string.IsNullOrEmpty(downloadFileName))
                            {
                                answerType = LPackClientAnswer.AnswerTypeRef.File;

                                var base64EncodedBytes = System.Convert.FromBase64String(downloadFileName);
                                var decodedFileName = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);

                                Answer.DownloadFileOriginalName = decodedFileName;

                                // Проверяем имя файла на недопустимые символы
                                if (decodedFileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0)
                                {
                                    downloadFileName = decodedFileName;
                                }
                                else
                                {
                                    // Если недопустимые символы есть, берем за основу не декодированное имя
                                    string ext = Path.GetExtension(decodedFileName);
                                    downloadFileName = downloadFileName.Replace('/', 'a');
                                    if(downloadFileName.Length > 15)
                                    {
                                        downloadFileName = $"{downloadFileName.Substring(0, 15)}{ext}";
                                    }
                                    else
                                    {
                                        downloadFileName = $"{downloadFileName}{ext}";
                                    }
                                }
                            }


                            //если в параметрах запроса установлен желаемый тип ответа, используем его
                            if(Request.RequiredAnswerType != LPackClientAnswer.AnswerTypeRef.Default)
                            {
                                answerType = Request.RequiredAnswerType;
                            }

                            //Answer=new LPackClientAnswer();
                            Answer.Type = answerType;

                            //работаем с данными ответа в зависимости от типа ответа
                            switch(answerType)
                            {
                                case LPackClientAnswer.AnswerTypeRef.Default:
                                case LPackClientAnswer.AnswerTypeRef.Data:
                                    {
                                        var answerReader = new StreamReader(answerStream);
                                        var answerString = answerReader.ReadToEnd();

                                        Answer = JsonConvert.DeserializeObject<LPackClientAnswer>(answerString);
                                        Answer.DataRaw = answerString;
                                    }
                                    break;

                                case LPackClientAnswer.AnswerTypeRef.File:
                                    {
                                        if(Request.AnswerFileAddSiffix)
                                        {
                                            string suffix = "";
                                            if(!string.IsNullOrEmpty(Answer.ObjectId))
                                            {
                                                suffix = Answer.ObjectId.Substring(0, 6);
                                            }
                                            else
                                            {
                                                suffix = Cryptor.MakeRandom().ToString();
                                            }

                                            string baseName = Path.GetFileNameWithoutExtension(downloadFileName);
                                            string ext = Path.GetExtension(downloadFileName);
                                            downloadFileName = $"{baseName}_{suffix}{ext}";
                                        }

                                        downloadFilePath = $"{Path.GetTempPath()}{downloadFileName}";

                                        using(var fileStream = File.Create(downloadFilePath))
                                        {
                                            Copy(answerStream, fileStream);
                                            fileStream.Close();
                                        }

                                        Answer.DownloadFileName = downloadFileName;
                                        Answer.DownloadFilePath = downloadFilePath;
                                    }
                                    break;

                                case LPackClientAnswer.AnswerTypeRef.Stream:
                                    {
                                        Answer.DataStream = new MemoryStream();
                                        Copy(answerStream, Answer.DataStream);
                                        Answer.DataStream.Position = 0;
                                    }
                                    break;
                            }
                        }
                        webResponse?.Close();
                        webResponse?.Dispose();
                    }
                    else
                    {
                        Answer.Status = 8;
                        Answer.Error.Code = Answer.Status;
                        Answer.Error.Message = "Ошибка получения ответа (8)";
                        Answer.Error.Description = "Невозможно разобрать ответ сервера";
                    }
                }
                catch(Exception e)
                {
                    bool timeoutFlag = false;
                    {
                        var e2 = e as WebException;
                        if(e2 != null)
                        {
                            var t = e2.Status.ToString();
                            if(!string.IsNullOrEmpty(t))
                            {
                                t = t.ToLower();
                                if(t.IndexOf("timeout") > -1)
                                {
                                    timeoutFlag = true;
                                }
                            }
                        }
                    }

                    if(timeoutFlag)
                    {
                        //таймаут
                        Answer.Status = 7;
                        Answer.Error.Code = Answer.Status;
                        Answer.Error.Message = "Ошибка получения ответа (7)";
                        Answer.Error.Description = "Время ожидания ответа истекло";
                    }
                    else
                    {
                        //прочая ошибка уровня HTTP
                        Answer.Status = 9;
                        Answer.Error.Code = Answer.Status;
                        Answer.Error.Message = "Ошибка получения ответа (9)";
                        Answer.Error.Description = $"{e.ToString()}";
                    }
                }
                WebRequest.Abort();
            }

            Time = (int)Profiler.GetDelta();
            TimeTotal = TimeTotal + Time;
            Answer.Time = Time;
            Code = Answer.Status;
            var route = p.CheckGet("Module") + ">" + p.CheckGet("Object") + ">" + p.CheckGet("Action");

            {
                var row = new Dictionary<string, string>();                
                row.CheckAdd("RESULT_LAST", Code.ToString());
                row.CheckAdd("ON_DATE", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));

                {
                    var k = "COUNT";
                    var v = row.CheckGet(k).ToInt();
                    v = v + 1;
                    row.CheckAdd(k, v.ToString());
                }

                {
                    var k = "TIME_TOTAL";
                    var v = row.CheckGet(k).ToInt();
                    v=v + Time;
                    row.CheckAdd(k, v.ToString());
                }

                if(Code==0)
                {
                    var k = "RESULT_COMPLETE_COUNT";
                    var v = row.CheckGet(k).ToInt();
                    v = v + 1;
                    row.CheckAdd(k, v.ToString());
                }
                else
                {
                    var k = "RESULT_FAILED_COUNT";
                    var v = row.CheckGet(k).ToInt();
                    v = v + 1;
                    row.CheckAdd(k, v.ToString());
                }

                Central.Stat.RequestAdd(route, row);
            }

            if(Answer.Status != 0)
            {
                var s = " ERROR ";
                s = $"{s} Code=[{Answer.Error.Code}]";
                s = $"{s} Message=[{Answer.Error.Message}]";
                s = $"{s} Description=[{Answer.Error.Description}]";

                LogMsg(s, true);
                Report = $"ERROR {ServerIp} {Answer.Error.Code} {Answer.Error.Message} {Answer.Error.Description}";
            }

            Log = Log.AddCR();
            Log = Log.Append(Report);

            Central.LPackClient.OnlineMode = true;

            {
                var s = "QUERY   ";                
                route = route.PadRight(45);
                s = $"{s} ServerIp=[{ServerIp.SPadLeft(20)}]";
                s = $"{s} Route=[{route.SPadLeft(50)}]";
                s = $"{s} Status=[{Answer.Status.ToString().SPadLeft(4)}]";
                s = $"{s} Url=[{url.SPadLeft(32)}]";
                s = $"{s} RequestId=[{p.CheckGet("RequestId").ToString().SPadLeft(8)}]";
                s = $"{s} ClientId=[{p.CheckGet("ClientId").ToString().SPadLeft(8)}]";
                s = $"{s} Token=[{p.CheckGet("Token").ToString().SPadLeft(32)}]";
                s = $"{s} Time=[{Time.ToString().SPadLeft(8)}]";
                s = $"{s} Timeout=[{WorkingTimeout.ToString().SPadLeft(8)}]";
                s = $"{s} Oid=[{Answer.ObjectId.ToString().SPadLeft(32)}]";

                if(Answer.Status > 0)
                {
                    s = $"{s} Code=[{Answer.Error.Code.ToString().SPadLeft(4)}]";
                    s = $"{s} Message=[{Answer.Error.Message.ToString().SPadLeft(16)}]";
                    s = $"{s} Description=[{Answer.Error.Description.ToString()}]";
                }
                LogMsg(s, true);
            }

            Finish = DateTime.Now;
            InProgress = false;
        }


        /// <summary>
        /// Копирует данные из одного потока в другой.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        public static void Copy(Stream input, Stream output)
        {
            //копирование идет поблочно
            var buffer = new byte[8192];
            int read;
            while((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
            }
        }

        /// <summary>
        /// гарантировано сообщит пользователю (messagebox) какая ошибка произошла на сервере, если код > 145
        /// </summary>
        public void ProcessError()
        {
            Central.LPackClient.ProcessError(this);
        }

        public string GetError()
        {
            return Central.LPackClient.GetError(this);
        }

        public void LogMsg(string text, bool addCr = false, int offset = 0)
        {
            var today = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss ffffff");

            var o = "";
            if(addCr)
            {
                if(offset > 0)
                {
                    for(int i = 0; i <= offset; i++)
                    {
                        o = $"{o}    ";
                    }
                }
                if(text.IndexOf("\n") > -1)
                {
                    if(!o.IsNullOrEmpty())
                    {
                        text = text.Replace("\n", $"\n{o}");
                    }
                }
            }

            var s = $"{today} {Label}<{RequestId}>: {o}{text}";
            InnerLog = InnerLog.Append(s, addCr);
            InnerLog = InnerLog.Crop(32000);

            Central.LPackClient.LogMsg(s);
        }

    }
}
