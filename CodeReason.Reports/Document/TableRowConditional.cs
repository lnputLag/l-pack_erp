using CodeReason.Reports.Interfaces;
using System;
using System.Windows.Documents;

namespace CodeReason.Reports.Document
{
    /// <summary>
    /// TableRow which is visible if data row conditional is met
    /// </summary>
    public class TableRowConditional : TableRow, ITableRowCondition
    {
        /// <summary>
        /// Inverts the condition
        /// </summary>
        public bool ConditionInvert { get; set; }

        /// <summary>
        /// Condition property name
        /// </summary>
        public string ConditionPropertyName { get; set; }

        /// <summary>
        /// Condition property value
        /// </summary>
        public object ConditionPropertyValue { get; set; }

        /// <summary>
        /// Checks if a condition is fulfilled
        /// </summary>
        /// <param name="reportData">report data</param>
        /// <returns>true, if condition is fulfilled</returns>
        /// <exception cref="ArgumentNullException">reportData</exception>
        public bool CheckConditionFulfilled(ReportData reportData, string tableName, int rowIndex)
        {
            if (reportData == null) throw new ArgumentNullException("reportData");
            if (ConditionPropertyName == null) return false;

            bool result=true;

            if( reportData.DataTables.Count>0 )
            {
                // find our table section in dataarray
                foreach( System.Data.DataTable dt in reportData.DataTables )
                {
                    if( (string)dt.TableName == (string)tableName )
                    {
                        // find needle column index
                        int colIndex=-1;
                        for (int c = 0; c < dt.Columns.Count; c++)
                        {
                            var col = dt.Columns[c];
                            if( col.Caption == ConditionPropertyName )
                            {
                                colIndex=c;
                            }
                        }

                        var ri=colIndex;
                        if( colIndex != -1 )
                        {
                            if( dt.Rows.Count > 0 )
                            {
                                if( dt.Rows[rowIndex] != null )
                                {
                                    // find row by id
                                    var row=dt.Rows[rowIndex];
                                    if( row[colIndex] != null )
                                    {
                                        var v=row[colIndex];
                                        if( (string)v == (string)ConditionPropertyValue )
                                        {
                                            result=true;
                                        }
                                        else
                                        {
                                            result=false;
                                        }
                                    }                                        
                                }
                            }
                        }                        
                    }
                }
            }

            if( ConditionInvert )
            {
                result=!result;
            }

            return result;
        }

        /// <summary>
        /// Changes the visibility of this TextBlock if needed
        /// </summary>
        /// <param name="data">report document data</param>
        public void PerformRenderUpdate(ReportData data)
        {
        }
    }
}
