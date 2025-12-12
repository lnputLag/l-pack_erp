using Client.Interfaces.Main;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Collections.Generic;
using System.IO;

namespace Client.Common
{
    /*
        Генератор таблиц Excel.
        На входе получает список строк и список колонок. Генерирует
        таблицу по заданному списку колонок, сохраняет во временный файл
        с рандомным именем и открывает файл в системе (в ассоциированной программе).
        По умолчанию данные сохраняются в файле XLSX.

        Примеры использования.

        Экспорт всех видимых полей грида.
            
            var eg=new ExcelGrid();
            eg.SetColumnsFromGrid(Grid.Columns);
            eg.Items=Grid.GridItems;            
            eg.Make();     

        Экспорт произвольного набора колонок.

            var eg=new ExcelGrid();
            eg.Columns=new List<ExcelGridColumn>(){
                {new ExcelGridColumn("ID",                    "Id ПЗ",                                50,    ExcelGridColumn.ColumnTypeRef.String) },
                {new ExcelGridColumn("NUM",                   "Номер ПЗ",                             70,    ExcelGridColumn.ColumnTypeRef.String) },
                {new ExcelGridColumn("WIDTH",                 "Ширина полотна",                       40,    ExcelGridColumn.ColumnTypeRef.Double) },
            };
            eg.Items=Grid.GridItems;            
            eg.Make();                

     */

    /// <summary>
    /// Генератор таблиц Excel
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public class ExcelGrid
    {
        /// <summary>
        /// минимальная ширина колонки таблицы
        /// </summary>
        public int CellMinWidth { get; set; }
        /// <summary>
        /// коллекция колонок для создания таблицы
        /// </summary>
        public List<ExcelGridColumn> Columns { get; set; }
        /// <summary>
        /// коллекция строк 
        /// </summary>
        public List<Dictionary<string, string>> Items { get; set; }

        /// <summary>
        /// Заголовок таблицы будет добавлен если будет указан
        /// </summary>
        public string GridTitle { get; set; }

        /// <summary>
        /// Название листа, если не указан будет значение по умолчанию List1
        /// </summary>
        public string SheetTitle { get; set; }

        public ExcelGrid()
        {
            CellMinWidth = 70;
            Columns = new List<ExcelGridColumn>();
            Items = new List<Dictionary<string, string>>();

            SheetTitle = "List1";
        }

        /// <summary>
        /// установка набора колонок из коллекции колонок грида
        /// (может быть полезно, когда мы хотим экспортировать все колонки грида)
        /// </summary>
        /// <param name="columns"></param>
        public void SetColumnsFromGrid(List<DataGridHelperColumn> columns)
        {
            if (columns != null)
            {
                if (columns.Count > 0)
                {
                    foreach (var c in columns)
                    {
                        var colWidth = c.Width2 * 7;
                        if (colWidth == 0)
                        {
                            colWidth = (int)(c.MinWidth * 1.4);
                        }
                        if (colWidth == 0)
                        {
                            colWidth = (int)(c.Width * 1.4);
                        }

                        var c2 = new ExcelGridColumn
                        {
                            Name = c.Path,
                            Width = colWidth
                        };

                        switch (c.ColumnType)
                        {
                            case DataGridHelperColumn.ColumnTypeRef.String:
                                c2.ColumnType = ExcelGridColumn.ColumnTypeRef.String;
                                break;
                            case DataGridHelperColumn.ColumnTypeRef.Integer:
                                c2.ColumnType = ExcelGridColumn.ColumnTypeRef.Integer;
                                break;
                            case DataGridHelperColumn.ColumnTypeRef.Double:
                                c2.ColumnType = ExcelGridColumn.ColumnTypeRef.Double;
                                break;
                            case DataGridHelperColumn.ColumnTypeRef.DateTime:
                                c2.ColumnType = ExcelGridColumn.ColumnTypeRef.DateTime;
                                break;
                            case DataGridHelperColumn.ColumnTypeRef.Boolean:
                                c2.ColumnType = ExcelGridColumn.ColumnTypeRef.Boolean;
                                break;
                        }

                        c2.Header = c.Header;
                        var g = c.Group;
                        g = g.Trim();
                        if (!string.IsNullOrEmpty(g))
                        {
                            c2.Header = $"{c.Group} | {c2.Header}";
                        }

                        if (c.Enabled && !c.Hidden && c.Exportable)
                        {
                            Columns.Add(c2);
                        }

                    }
                }

            }
        }

        /// <summary>
        /// генерация таблицы
        /// </summary>
        public void Make()
        {
            var filePath = Central.GetTempFilePathWithExtension("xlsx");
            //Central.Dbg($"ExcelGenerator: Export: to file [{filePath}]");

            var resume = true;

            IWorkbook wb = new XSSFWorkbook();

            var sheet = wb.CreateSheet(SheetTitle);

            if (Columns.Count <= 0)
            {
                //Central.Dbg($"ExcelGenerator: Export: Error: No columns in grid");
                resume = false;
            }

            var iRow = 0;

            if (resume)
            {
                if (!string.IsNullOrEmpty(GridTitle))
                {
                    var tRow = sheet.CreateRow(iRow);
                    var tCell = tRow.CreateCell(0);
                    tCell.SetCellType(CellType.String);
                    tCell.SetCellValue(GridTitle);
                    iRow += 2;
                }
            }


            if (resume)
            {
                IRow headerRow = sheet.CreateRow(iRow++);

                //поправочный коэффициент
                int m = 50;
                //минимальная ширина
                int n = CellMinWidth;

                int iCol = 0;
                foreach (var item in Columns)
                {
                    var tCol = item;
                    headerRow.CreateCell(iCol).SetCellValue(tCol.Header);
                    if (tCol.Width < n)
                    {
                        sheet.SetColumnWidth(iCol, n * m);
                    }
                    else
                    {
                        sheet.SetColumnWidth(iCol, tCol.Width * m);
                    }

                    //Central.Dbg($"ExcelGenerator: Export: Row: [{iCol}] {tCol.Width} {tCol.Name} {tCol.Header}");
                    iCol++;
                }

            }




            if (resume)
            {
                if (Items != null)
                {
                    if (Items.Count > 0)
                    {
                        foreach (var r in Items)
                        {
                            var tRow = sheet.CreateRow(iRow);

                            int iCol = 0;
                            foreach (var item in Columns)
                            {
                                var tCol = item;

                                var v = "";
                                if (r != null)
                                {
                                    if (!string.IsNullOrEmpty(tCol.Name))
                                    {
                                        if (r.ContainsKey(tCol.Name))
                                        {
                                            if (r[tCol.Name] != null)
                                            {
                                                v = r[tCol.Name];
                                            }
                                        }
                                    }
                                }

                                var tCell = tRow.CreateCell(iCol);


                                /*
                                 *  0 -- auto
                                 *  1 -- string
                                 *  2 -- digit
                                 *  3 -- bool
                                 */
                                var type = 1;


                                switch (tCol.ColumnType)
                                {
                                    case ExcelGridColumn.ColumnTypeRef.Boolean:
                                        type = 3;
                                        break;

                                    case ExcelGridColumn.ColumnTypeRef.DateTime:
                                        type = 1;
                                        break;

                                    case ExcelGridColumn.ColumnTypeRef.Double:
                                        type = 2;
                                        break;

                                    case ExcelGridColumn.ColumnTypeRef.Integer:
                                        type = 2;
                                        break;

                                    case ExcelGridColumn.ColumnTypeRef.String:
                                        type = 1;
                                        break;
                                }

                                switch (type)
                                {
                                    // 1 -- string
                                    case 1:
                                        tCell.SetCellType(CellType.String);
                                        tCell.SetCellValue(v);
                                        break;

                                    // 2 -- digit
                                    case 2:
                                        {
                                            tCell.SetCellType(CellType.Numeric);

                                            if(tCol.ColumnType==ExcelGridColumn.ColumnTypeRef.Integer)
                                            {
                                                tCell.SetCellValue(v.ToInt());
                                            }

                                            if(tCol.ColumnType==ExcelGridColumn.ColumnTypeRef.Double)
                                            {
                                                tCell.SetCellValue(v.ToDouble());
                                            }
                                            
                                        }
                                        break;

                                    // 3 -- bool
                                    case 3:
                                        {
                                            tCell.SetCellType(CellType.String);

                                            var vs = v.ToLower();
                                            // Значения "0" или "0.0" или "false" - пишем "нет"
                                            if (vs.ToInt() == 0 || vs == "false")
                                            {
                                                tCell.SetCellValue("нет");
                                            }
                                            else
                                            {
                                                tCell.SetCellValue("да");
                                            }
                                        }
                                        break;
                                }

                                if(!string.IsNullOrEmpty(item.Options))
                                {
                                    if(item.Options.IndexOf("hexcolor")>=0)
                                    {
                                        string hexColor = v;
                                        int color = short.MaxValue;

                                        if (int.TryParse(hexColor, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out color))
                                        {
                                            // https://stackoverflow.com/questions/22687901/custom-color-for-icellstyle-fillforegroundcolor-than-provided-named-colors

                                            /*byte[] rgb = System.BitConverter.GetBytes(color);

                                            var xcolor = new XSSFColor(rgb);
                                            
                                            var boldFont = wb.CreateFont();
                                            boldFont.Boldweight = (short)FontBoldWeight.Bold;
                                            var boldStyle = wb.CreateCellStyle();
                                            boldStyle.SetFont(boldFont);

                                            //boldStyle.FillBackgroundColor

                                            boldStyle.BorderBottom = BorderStyle.Medium;
                                            boldStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;// xcolor.Index;
                                            boldStyle.FillPattern = FillPattern.SolidForeground;

                                            tCell.CellStyle = boldStyle;*/
                                        }
                                    }
                                }

                                iCol++;
                            }

                            iRow++;
                        }
                    }
                }

            }


            if (resume)
            {
                var fs = File.Create(filePath);
                wb.Write(fs);
                fs.Close();
            }

            if (resume)
            {
                Central.OpenFile(filePath);
            }

        }

         /// <summary>
        /// генерация таблицы
        /// </summary>
        public void MakeHtml()
        {
            var filePath = Central.GetTempFilePathWithExtension("html");

            var resume = true;

            var table="";

            if (Columns.Count <= 0)
            {
                //Central.Dbg($"ExcelGenerator: Export: Error: No columns in grid");
                resume = false;
            }

            var iRow = 0;

            /*
            if (resume)
            {
                if (!string.IsNullOrEmpty(GridTitle))
                {
                    var tRow = sheet.CreateRow(iRow);
                    var tCell = tRow.CreateCell(2);
                    tCell.SetCellType(CellType.String);
                    tCell.SetCellValue(GridTitle);
                    iRow += 2;
                }
            }
            */

             //поправочный коэффициент
                int m = 1;
                //минимальная ширина
                int n = 40;


            if (resume)
            {
                var row="";

               

                int iCol = 0;
                foreach (var item in Columns)
                {
                    var tCol = item;

                    /*
                    headerRow.CreateCell(iCol).SetCellValue(tCol.Header);
                    if (tCol.Width < n)
                    {
                        sheet.SetColumnWidth(iCol, n * m);
                    }
                    else
                    {
                        sheet.SetColumnWidth(iCol, tCol.Width * m);
                    }

                    */

                    var w=0;
                    if (tCol.Width < n)
                    {
                        w=n*m;
                    }
                    else
                    {
                        w=tCol.Width*m;
                    }
                    
                    var td=$"<td><div style='width:{w}px;'>{w}</div></td>";
                    //var td=$"<td><div>{w}</div></td>";
                    row=$"{row}{td}";

                    //Central.Dbg($"ExcelGenerator: Export: Row: [{iCol}] {tCol.Width} {tCol.Name} {tCol.Header}");
                    iCol++;
                }

                table=$"{table}\n<tr>{row}</tr>";
            }


            if (resume)
            {
                var row="";

              

                int iCol = 0;
                foreach (var item in Columns)
                {
                    var tCol = item;

                    /*
                    headerRow.CreateCell(iCol).SetCellValue(tCol.Header);
                    if (tCol.Width < n)
                    {
                        sheet.SetColumnWidth(iCol, n * m);
                    }
                    else
                    {
                        sheet.SetColumnWidth(iCol, tCol.Width * m);
                    }

                    */

                    var w=0;
                    if (tCol.Width < n)
                    {
                        w=n*m;
                    }
                    else
                    {
                        w=tCol.Width*m;
                    }
                    
                    //var td=$"<td style='width:{w}px;'><div>{tCol.Header}</div></td>";
                    var td=$"<td><div style='width:{w}px;'>{tCol.Header}</div></td>";
                    row=$"{row}{td}";

                    //Central.Dbg($"ExcelGenerator: Export: Row: [{iCol}] {tCol.Width} {tCol.Name} {tCol.Header}");
                    iCol++;
                }

                table=$"{table}\n<tr>{row}</tr>";
            }

            

            if (resume)
            {
              

                if (Items != null)
                {
                    if (Items.Count > 0)
                    {
                        foreach (var r in Items)
                        {
                            var row="";

                            int iCol = 0;
                            foreach (var item in Columns)
                            {
                                var cell="";
                                var tCol = item;

                                var v = "";
                                if (r != null)
                                {
                                    if (!string.IsNullOrEmpty(tCol.Name))
                                    {
                                        if (r.ContainsKey(tCol.Name))
                                        {
                                            if (r[tCol.Name] != null)
                                            {
                                                v = r[tCol.Name];
                                            }
                                        }
                                    }
                                }



                                /*
                                 *  0 -- auto
                                 *  1 -- string
                                 *  2 -- digit
                                 *  3 -- bool
                                 */
                                var type = 1;


                                switch (tCol.ColumnType)
                                {
                                    case ExcelGridColumn.ColumnTypeRef.Boolean:
                                        type = 3;
                                        break;

                                    case ExcelGridColumn.ColumnTypeRef.DateTime:
                                        type = 1;
                                        break;

                                    case ExcelGridColumn.ColumnTypeRef.Double:
                                        type = 2;
                                        break;

                                    case ExcelGridColumn.ColumnTypeRef.Integer:
                                        type = 2;
                                        break;

                                    case ExcelGridColumn.ColumnTypeRef.String:
                                        type = 1;
                                        break;
                                }

                                switch (type)
                                {
                                    // 1 -- string
                                    case 1:
                                        cell=v;
                                        break;

                                    // 2 -- digit
                                    case 2:
                                        {
                                            if(tCol.ColumnType==ExcelGridColumn.ColumnTypeRef.Integer)
                                            {
                                                cell=v.ToInt().ToString();
                                            }

                                            if(tCol.ColumnType==ExcelGridColumn.ColumnTypeRef.Double)
                                            {
                                                cell=v.ToDouble().ToString();
                                            }
                                            
                                        }
                                        break;

                                    // 3 -- bool
                                    case 3:
                                        {

                                            var vs = v.ToLower();
                                            // Значения "0" или "0.0" или "false" - пишем "нет"
                                            if (vs.ToInt() == 0 || vs == "false")
                                            {
                                                cell="<input type='checkbox' >";
                                            }
                                            else
                                            {
                                                cell="<input type='checkbox' checked>";
                                            }
                                        }
                                        break;


                                }

                                var w=0;
                                if (tCol.Width < n)
                                {
                                    w=n*m;
                                }
                                else
                                {
                                    w=tCol.Width*m;
                                }

                                var td=$"<td><div style='width:{w}px;'>{cell}</div></td>";
                                row=$"{row}{td}";


                                iCol++;
                            }

                            table=$"{table}\n<tr>{row}</tr>";

                            iRow++;
                        }
                    }
                }

            }
            


            table=$"<table class='brd grid'>\n{table}\n</table>";
            
            var style=@"
                html,body,table,tr,td,div,p,span,li{
                    font-weight: 300;
                    color: #3d464c;
                    font-size:10px;
                    font-family: Ubuntu, sans-serif;
                }
                body,html{
                    background-color:#fff;
                }

.brd table, table.brd {
    border-collapse: collapse;
    border-color: #ccc;
    border-style: solid;
    border-width: 0px;
    margin-top: 10px;
    margin-bottom: 10px;
    display:inline-block;
}
.brd table tbody tr td, table.brd tbody tr td, .brd table tr td, table.brd tr td {
    vertical-align: top;
    border-collapse: collapse;
    border-color: #ccc;
    border-style: solid;
    border-width: 1px;
    padding: 5px;
}

.grid table, table.grid, .grid table, table.grid{
    min-width:1900px;
}
.grid table tbody tr td, table.grid tbody tr td, .grid table tr td, table.grid tr td {
    height:27px;
    display:inline-block;
    overflow-y: hidden;
    overflow-x: hidden;
}
.grid table tbody tr td div, table.grid tbody tr td div, .grid table tr td div, table.grid tr td div {
    height:27px;
    display:inline-block;
    overflow-y: hidden;
    overflow-x: hidden;
}
     

            ";

            var html="";
            html=$"<html><head><style>{style}</style></head><body>\n{table}\n</body></html>";

            if (resume)
            {
                //var fs = File.Create(filePath);
                //var sw = new StreamWriter(fs);
                //sw.WriteLine(html);
                //fs.Close();
                File.WriteAllText(filePath, html);
            }

            if (resume)
            {
                Central.OpenFile(filePath);
            }

        }
    }


    /// <summary>
    /// вспомогательная структура для хранения данных колонки таблицы
    /// (используется для экспорта данных таблицы в XLS)
    /// </summary>
    public class ExcelGridColumn
    {
        /// <summary>
        /// символьное имя, должно быть уникальным (технологическое поле)
        /// оно же является ключом для извлечения данных из датасета
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// текст заголовка колонки
        /// </summary>
        public string Header { get; set; }
        /// <summary>
        /// ширина колонки
        /// (в пикселях грида)
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Поле для дополнительной информацции при генерации данных (например цвет hexcolor)
        /// </summary>
        public string Options { get; set; }
        /// <summary>
        /// тип колонки
        /// </summary>
        public enum ColumnTypeRef
        {
            String = 1,
            Integer = 2,
            Double = 3,
            DateTime = 4,
            Boolean = 5,
        }

        /// <summary>
        /// тип данных: s,str,string, i,int,integer, d,digit, double, b,bool,boolean
        /// </summary>
        public ColumnTypeRef ColumnType { get; set; }

        public ExcelGridColumn()
        {
            Name = "";
            Header = "";
            Width = 50;
            ColumnType = ColumnTypeRef.String;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="header"></param>
        /// <param name="width"></param>
        /// <param name="columnType"></param>
        public ExcelGridColumn(string name = "", string header = "", int width = 50, ColumnTypeRef columnType = ColumnTypeRef.String)
        {
            Name = name;
            Header = header;
            Width = width;
            ColumnType = columnType;
        }
    }
}
