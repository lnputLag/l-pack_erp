using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Client.Common
{
    /// <summary>
    /// реестр локальных параметров на клиентской стороне
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-09-19</released>
    /// <changed>2023-09-20</changed>
    public class Settings
    {
        /*
            Реестр локальных параметров предназначен для хранения параметров на стороне клиентской программы.
            При старте программы параметры считываются из файла.
            При необходимости, параметры записываются в файл для дальнейшего использования.
            В реестре хранятся произвольные данные. Формат следующий:
            
                Dictionary<string,List<Dictionary<string,string>>>

            Например:
                
                [PRINTING_SETTINGS]
                    {
                        [PRINTER_NAME]="printer1";
                        [WIDTH]="210";
                        [HEIGHT]="297";
                    },
                    {
                        [PRINTER_NAME]="printer2";
                        [WIDTH]="72";
                        [HEIGHT]="800";
                    }
         
         */

        public Settings()
        {
            FileName="application.settings";
            Params=new Dictionary<string, List<Dictionary<string, string>>>();
        }

        private string FileName {get;set;}
        private Dictionary<string,List<Dictionary<string,string>>> Params {get;set;}

        /// <summary>
        /// восстановление состояния из файла
        /// </summary>
        public void Restore()
        {
            var pathInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
            string settingsFile = $"{pathInfo.Directory}\\{FileName}";

            try
            {
                if(System.IO.File.Exists(settingsFile))
                {
                    var settingsContent=System.IO.File.ReadAllText(settingsFile);
                    if(!settingsContent.IsNullOrEmpty())
                    {
                        var result = JsonConvert.DeserializeObject<Dictionary<string,List<Dictionary<string,string>>>>(settingsContent);
                        if (result != null)
                        {
                            Params=result;
                        }
                    }
                }                
            }
            catch (Exception e)
            {
            }
            
        }

        /// <summary>
        /// запись состояния в файл
        /// </summary>
        public void Store()
        {
            var pathInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
            string settingsFile = $"{pathInfo.Directory}\\{FileName}";

            try
            {
                var content=JsonConvert.SerializeObject(Params);
                if(!content.IsNullOrEmpty())
                {
                    System.IO.File.WriteAllText(settingsFile,content);
                }
            }
            catch (Exception e)
            {
            }
        }

        public List<Dictionary<string,string>> SectionGet(string sectionName)
        {
            var result=new List<Dictionary<string,string>>();
            if(!sectionName.IsNullOrEmpty())
            {
                foreach(KeyValuePair<string,List<Dictionary<string,string>>> item in Params)
                {
                    if(item.Key==sectionName)
                    {
                        result=item.Value;
                    }
                }
            }
            return result;
        }

        public Dictionary<string,string> SectionFindRow(string sectionName, string rowKey, string rowValue)
        {
            var result=new Dictionary<string,string>();
            if(!sectionName.IsNullOrEmpty() && !rowKey.IsNullOrEmpty())
            {
                var section=SectionGet(sectionName);    
                if(section.Count > 0)
                {
                    foreach(Dictionary<string,string> row in section)
                    {
                        if(row.CheckGet(rowKey) == rowValue)
                        {
                            result=row;
                            break;
                        }
                    }
                }                
            }
            return result;
        }

        public Dictionary<string,string> SectionDeleteRow(string sectionName, string rowKey, string rowValue)
        {
            var result=new Dictionary<string,string>();
            if(!sectionName.IsNullOrEmpty() && !rowKey.IsNullOrEmpty())
            {
                var section=SectionGet(sectionName);    
                if(section.Count > 0)
                {
                    var list=new List<Dictionary<string,string>>(section);
                    foreach(Dictionary<string,string> row in list)
                    {
                        if(row.CheckGet(rowKey) == rowValue)
                        {
                            section.Remove(row);
                        }
                    }
                }                
            }
            return result;
        }
        

        public void SectionAddRow(string sectionName, Dictionary<string,string> row, string rowKey="NAME")
        {
            if(!sectionName.IsNullOrEmpty())
            {
                var section=SectionGet(sectionName);    
                if(section.Count == 0)
                {
                    var newSection=new List<Dictionary<string,string>>();
                    if(!Params.ContainsKey(sectionName))
                    {
                        Params.Add(sectionName,newSection);
                    }
                    //Params.CheckAdd(sectionName,newSection);
                }

                section=SectionGet(sectionName);  

                var sectionOld=new List<Dictionary<string,string>>(section);
                foreach(Dictionary<string,string> r in sectionOld)
                {
                    if(r.CheckGet(rowKey) == row.CheckGet(rowKey))
                    {
                        section.Remove(r);
                    }
                }

                section.Add(row);
            }    
            
            Store();
        }
    }
}
