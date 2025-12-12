using Client.Common;
using Client.Interfaces.Production;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс для предоставления чертежей и дизайнов 
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public class TkFileRequestInterface
    {
        /// <summary>
        /// Интерфейс для предоставления чертежей и дизайнов 
        /// </summary>
        public TkFileRequestInterface()
        {
            Central.WM.AddTab<TkFileRequestTab>("tk_file_request", true);
            Central.WM.SetActive("TkFileRequestTab");
        }
    }
}
