using Client.Assets.HighLighters;
using Client.Common;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Interfaces.Shipments
{
    /// <summary> 
    /// Вспомогательный класс: Таймлайн с блоками
    /// </summary>
    /// <author>balchugov_dv</author>   
    public class TimelineBLock
    {
        public TimelineBLock()
        {
            Items = new List<TimelineItem>();
            Initialized = false;
            ColumnWidth = 100;
            RowHeight = 60;
            ColumnIndexMax = 0;
            RowIndexMax = 0;
            ColumnStyle = "";
            RowStyle = "";
            ActualHeight = 0;
            GridBorderOffset = 1;

            //RowStyle = "SHMonBlockRowStyle";
            //ColumnStyle = "SHMonBlockColStyle";
        }
        public Grid Container { get; set; }
        public List<TimelineItem> Items { get; set; }
        public bool Initialized { get; set; }
        
        public int ColumnWidth { get; set; }
        public int ColumnIndexMax { get; set; }
        public string ColumnStyle { get; set; }
        public int RowHeight { get; set; }
        public int RowIndexMax { get; set; }
        public string RowStyle { get; set; }
        public int ActualHeight { get; set; }
        public int GridBorderOffset { get; set; }
        
        public void Init()
        {
            
            Initialized = true;
        }

        public void AddItem(TimelineItem item)
        {
            Items.Add(item);
        }

        public virtual void PrepareItems()
        {
            
        }
        
        public void RenderItems()
        {
            bool resume = true;

            if (resume)
            {
                if (!Initialized)
                {
                    resume = false;
                }

                if (Container==null)
                {
                    resume = false;
                }
            }
            
            if (resume)
            {
                for (int c = 0; c<=ColumnIndexMax; c++)
                {
                    var cd = new ColumnDefinition
                    {
                        Width = new GridLength(ColumnWidth+GridBorderOffset, GridUnitType.Pixel)
                    };
                    Container.ColumnDefinitions.Add(cd);
                }
                
                for (int r = 0; r<=RowIndexMax; r++)
                {
                    var rd = new RowDefinition
                    {
                        Height = new GridLength(RowHeight+GridBorderOffset, GridUnitType.Pixel)
                    };
                    Container.RowDefinitions.Add(rd);
                }
            }

            if (resume)
            {
                if (Container.Children.Count>0)
                {
                    Container.Children.Clear();
                }
                
                /*
                    для отображения сетки нарисуем длинные блоки:
                    на полную высоту или полную ширину грида
                    RowStyle = "SHMonBlockRowStyle";
                    ColumnStyle = "SHMonBlockColStyle";
                 */
                if (!string.IsNullOrEmpty(RowStyle))
                {
                    {
                        int x = (ColumnIndexMax+1) * (ColumnWidth+GridBorderOffset);
                        for (int r = 0; r<=RowIndexMax; r++)
                        {
                            var block = new Border();
                            block.Style =(Style) Container.TryFindResource(RowStyle);
                            block.Height = RowHeight+GridBorderOffset;
                            block.Width = x;
                            Container.Children.Add(block);   
                            Grid.SetRow(block, r);
                            Grid.SetColumn(block, 0);
                            Grid.SetColumnSpan(block, ColumnIndexMax+1);
                        }    
                    }
                }
                
                if (!string.IsNullOrEmpty(ColumnStyle))
                {
                    {
                        int x = (RowIndexMax+1) * (RowHeight+GridBorderOffset);
                        for (int c = 0; c<=ColumnIndexMax; c++)
                        {
                            var block = new Border();
                            block.Style =(Style) Container.TryFindResource(ColumnStyle);
                            block.Height = x;
                            block.Width = ColumnWidth+GridBorderOffset;
                            Container.Children.Add(block);   
                            Grid.SetRow(block, 0);
                            Grid.SetColumn(block, c);
                            Grid.SetRowSpan(block, RowIndexMax+1);
                        }    
                    }
                }
            }

            if (resume)
            {
                if (Items.Count==0)
                {
                    resume = false;
                }
            }
            
            if (resume)
            {
                
                
                if (Items.Count>0)
                {
                    foreach (TimelineItem item in Items )
                    {
                        {
                            var o = (UserControl)item.Object;
                            int r = item.RowIndex;
                            int c=item.ColIndex;
                            if (o!=null)
                            {
                                o.Width = ColumnWidth;
                                o.Height = RowHeight;
                                Container.Children.Add(o);   
                                Grid.SetRow(o, r);
                                Grid.SetColumn(o, c);
                            }    
                        }
                    }    
                }

                ActualHeight = RowIndexMax * (RowHeight+GridBorderOffset);
                Container.Height = ActualHeight;

            }
        }
    }

    public class TimelineItem
    {
        public TimelineItem()
        {
            Index = "";
            RowIndex = 0;
            ColIndex = 0;
            Object = null;
            Values = new Dictionary<string, string>();
        }
        
        public string Index { get; set; }
        public int RowIndex { get; set; }
        public int ColIndex { get; set; }
        public object Object { get; set; }
        public Dictionary<string,string> Values { get; set; }
    }
}