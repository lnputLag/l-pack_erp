using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Client.Common
{
    /// <summary>
    /// Вспомогательный класс для работы с кастомными свойствами полей грида
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public static class DataGridUtil
    {
        /*
            установка свойства
            lib:DataGridUtil.Type="Date"
            
            Чтение свойства:
            var column = Grid.Columns[columnIndex];
            var type=DataGridUtil.GetType(column);
             
         */

        public static string GetName(DependencyObject obj)
        {
            if(obj!=null)
            {
                return (string)obj.GetValue(NameProperty);
            }
            else
            {
                return "";
            }
            
        }

        public static void SetName(DependencyObject obj, string value)
        {
            obj.SetValue(NameProperty, value);
        }

        public static readonly DependencyProperty NameProperty =  DependencyProperty.RegisterAttached("Name", typeof(string), typeof(DataGridUtil), new UIPropertyMetadata(""));



        public static string GetType(DependencyObject obj)
        {
            return (string)obj.GetValue(TypeProperty);
        }

        public static void SetType(DependencyObject obj, string value)
        {
            
        }

        public static readonly DependencyProperty TypeProperty =  DependencyProperty.RegisterAttached("Type", typeof(string), typeof(DataGridUtil), new UIPropertyMetadata(""));



        public static string GetExportHeader(DependencyObject obj)
        {
            return (string)obj.GetValue(ExportHeaderProperty);
        }

        public static void SetExportHeader(DependencyObject obj, string value)
        {
            
        }

        public static readonly DependencyProperty ExportHeaderProperty =  DependencyProperty.RegisterAttached("ExportHeader", typeof(string), typeof(DataGridUtil), new UIPropertyMetadata(""));




        public static IEnumerable<DataGridRow> GetDataGridRows(DataGrid grid)
        {
            var itemsSource = grid.ItemsSource as IEnumerable;
            if (null == itemsSource) yield return null;
            foreach (var item in itemsSource)
            {
                var row = grid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (null != row) yield return row;
            }
        }

        public static string GetIsChecked(DependencyObject obj)
        {
            return (string)obj.GetValue(IsCheckedProperty);
        }

        public static void SetIsChecked(DependencyObject obj, string value)
        {
            
        }

        public static readonly DependencyProperty IsCheckedProperty =  DependencyProperty.RegisterAttached("IsChecked", typeof(string), typeof(DataGridUtil), new UIPropertyMetadata(""));



        public static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                    child = GetVisualChild<T>(v);
                if (child != null)
                    break;
            }
            return child;
        }

        public static DataGridCell GetCell(this DataGrid grid, DataGridRow row, int column)
        {
            if (row != null)
            {
                DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(row);
                if (presenter == null)
                {
                    grid.ScrollIntoView(row, grid.Columns[column]);
                    presenter = GetVisualChild<DataGridCellsPresenter>(row);
                }
                DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                return cell;
            }
            return null;
        }

       
       

    }
}
