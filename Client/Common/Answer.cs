using System;

namespace Client.Common
{
    /// <summary>
    /// структура ответа сервера
    /// (объект устарел)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    [Obsolete]
    public class Answer
    {
        /// <summary>
        /// Данные ответа
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Время ответа
        /// </summary>
        public DateTime OnDate { get; set; }

        /// <summary>
        /// Признак того что произошла ошибка.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Детальное описание ошибки
        /// </summary>
        public Error Error { get; set; }

        public Answer()
        {
            Error = new Error();
        }
    }
}
