/************************************************************************
 * Copyright: 
 *
 * License:  
 *
 * Author:   DM
 *
 ************************************************************************/

using System.Windows.Documents;

namespace CodeReason.Reports.Interfaces
{
    /// <summary>
    /// </summary>
    public interface ITableCellStyled
    {
        /// <summary>
        /// Gets or sets the property name
        /// </summary>
        string ColumnName { get; set; }

        void AssignStyle(ReportData reportData, string tableName, TableRow tableRow, int rowIndex);
    }
}
