using Client.Common;
using Client.Interfaces.Main;
using System.Collections.Generic;

namespace Client.Interfaces.Service
{
    /// <summary>
    /// Список помещений для отображения при пожаре на БДМ2
    /// </summary>
    /// <author>Грешных Н.И.</author>
    /// <version>1</version>
    /// <released>2024-04-20</released>
    /// <changed>2024-04-20</changed>
    public class FirePlanRoomInterface : InterfaceBase
    {
        public FirePlanRoomInterface()
        {
            Central.WM.AddTab("fire_plane_room", "(План помещений БДМ2)");
            Central.WM.AddTab<RoomList>("fire_plane_room");
            Central.WM.SetActive("RoomList");
        }
    }
}
