using Client.Common;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Main
{
   
    public class InformationTable
    {

        public InformationTable()
        {
            Items=new Dictionary<string, string>();
        }

        public Dictionary<string,string> Items {get;set;}

        public void AddRow(string label, string value="")
        {
            Items.CheckAdd(label,value);
        }

        public Border GetObject()
        {
             var result = new Border();

             //var g=new StackPanel();
             //g.Orientation=Orientation.Vertical;

             var g=new Grid();

             {
                 {
                     var cd = new ColumnDefinition
                     {                     
                         Width=GridLength.Auto,                         
                     };
                     g.ColumnDefinitions.Add(cd);
                 }

                 {
                     var cd = new ColumnDefinition
                     {                     
                         Width=GridLength.Auto,
                     };
                     g.ColumnDefinitions.Add(cd);
                 }
             }
              
             int row=0;
             foreach(KeyValuePair<string,string> item in Items)
             {

                {
                    var rd = new RowDefinition
                    {
                        //Style = (Style)g.FindResource("SHMonTermGridTLRow"),
                    };
                    g.RowDefinitions.Add(rd);
                }

                // 0=1 col,1=2 cols,
                var mode=0;
                if(
                    !item.Key.IsNullOrEmpty()
                    && !item.Value.IsNullOrEmpty()
                )
                {
                    mode=1;
                }

                switch(mode)
                {
                    case 0:
                    {
                        {
                            var t=new TextBlock();
                            t.Text=item.Key;
                            g.Children.Add(t);
                            Grid.SetRow(t,row);
                            Grid.SetColumn(t,0);
                            Grid.SetColumnSpan(t,2);
                        }
                    }
                    break;

                    case 1:
                    {
                        {
                            var t=new TextBlock();
                            t.Text=$"{item.Key}:";
                            t.HorizontalAlignment=HorizontalAlignment.Right;
                            t.Margin=new Thickness(0,0,5,0);
                            g.Children.Add(t);
                            Grid.SetRow(t,row);
                            Grid.SetColumn(t,0);
                        }

                        {
                            var t=new TextBlock();
                            t.Text=item.Value;
                            g.Children.Add(t);
                            Grid.SetRow(t,row);
                            Grid.SetColumn(t,1);
                        }
                    }
                    break;
                }

                

                row=row+1;

             }

             result.Child=g;

             return result;
        }

    }
}
