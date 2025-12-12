using Client.Common;
using DevExpress.ReportServer.ServiceModel.DataContracts;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using static Client.Common.Msg;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// прототип интерфейса
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-04-04</released>
    /// <changed>2024-04-04</changed>
    public partial class InterfaceBase : UserControl
    {
        public InterfaceBase()
        {
            if(Central.InDesignMode()){
                return;
            }
        }
    }
}
