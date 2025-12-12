using Client.Common;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Интерфейс списка этикеток для литой тары
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public class MoldedContainerStickerInterface
    {
        /// <summary>
        /// Интерфейс списка этикеток для литой тары
        /// </summary>
        public MoldedContainerStickerInterface()
        {
            Central.WM.AddTab<MoldedContainerStickerTab>("molded_container_sticker", true);
            Central.WM.SetActive("MoldedContainerStickerTab");
        }
    }
}
