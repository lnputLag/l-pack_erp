using CodeReason.Reports.Interfaces;
using System;
using System.Windows;
using System.Windows.Documents;

namespace CodeReason.Reports.Document
{
    /// <summary>
    /// TableRow which is visible if data row conditional is met
    /// </summary>
    public class TableCellStyled : TableCell, ITableCellStyled
    {
        
        public string ColumnName { get; set; }

        public void AssignStyle(ReportData reportData, string tableName, TableRow tableRow, int rowIndex)
        {
            
            if (reportData == null) throw new ArgumentNullException("reportData");

            if( reportData.StyleTables.Count>0 )
            {
                // find our table section in dataarray
                foreach( StyleTable s in reportData.StyleTables )
                {
                    if( (string)s.TableName == (string)tableName )
                    {
                        

                        if( s.Rows.Count > 0 && s.Rows.Count>=(rowIndex+1) )
                        {
                            if( s.Rows[rowIndex] != null )
                            {
                                // find row by id
                                var row=s.Rows[rowIndex];
                                
                                var n=ColumnName;

                                if( !string.IsNullOrEmpty( ColumnName ) )
                                {
                                    if( row.ContainsKey( ColumnName )  )
                                    {
                                        if( !string.IsNullOrEmpty( row[ColumnName] ) )
                                       {
                                            var style=(Style)tableRow.FindResource( (string)row[ColumnName] );
                                            if( style != null )
                                            {
                                                Style=style;
                                            }
                                        }
                                    }
                                }

                                
                            }
                        }

                        
                    }
                }
            }

        }

    }
}
