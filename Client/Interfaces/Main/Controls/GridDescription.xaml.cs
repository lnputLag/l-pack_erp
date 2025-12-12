using Client.Common;
using Client.Interfaces.Production.Testing;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Main
{

    /// <summary>
    /// описание таблицы (грида)
    /// вспомогательный класс, который генерироут программно описание 
    /// талицы, используется как подсказака о назначении полей и цветовой маркировке
    /// вызывается из грида по правому клику (Описание)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-10-20</released>
    /// <changed>2023-10-20</changed>
    public partial class GridDescription : System.Windows.Window
    {
        public GridDescription()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            Initialized=false;
            ColumnNavigatorList=new List<Border>();
            SelectedColumn="";
        }

        public string SelectedColumn {get;set;}
        public GridBox3 GridBox {get;set;}
        private bool Initialized {get;set;}
        private List<Border> ColumnNavigatorList {get;set;}

        public void Init()
        {
            var resume=true;
            
            if(resume)
            {
                Title="Описание таблицы";
            }

            if(resume)
            {
                if(GridBox == null)
                {
                    resume=false;
                }
            }

            if(resume)
            {
                ColumnClearItem();

                Description.Text=GridBox.Descriription;
                ColumnRenderNavigator();
            }

            if(resume)
            {
                Initialized=true;
            }
        }

        private void ColumnClearItem()
        {
            StylersTitle.Visibility=Visibility.Collapsed;
            StylerBackgroundLabel.Visibility=Visibility.Collapsed;
            StylerBackgroundContent.Visibility=Visibility.Collapsed;
            StylerForegroundLabel.Visibility=Visibility.Collapsed;
            StylerForegroundContent.Visibility=Visibility.Collapsed;
        }

        private void ColumnRenderNavigator()
        {
            {
                ColumnNavigatorList=new List<Border>();
                ColumnDefinitionContainer.Children.Clear();
            }

            var rowIndex=0;
            foreach(DataGridHelperColumn c in GridBox.Columns)
            {
                var render=false;
                var k=c.Path;

                if(c.Visible && !c.Hidden)
                {
                    render=true;
                }

                if(render)
                {
                    {
                        var elementText=new TextBlock();
                        elementText.Text=c.Header;
                        elementText.Style=(Style)Description.TryFindResource("ColumnDefinitionContainerText");

                        var elementBlock=new Border();                        
                        elementBlock.Child=elementText;
                        elementBlock.Tag=$"{c.Path}";
                        elementBlock.ToolTip=c.Header;
                        elementBlock.MouseUp += ColumnOnMouseClick;
                        elementBlock.Style=(Style)Description.TryFindResource("ColumnDefinitionContainerItem");

                        ColumnDefinitionContainer.Children.Add(elementBlock);
                        ColumnNavigatorList.Add(elementBlock);
                    }

                    rowIndex++;
                }
            }
        }

        private DataGridHelperColumn GetColumnByPath(string path)
        {
            var result=new DataGridHelperColumn();

            foreach(DataGridHelperColumn c in GridBox.Columns)
            {
                var k=c.Path;
                if(k == path)
                {
                    result=c;
                }
            }

            return result;
        }

        private void ColumnRenderItem(string path)
        {
            var c=GetColumnByPath(path);
            ColumnNavigatorItemSetActive(path);

            ColumnClearItem();

            {
                var s="";
                if(!c.Doc.IsNullOrEmpty())
                {
                    s=c.Doc;
                }
                if(s.IsNullOrEmpty())
                {
                    s="Нет описания";
                }
                ColumnTitle.Text=s;
            }


            ColumnRenderStylerDescription(c);
        }

        private void ColumnNavigatorItemSetActive(string path)
        {
            foreach(Border b in ColumnNavigatorList)
            {
                b.Style=(Style)Description.TryFindResource("ColumnDefinitionContainerItem");
                var s=(TextBlock)b.Child;
                s.Style=(Style)Description.TryFindResource("ColumnDefinitionContainerText");
            }

            foreach(Border b in ColumnNavigatorList)
            {
                var t=b.Tag.ToString();
                if(t == path)
                {
                    b.Style=(Style)Description.TryFindResource("ColumnDefinitionContainerItemActive");
                    var s=(TextBlock)b.Child;
                    s.Style=(Style)Description.TryFindResource("ColumnDefinitionContainerTextActive");
                }                
            }

        }

        private void ColumnRenderStylerDescription(DataGridHelperColumn c)
        {
            //var container=StylerColContent;

            if (c.Stylers2.Count > 0)
            {
                var row=new Dictionary<string, string>();
                foreach (StylerProcessor processor in c.Stylers2)
                {
                    var type = processor.StylerType;
                    var d = processor.Processor;
                    if (d != null)
                    {
                        var stylerDescription=(Dictionary<string, string>)d.Invoke(row, 1);
                        
                        if(stylerDescription.Count > 0)
                        {
                            var stylerTypeTitle="";
                            switch(type)
                            {
                                case StylerTypeRef.BackgroundColor:
                                    stylerTypeTitle="Цвет фона";
                                    ColumnRenderStylerDescriptionBlockBackground(stylerDescription);
                                    break;

                                case StylerTypeRef.ForegroundColor:
                                    stylerTypeTitle="Цвет текста";
                                    ColumnRenderStylerDescriptionBlockForeground(stylerDescription);
                                    break;

                                //case StylerTypeRef.BorderColor:
                                //    stylerTypeTitle="Цвет границы";
                                //    break;

                                //case StylerTypeRef.FontWeight:
                                //    stylerTypeTitle="Шрифт";
                                //    break;

                                
                            }
                        }
                    }
                }
            }
        }

        private void ColumnRenderStylerDescriptionBlockBackground(Dictionary<string, string> styleList)
        {
            var container=StylerBackgroundGrid;

            container.RowDefinitions.Clear();
            container.Children.Clear();

            int rowIndex=0;
            foreach(KeyValuePair<string, string> item in styleList)
            {
                var k=item.Key;
                var v=item.Value;

                if(
                    !k.IsNullOrEmpty()
                    && !v.IsNullOrEmpty()
                )
                {
                    {
                        var rd=new RowDefinition();
                        rd.Height=new GridLength(1, GridUnitType.Auto);
                        container.RowDefinitions.Add(rd);
                    }

                    {
                        var elementText=new TextBlock();
                        elementText.Text=GetRandomText();
                        elementText.Padding=new Thickness(5,1,5,1);

                        var elementBlock=new Border();                        
                        elementBlock.Child=elementText;
                        elementBlock.Height=20;
                        elementBlock.Background=$"{k}".ToBrush();
                        elementBlock.HorizontalAlignment=HorizontalAlignment.Stretch;

                        var elementContainer=new Border();  
                        elementContainer.Style=(Style)Description.TryFindResource("FormFieldContainer");
                        elementContainer.Child=elementBlock;

                        container.Children.Add(elementContainer);
                        Grid.SetRow(elementContainer, rowIndex);
                        Grid.SetColumn(elementContainer, 0);
                    }

                    {
                        var elementText=new TextBlock();
                        elementText.Text=v;
                        elementText.HorizontalAlignment=HorizontalAlignment.Stretch;
                        elementText.TextWrapping= TextWrapping.Wrap;
                        elementText.Padding=new Thickness(0,3,0,0);

                        var elementBlock=new Border();                        
                        elementBlock.Child=elementText;
                        //elementBlock.Height=20;
                        elementBlock.HorizontalAlignment=HorizontalAlignment.Stretch;

                        var elementContainer=new Border();  
                        elementContainer.Style=(Style)Description.TryFindResource("FormFieldContainer");
                        elementContainer.Child=elementBlock;

                        container.Children.Add(elementContainer);
                        Grid.SetRow(elementContainer, rowIndex);
                        Grid.SetColumn(elementContainer, 2);
                    }

                    rowIndex++;
                }
            }

            if(container.RowDefinitions.Count > 0)
            {
                StylerBackgroundLabel.Visibility=Visibility.Visible;
                StylerBackgroundContent.Visibility=Visibility.Visible;
                StylersTitle.Visibility=Visibility.Visible;
            }
        }

        private void ColumnRenderStylerDescriptionBlockForeground(Dictionary<string, string> styleList)
        {
            var container=StylerForegroundGrid;

            container.RowDefinitions.Clear();
            container.Children.Clear();

            int rowIndex=0;
            foreach(KeyValuePair<string, string> item in styleList)
            {
                var k=item.Key;
                var v=item.Value;

                if(
                    !k.IsNullOrEmpty()
                    && !v.IsNullOrEmpty()
                )
                {
                    {
                        var rd=new RowDefinition();
                        rd.Height=new GridLength(1, GridUnitType.Auto);
                        container.RowDefinitions.Add(rd);
                    }

                    {
                        var elementText=new TextBlock();
                        elementText.Text=GetRandomText();
                        elementText.Padding=new Thickness(5,1,5,1);
                        elementText.Foreground=$"{k}".ToBrush();

                        var elementBlock=new Border();                        
                        elementBlock.Child=elementText;
                        elementBlock.Height=20;
                        elementBlock.HorizontalAlignment=HorizontalAlignment.Stretch;

                        var elementContainer=new Border();  
                        elementContainer.Style=(Style)Description.TryFindResource("FormFieldContainer");
                        elementContainer.Child=elementBlock;

                        container.Children.Add(elementContainer);
                        Grid.SetRow(elementContainer, rowIndex);
                        Grid.SetColumn(elementContainer, 0);
                    }

                    {
                        var elementText=new TextBlock();
                        elementText.Text=v;
                        elementText.HorizontalAlignment=HorizontalAlignment.Stretch;
                        elementText.TextWrapping= TextWrapping.Wrap;
                        elementText.Padding=new Thickness(0,3,0,0);

                        var elementBlock=new Border();                        
                        elementBlock.Child=elementText;
                        elementBlock.HorizontalAlignment=HorizontalAlignment.Stretch;

                        var elementContainer=new Border();  
                        elementContainer.Style=(Style)Description.TryFindResource("FormFieldContainer");
                        elementContainer.Child=elementBlock;

                        container.Children.Add(elementContainer);
                        Grid.SetRow(elementContainer, rowIndex);
                        Grid.SetColumn(elementContainer, 2);
                    }

                    rowIndex++;
                }
            }

            if(container.RowDefinitions.Count > 0)
            {
                StylerForegroundLabel.Visibility=Visibility.Visible;
                StylerForegroundContent.Visibility=Visibility.Visible;
                StylersTitle.Visibility=Visibility.Visible;
            }
        }

        private string GetRandomText()
        {
            var result="quasi architecto";
            return result;
        }

        private void ColumnOnMouseClick(object sender, MouseButtonEventArgs e)
        {
            var b=(Border)sender;
            if(b != null)
            {
                var path=b.Tag.ToString();
                ColumnRenderItem(path);
            }
        }

        public void Open()
        {
            if(Initialized)
            {
                if(!SelectedColumn.IsNullOrEmpty())
                {
                    ColumnRenderItem(SelectedColumn);
                }
                Show();
            }
        }

        public void ProcessCommand(string command)
        {
            command=command.ClearCommand();
            if(!command.IsNullOrEmpty())
            {
                switch(command)
                {
                    case "close":
                    {
                        Close();
                    }
                        break;

                    
                }
            }
        }

        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b=(Button)sender;
            if(b != null)
            {
                var t=b.Tag.ToString();
                ProcessCommand(t);
            }
        }
    }
}
