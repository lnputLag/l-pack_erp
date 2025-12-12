using Client.Interfaces.Main;
using Client.Interfaces.Production;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;
using static Client.Common.FormExtend;
using static Client.Common.FormHelperField;
using static Client.Interfaces.Main.FormDialog;

namespace Client.Common
{
    public class FormHelperComment
    {
        public FormHelperComment()
        {
            Name = "";
            Content = "";
        }

        public string Name { get; set; }
        public string Content { get; set; }
    }
}
