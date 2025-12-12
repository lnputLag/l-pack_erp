using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Client.Common;
using Client.Common.Extensions;

namespace Client.Interfaces.Main.Controls.Tabs
{
    public class ClosableTab : TabItem
    {
        public ClosableTab(string title, bool closeable = true)
        {
            Closeable = closeable;
            Title2 = "";

            var closableTabHeader = new CloseableTabHeader();
            Header = closableTabHeader;            
            
            if(title.IndexOf("glyph", StringComparison.Ordinal)!=-1)
            {
                var glyph = title.CropAfter(":");
                if(!string.IsNullOrEmpty(glyph))
                {
                    switch(glyph)
                    {
                        case "menu":
                            closableTabHeader.ContainerIcon.Visibility = Visibility.Visible;
                            closableTabHeader.ContainerTitle.Visibility = Visibility.Collapsed;
                            closableTabHeader.ContainerInnerCloseButton.Visibility = Visibility.Collapsed;
                            closableTabHeader.IconHome.MouseDown += IconHomeOnMouseDown;
                            break;
                    }
                }
            }
            else
            {
                Title = title;
            }

            closableTabHeader.MouseEnter += CloseButtonOnMouseEnter;
            closableTabHeader.MouseDown += HeaderOnMouseDown;

            if(Closeable)
            {
                closableTabHeader.CloseButton.Click += CloseButtonOnClick;
                closableTabHeader.CloseButton.MouseEnter += CloseButtonOnMouseEnter;
                closableTabHeader.CloseButton.MouseLeave += CloseButtonOnMouseLeave;
                closableTabHeader.CloseButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                closableTabHeader.CloseButton.Visibility = Visibility.Collapsed;
            }
        }

        private bool Closeable { get; }        
        public delegate void OnCloseDelegate();        
        public OnCloseDelegate OnClose { get; set; }
        public string Title2 { get; set; }

        private string Title
        {
            set => ((CloseableTabHeader)Header).LabelTabTitle.Content = value;
        }        

        private void ProcessTab(string tabName, string source = "")
        {
            
            //if(source!="OnSelected")
            {
                Central.WM.DebugLogActives($"T ProcessTab   name=[{Name.ToString().SPadLeft(24)}] source=[{source}]");
                Central.WM.SetActive2(tabName);
            }            
            //else
            //{
            //    Central.WM.DebugLogActives($"_ ProcessTab   name=[{Name.ToString().SPadLeft(24)}] source=[{source}]");
            //}
        }

        private void HeaderOnMouseDown(object sender,MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            ProcessTab(Name, "HeaderOnMouseDown");            
        }

        private void IconHomeOnMouseDown(object sender, MouseButtonEventArgs e)
        {
            ProcessTab(Name, "IconHomeOnMouseDown");           
        }

        protected override void OnSelected(RoutedEventArgs e)
        {
            base.OnSelected(e);
            if(Closeable)
            {
                ((CloseableTabHeader)Header).CloseButton.Visibility = Visibility.Visible;                
            }

            if(Name.ToLower()=="gohome")
            {
                ProcessTab(Name, "OnSelected");
            }            
        }

        private void CloseButtonOnClick(object sender, RoutedEventArgs e)
        {
            if(!string.IsNullOrEmpty(Name))
            {
                Central.WM.RemoveTab(Name);
            }
        }





        protected override void OnUnselected(RoutedEventArgs e)
        {
            base.OnUnselected(e);
            ((CloseableTabHeader)Header).CloseButton.Visibility = Visibility.Collapsed;
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            if(Closeable)
            {
                ((CloseableTabHeader)Header).CloseButton.Visibility = Visibility.Visible;
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if(!IsSelected)
            {
                ((CloseableTabHeader)Header).CloseButton.Visibility = Visibility.Collapsed;
            }
        }

        private void CloseButtonOnMouseEnter(object sender, MouseEventArgs e)
        {
            if(Closeable)
            {
                ((CloseableTabHeader)Header).CloseButton.Visibility = Visibility.Visible;
            }
        }

        private void CloseButtonOnMouseLeave(object sender, MouseEventArgs e)
        {
            ((CloseableTabHeader)Header).CloseButton.Foreground = Brushes.Black;
        }

        
    }
}
