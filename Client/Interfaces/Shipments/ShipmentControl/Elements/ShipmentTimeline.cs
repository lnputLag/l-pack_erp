using Client.Common;
using System;
using System.Collections.Generic;

namespace Client.Interfaces.Shipments
{
    /// <summary> 
    /// Вспомогательный класс: таймлайн с блоками
    /// </summary>
    /// <author>balchugov_dv</author>   
    public class ShipmentTimeline:TimelineBLock
    {
        public ShipmentTimeline()
        {
            StartHour = 8;
            BaseDay = 0;
            BaseDateTime=DateTime.Now;
            RowIndexMax = 24;
            ColumnIndexMax = 10;
            ColumnWidth = 100;
            RowHeight = 60;
            RowStyle = "SHMonBlockRowStyle";
            //ColumnStyle = "SHMonBlockColStyle";
        }
        
        public int StartHour { get; set; }
        public int BaseDay { get; set; }
        public DateTime BaseDateTime { get; set; }

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

                var baseDay = 0;
                if (BaseDay==0)
                {
                    baseDay = DateTime.Now.ToString("dd").ToInt();
                }
                else
                {
                    baseDay = BaseDay;
                }
                
                int j = 0;
                foreach (TimelineItem item in Items)
                {
                    var id = item.Values.CheckGet("ID").ToInt();
                    int r = 0;

                    if (item.Values.CheckGet("LATE").ToInt() > 0
                        || item.Values.CheckGet("UNSHIPPED").ToInt() > 0)
                    {
                        r = 0;
                    }
                    else
                    {
                        r = j;
                        if (!string.IsNullOrEmpty(item.Values.CheckGet("SHIPMENT_DATE_TIME")))
                        {
                            var dt = item.Values.CheckGet("SHIPMENT_DATE_TIME").ToDateTime();
                            var h = dt.ToString("HH").ToInt();
                            var d = dt.ToString("dd").ToInt();

                            r = h - StartHour;

                            int lastDayInMonth = 0;
                            {
                                var currentYear = BaseDateTime.ToString("yyyy").ToInt();
                                var currentMonth = BaseDateTime.ToString("MM").ToInt();
                                lastDayInMonth = DateTime.DaysInMonth(currentYear, currentMonth);
                            }

                            //переход суток: продолжаем отсчет строк
                            //первый день в месяце
                            var dc = 0;
                            if (
                                d > baseDay
                                || (
                                    d == 1
                                    && (baseDay == lastDayInMonth)
                                ))
                            {
                                r = r + 24;
                                dc = 1;
                            }

                            //Central.Logger.Debug($"shipment=[{dt.ToString()}] start hour=[{StartHour}] shipment hour=[{h}] base day=[{baseDay}] r=[{r}] dc=[{dc}]");

                            if (r < 0)
                            {
                                r = Math.Abs(r);
                            }
                        }

                        j++;
                    }

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
                }
            }
        }
       
    }
}