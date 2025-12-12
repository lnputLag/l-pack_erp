using System;
using System.Collections.Generic;

namespace Client.Common
{
    /// <summary>
    /// структура данных запроса
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>3</version>
    public class LPackClientRequest
    {
        public LPackClientRequest()
        {
            Params = new Dictionary<string, string>();
            Type = RequestTypeRef.FormUrlencoded;
            Timeout = 45000;
            Attempts = 1;
            UploadFilePath = "";
            RequiredAnswerType = LPackClientAnswer.AnswerTypeRef.Default;
            AnswerFileAddSiffix = true;
        }

        /// <summary>
        /// Параметры запроса. Установить значение параметра также можно через метод SetParam.
        /// </summary>
        public Dictionary<string, string> Params { get; set; }

        /// <summary>
        /// Тип запроса: простой или multipart
        /// Простой: для обычных запросов, multipart: для отправки больших блоков данных (аплоад файлов)
        /// По умолчанию: простой
        /// form -- POST: application/x-www-form-urlencoded, multipart -- POST: multipart/form
        /// </summary>
        public RequestTypeRef Type { get; set; }
        public enum RequestTypeRef
        {
            FormUrlencoded = 1,
            MultipartForm = 2,
        }
        /// <summary>
        /// Таймаут ожидания ответа на запрос. мс,
        /// выставляется для каждого запроса,
        /// по умолчанию 10000
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// число попыток повторной отправки запроса
        /// </summary>
        public int Attempts { get; set; }

        /// <summary>
        /// Путь к файлу для запроса загрузки файла на сервер.
        /// </summary>
        public string UploadFilePath { get; set; }

        /// <summary>
        /// Требуемый тип ответа
        /// </summary>
        public LPackClientAnswer.AnswerTypeRef RequiredAnswerType { get; set; }

        /// <summary>
        /// добаить уникальный суффикс к имени загруженного файла
        /// </summary>
        public bool AnswerFileAddSiffix { get; set; }



        /// <summary>
        /// Установка значения параметра.
        /// Проверяет, есть ли параметр в словаре, если нет создает.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetParam(string key, string value = "")
        {
            try
            {
                if(!string.IsNullOrEmpty(key))
                {
                    if(!Params.ContainsKey(key))
                    {
                        Params.Add(key, "");
                    }
                    Params[key] = value;
                }
            }
            catch(Exception e)
            {

            }
        }

        public void SetParams(Dictionary<string, string> p)
        {
            if(p != null)
            {
                if(p.Count > 0)
                {
                    foreach(var i in p)
                    {
                        if(i.Value != null)
                        {
                            SetParam(i.Key, i.Value);
                        }
                    }
                }
            }
        }
    }
}