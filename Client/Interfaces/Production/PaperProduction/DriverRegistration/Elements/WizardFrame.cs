using Client.Common;
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

namespace Client.Interfaces.Production
{
    public partial class WizardFrame:TabControl
    {
        public WizardFrame()
        {
            Wizard=null;
            Form=null;
        }

        public Wizard Wizard { get; set; }
        public FormHelper Form { get; set; }

        public void LoadValues()
        {
            if(Wizard!=null){
                var v=Wizard.Values;
                Form.SetValues(v);
            }            
        }

        public void SaveValues()
        {
            if(Wizard!=null)
            {
                var v=Form.GetValues();
                foreach(KeyValuePair<string,string> item in v)
                {
                    Wizard.Values.CheckAdd(item.Key, item.Value);
                }
            }
        }
    }
}
