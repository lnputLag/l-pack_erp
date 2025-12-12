/************************************************************************
 * Copyright: 
 *
 * License:  Freeware
 *
 * Author:   DM
 *
 ************************************************************************/

using CodeReason.Reports.Interfaces;

namespace CodeReason.Reports.Document
{
    /// <summary>
    /// Contains a single report value that is to be displayed on the report (e.g. report title)
    /// </summary>
    public class InlineObject : InlinePropertyValue, IInlineObject, IInlinePropertyValue
    {
        private string _aggregateGroup;
        /// <summary>
        /// Gets or sets the aggregate group
        /// </summary>
        public string AggregateGroup
        {
            get { return _aggregateGroup; }
            set { _aggregateGroup = value; }
        }
    }
}
