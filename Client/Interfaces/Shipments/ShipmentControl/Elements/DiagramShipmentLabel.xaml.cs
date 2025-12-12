using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Client.Assets.HighLighters;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// блок "временная метка"
    /// (план отгрузок)
    /// </summary>
    /// <author>balchugov_dv</author>   
    public partial class DiagramShipmentLabel : UserControl
    {
        public DiagramShipmentLabel(Dictionary<string, string> values, int factoryId = 1, Dictionary<string, string> normative = null)
        {
            InitializeComponent();
            Values = values;
            FactoryId = factoryId;
            if (normative != null && normative.Count > 0)
            {
                Normative = normative;
            }
            else
            {
                if (FactoryId == 2)
                {
                    Normative = NormativeKashira;
                }
                else
                {
                    Normative = NormativeLipetsk;
                }
            }

            Init();
        }

        private static Dictionary<string, string> NormativeLipetsk = new Dictionary<string, string>()
        {
            { "00:00", "5" } ,
            { "01:00", "4" } ,
            { "02:00", "4" } ,
            { "03:00", "4" } ,
            { "04:00", "4" } ,
            { "05:00", "4" } ,
            { "06:00", "5" } ,
            { "07:00", "5" } ,
            { "08:00", "5" } ,
            { "09:00", "5" } ,
            { "10:00", "6" } ,
            { "11:00", "10" } ,
            { "12:00", "10" } ,
            { "13:00", "9" } ,
            { "14:00", "9" } ,
            { "15:00", "9" } ,
            { "16:00", "8" } ,
            { "17:00", "8" } ,
            { "18:00", "9" } ,
            { "19:00", "9" } ,
            { "20:00", "11" } ,
            { "21:00", "11" } ,
            { "22:00", "10" } ,
            { "23:00", "7" }
        };

        private static Dictionary<string, string> NormativeKashira = new Dictionary<string, string>()
        {
            { "00:00", "0" } ,
            { "01:00", "0" } ,
            { "02:00", "0" } ,
            { "03:00", "0" } ,
            { "04:00", "0" } ,
            { "05:00", "0" } ,
            { "06:00", "0" } ,
            { "07:00", "0" } ,
            { "08:00", "3" } ,
            { "09:00", "3" } ,
            { "10:00", "3" } ,
            { "11:00", "3" } ,
            { "12:00", "2" } ,
            { "13:00", "2" } ,
            { "14:00", "3" } ,
            { "15:00", "3" } ,
            { "16:00", "3" } ,
            { "17:00", "3" } ,
            { "18:00", "3" } ,
            { "19:00", "3" } ,
            { "20:00", "0" } ,
            { "21:00", "0" } ,
            { "22:00", "0" } ,
            { "23:00", "0" }
        };

        private Dictionary<string, string> Normative { get; set; } 

        /// <summary>
        /// данные блока
        /// </summary>
        public Dictionary<string, string> Values { get; set; }

        private int FactoryId { get; set; }

        /// <summary>
        /// инициализация блока
        /// </summary>
        public void Init()
        {
            Label.Text=Values.CheckGet("LABEL");
            Label2.Text = Values.CheckGet("BLOCK") + "/" + Normative.CheckGet(Label.Text);
        }
    }
}
