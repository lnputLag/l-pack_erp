namespace Client.Common
{
    /// <summary>
    /// Структура соединения (профиль соединения).
    /// Хранит основные параметры соединения. Одно соединение -- один сервер.
    /// Данные соединения берутся из конфиг-файла.
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>3</version>
    public class LPackClientConnection
    {
        /// <summary>
        /// Флаг активности соединения.
        /// (Может быть снят, если сервер исключен из списка доступных).
        /// Неактивные соединения не участвуют в хоппинге.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Текущий счетчик неудачных попыток отправки запроса.        
        /// Если запрос завершился ошибкой "CONNECTION ERROR", значение счетчика увеличивается
        /// на единицу. Когда оно достингет лимита (AttemptsFailedMax), соединение будет неактивным.
        /// </summary>
        public int AttemptsFailed { get; set; }

        /// <summary>
        /// Лимит ошибок соединения.
        /// 0 -- нет лимита.
        /// </summary>
        public int AttemptsFailedMax { get; set; }

        /// <summary>
        /// Хост
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Адрес запроса
        /// По умолчанию: /api/4/
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Порт сервера
        /// По умолчанию: 5678
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Логин пользователя
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Пароль пользователя
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Токен соединения
        /// </summary>
        private string Token { get; set; }

        /// <summary>
        /// Таймаут запросов.
        /// По умолчанию: 10000 (10 сек)
        /// Параметр может быть переопределен для каждого запроса персонально.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// текущий статус запроса
        /// (для выводв ы разных информационных строках)
        /// </summary>
        public string DebugStatusString { get; set; }

        /// <summary>
        /// лог статуса запроса
        /// </summary>
        public string DebugStatusStringLog { get; set; }

        /// <summary>
        /// текущий статус системного запроса
        /// </summary>
        public string DebugStatusString2 { get; set; }

        /// <summary>
        /// лог статуса cистемного запроса
        /// </summary>
        public string DebugStatusString2Log { get; set; }

        public LPackClientConnection()
        {
            Enabled = true;
            Host = "";
            Path = "/api/4/";
            Port = 5678;
            Login = "";
            Password = "";
            Token = "";
            Timeout = 0;
            AttemptsFailed = 0;
            AttemptsFailedMax = 2;
            DebugStatusString = "";
            DebugStatusStringLog = "";
            DebugStatusString2 = "";
            DebugStatusString2Log = "";
        }
    }
}