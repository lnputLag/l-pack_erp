using Client.Common;
using Client.Interfaces.Main;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.Corrugator.TaskPlannings
{
    public class DxGridHelper
    {
        public static DataTable ConvertToDataTable(ListDataSet ds, List<DataGridHelperColumn> Columns)
        {
            DataTable dt = new DataTable();

            foreach (var columnName in ds.Cols)
            {
                Type t = typeof(string);
                var column = Columns.FirstOrDefault(x => x.Path == columnName);

                if (column != null)
                {
                    switch (column.ColumnType)
                    {
                        case ColumnTypeRef.Boolean:
                            t = typeof(bool);
                            break;
                        case ColumnTypeRef.Integer:
                            t = typeof(int);
                            break;
                        case ColumnTypeRef.Double:
                            t = typeof(double);
                            break;
                        case ColumnTypeRef.DateTime:
                            t = typeof(DateTime);
                            break;
                    }
                }

                dt.Columns.Add(columnName, t);
            }

            // проверить columns которые не присутствуют в datatable но есть в описании
            Columns.ForEach(column =>
            {
                if (!dt.Columns.Contains(column.Path))
                {
                    Type t = typeof(string);

                    switch (column.ColumnType)
                    {
                        case ColumnTypeRef.Boolean:
                            t = typeof(bool);
                            break;
                        case ColumnTypeRef.Integer:
                            t = typeof(int);
                            break;
                        case ColumnTypeRef.Double:
                            t = typeof(double);
                            break;
                        case ColumnTypeRef.DateTime:
                            t = typeof(DateTime);
                            break;
                    }

                    dt.Columns.Add(column.Path, t);
                }
            });


            foreach (var item in ds.Items)
            {
                DataRow row = dt.NewRow();

                foreach (var columnName in ds.Cols)
                {
                    var column = Columns.FirstOrDefault(x => x.Path == columnName);

                    var strVal = item[columnName];
                    if (column != null)
                    {
                        switch (column.ColumnType)
                        {
                            case ColumnTypeRef.Boolean:
                                {
                                    row[columnName] = strVal == null ? false : strVal.ToBool();
                                }
                                break;

                            case ColumnTypeRef.Integer:
                                {
                                    row[columnName] = strVal.ToInt();
                                }
                                break;
                            case ColumnTypeRef.Double:
                                {
                                    row[columnName] = strVal.ToDouble();
                                }
                                break;
                            case ColumnTypeRef.String:
                            default:
                                {
                                    row[columnName] = strVal == null ? string.Empty : strVal.ToString();
                                }
                                break;
                        }

                    }
                    else
                    {
                        row[columnName] = strVal == null ? string.Empty : strVal;
                    }
                }

                dt.Rows.Add(row);
            }

            return dt;
        }
    }
}

