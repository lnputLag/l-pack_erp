using Prism.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;

namespace Client.Common
{
    /// <summary>
    /// Список строк
    /// Датасет, класс, используемый для получения данных запроса к серверу.    
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public class ListDataSet
    {
        public ListDataSet()
        {
            Cols = new List<string>();
            AdditionalCols=new List<string>();
            Rows = new List<List<string>>();
            Initialized = false;
            Items = new List<Dictionary<string, string>>();
        }

        private Dictionary<string, int> ColsIndex { get; set; }
        public List<string> Cols { get; set; }
        public List<string> AdditionalCols { get; set; }
        public List<List<string>> Rows { get; set; }
        public List<Dictionary<string, string>> Items { get; set; }
        public bool Initialized { get; set; }

        public void Init()
        {
            if (!Initialized)
            {
                if (AdditionalCols.Count > 0)
                {
                    int j = 0;
                    foreach (var c in AdditionalCols)
                    {
                        Cols.Add(c.ToString());
                        j++;
                    }
                }
                
                
                ColsIndex = new Dictionary<string, int>();
                if (Cols.Count > 0)
                {
                    int j = 0;
                    foreach (var c in Cols)
                    {
                        ColsIndex.Add(c, j);
                        j++;
                    }
                }
                
                


                //Items=new List<Dictionary<string, string>>();
                if (Rows.Count > 0)
                {
                    int j = 0;
                    foreach (var r in Rows)
                    {
                        int i = 0;
                        var row = new Dictionary<string, string>();
                        foreach (var c in r)
                        {
                            if(i<Cols.Count)
                            {
                                var k=Cols[i];
                                var v=c;

                                if(k=="ID" || k=="KEY")
                                {
                                    v=v.ToInt().ToString();
                                }

                                row.Add(k,v);                            
                                i++;
                            }
                        }

                        if(!row.ContainsKey("_"))
                        {
                            row.Add("_","");
                            i++;
                        }

                        if(!row.ContainsKey("_SELECTED"))
                        {
                            row.Add("_SELECTED","");
                            i++;
                        }
                        

                        Items.Add(row);                        
                        j++;
                    }

                    Rows.Clear();
                }


                Initialized = true;
            }
        }

        public bool CheckInitialized()
        {
            bool result=Initialized;
            if(Initialized)
            {
                //if(DataSet==null)
                //{
                //    Initialized=false;
                //}
            }
            return result;
        }

        public string GetFirstItemValueByKey(string key="ID")
        {
            string result="";
            CheckInitialized();
            if(Initialized)
            {
                if(Items.Count>0)
                {
                    var first=Items.First();
                    if(first!=null)
                    {
                        if(first.ContainsKey(key))
                        {
                            if(first[key]!=null)
                            {
                                result=first[key].ToString();
                            }
                        }
                    }                    
                }
            }            
            return result;
        }

        public Dictionary<string, string> GetItemByKeyValue(string key="ID", string value="")
        {
            var result=new Dictionary<string, string>();
            CheckInitialized();
            if(Initialized)
            {
                if(Items.Count>0)
                {
                    foreach(Dictionary<string, string> row in Items)
                    {
                        var v=row.CheckGet(key);
                        if(v==value)
                        {
                            result=row;
                        }
                    }
                }
            }            
            return result;
        }

        public void AddItem(Dictionary<string, string> row)
        {
            CheckInitialized();    
            if(Initialized)
            {
                Items.Add(row);
            }
        }

        public void RemoveItemByKeyValue(string key="ID", string value="")
        {
            CheckInitialized();
            if(Initialized)
            {
                if(Items.Count>0)
                {
                    var items2=new List<Dictionary<string, string>>(Items);
                    foreach(Dictionary<string, string> row in items2)
                    {
                        var v=row.CheckGet(key);
                        if(v==value)
                        {
                            Items.Remove(row);
                        }
                    }
                }
            }            
        }

        public Dictionary<string, string> GetFirstItem()
        {
            var result=new Dictionary<string, string>();
            CheckInitialized();
            if(Initialized)
            {
                if(Items.Count>0)
                {
                    result=Items.First();
                    
                }
            }            
            return result;
        }

        /// <summary>
        /// возврат "среза" элементов
        /// будет возвращен словарь, с указанными ключами
        /// </summary>
        /// <param name="k"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public Dictionary<string,string> GetItemsList(string k="KEY", string v="VALUE", bool distinct=false)
        {
            var result=new Dictionary<string,string>();
            CheckInitialized();
            if(Initialized)
            {
                if(Items.Count>0)
                {                                                        
                    foreach(Dictionary<string,string> row in Items)
                    {
                        if(row.ContainsKey(k) && row.ContainsKey(v)) 
                        {
                            if(!distinct)
                            {
                                result.Add(row[k],row[v]);
                            }
                            else
                            {
                                if(!result.ContainsKey(row[k]))
                                {
                                    result.Add(row[k],row[v]);
                                }
                            }                            
                        }
                    }
                }   
            }
            return result;
        }
      
        /// <summary>
        /// добавляет колонки в датасет
        /// на входе: словарь ключ-значение по умолчанию
        /// </summary>
        public void AddColumns(Dictionary<string,string> columns)
        {
            CheckInitialized();    
            if(Initialized)
            {
                if(Items!=null)
                {
                    if(Items.Count>0)
                    {
                        foreach(Dictionary<string,string> row in Items)
                        {
                            foreach(KeyValuePair<string,string> col in columns)
                            {
                                var k=col.Key;
                                var v=col.Value;
                                row.CheckAdd(k,v);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// добавление элемента в список элементов
        /// в начало списка
        /// </summary>
        /// <param name="item"></param>
        public void ItemsPrepend(Dictionary<string,string> item)
        {
            CheckInitialized();
            if (Initialized)
            {
                var list=new List<Dictionary<string,string>>();
                list.Add(item);
                if(Items.Count>0)
                {
                    list.AddRange(Items);
                }
                Items=list;
            }
        }

        /// <summary>
        /// Делает копию ListDataSet
        /// </summary>
        /// <returns></returns>
        public ListDataSet Clone()
        {
            // var resultDataSet = JsonConvert.DeserializeObject<ListDataSet> (JsonConvert.SerializeObject(GroupDataSet));

            ListDataSet result = new ListDataSet();

            foreach (var cols in Cols) result.Cols.Add(cols);
            foreach(var additionalCols in AdditionalCols) result.AdditionalCols.Add(additionalCols);
            foreach(var rows in Rows) result.Rows.Add(rows.ToList<string>());
            result.Initialized = Initialized;
            foreach (var dict in Items) result.Items.Add(dict.ToDictionary(entry => entry.Key,
                                               entry => entry.Value));

            result.ColsIndex = ColsIndex.ToDictionary(entry => entry.Key,
                                               entry => entry.Value);

            return result;
        }


        /// <summary>
        /// Поиск секции в ответе и инициализация датасета.
        /// Ответ -- массив данных. Каждая секция -- датасет.
        /// Производится поиск секции с указанным ключом, если секция существует,
        /// будет создан датасет и проинициализирован.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static ListDataSet CreateFromResultSection(Dictionary<string,ListDataSet> result, string key, List<string> additionalCols=null)
        {
            /*
                автоматизирует следующий код:

                var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);
                if(result!=null)
                {
                    if(result.ContainsKey("TOTALS"))
                    {
                        totalsDS=(ListDataSet)result["ForkliftDrivers"];
                        ForkliftDriverDS?.Init();
                        ForkliftDriverId.GridDataSet=ForkliftDriverDS;
                    }
                }

                =>

                var result = JsonConvert.DeserializeObject<Dictionary<string,ListDataSet>>(q.Answer.Data);    
                totalsDS=ListDataSet.Create(result);
                if(totalsDS.Initialized){
                }
             
             */

            var ds=new ListDataSet();
            

            if(!string.IsNullOrEmpty(key))
            {
                if(result.Count>0)
                {
                    if(result.ContainsKey(key))
                    {
                        ds=(ListDataSet)result[key];
                        if(ds!=null)
                        {
                            if(additionalCols!=null)
                            {
                                ds.AdditionalCols=additionalCols;
                            }            
                            ds.Init();
                        }                                                
                    }
                }
            }

            return ds;
        }

        /// <summary>
        /// Поиск секции в ответе и инициализация датасета.
        /// Ответ -- массив данных. Каждая секция -- датасет.
        /// Производится поиск секции с указанным ключом, если секция существует,
        /// будет создан датасет и проинициализирован.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static ListDataSet Create(Dictionary<string,ListDataSet> result, string key)
        {
            return CreateFromResultSection(result, key);
        }

        public static ListDataSet Create(Dictionary<string,ListDataSet> result, string key, List<string> additionalCols)
        {
            return CreateFromResultSection(result, key, additionalCols);
        }

        public static ListDataSet Create2(List<Dictionary<string, string>> list)
        {
            var ds = new ListDataSet();

            var cols = new List<string>();
            var rows = new List<List<string>>();

            var rowColsMax = 0;
            var rowColsIndex = 0;
            {
                var j = 0;
                foreach(Dictionary<string, string> row in list)
                {
                    var c = row.Count;
                    if(c > rowColsMax)
                    {
                        rowColsMax = c;
                        rowColsIndex = j;
                    }
                    j++;
                }
            }

            {
                var j = 0;
                foreach(Dictionary<string, string> row in list)
                {
                    if(j == rowColsIndex)
                    {
                        foreach(KeyValuePair<string, string> item in row)
                        {
                            cols.Add(item.Key);
                        }
                        break;
                    }
                    j++;
                }
            }

            foreach(Dictionary<string, string> row in list)
            {
                if(cols.Count == 0)
                {
                    foreach(KeyValuePair<string, string> item in row)
                    {
                        cols.Add(item.Key);
                    }
                }

                var oneRow = new List<string>();
                foreach(KeyValuePair<string, string> item in row)
                {
                    oneRow.Add(item.Value);
                }
                rows.Add(oneRow);
            }

            ds.Cols = cols;
            ds.Rows = rows;
            ds.Init();

            return ds;
        }

        public static ListDataSet Create(List<Dictionary<string,string>> list)
        {
            var ds=new ListDataSet();

            var cols=new List<string>();
            var rows=new List<List<string>>();

            foreach(Dictionary<string,string> row in list)
            {
                if(cols.Count==0)
                {
                    foreach(KeyValuePair<string,string> item in row)
                    {
                        cols.Add(item.Key);
                    }
                }

                var oneRow=new List<string>();
                foreach(KeyValuePair<string,string> item in row)
                {
                    oneRow.Add(item.Value);
                }
                rows.Add(oneRow);
            }

            ds.Cols=cols;
            ds.Rows=rows;
            ds.Init();

            return ds;
        }

        public static void ExportDS(ListDataSet ds)
        {
            bool resume=true;

            var fileData="";
            var today=DateTime.Now.ToString("ds_dd-MM-yyyy_HH-mm-ss");
            var fileName=$"{today}.csv";

            if(resume)
            {
                if(ds!=null)
                {
                    if(ds.Items.Count>0)
                    {
                        {
                            var i=0;
                            foreach(Dictionary<string,string> row in ds.Items)
                            {
                                i++;
                                if(i==1)
                                {
                                    foreach(KeyValuePair<string,string> item in row)
                                    {
                                        fileData=fileData.Append($"{item.Key};");                                        
                                    }
                                    fileData=fileData.AddCR();
                                }      
                                
                            }
                        }
                        

                        {
                            var i=0;
                            foreach(Dictionary<string,string> row in ds.Items)
                            {
                                i++;
                                {
                                    foreach(KeyValuePair<string,string> item in row)
                                    {
                                        fileData=fileData.Append($"{item.Value};");
                                    }
                                }  
                                fileData=fileData.AddCR();
                            }
                        }
                    }
                }
            }

            var f="";
            if(resume)
            {
                if(!fileData.IsNullOrEmpty())
                {
                    f = $"{System.IO.Path.GetTempPath()}{fileName}";
                    System.IO.File.WriteAllText(f, fileData);
                }
            }

            if(resume)
            {
                if(System.IO.File.Exists(f))
                {
                    Central.OpenFile(f);
                }
            }
               
        }

        public static List<Dictionary<string, string>> AddColumnToList(List<Dictionary<string, string>> list, string key, string key2, Dictionary<string, string> dict)
        {
            if(list.Count > 0)
            {
                foreach(Dictionary<string, string> row in list)
                {
                    var k=row.CheckGet(key).ToInt().ToString();
                    var v=dict.CheckGet(k);
                    row.CheckAdd(key2,v);
                }
            }
            return list;
        }

        public static Dictionary<string, string> AddColumnToRow(Dictionary<string, string> row, string key, string key2, Dictionary<string, string> dict)
        {
            var k=row.CheckGet(key).ToInt().ToString();
            var v=dict.CheckGet(k);
            row.CheckAdd(key2,v);
            return row;
        }
    }
}
