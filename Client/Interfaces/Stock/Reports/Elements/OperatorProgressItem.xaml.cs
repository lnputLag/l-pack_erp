using Client.Assets.HighLighters;
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
    /// Interaction logic for OperatorProgress.xaml
    /// </summary>
    public partial class OperatorProgressItem : UserControl
    {
        public OperatorProgressItem()
        {
            InitializeComponent();
        }

        public delegate void MouseDown(object sender, MouseEventArgs e);
        public MouseDown OnMouseDown;

        public int Percent
        {
            get; set;
        }

        public void SetProgress(int percent, string description)
        {
            Percent = percent;

            Buf1ColorRow.Width = new GridLength(Convert.ToDouble(percent), GridUnitType.Star);
            Buf1GrayRow.Width = new GridLength(Convert.ToDouble(100 - percent), GridUnitType.Star);

            Description.Text = description;

        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OnMouseDown?.Invoke(this, e);
        }
    }
}
