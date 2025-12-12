using Client.Assets.HighLighters;
using Client.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Client.Interfaces.Service.Mail
{
    /// <summary>
    /// шаблон ярлыка (для печати)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-06-22</released>
    /// <changed>2023-06-22</changed>
    public partial class LabelBlock : UserControl
    {
        public LabelBlock()
        {
            InitializeComponent();
        }
    }
}
