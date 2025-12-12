namespace Client.Common
{
    /// <summary>
    /// структура ошибки ответа сервера
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public class Error
    {
        public int Code { get; set; }
        public string Sig { get; set; }
        public string Message { get; set; }
        public string Description { get; set; }
    }
}
