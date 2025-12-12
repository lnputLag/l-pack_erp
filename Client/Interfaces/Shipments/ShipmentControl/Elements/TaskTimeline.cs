using Client.Assets.HighLighters;
using Client.Common;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Interfaces.Shipments
{
    /// <summary> 
    /// Вспомогательный класс: таймлайн с блоками
    /// </summary>
    /// <author>balchugov_dv</author>   
    public class TaskTimeline:TimelineBLock
    {
        public TaskTimeline()
        {
            StartHour = 8;
            RowIndexMax = 0;
            ColumnIndexMax = 10;
            ColumnWidth = 100;
            RowHeight = 60;
            RowStyle = "SHMonBlockRowStyle";
            //ColumnStyle = "SHMonBlockColStyle";
            RowsMap=new Dictionary<int, int>();
        }
        
        public int StartHour { get; set; }
        // FORKLIFT_ID - row id
        public Dictionary<int,int> RowsMap { get; set; }

        public override void PrepareItems()
        {
            if (Items.Count>0)
            {
                /*
                    расстановка блоков по гриду:
                    -- назначение колонки: по времени отгрузки
                           час отгрузки
                    -- назначение столбца: по времени отгрузки
                           по возрастанию времени
                           
                   ID
                   SHIPMENT_DATE_TIME dd.mm.yyyy hh24:mi:ss
                 */

                //последний индекс элемента в строке zerobased
                var rowColumns = new Dictionary<int, int>();

                int j = 0;
                int rowId = 0;
                foreach (TimelineItem item in Items)
                {
                    var id = item.Values.CheckGet("ID").ToInt();

                    var forkliftId = item.Values.CheckGet("FORKLIFT_ID").ToInt();

                    var r = 0;
                    if (!RowsMap.ContainsKey(forkliftId))
                    {
                        RowsMap.Add(forkliftId,rowId);
                        rowId++;
                    }
                    r = RowsMap[forkliftId];
                    
                    var c = 0;
                    if (!rowColumns.ContainsKey(r))
                    {
                        rowColumns.Add(r,0);
                    }
                    c = rowColumns[r];
                    rowColumns[r]++;
                    
                    item.Index = $"{id}";
                    item.RowIndex = r;
                    item.ColIndex = c;

                    {
                        if (c>ColumnIndexMax)
                        {
                            ColumnIndexMax = c;
                        }

                        if (r>RowIndexMax)
                        {
                            RowIndexMax = r;
                        }
                    }

                    j++;
                }
            }
        }
       
    }
}