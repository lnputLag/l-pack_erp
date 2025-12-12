using Client.Assets.HighLighters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production.Testing
{
    public static class CardboardGroupColor
    {
        public static Dictionary<string, string> Items = new Dictionary<string, string>()
        {
            { "0", HColor.Gray },
            { "1", HColor.Green },
            { "2", HColor.Violet },
            { "3", HColor.Blue },
            { "4", HColor.Olive },
            { "5", HColor.LightSelection },
            { "6", HColor.Orange },
            { "7", HColor.Pink },
            { "8", HColor.YellowOrange },
            { "9", HColor.VioletPink },
        };
    }
}
