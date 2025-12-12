using System.Collections.Generic;

namespace CodeReason.Reports
{
    public class StyleTable
    {
        public StyleTable()
        {
            Init();
        }
        public StyleTable(string tableName)
        {
            TableName=tableName;
            Init();
        }
        
        public void Init()
        {
            Rows=new List<Dictionary<string,string>>();
        }

        public string TableName { get; set; }                
        public List<Dictionary<string,string>> Rows { get; set;}

    }
}
