using System;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Client.Common.Extensions
{

  

    //экспериментальные функции для низкоуровневой работы с элементами гридов
    static class DataGridHelpers
    {
        /*
         static public DataGridCell GetCell2(DataGrid dg, DataGridRow row, int column)
            {
                var rowContainer = row;

                if (rowContainer != null)
                {
                    var presenter = GetVisualChild<DataGridCellsPresenter>(rowContainer);

                    DataGridCell cell = null;
                    try
                    {
                        cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                        if (cell == null)
                        {
                            dg.ScrollIntoView(rowContainer, dg.Columns[column]);
                            cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    return cell;
                }
                return null;
            }

            public static DataGridCell GetCell(DataGrid dg, int row, int column)
            {
                var rowContainer = GetRow(dg, row);

                if (rowContainer != null)
                {
                    var presenter = GetVisualChild<DataGridCellsPresenter>(rowContainer);

                    DataGridCell cell = null;
                    try
                    {
                        cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                        if (cell == null)
                        {
                            dg.ScrollIntoView(rowContainer, dg.Columns[column]);
                            cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    return cell;
                }
                return null;
            }

            private static DataGridRow GetRow(DataGrid dg, int index)
            {
                var row = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(index);
                if (row == null)
                {
                    dg.ScrollIntoView(dg.Items[index]);
                    row = (DataGridRow)dg.ItemContainerGenerator.ContainerFromIndex(index);
                }
                return row;
            }

            static T GetVisualChild<T>(Visual parent) where T : Visual
            {
                T child = default;
                int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < numVisuals; i++)
                {
                    var v = (Visual)VisualTreeHelper.GetChild(parent, i);
                    child = v as T;
                    if (child == null)
                    {
                        child = GetVisualChild<T>(v);
                    }
                    if (child != null)
                    {
                        break;
                    }
                }
                return child;
            }

        */
        /*
        public string GetSelectedCellValue()
        {
            DataGridCellInfo cellInfo = MatrixGrid.SelectedCells[0];
            if (cellInfo == null) return null;

            DataGridBoundColumn column = cellInfo.Column as DataGridBoundColumn;
            if (column == null) return null;

            FrameworkElement element = new FrameworkElement() { DataContext = cellInfo.Item };
            BindingOperations.SetBinding(element, TagProperty, column.Binding);

            return element.Tag.ToString();
        }
        */

        

        

    }


}
