using System;
using System.Reflection;

namespace Client.Common
{
    /// <summary>
    /// структура параметров сборки
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public class AssemblyInfo
    {
        public string Title = "", Description = "", Company = "",
        Product = "", Copyright = "", Trademark = "",
        AssemblyVersion = "", FileVersion = "", Guid = "",
        AssemblyDate = "",
        NeutralLanguage = "";
        public bool IsComVisible = false;

        public AssemblyInfo() : this( Assembly.GetExecutingAssembly() )
        {

        }

        public AssemblyInfo( Assembly assembly )
        {
            AssemblyTitleAttribute titleAttr = GetAssemblyAttribute<AssemblyTitleAttribute>(assembly);
            if( titleAttr != null )
                Title = titleAttr.Title;

            AssemblyDescriptionAttribute assemblyAttr =GetAssemblyAttribute<AssemblyDescriptionAttribute>(assembly);
            if( assemblyAttr != null )
                Description =assemblyAttr.Description;

            AssemblyCompanyAttribute companyAttr =GetAssemblyAttribute<AssemblyCompanyAttribute>(assembly);
            if( companyAttr != null )
                Company = companyAttr.Company;

            AssemblyProductAttribute productAttr =GetAssemblyAttribute<AssemblyProductAttribute>(assembly);
            if( productAttr != null )
                Product = productAttr.Product;

            AssemblyCopyrightAttribute copyrightAttr =GetAssemblyAttribute<AssemblyCopyrightAttribute>(assembly);
            if( copyrightAttr != null )
                Copyright = copyrightAttr.Copyright;

            AssemblyTrademarkAttribute trademarkAttr =GetAssemblyAttribute<AssemblyTrademarkAttribute>(assembly);
            if( trademarkAttr != null )
                Trademark = trademarkAttr.Trademark;

            AssemblyVersion = assembly.GetName().Version.ToString();
           

            AssemblyFileVersionAttribute fileVersionAttr =GetAssemblyAttribute<AssemblyFileVersionAttribute>(assembly);
            if( fileVersionAttr != null )
                FileVersion = fileVersionAttr.Version;

           
        }


        public static T GetAssemblyAttribute<T>( Assembly assembly ) where T : Attribute
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(T), true);

            if( (attributes == null) || (attributes.Length == 0) )
                return null;
            
            return (T)attributes[0];
        }


        public static string getOSInfo()
        {
           //Get Operating system information.
           OperatingSystem os = Environment.OSVersion;
           //Get version information about the os.
           Version vs = os.Version;

           //Variable to hold our return value
           string operatingSystem = "";

           if (os.Platform == PlatformID.Win32Windows)
           {
               //This is a pre-NT version of Windows
               switch (vs.Minor)
               {
                   case 0:
                       operatingSystem = "95";
                       break;
                   case 10:
                       if (vs.Revision.ToString() == "2222A")
                           operatingSystem = "98SE";
                       else
                           operatingSystem = "98";
                       break;
                   case 90:
                       operatingSystem = "Me";
                       break;
                   default:
                       break;
               }
           }
           else if (os.Platform == PlatformID.Win32NT)
           {
               switch (vs.Major)
               {
                   case 3:
                       operatingSystem = "NT 3.51";
                       break;
                   case 4:
                       operatingSystem = "NT 4.0";
                       break;
                   case 5:
                       if (vs.Minor == 0)
                           operatingSystem = "2000";
                       else
                           operatingSystem = "XP";
                       break;
                   case 6:
                       if (vs.Minor == 0)
                           operatingSystem = "Vista";
                       else if (vs.Minor == 1)
                           operatingSystem = "7";
                       else if (vs.Minor == 2)
                           operatingSystem = "8";
                       else
                           operatingSystem = "8.1";
                       break;
                   case 10:
                       operatingSystem = "10";
                       break;
                   default:
                       break;
               }
           }
           //Make sure we actually got something in our OS check
           //We don't want to just return " Service Pack 2" or " 32-bit"
           //That information is useless without the OS version.
           if (operatingSystem != "")
           {
               //Got something.  Let's prepend "Windows" and get more info.
               operatingSystem = "Windows " + operatingSystem;
               //See if there's a service pack installed.
               if (os.ServicePack != "")
               {
                   //Append it to the OS name.  i.e. "Windows XP Service Pack 3"
                   operatingSystem += " " + os.ServicePack;
               }
               //Append the OS architecture.  i.e. "Windows XP Service Pack 3 32-bit"
               //operatingSystem += " " + getOSArchitecture().ToString() + "-bit";
           }
           //Return the information we've gathered.
           return operatingSystem;
        }

    }
}
