using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Production.Corrugator.TaskPlannings
{
    public class Errors
    {
        public class ProfileType
        {
            public static String BC { get => "BC"; }
            public static String EB { get => "EB"; }
            public static String BE { get => "BE"; }

            public static String BA { get => "B*"; }
            public static String AB { get => "*B"; }
            public static String CA { get => "C*"; }
            public static String AE { get => "*E"; }
            public static String EA { get => "E*"; }
            public static string EE { get => "EE"; }
            public static string EC { get => "EC"; }
            public static string AC { get => "*C"; }
        }

        /// <summary>
        /// Ощибочные переходы слоев
        /// </summary>
        public static Dictionary<string, List<string>> ChangeLayerError = new Dictionary<string, List<string>>
        {
            // •	ВС -*Е,  ВС - Е*, ВС - *В, ВС - ЕВ,  ВС - ВЕ,  ВС – ЕЕ, ВС – ЕС.
            { ProfileType.BC, new List<string> { ProfileType.AE, ProfileType.EA, ProfileType.AB, ProfileType.EB, ProfileType.BE, ProfileType.EE, ProfileType.EC }  },
            // •	ЕВ - *Е, ЕВ - В*, ЕВ - *С, ЕВ - ВС, ЕВ - ЕЕ, ЕВ – ЕС, ЕВ - ВЕ.
            { ProfileType.EB, new List<string> { ProfileType.AE, ProfileType.BA, ProfileType.AC, ProfileType.BC, ProfileType.EE, ProfileType.EC, ProfileType.BE }  },
            
            // •	В* - Е*, *В - *Е, В* -ЕВ , *В - *С, *В - ЕС, В* - ЕС, *В – ВС, В* - ЕЕ, *В – ЕЕ, *В - ВЕ.
            { ProfileType.BA, new List<string> { ProfileType.EA, ProfileType.EB , ProfileType.EC, ProfileType.EE }  },
            { ProfileType.AB, new List<string> { ProfileType.AE, ProfileType.AC, ProfileType.EC, ProfileType.BC, ProfileType.EE, ProfileType.BE  }  },

            // •	*С - *Е, *С - *В, *С – ЕВ, *С – ВЕ, *С - ЕЕ.
            { ProfileType.AC, new List<string> { ProfileType.AE, ProfileType.AB, ProfileType.EB, ProfileType.BE, ProfileType.EE }  },

            // •	Е* - В*, *Е - *В, *Е - *С, *Е – ЕВ, *Е – ВС, Е* - ВС, *Е – ЕС, Е* - ВЕ.
            { ProfileType.EA, new List<string> { ProfileType.AE, ProfileType.AB, ProfileType.EB, ProfileType.BE, ProfileType.EE }  },

            // •	ЕС - В*, ЕС - *В, ЕС - *Е, ЕС – ЕВ, ЕС – ВЕ, ЕС - ВС, ЕС – ЕЕ
            { ProfileType.EC, new List<string> { ProfileType.BA, ProfileType.AB, ProfileType.AE, ProfileType.EB, ProfileType.BE, ProfileType.BC, ProfileType.EE  }  },

            //•	ЕЕ - В*, ЕЕ - *В, ЕЕ - *С, ЕЕ – ЕВ, ЕЕ – ВЕ, ЕЕ - ВС, ЕЕ – ЕС.
            { ProfileType.EE, new List<string> { ProfileType.BA, ProfileType.AB, ProfileType.AC, ProfileType.EB, ProfileType.BE, ProfileType.BC, ProfileType.EC  }  },

            // •	ВЕ - *В, ВЕ - Е*, ВЕ - *С, ВЕ – ЕВ, ВЕ – ВС, ВЕ - ЕС, ВЕ – ЕЕ.
            { ProfileType.BE, new List<string> { ProfileType.AB, ProfileType.EA, ProfileType.AC, ProfileType.EB, ProfileType.BC, ProfileType.EC, ProfileType.EE  }  }
        };

        /// <summary>
        /// Проыерка на правильность перехода с одного уровня на другой
        /// </summary>
        /// <param name="layer1"></param>
        /// <param name="layer2"></param>
        /// <returns></returns>
        public static bool IsWrongLayerChanges(string layer1, string layer2)
        {
            bool res = false;

            if(ChangeLayerError.ContainsKey(layer1))
            {
                if (ChangeLayerError[layer1].Contains(layer2))
                {
                    res = true;
                }
            }

            return res;
        }
    }
}
