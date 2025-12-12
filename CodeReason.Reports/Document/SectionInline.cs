/************************************************************************
 * Copyright: 
 *
 * License:  
 *
 * Author:   DM
 *
 ************************************************************************/

using CodeReason.Reports.Interfaces;
using System;
using System.Windows.Documents;

namespace CodeReason.Reports.Document
{
    /// <summary>
    /// </summary>
    public class SectionInline : Section, ISectionInline 
    {
        /// <summary>
        /// Gets or sets the property name
        /// </summary>
        public string BlockName { get; set; }


        public void FillSection(ReportData reportData, string tableName, int rowIndex)
        {

            if (reportData == null) throw new ArgumentNullException("reportData");

            if( reportData.InlineSections.Count>0 )
            {
                // find our table section in dataarray
                foreach( InlineSection s in reportData.InlineSections )
                {
                    if( (string)s.TableName == (string)tableName )
                    {
                        if( (string)s.BlockName == (string)BlockName )
                        {

                            if( s.Rows.Count > 0 && s.Rows.Count>=(rowIndex+1) )
                            {
                                if( s.Rows[rowIndex] != null )
                                {
                                    // find row by id
                                    var block=s.Rows[rowIndex];
                                    Blocks.Add((Block)block);
                                    
                                }
                            }

                        }
                    }
                }
            }

            /*
            var p=new Paragraph(new Run($"#INLINE table:[{tableName}] row:[{rowIndex}] [{BlockName}]#"));                                        
            p.Margin=new Thickness(0,0,0,0);
            Blocks.Add(p);
            */
        }
    }
}
