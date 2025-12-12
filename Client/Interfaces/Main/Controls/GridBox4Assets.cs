using Client.Assets.Converters;
using Client.Common;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.ConditionalFormattingManager;
using DevExpress.Xpf.Editors.Helpers;
using DevExpress.Xpf.Grid;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Main
{
    public class GridBox4Assets
    {
        public static GridBox4CellData _GetCellData1(object o)
        {
            var result = new GridBox4CellData();

            if(o!=null)
            {
                try
                {
                    var cellData = (EditGridCellData)o;
                    if(cellData != null)
                    {
                        var path = cellData.Column.FieldName;
                        var val = cellData.Value.ToString();
                        var rowView = (DataRowView)cellData.Row;
                        var dataRow = (DataRow)rowView.Row;
                        var rowItems = dataRow.ItemArray;
                        var rowData = (RowData)cellData.RowData;
                        var cellDataList = rowData.CellData;

                        var colItems = new List<string>();

                        if(cellDataList != null)
                        {
                            if(cellDataList.Count > 0)
                            {
                                foreach(var c in cellDataList)
                                {
                                    colItems.Add(c.Column.FieldName);
                                }
                            }
                        }

                        int j = 0;
                        foreach(var c in colItems)
                        {
                            result.Row.CheckAdd(c, rowItems[j].ToString());
                            j++;
                        }

                        result.Path = path;
                        result.Value = result.Row.CheckGet(result.Path);

                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("1 "+e.ToString());
                }
            }
            return result;
        }

        public static int _GetColCount(object o)
        {
            var result = 0;

            if(o != null)
            {
                try
                {
                    var colItems = new List<string>();

                    var dataRowView = (DataRowView)o;
                    if(dataRowView != null)
                    {
                        var dataRow = (DataRow)dataRowView.Row;
                        if(dataRow != null)
                        {
                            var table = dataRow.Table;
                            if(table != null)
                            {
                                foreach(DataColumn c in table.Columns)
                                {
                                    var s = c.ColumnName;
                                    colItems.Add(s);
                                }
                            }
                        }
                    }

                    result = colItems.Count;
                }
                catch(Exception e)
                {
                    Console.WriteLine("2 " + e.ToString());
                }
            }

            return result;
        }

        //for template selector
        public static GridBox4CellData GetCellData(EditGridCellData item)
        {
            var result = new GridBox4CellData();

            var data = (EditGridCellData)item;
            var columnName = (string)data.Column.FieldName;
            var dataRowView = (DataRowView)data.Row;
            if(dataRowView!=null)
            {
                var dataRow = (DataRow)dataRowView.Row;
                result = GridBox4Assets.GetCellData(columnName, dataRow);
            }

            return result;
        }

        //for styler
        public static GridBox4CellData GetCellData(object[] value)
        {
            var result = new GridBox4CellData();

            if( 
                value[0]!=null 
                && value[1] != null
            )
            {
                //if(value[0].GetType() != DependencyProperty.UnsetValue )
                if(value[0] != DependencyProperty.UnsetValue)
                {
                    var dataRow = (DataRow)value[0];
                    var columnName = (string)value[1];                    
                    ///
                    result = GridBox4Assets.GetCellData(columnName, dataRow);
                }
            }

            return result;
        }

        //public static GridBox4CellData GetCellData(object[] o, int columnIndex = 0)
        public static GridBox4CellData GetCellData(string columnName0=null, DataRow dataRow0=null)
        {
            var result = new GridBox4CellData();

            //if(o != null)
            {
                try
                {
                    var colItems = new List<string>();

                    //var column = (string)o[1];
                    if(columnName0 != null)
                    {
                        result.Path= columnName0;
                    }

                    if(dataRow0 != null)
                    {
                        //if(o[0].GetType() == typeof(DataRow))
                        if(dataRow0.GetType() == typeof(DataRow))
                        {
                            //var dataRow = (DataRow)o[0];
                            var dataRow = (DataRow)dataRow0;
                            if(dataRow != null)
                            {
                                var table = dataRow.Table;
                                if(table != null)
                                {
                                    {
                                        var j = 0;
                                        foreach(DataColumn c in table.Columns)
                                        {
                                            var s = c.ColumnName;
                                            colItems.Add(s);
                                            j++;
                                        }
                                    }


                                    DataRow rowFirst = null;
                                    foreach(DataRow r in table.Rows)
                                    {
                                        rowFirst = r;
                                        break;
                                    }

                                    {
                                        var j = 0;
                                        foreach(var i in dataRow.ItemArray)
                                        {
                                            var v = i.ToString();
                                            var k = colItems[j];
                                            result.Row.CheckAdd(k, v);
                                            j++;

                                            if(k == result.Path)
                                            {
                                                result.Value = v;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("3 " + e.ToString());
                }
            }
            return result;
        }
    }

    public class GridBox4CellData
    {
        public GridBox4CellData()
        {
            Row = new Dictionary<string, string>();
            Path = "";
            Value = "";
        }
        /// <summary>
        /// значения колонок текущей строки
        /// </summary>
        public Dictionary<string, string> Row { get; set; }
        /// <summary>
        /// имя колонки
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// значение текущей ячейки
        /// </summary>
        public string Value { get; set; }

    }

    public class GridBox4StylerElement
    {
        public GridBox4StylerElement()
        {

        }
        public StylerTypeRef Type { get; set; }
        public StylerDelegate Callback { get; set; }
        public string Scope { get; set; }
        public string Path { get; set; }
    }

    public class GridBox4CellTemplateSelector: DataTemplateSelector
    {
        public GridBox4CellTemplateSelector()
        {
            Mode = 0;
        }

        public DataGridHelperColumn Column {  get; set; }
        public Style CellContainerStyle { get; set; }
        public GridBox4 GridBox { get; set; }
        /// <summary>
        /// режим: 1=view, 2=edit
        /// </summary>
        public int Mode { get; set; }
        //public Style CellContainerBorderStyle { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var data = (EditGridCellData)item;
            var cell = GridBox4Assets.GetCellData((EditGridCellData)item);
            //var cell = new GridBox4CellData();

            var tpl = new DataTemplate();

            var generate = true;

            {
                var k = Column.Path;
                if(GridBox.CellTplCache.ContainsKey(k))
                {
                    tpl = GridBox.CellTplCache[k];
                    generate = false;
                }
            }
            
            if(generate)
            {
                /*
                    ColumnType=
                        String = 1,
                        Integer = 2,
                        Double = 3,
                        DateTime = 4,
                        Boolean = 5,
                        Image = 6,
                        SelectBox = 7,

                    controlType=
                        string
                        checkbox
                 */

                string controlType = "string";
                var textAlignment = TextAlignment.Left;
                var horizontalAlignment = HorizontalAlignment.Left;
                string tooltip = "";

                switch(Column.ColumnType)
                {
                    case ColumnTypeRef.Boolean:
                        {
                            controlType = "checkbox";
                            horizontalAlignment= HorizontalAlignment.Center;
                        }
                        break;

                    case ColumnTypeRef.Integer:
                        {
                            controlType = "string";
                            textAlignment = TextAlignment.Right;
                            horizontalAlignment = HorizontalAlignment.Right;
                        }
                        break;

                    case ColumnTypeRef.Double:
                        {
                            controlType = "string";
                            textAlignment = TextAlignment.Right;
                            horizontalAlignment = HorizontalAlignment.Right;
                        }
                        break;

                    case ColumnTypeRef.String:
                        {
                            controlType = "string";
                            textAlignment = TextAlignment.Left;
                            horizontalAlignment = HorizontalAlignment.Left;
                        }
                        break;

                    case ColumnTypeRef.DateTime:
                        {
                            controlType = "string";
                            textAlignment = TextAlignment.Left;
                            horizontalAlignment = HorizontalAlignment.Left;
                        }
                        break;
                }

                
                var factory = new FrameworkElementFactory(typeof(Grid));
                {
                    factory.SetValue(Grid.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
                    factory.SetValue(Grid.VerticalAlignmentProperty, VerticalAlignment.Center);
                    factory.SetValue(Grid.StyleProperty, CellContainerStyle);

                    var column1 = new FrameworkElementFactory(typeof(ColumnDefinition));
                    column1.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
                    factory.AppendChild(column1);

                    var row1 = new FrameworkElementFactory(typeof(RowDefinition));
                    row1.SetValue(RowDefinition.HeightProperty, new GridLength(0, GridUnitType.Auto));
                    factory.AppendChild(row1);
                }

                FrameworkElementFactory ctl;

                switch(controlType)
                {
                    case "checkbox":
                        {
                            if (Column.Editable)
                            {
                                ctl = new FrameworkElementFactory(typeof(CheckBox));
                                var binding = new Binding();
                                binding.Path = new PropertyPath($"Row.[{Column.Path}]");

                                ctl.SetBinding(CheckBox.IsCheckedProperty, binding);

                                ctl.SetValue(CheckBox.StyleProperty, Column.CellControlStyle);
                                ctl.SetValue(CheckBox.HorizontalAlignmentProperty, horizontalAlignment);

                                binding.Mode = BindingMode.TwoWay;
                                ctl.SetValue(CheckBox.IsEnabledProperty, true);

                                ctl.AddHandler(CheckBox.PreviewMouseDownEvent, new MouseButtonEventHandler(CheckBoxOnClick0));
                                ctl.AddHandler(CheckBox.ClickEvent, new RoutedEventHandler(CheckBoxOnClick));
                            }
                            else
                            {
                                ctl = new FrameworkElementFactory(typeof(Border));
                                var imageFactory = new FrameworkElementFactory(typeof(Image));
                                var binding = new Binding();
                                binding.Path = new PropertyPath($"Row.[{Column.Path}]");
                                var c = new BoolToCheckboxIconConverter();
                                c.style = Column.CellControlStyle2;
                                c.style2 = Column.CellControlStyle3;
                                binding.Converter = c;

                                imageFactory.SetBinding(Image.StyleProperty, binding);

                                // Устанавливаем свойства для Image
                                //imageFactory.SetValue(Image.WidthProperty, 40.0);
                                //imageFactory.SetValue(Image.HeightProperty, 40.0);
                                //imageFactory.SetValue(Image.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                                //imageFactory.SetValue(Image.VerticalAlignmentProperty, VerticalAlignment.Center);

                                // Добавляем Image в StackPanel
                                ctl.AppendChild(imageFactory);

                                ctl.SetValue(StackPanel.HorizontalAlignmentProperty, horizontalAlignment);
                                ctl.SetValue(StackPanel.VerticalAlignmentProperty, VerticalAlignment.Center);
                            }
                                
                            factory.AppendChild(ctl);
                        }
                        break;

                    default:
                    case "string":
                        {
                            ctl = new FrameworkElementFactory(typeof(TextBlock));

                            var binding = new Binding();
                            binding.Path = new PropertyPath($"Row.[{Column.Path}]");

                            ctl.SetBinding(TextBlock.ToolTipProperty, binding);

                            if(Column.Options.IndexOf("valuestripbr") > -1)
                            {
                                var binding2 = new Binding();
                                binding2.Path = new PropertyPath($"Row.[{Column.Path}]");

                                binding2.Converter=new GridBox4CellValueConverter();
                                binding2.ConverterParameter=Column;

                                ctl.SetBinding(TextBlock.TextProperty, binding2);
                            }
                            else
                            {
                                ctl.SetBinding(TextBlock.TextProperty, binding);
                            }

                            ctl.SetValue(TextBlock.StyleProperty, Column.CellControlStyle);
                            ctl.SetValue(TextBlock.HorizontalAlignmentProperty, horizontalAlignment);

                            {
                                List<FrameworkElementFactory> renderResult = _MakeLabel(Column);
                                if(renderResult.Count > 0)
                                {
                                    var leftLabelBox = new FrameworkElementFactory(typeof(StackPanel));
                                    leftLabelBox.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
                                    leftLabelBox.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Left);
                                    
                                    var rightLabelBox = new FrameworkElementFactory(typeof(StackPanel));
                                    rightLabelBox.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
                                    rightLabelBox.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Right);
                                    
                                    var leftLabels = new List<FrameworkElementFactory>();
                                    var rightLabels = new List<FrameworkElementFactory>();
                                    
                                    foreach(var label in Column.Labels)
                                    {
                                        int index = Column.Labels.IndexOf(label);
                                        if(index < renderResult.Count)
                                        {
                                            if(label.Position == LabelPosition.Left)
                                            {
                                                leftLabels.Add(renderResult[index]);
                                            }
                                            else
                                            {
                                                rightLabels.Add(renderResult[index]);
                                            }
                                        }
                                    }
                                    
                                    foreach(var labelElement in leftLabels)
                                    {
                                        leftLabelBox.AppendChild(labelElement);
                                    }
                                        
                                    foreach(var labelElement in rightLabels)
                                    {
                                        rightLabelBox.AppendChild(labelElement);
                                    }
                                    
                                    if(leftLabels.Count > 0)
                                    {
                                        factory.AppendChild(leftLabelBox);
                                    }

                                    if(rightLabels.Count > 0)
                                    {
                                        factory.AppendChild(rightLabelBox);
                                    }
                                }
                            }

                            factory.AppendChild(ctl);
                        }
                        break;
                }

               
                tpl.VisualTree = factory;
                {
                    var k = Column.Path;
                    if(!GridBox.CellTplCache.ContainsKey(k))
                    {
                        GridBox.CellTplCache.Add(k,tpl);
                    }
                }
            }

            DataTemplate result = null;
            result=tpl;
            return result;
        }

        public class BoolToCheckboxIconConverter : IValueConverter
        {
            public Style style { get; set; }
            public Style style2 { get; set; }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {

                if (value is bool isChecked && isChecked)
                {
                    return style;
                }
                else
                {
                    return style2;
                }
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public List<FrameworkElementFactory> _MakeLabel(DataGridHelperColumn column)
        {
            var result= new List<FrameworkElementFactory>();
            if(Column.Labels.Count > 0)
            {
                foreach(DataGridHelperColumnLabel label in Column.Labels)
                {
                    if(label.Construct != null)
                    {
                        var el = label.Construct.Invoke();
                        if(el != null)
                        {
                            {
                                var processor= new RowLabelsProcessor();
                                processor.Column = column;
                                processor.Label = label;

                                var binding = new Binding();
                                binding.Path = new PropertyPath($"RowData.Row");
                                binding.Converter = processor;
                                el.SetBinding(Border.VisibilityProperty, binding);
                            }
                            result.Add(el);
                        }
                    }
                }
            }
            return result;
        }

        public static System.IO.Stream GetStreamFromString(string s)
        {
            var stream = new System.IO.MemoryStream();
            var writer = new System.IO.StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public void TextBoxOnTest(object sender, RoutedEventArgs e)
        {
            var r = sender;

        }

        public void TextBoxOnClick(object sender, RoutedEventArgs e)
        {
            if(sender != null)
            {
                var t = (TextBlock)sender;
                var parent= (Grid)t.Parent;
                if(parent != null)
                {
                    parent.Children.Clear();
                    


                    ////Mode = 2;

                    //var factory = new FrameworkElementFactory(typeof(Grid));
                    //{
                    //    factory.SetValue(Grid.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
                    //    factory.SetValue(Grid.VerticalAlignmentProperty, VerticalAlignment.Center);
                    //    factory.SetValue(Grid.StyleProperty, CellContainerStyle);

                    //    var column1 = new FrameworkElementFactory(typeof(ColumnDefinition));
                    //    column1.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
                    //    factory.AppendChild(column1);

                    //    var row1 = new FrameworkElementFactory(typeof(RowDefinition));
                    //    row1.SetValue(RowDefinition.HeightProperty, new GridLength(0, GridUnitType.Auto));
                    //    factory.AppendChild(row1);
                    //}

                    //{
                    //    var ctl = new FrameworkElementFactory(typeof(TextBox));

                    //    var binding = new Binding();
                    //    binding.Path = new PropertyPath($"Row.[{Column.Path}]");
                    //    ctl.SetBinding(TextBox.TextProperty, binding);

                    //    ctl.SetValue(TextBox.StyleProperty, Column.CellControlStyle);
                    //    ctl.SetValue(TextBox.HorizontalAlignmentProperty, HorizontalAlignment.Left);

                    //    ctl.AddHandler(TextBox.PreviewKeyDownEvent, new KeyEventHandler(TextBoxBeforeKeyDown));

                    //    factory.AppendChild(ctl);
                    //}

                    ////parent.

                    //var tpl = new DataTemplate();
                    //tpl.VisualTree = factory;



                }
            }
            
        }

        public void CheckBoxOnClick0(object sender, MouseButtonEventArgs e)
        {
            var result = true;

            GridBox.MouseClickType = GridBox4.MouseClickTypeRef.LeftButtonClick;
            Central.Dbg($"ProcessCellClick3");
            GridBox.ProcessCellClick(e, false);

            //GridBox.ProcessCellClick(e);

            if(Column.OnClickAction != null)
            {
                var el = new FrameworkElement();
                var onclickResult = (bool)Column.OnClickAction.Invoke(GridBox.SelectedItem, el);
                if (!onclickResult)
                {
                    result = false;
                }
            }           

            if (result)
            {
                //enable
                e.Handled = false;
            }
            else
            {
                //disable
                e.Handled = true;
            }
        }

        public void TextBlockOnClick0(object sender, MouseButtonEventArgs e)        
        {
            var result = true;

            Central.Dbg($"ProcessCellClick4");
            GridBox.ProcessCellClick(e);

            var columnName = "";
            var cellContent = "";

            try
            {
                if(sender != null)
                {
                    var cb = sender as TextBlock;
                    var dc = cb.DataContext;
                    var cd = dc as EditGridCellData;
                    columnName = cd.Column.Name.ToString();
                    cellContent = cb.Text.ToString();
                }
            }
            catch(Exception ex)
            {

            }

            if(cellContent.IsNullOrEmpty())
            {
                GridBox.SelectedItemContent = cellContent;
            }

            if(Column.OnClickAction != null)
            {
                var el = new FrameworkElement();
                var onclickResult= (bool)Column.OnClickAction.Invoke(GridBox.SelectedItem, el);
                if (!onclickResult)
                {
                    result = false;
                }
            }

            if(result)
            {
                //enable
                e.Handled = false;
            }
            else
            {
                //disable
                e.Handled = true;
            }
        }

        public void CheckBoxOnClick(object sender, RoutedEventArgs e)
        {
            //Central.Dbg($"ProcessCellClick3");
            //GridBox.ProcessCellClick(e, false);

            var selectedRowKey = "";
            var primaryIndex = GridBox.GetPrimaryIndex();

            try
            {
                if(sender != null)
                {
                    var cb = sender as CheckBox;
                    var dc = cb.DataContext;
                    var cd = dc as EditGridCellData;
                    var dv = cd.Row as System.Data.DataRowView;
                    var dr = dv.Row as System.Data.DataRow;
                    selectedRowKey = dr[primaryIndex].ToString();
                }
            }
            catch(Exception ex)
            {

            }

            if(!selectedRowKey.IsNullOrEmpty())
            {
                GridBox.RowUpdate(selectedRowKey);
            }

            var result = true;

            if (Column.OnAfterClickAction != null)
            {
                var el = new FrameworkElement();
                var onclickResult = (bool)Column.OnAfterClickAction.Invoke(GridBox.SelectedItem, (((CheckBox)sender) as FrameworkElement));
                if (!onclickResult)
                {
                    result = false;
                }
            }

            if (result)
            {
                //enable
                e.Handled = false;
            }
            else
            {
                //disable
                e.Handled = true;
            }
        }

        public void TextBoxBeforeKeyDown(object sender, RoutedEventArgs e)
        {
            var breakEvent = false;

            if(!breakEvent)
            {
                if(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                }
                else
                {
                    breakEvent = true;
                }
            }            

            if(breakEvent)
            {
                //e.Handled = true;
            }
        }

        public void TextBoxBeforeKeyDown2(object sender, KeyEventArgs e)
        {
            var breakEvent = false;

            if(!breakEvent)
            {
                if(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                }
                else
                {
                    breakEvent = true;
                }
            }

            if(breakEvent)
            {
                //e.Handled = true;
            }
        }
    }

    public class GridBox4Styler : IMultiValueConverter
    {
        public GridBox4Styler(List<GridBox4StylerElement> stylers, GridBox4 gridBox)
        {
            Stylers=stylers;
            GridBox= gridBox;
        }

        public List<GridBox4StylerElement> Stylers { get; set; } = new List<GridBox4StylerElement>();
        public GridBox4 GridBox { get; set; }

        public object Convert(object[] value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var result = DependencyProperty.UnsetValue;
            var cell = GridBox4Assets.GetCellData(value);
            var type = (StylerTypeRef)parameter;
            var row = new Dictionary<string, string>();
            {
                var r = GridBox.GetRow(cell);
                if(r.Count > 0)
                {
                    row = r;
                }
                else
                {
                    row = cell.Row;
                }
            }

            if(row.CheckGet("ID").ToInt() == 42)
            {
                var rr = 0;
            }

            foreach(GridBox4StylerElement s in Stylers)
            {
                if(
                    ( s.Scope == "cell" && s.Path == cell.Path)
                    || (s.Scope == "row")
                )
                {
                    if(type == s.Type)
                    {
                        var stylerResult = s.Callback.Invoke(row);
                        if(stylerResult != DependencyProperty.UnsetValue)
                        {
                            result = stylerResult;
                        }
                    }
                }
            }

            //defaults
            if(result == DependencyProperty.UnsetValue)
            {
                if(type == DataGridHelperColumn.StylerTypeRef.BorderColor)
                {
                    result = $"#ffcccccc".ToBrush();
                }
            }


            return result;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class GridBox4DataConverter
    {
        public GridBox4DataConverter() 
        {
            Type = ColumnTypeRef.String;
            Format = "";
            Culture = new CultureInfo("ru-RU");
            FormatterRaw = null;
        }

        public ColumnTypeRef Type { get; set; }
        public string Format { get; set; }
        private CultureInfo Culture { get; set; }
        public FormatterRawDelegate FormatterRaw { get; set; } = null;

        public void Init()
        {
            if(Type == ColumnTypeRef.Double)
            {
                if(Format.IsNullOrEmpty())
                {
                    Format = "N2";
                }
                
                Format= "{0:" + Format + "}";
            }
        }

        public string DoConvert(object o)
        {
            var result = "";
            if(o != null)
            {
                var value = "";
                value = o.ToString();
                if(!value.IsNullOrEmpty())
                {
                    switch(Type)
                    {
                        case ColumnTypeRef.Boolean:
                            {
                                var r = value.ToBool();
                                result = r.ToString();
                            }
                            break;

                        case ColumnTypeRef.Integer:
                            {
                                var r = value.ToInt();
                                result = r.ToString();
                            }
                            break;

                        case ColumnTypeRef.Double:
                            {
                                if(!Format.IsNullOrEmpty())
                                {
                                    result=string.Format(Culture, Format, value.ToDouble());
                                }
                                else
                                {
                                    result = value;
                                }
                            }
                            break;

                        case ColumnTypeRef.DateTime:
                            {
                                if(!Format.IsNullOrEmpty())
                                {
                                    result = value.ToDateTime().ToString(Format);
                                }
                                else
                                {
                                    result = value.ToString();
                                }
                            }
                            break;

                        case ColumnTypeRef.String:
                        default:
                            {
                                var r = value.ToString();
                                result = r.ToString();
                            }
                            break;
                    }
                }
            }
            return result;
        }

        public string DoFormat(string v, Dictionary<string,string> row)
        {
            var result = v;
            if(row.Count > 0)
            {
                if(FormatterRaw != null)
                {
                    var r = FormatterRaw.Invoke(row);
                    result = r.ToString();
                }
            }
            return result;
        }
    }

    public class GridBox4CellValueConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string result = "";
            var col=parameter as DataGridHelperColumn;

            try
            {
                var s = value.ToString();
                if (!string.IsNullOrEmpty(s))
                {
                    if(col.Options.IndexOf("valuestripbr") > -1)
                    {
                        s = s.Replace("\n", "");
                        s = s.Replace("\r", "");
                        result = s;
                    }                    
                }
            }
            catch { }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class RowLabelsProcessor : IValueConverter
    {
        public RowLabelsProcessor()
        {
            Column = new DataGridHelperColumn();
            Label = new DataGridHelperColumnLabel();
        }

        public DataGridHelperColumn Column { get; set; }
        public DataGridHelperColumnLabel Label { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var result = DependencyProperty.UnsetValue;
            var cell = new GridBox4CellData();

            if(value != null)
            {
                var column = Column;
                var drv = (DataRowView)value;
                var dr = drv.Row;
                var dataRow = (DataRow)dr;
                var columnName = (string)column.Path;
                
                cell = GridBox4Assets.GetCellData(columnName, dataRow);                
            }

            if(cell.Row.Count > 0)
            {
                if(Label.Update != null)
                {
                    var v = Label.Update.Invoke(cell.Row);
                    {
                        result = v;
                    }
                }
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

}
