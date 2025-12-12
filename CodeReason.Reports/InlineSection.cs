using System.Collections.Generic;

namespace CodeReason.Reports
{
    public class InlineSection
    {
        public InlineSection()
        {
            Init();
        }
        public InlineSection(string tableName)
        {
            TableName=tableName;
            Init();
        }
        public InlineSection(string tableName,string blockName)
        {
            TableName=tableName;
            BlockName=blockName;
            Init();
        }
        
        public void Init()
        {
            Rows=new List<object>();
        }

        public string TableName { get; set; }        
        public string BlockName { get; set; }        
        public List<object> Rows { get; set;}

    }
}
