using System;
using System.Collections.Generic;

namespace Client.Common
{
    /// <summary>
    /// Одиночная строка.
    /// Датасет, класс, используемый для получения данных запроса к серверу.    
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public class RowDataSet
    {
        public RowDataSet()
        {
            Cols=new List<string>();
            Rows=new List<List<string>>();
            Initialized=false;
            Values=new Dictionary<string, string>();
        }

        public List<string> Cols { get; set; }
        public List<List<string>> Rows { get; set; }
        public Dictionary<string,string> Values { get; set;}

        private Dictionary<string,int> ColsIndex { get; set; }
        private bool Initialized { get; set; }

        public void Init()
        {
            if( !Initialized )
            {
                ColsIndex=new Dictionary<string, int>();
                if( Cols.Count > 0 )
                {
                    int j=0;
                    foreach(string c in Cols)
                    {
                        ColsIndex.Add(c,j);
                        j++;
                    }
                }

                
                //Items=new List<Dictionary<string, string>>();
                if( Rows.Count > 0 )
                {
                    int j=0;
                    foreach(List<string> r in Rows)
                    {
                        if(j==0)
                        {
                            int i=0;
                            foreach( string c in r )
                            {
                                Values.Add( Cols[i], c );
                                i++;
                            }
                        }
                        j++;
                    }

                    Rows.Clear();                    
                }

                Initialized=true;
            }
        }
        
        public T getValue<T>(string k)
        {
            //T result=default(T);

            if(!string.IsNullOrEmpty(k))
            {
                
                if(Values.Count>0)
                {
                    if(Values.ContainsKey(k))
                    {
                        if(Values[k]!=null)
                        {
                            return (T) Convert.ChangeType(Values[k], typeof(T));
                        }
                        //result=Convert.ChangeType( Values[k], typeof(T));
                    }
                }
            }

            //return (T) Convert.ChangeType(result, typeof(T));
            return default(T);
        }

        public string getValue(string k)
        {
            string result="";

            if(!string.IsNullOrEmpty(k))
            {
                
                if(Values.Count>0)
                {
                    if(Values.ContainsKey(k))
                    {
                        if(Values[k]!=null)
                        {
                            result=Values[k];
                        }
                    }
                }
            }

            return result;
        }

    }

}
