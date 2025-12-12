using Client.Interfaces.Main;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace Client.Common
{
   
    public class GridBoxComparer:IComparer<Dictionary<string,string>>
    {
       readonly ListSortDirection _direction;
        readonly string _path;
        readonly DataGridHelperColumn.ColumnTypeRef _type;
        readonly DataGridHelperColumn _column;

        /*
                            C#                          Oracle DB
            --------------  --------------------------  -------------------
            0--num, 
            1--string,                                
            2--date         dd.MM.yyyy                  dd.mm.yy
            3--datetime     dd.MM.yyyy HH:mm:ss         dd.mm.yy hh24:mi:ss
            4--datetimehm   dd.MM.yyyy HH:mm            dd.mm.yy hh24:mi
            5--dateshorthm  dd.MM HH:mm                 dd.mm hh24:mi
            6--dateshort    dd.MM                       dd.mm 
        */
        public GridBoxComparer(ListSortDirection direction,string path,DataGridHelperColumn column)
        {
            _direction = direction;
            _path = path;
            _column=column;
            _type=column.ColumnType;
        }

        public int Compare(Dictionary<string,string> x = null, Dictionary<string,string> y = null)
        {
            int result = 0;
            string dbg = ".";
            var provider = CultureInfo.InvariantCulture;

            if(x!=null && y!=null)
            {

                bool resume = true;

                var x1 = x as Dictionary<string,string>;
                var y1 = y as Dictionary<string,string>;

                if(resume)
                {
                    if(x1==null || y1==null)
                    {
                        resume=false;
                        dbg=$"{dbg} b0";
                    }
                }


                var p = _path;
                p = p.Replace("[","").Replace("]","");

                var x2 = "";
                var y2 = "";

                if(resume)
                {
                    if(x1.ContainsKey(p))
                    {
                        if(!string.IsNullOrEmpty(x1[p]))
                        {
                            x2=x1[p].ToString();
                        }

                    }

                    if(y1.ContainsKey(p))
                    {
                        if(!string.IsNullOrEmpty(y1[p]))
                        {
                            y2=y1[p].ToString();
                        }
                    }
                }


                bool x0 = false;
                bool y0 = false;

                if(resume)
                {
                    /*
                        Если из базы вернется null
                        то здесь образуется пустая строка
                        в этом случае нужно приравнять аргумент значению по умолчанию.
                        Для каждого типа данных это будет свое значение.
                        Если оба значения нулевые, то сравнивать их нечего.
                     */
                    if(string.IsNullOrEmpty(x2) || string.IsNullOrEmpty(y2))
                    {
                        if(string.IsNullOrEmpty(x2))
                        {
                            x0=true;
                        }

                        if(string.IsNullOrEmpty(y2))
                        {
                            y0=true;
                        }

                        if(x0 && y0)
                        {
                            dbg=$"{dbg} b2";
                        }
                    }
                }
                else
                {
                    dbg=$"{dbg} b3";
                }



                if(resume)
                {
                    // меняем направление x1 <-> y1

                    dbg=$"{dbg}.";


                    if(_direction == ListSortDirection.Descending)
                    {
                        var t = x2;
                        x2 = y2;
                        y2 = t;

                        var t0 = x0;
                        x0 = y0;
                        y0 = t0;
                    }


                    dbg=$"{dbg} type=[{_column.ColumnType}] p=[{p}]";


                    switch(_column.ColumnType)
                    {
                        case DataGridHelperColumn.ColumnTypeRef.String:

                            if(x0)
                            {
                                x2="";
                            }

                            if(y0)
                            {
                                y2="";
                            }

                            result = string.CompareOrdinal(x2,y2);
                            break;

                        case DataGridHelperColumn.ColumnTypeRef.Integer:
                        {
                            int xi = 0;
                            int yi = 0;

                            if(!x0)
                            {
                                xi=x2.ToInt();
                            }

                            if(!y0)
                            {
                                yi=y2.ToInt();
                            }

                            dbg=$"{dbg} x=[{xi}] y=[{yi}]";

                            result = xi.CompareTo(yi);
                        }
                        break;

                        case DataGridHelperColumn.ColumnTypeRef.Double:
                        {
                            double xd = 0;
                            double yd = 0;

                            if(!x0)
                            {
                                xd=x2.ToDouble();
                            }

                            if(!y0)
                            {
                                yd=y2.ToDouble();
                            }

                            dbg=$"{dbg} x=[{xd}] y=[{yd}]";

                            result = xd.CompareTo(yd);
                        }
                        break;

                        case DataGridHelperColumn.ColumnTypeRef.DateTime:
                        {
                            //string format="dd.MM.yyyy";
                            string format = "";
                            if(!string.IsNullOrEmpty(_column.FormatInput))
                            {
                                format=_column.FormatInput;
                            }

                            DateTime xd = DateTime.MinValue;
                            DateTime yd = DateTime.MinValue;

                            if(!x0)
                            {
                                xd = x2.ToDateTime(format);
                            }

                            if(!y0)
                            {
                                yd = y2.ToDateTime(format);
                            }

                            dbg=$"{dbg} x=[{xd}] y=[{yd}]  x0=[{x0}] y0=[{y0}] f=[{format}] cf=[{_column.Format}]";

                            result =  DateTime.Compare(xd,yd);
                        }
                        break;

                        case DataGridHelperColumn.ColumnTypeRef.Boolean:
                        {

                            bool xb = false;
                            bool yb = false;

                            if(!x0)
                            {
                                xb = x2.ToBool();
                            }

                            if(!y0)
                            {
                                yb = y2.ToBool();
                            }

                            dbg=$"{dbg} x=[{xb}] y=[{yb}]";

                            result = xb.CompareTo(yb);
                        }
                        break;

                    }
                }
            }
            else
            {
                dbg=$"{dbg} bx";
            }

            //Central.Dbg($"           {dbg}");

            return result;
        }

    }
   

}
