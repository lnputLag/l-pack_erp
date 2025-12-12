using System.Collections;
using System.Windows.Controls;

namespace Client.Interfaces.Main.Controls.Tabs
{
    public class ExtTabItem : ClosableTab
    {
        public ExtTabItem(string name, string title, bool closeable, object content = null, string type = "top") : base(title, closeable)
        {
            Tabs = null;
            Name = name;
            ChildNames = new ArrayList();
            LastSelectedChildTabName="";
            InFocus=false;
            Active=false;
            Level=0;
            Root=false;

            if( content == null )
            {               
                if( type=="top" )
                {
                    Tabs=new ExtTabItemTop().Tabs;
                }
                else
                {
                    Tabs=new ExtTabItemBottom().Tabs;
                }
                Content = Tabs.Parent;
            }
            else
            {                
                Content = content;
            }
        }

        public TabControl Tabs { get; }
        public string ParentName { get; set; }
        public ArrayList ChildNames { get; }
        public string LastSelectedChildTabName { get; set; }
        public bool InFocus { get; set; }
        public bool Active { get; set; }
        public int Level { get; set; }
        public bool Root { get; set; }
    }
}
