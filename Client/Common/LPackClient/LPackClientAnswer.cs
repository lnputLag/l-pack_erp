using Client.Common;
using System;
using System.IO;

namespace Client.Common
{
    /// <summary>
    /// структура данных ответа
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>3</version>
    public class LPackClientAnswer
    {
        public LPackClientAnswer()
        {
            Status = 0;
            RequestId = "";
            ClientId = "";
            Error = new LPackClientError();
            Type = AnswerTypeRef.Data;
            Data = "";
            DataRaw = "";
            DownloadFilePath = "";
            DownloadFileName = "";
            Time = 0;
            ObjectId = "";
        }

        /// <summary>
        /// Время ответа
        /// </summary>
        public DateTime OnDate { get; set; }

        /// <summary>
        /// статус ответа
        /// 0 -- нет ошибок, иначе код ошибки.
        /// Набор кодов ошибок сквозной с сервером.
        /// </summary>        
        public int Status { get; set; }

        /// <summary>
        /// идентификатор запроса
        /// (уникальный ID запроса с определенными параметрами)
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// идентификатор клиента
        /// (уникальный ID запущенной пользователем программы)
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Распакованный результат запроса
        /// </summary>
        public ListDataSet QueryResult { get; set; }

        /// <summary>
        /// Структура ошибки
        /// </summary>
        public LPackClientError Error { get; set; }
        /// <summary>
        /// Тип ответа
        /// </summary>
        public AnswerTypeRef Type { get; set; }

        public enum AnswerTypeRef
        {
            Default = 0,
            Data = 1,
            File = 2,
            Stream = 3,
        }

        /// <summary>
        /// Данные ответа
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// сырая строка ответа
        /// </summary>
        public string DataRaw { get; set; }

        /// <summary>
        /// поток чтения/записи данных ответа
        /// </summary>
        public MemoryStream DataStream { get; set; }

        /// <summary>
        /// Путь к файлу, получаемому с сервера.
        /// Абсолютный путь в файловой системе клиента.
        /// </summary>
        public string DownloadFilePath { get; set; }

        /// <summary>
        /// Имя файла, получаемого с сервера. 
        /// Проходит ряд преобразваний для достижения уникальности
        /// </summary>
        public string DownloadFileName { get; set; }

        /// <summary>
        /// Исходное имя файла, получаемого с сервера
        /// </summary>
        public string DownloadFileOriginalName { get; set; }

        /// <summary>
        /// время выполнения запроса
        /// </summary>
        public int Time { get; set; }

        /// <summary>
        /// id запроса сервера
        /// (возвращается сервером после удачного запроса)
        /// </summary>
        public string ObjectId { get; set; }

        public void Clear()
        {
            Status = 0;
            RequestId = "";
            ClientId = "";
            Error = new LPackClientError();
            Type = AnswerTypeRef.Data;
            Data = "";
            DataRaw = "";
            DownloadFilePath = "";
            DownloadFileName = "";
            ObjectId = "";
        }
    }
}