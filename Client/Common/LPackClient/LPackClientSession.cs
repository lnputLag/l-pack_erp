namespace Client.Common
{
    /// <summary>
    /// структура данных сессии
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>3</version>
    public class LPackClientSession
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
        public string Sid { get; set; }
        public LPackClientSession()
        {
            Login = "";
            Password = "";
            Token = "";
            Sid="";
        }
    }
}