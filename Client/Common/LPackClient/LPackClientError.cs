namespace Client.Common
{
    /// <summary>
    /// структура данных ошибки
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>3</version>
    public class LPackClientError
    {
        public int Code { get; set; }
        public string Sig { get; set; }
        public string Message { get; set; }
        public string Description { get; set; }
    }
}