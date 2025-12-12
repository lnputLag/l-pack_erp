using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Client.Common
{
    /// <summary>
    /// адрес навигации
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public class NavigationAddress
    {
        private string ProgramName = "l-pack_erp";
        
        public string Schema { get; set; }
        public string Programm { get; set; }
        public string Address { get; set; }
        public List<string> AddressInner { get; set;}
        public string AddressRaw { get; set; }
        public Dictionary<string,string> Params { get; set; }
        public string Anchor { get; set; }
        public bool Processed { get; set; }

        public NavigationAddress()
        {
            Schema="";
            Programm="";
            Address="";
            AddressInner=new List<string>();
            AddressRaw="";
            Params=new Dictionary<string, string>();
            Anchor="";
            Processed=true;
        }

        public bool Parse( string url )
        {
              /*
                SCHEMA://PROGRAM/ADDRESS?PARAMETERS#ANCHOR
                l-pack://l-pack_erp/service/task_control/tasks?id=235&date=29.04.2020
                l-pack://l-pack_erp/service/task_control/tasks#important

                1 SCHEMA     -- псевдопротокол (l-pack)
                2 PROGRAM    -- имя приложения, системы (l-pack_erp)
                3 ADDRESS    -- адрес элемента
                4 PARAMETERS -- набор параметров (key=value&key2=value2)
                5 ANCHOR     -- якорь (id "секции", "перспективы вида" и т.д.)
             */

            bool resume = true;
            bool result=false;

            //Central.Dbg($"ProcessURL url=[{url}]");

            if( resume )
            {
                if( !string.IsNullOrEmpty( url ) )
                { 
                    url=url.Trim();
                }
                else
                {
                    resume=false;
                    Central.Dbg($"    Url is empty");
                }
            }

            string s="";

            // Определяем схему
            if (resume)
            {
                var scIndex = url.IndexOf(":");
                if (scIndex > -1)
                {
                    Schema = url.Substring(0, scIndex);
                }

                if (Schema == "l-pack")
                {
                    var pnPos = url.IndexOf(ProgramName);
                    var pnLen = ProgramName.Length;
                    if (pnPos > -1)
                    {
                        // l-pack://l-pack_erp/service/task_control/tasks?id=235&date=29.04.2020
                        //                    >.................................  
                        s = url.Substring((pnPos + pnLen), url.Length - (pnPos + pnLen)).ToLower();
                        //Central.Dbg($"Parse s=[{s}]");
                    }
                    else
                    {
                        resume = false;
                        Central.Dbg($"    Cant parse address");
                    }
                }
                else if (Schema == "action")
                {
                    s = url.Substring(9);
                }
                // Если схема = file, то адресом будет путь к файлу
                else if (Schema == "file")
                {
                    string filePath = url.Substring(5);
                    if (File.Exists(filePath))
                    {
                        AddressRaw = filePath;
                        result = true;
                    }
                    resume = false;
                }
                else
                {
                    resume = false;
                    Central.Dbg($"    Can't define scheme");
                }
            }

            string p="";

            if( resume )
            {
                var prPos=s.IndexOf( "?" );
                var prLen=1;
                if( prPos > -1 )
                {
                    // /service/task_control/tasks?id=235&date=29.04.2020
                    // >.........................< >....................<  
                    AddressRaw=s.Substring( 0, (prPos) );     
                    Address=AddressRaw;
                    p=s.Substring( (prPos+prLen), s.Length-(prPos+prLen) );                    
                    //Central.Dbg($"Parse AddressRaw=[{AddressRaw}] p=[{p}]");

                    result=true;
                }
                else
                {
                    // /service/task_control/tasks
                    // >.........................<
                    AddressRaw=s;     
                    Address=AddressRaw;
                    //Central.Dbg($"Parse AddressRaw=[{AddressRaw}]");

                    result=true;
                }
            }
            
            if( resume )
            {
                if( !string.IsNullOrEmpty( p ) )
                {
                    List<string> elements = p.Split('&').ToList();
                    if( elements.Count > 0 )
                    {
                        foreach( string e in elements )
                        {
                            string[] i = e.Split('=');
                            
                            string k="";
                            string v="";
                            if( !string.IsNullOrEmpty( i[0] ) )
                            {
                                k=i[0];
                                k=k.ToLower();
                            }
                            if( !string.IsNullOrEmpty( i[1] ) )
                            {
                                v=i[1];
                            }
                            //Central.Dbg($"    [{k}]=[{v}]");
                            Params.Add(k,v);
                        }
                    }
                }                
            }

            return result;
        }

        /// <summary>
        /// получение последнего слова из адреса
        /// </summary>
        /// <returns></returns>
        public string GetLastBit()
        {
            string result="";
            if(AddressInner != null)
            {
                var m=AddressInner.Count;
                m=m-1;
                if (m >= 0)
                {
                    if (AddressInner[m] != null)
                    {
                        result = AddressInner[m];
                        result = result.ClearCommand();
                    }
                }
            }
            return result;
        }

    }
}
