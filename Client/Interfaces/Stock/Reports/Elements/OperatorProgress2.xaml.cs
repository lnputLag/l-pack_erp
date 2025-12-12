using Client.Assets.HighLighters;
using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Interaction logic for OperatorProgress2.xaml
    /// </summary>
    public partial class OperatorProgress2 : UserControl
    {
        public OperatorProgress2()
        {
            InitializeComponent();
        }

        public delegate void MouseDown(object sender, MouseEventArgs e);
        public MouseDown OnMouseDown;

        public int Percent
        {
            get; set;
        }

        public void SetProgress(int percent, int move, int arrival, int writeOff, string txtArrival, string txtMove, string txtWriteOff)
        {
            Percent = percent;


            ItemArrival.SetProgress(arrival, txtArrival);
            ItemWriteOff.SetProgress(writeOff, txtWriteOff);
            ItemMove.SetProgress(move, txtMove);


            ItemArrival.Buf3ColorRectangle.Fill = "LightBlue".ToBrush();
            ItemMove.Buf3ColorRectangle.Fill = "LightGreen".ToBrush();
            ItemWriteOff.Buf3ColorRectangle.Fill = "Orange".ToBrush();
            

            Buf1ColorRow.Width = new GridLength(Convert.ToDouble(percent), GridUnitType.Star);
            Buf1GrayRow.Width = new GridLength(Convert.ToDouble(100 - percent), GridUnitType.Star);
            //Buf1GreenRectangle.Fill = color.ToBrush();

            //ColumnArrivale.Width = new GridLength(Convert.ToDouble(arrival), GridUnitType.Star);
            //ColumnMove.Width = new GridLength(Convert.ToDouble(move), GridUnitType.Star);
            //ColumnWriteOff.Width = new GridLength(Convert.ToDouble(writeOff), GridUnitType.Star);



            //Description.Text = description;
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OnMouseDown?.Invoke(this, e);
        }
    }
}
