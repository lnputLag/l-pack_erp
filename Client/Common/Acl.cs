using System.Collections.Generic;
using System.Linq;

namespace Client.Common
{
    /// <summary>
    /// Acess Control Lists 
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version> 
    /// <released>2022-08-03</released>
    /// <changed>2022-08-03</changed>
    public class Acl
    {
        public static Dictionary<string, string> GetAccessModeList()
        {
            var result=new Dictionary<string, string>(){
                {"0","Запрещен"},
                {"1","Только чтение"},
                {"2","Полный доступ"},
                {"3","Спецправа"},
            };
            return result;
        }

        public const string AccessTag = "access_mode_";
        public const string ReadOnlyTag = "read_only";
        public const string FullAccessTag = "full_access";
        public const string SpecialTag = "special";
        public static Role.AccessMode FindTagAccessMode(List<string> tagList)
        {
            Role.AccessMode accessMode = Role.AccessMode.ReadOnly;

            if (tagList != null && tagList.Count > 0)
            {
                string accessTag = tagList.FirstOrDefault(x => x.Contains(AccessTag));
                if (!string.IsNullOrEmpty(accessTag))
                {
                    accessTag = accessTag.Replace(AccessTag, "");
                    switch (accessTag)
                    {
                        case ReadOnlyTag:
                            accessMode = Role.AccessMode.ReadOnly;
                            break;

                        case FullAccessTag:
                            accessMode = Role.AccessMode.FullAccess;
                            break;

                        case SpecialTag:
                            accessMode = Role.AccessMode.Special;
                            break;

                        default:
                            accessMode = Role.AccessMode.ReadOnly;
                            break;
                    }
                }
            }

            return accessMode;
        }
    }
}
