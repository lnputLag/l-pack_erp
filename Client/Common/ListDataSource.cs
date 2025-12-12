using Client.Interfaces.Main;
using Prism.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;

namespace Client.Common
{
    /// <summary>
    /// 
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-07-12</released>
    /// <changed>2024-07-12</changed>
    public class ListDataSource
    {
        public ListDataSource()
        {
            Initialized = false;
            Columns = new List<DataGridHelperColumn>();
            DataSet = null;
        }

        public bool Initialized { get; set; }
        public ListDataSet DataSet { get; set; }
        public List<DataGridHelperColumn> Columns { get; set; }

        public void Init()
        {
            if (!Initialized)
            {
                Initialized = true;
            }
        }

        public bool CheckInitialized()
        {
            bool result=Initialized;
            if(Initialized)
            {
            }
            return result;
        }

        
    }
}
