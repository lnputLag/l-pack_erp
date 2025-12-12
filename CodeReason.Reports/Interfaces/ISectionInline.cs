/************************************************************************
 * Copyright: 
 *
 * License:  
 *
 * Author:   DM
 *
 ************************************************************************/

namespace CodeReason.Reports.Interfaces
{
    /// <summary>
    /// </summary>
    public interface ISectionInline
    {
        /// <summary>
        /// Gets or sets the property name
        /// </summary>
        string BlockName { get; set; }

        void FillSection(ReportData reportData, string tableName, int rowIndex);
    }
}
