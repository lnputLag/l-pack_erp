using Client.Common;
using System.Collections.Generic;

namespace Client.Interfaces.Service.Storages
{
    /// <summary>
    /// Файловые хранилища
    /// </summary>
    /// <author>sviridov_ae</author>
    public class StoragesInterface
    {
        public StoragesInterface()
        {
            {
                Central.WM.AddTab("storages_control", "Файловые хранилища");
                Central.WM.AddTab<RedisTab>("storages_control");
                Central.WM.SetActive("RedisTab");
            }
        }
    }
}
