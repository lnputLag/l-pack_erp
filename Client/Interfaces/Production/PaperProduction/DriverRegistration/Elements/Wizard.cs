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
    public partial class Wizard:UserControl
    {
        public Wizard()
        {
            FrameList=new Dictionary<string, object>();
            MainFrame="";
            FrameCurrent="";
            FramePrev="";
            Values=new Dictionary<string, string>();
            ReturnHomeTimeout = 0;
            ReturnHomeTimer=null;
            

            //Central.Msg.Register(ProcessMessage);
        }

        private Dictionary<string,object> FrameList {get;set;}
        private string MainFrame {get;set;}
        private string FrameCurrent {get;set;}
        private string FramePrev {get;set;}
        public Dictionary<string,string> Values {get;set;}
        private Timeout ReturnHomeTimer { get; set; }
        /// <summary>
        /// Таймаут возврата к главному фрейму, сек.
        /// Если в течение этого времени не было переходов между фреймами,
        /// будет совершена навигация на главный фрейм.
        /// 0=механизм отключен
        /// </summary>
        public int ReturnHomeTimeout { get; set; }

        public void AddFrame(object frame, bool main=false)
        {
            if(frame != null)
            {
                var f=(TabControl)frame;
                var frameName=f.ControlName;
                FrameList.Add(frameName,frame);

                if(main)
                {
                    MainFrame=frameName;
                }
            }
        }

        public void Run()
        {
            if(FrameList.Count>0)
            {
                var j=0;
                foreach(KeyValuePair<string,object> item in FrameList)
                {
                    j++;
                    var frame=(TabControl)item.Value;
                    var frameName=item.Key;

                    if(
                        (MainFrame.IsNullOrEmpty() && j==1)
                        || (!MainFrame.IsNullOrEmpty() && frameName == MainFrame)
                    )
                    {
                        if(MainFrame.IsNullOrEmpty())
                        {
                            MainFrame=frameName;
                        }

                        Navigate(frameName);
                    }
                }

                if (ReturnHomeTimeout > 0)
                {
                    ReturnHomeTimer=new Timeout(
                        ReturnHomeTimeout,
                        () =>
                        {
                            Navigate(0);
                        }
                    );
                    ReturnHomeTimer.Run();
                }
                    
            }
        }

        public void Navigate(string f)
        {
            FrameCurrent=f;

            if(FrameList.Count>0)
            {
                if(!FramePrev.IsNullOrEmpty())
                {
                    foreach(KeyValuePair<string,object> item in FrameList)
                    {
                        var frame=(TabControl)item.Value;
                        var frameName=item.Key;
                        if(frameName == FramePrev)
                        {
                            frame.Close();
                        }
                    }
                }
                
                if(!FrameCurrent.IsNullOrEmpty())
                {
                    foreach(KeyValuePair<string,object> item in FrameList)
                    {
                        var frame=(TabControl)item.Value;
                        var frameName=item.Key;
                        if(frameName == FrameCurrent)
                        {
                            //FIXME:
                            //frame.Show("DriverRegistration");
                            frame.Show();
                        }
                    }
                }
            }
        }

        public void Navigate(int direction)
        {
            // -1=NavigateBack, 1=NavigateNext

            string framePrevName="";
            string frameCurrentName="";
            string frameNextName="";

            if(!FrameCurrent.IsNullOrEmpty())
            {
                foreach(KeyValuePair<string,object> item in FrameList)
                {
                    var resume=true;
                    var frame=(TabControl)item.Value;
                    var frameName=item.Key;

                    if(
                        frameCurrentName.IsNullOrEmpty()
                        && frameName != FrameCurrent
                    )
                    {
                        framePrevName=frameName;
                    }

                    if(resume)
                    {
                        if(frameName == FrameCurrent)
                        {
                            frameCurrentName=frameName;
                            resume=false;
                        }
                    }
                   
                    if(resume)
                    {
                        if(
                            !frameCurrentName.IsNullOrEmpty()
                            && frameNextName.IsNullOrEmpty()
                        )
                        {
                            frameNextName=frameName;
                            resume=false;
                        }
                    }
                }
            }

            var r0= framePrevName;
            var r1= frameCurrentName;
            var r2= frameNextName;

            //Central.Dbg($"{framePrevName} {frameCurrentName} {frameNextName}");

            if(direction == 1)
            {
                if(
                    !frameNextName.IsNullOrEmpty()
                    && frameNextName!=FrameCurrent
                )
                {
                    FramePrev=frameCurrentName;
                    Navigate(frameNextName);
                }
                ReturnHomeTimer.Restart();
            }
            else if(direction == -1)
            {
                if(
                    !framePrevName.IsNullOrEmpty()
                    &&  framePrevName!=FrameCurrent
                )
                {
                    FramePrev=frameCurrentName;
                    Navigate(framePrevName);
                }
                ReturnHomeTimer.Restart();
            }
            else if(direction == 0)
            {
                FramePrev=frameCurrentName;
                Navigate(MainFrame);
            }

        }
    }
}
