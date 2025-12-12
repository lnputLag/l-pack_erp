using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Common.Lib.Reporter
{
    /// <summary>
    /// Интерфейс элемента документа
    /// </summary>
    public interface IBlock
    {
        int CurrentX { get; set; }
        int CurrentY { get; set; }
        int PositionType { get; set; }
        int PositionX { get; set; }
        int PositionY { get; set; }

        void SetAbsPosition(int x, int y);
        void SetRelPosition();
        void Init(DocumentOptions options);
        void Render(DocumentOptions options);
    }

}
