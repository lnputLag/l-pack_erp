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
    /// прототип фрейма интерфейса
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-08-24</released>
    /// <changed>2024-04-04</changed>
    public partial class ControlBase: UserControl
    {
        public ControlBase()
        {
            if(Central.InDesignMode()){
                return;
            }

            Version = "3";
            RoleName = "";
            ControlName =this.GetType().Name;
            ControlSection = "";
            ControlTitle =ControlName;
            ControlId=Cryptor.MakeRandom().ToString();
            Initialized=false;
            InFocus=false;
            Active=false;
            Parameters = new Dictionary<string, string>();

            PrimaryKey = "";
            PrimaryKeyValue = "";
            FrameName = this.GetType().Name;
            FrameTitle = "";
            FrameMode = 1;

            Loaded += OnLoadInner;

            Commander = new CommandController();
            Commander.Init(this);
            OnGetFrameTitle = null;

            Input = new InputController();
        }

        public string Version { get; set; }

        /// <summary>
        /// код роли
        /// </summary>
        public string RoleName { get; set; }
        /// <summary>
        /// имя интерфейса
        /// (техническое имя, по умолчанию будет подставлено имя класса)
        /// </summary>
        public string ControlName {get;set;}
        /// <summary>
        /// имя секции для второй фазы навигации
        /// это слово будет добавлено к URL вызова интерфейса,
        /// для навигации внутри интерфейса
        /// </summary>
        public string ControlSection { get; set; }
        /// <summary>
        /// заголовок интерфейса
        /// (будет отображен на вкладке интерфейса, заголовок таба)
        /// </summary>
        public string ControlTitle {get;set;}
        private bool Initialized {get;set;}

        public delegate void OnLoadDelegate();
        /// <summary>
        /// коллбэк, вызывается при первом отображении интерфейса
        /// (все объекты проинициализированы, интерфейс получил фокус ввода, был отрендерен)
        /// </summary>
        public OnLoadDelegate OnLoad;
        public virtual void OnLoadAction(){}

        public delegate void OnUnloadDelegate();
        /// <summary>
        /// коллбэк, вызывается при уничтожении вкладки
        /// (вкладка закрыта программно или по клику пользователя)
        /// </summary>
        public OnUnloadDelegate OnUnload;
        public virtual void OnUnloadAction(){}

        public delegate void OnFocusGotDelegate();
        /// <summary>
        /// коллбэк, вызывается при получении фокуса интерфейсом
        /// (пользователь переключился на вкладку, содержащую интерфейс)
        /// </summary>
        public OnFocusGotDelegate OnFocusGot;
        public virtual void OnFocusGotAction(){}

        public delegate void OnFocusLostDelegate();
        /// <summary>
        /// коллбэк, вызывается при потере фокуса интерфейсом
        /// (пользователь переключился на другую вкладку)
        /// </summary>
        public OnFocusLostDelegate OnFocusLost;
        public virtual void OnFocusLostAction(){}

        public delegate string OnGetFrameTitleDelegate();
        public OnGetFrameTitleDelegate OnGetFrameTitle;
        public virtual void OnGetFrameTitleAction() { }

        public delegate void OnNavigateDelegate();
        /// <summary>
        /// коллбэк, вызывается при отработке второй фазы навигации
        /// </summary>
        public OnNavigateDelegate OnNavigate;
        public virtual void OnNavigateAction() { }

        /// <summary>
        /// коллбэк, вызывается при получении сообщения интерфейсом
        /// </summary>
        public ProcessMessageDelegate OnMessage;
        public virtual void OnMessageAction(ItemMessage message){}

        public delegate void OnKeyPressedDelegate(System.Windows.Input.KeyEventArgs e);
        public OnKeyPressedDelegate OnKeyPressed;
        public virtual void OnKeyPressedAction(System.Windows.Input.KeyEventArgs e){}
        
        /// <summary>
        /// параметры вызова
        /// </summary>
        public Dictionary<string,string> Parameters { get; set; }

        /// <summary>
        /// процессор команд
        /// </summary>
        public CommandController Commander { get;set;}

        /// <summary>
        /// ввод
        /// </summary>
        public InputController Input { get; set; }


        private string ControlId {get;set;}
        private string TabName {get;set;}
        public bool InFocus { get; set; }
        public bool Active { get; set; }

        public string PrimaryKey { get; set; }
        /// <summary>
        /// идентификатор записи, с которой работает форма
        /// </summary>
        public string PrimaryKeyValue { get; set; }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }
        /// <summary>
        /// заголовок фрейма 
        /// </summary>
        public string FrameTitle { get; set; }
        /// <summary>
        /// режим отображения: 0=по умолчанию, 1=новая вкладка, 2=новое окно
        /// </summary>
        public int FrameMode { get; set; }
        

        /// <summary>
        /// адрес страницы документации для данного фрейма
        /// относительный адрес, начинается со слеша:
        /// /doc/l-pack-erp/service/agent/agents
        /// </summary>
        public string DocumentationUrl {  get; set; }

        public string GetControlBaseVersion()
        {
            return Version;
        }

        public void DebugLog(string t)
        {
            LogMsg(t);
        }
        public void LogMsg(string t)
        {
            /*
                отправка сообщения через шину сообщений
                всем получателям, этой группы нинтерфейсов (ControlSection)

                прослушать можно в обработчике: OnMessage
                в конечной точке в коммандере: 
                    Commander.Add(new CommandItem()
                    {
                        Name = "debug_message",
                        ActionMessage = (ItemMessage message) =>
                        {
                            var m = message.Message;
                        },
                    }
             */

            if(!ControlSection.IsNullOrEmpty())
            {
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = ControlSection,
                    ReceiverName = "",
                    SenderName = ControlName,
                    Action = "debug_message",
                    Message = $"{t}",
                });
            }

            // вывод сообщения в консоль VisualStudio (Ctrl+Alt+O)
            var time = DateTime.Now.ToString("HH:mm:ss fff");
            Central.Dbg($"{time} {ControlName}: {t}");
        }

        private void OnLoadInner(object sender, RoutedEventArgs e)
        {
            if(!Initialized)
            {
                if(OnLoad != null)
                {
                    OnLoad.Invoke();
                }

                if(OnMessage != null)
                {
                    Central.Msg.Register(OnMessage);
                }

                Initialized=true;
            }
        }

        public void OnFocusGotInner()
        {
            if(Initialized)
            {
                if(OnFocusGot != null)
                {
                    OnFocusGot.Invoke();
                }
            }
        }

        

        public void OnNavigateInner()
        {
            if(Initialized)
            {
                if(OnNavigate != null)
                {
                    OnNavigate.Invoke();
                }
            }
        }

        public void OnFocusLostInner()
        {
            if(Initialized)
            {
                if(OnFocusLost != null)
                {
                    OnFocusLost.Invoke();
                }
            }
        }

        public void OnKeyPressInner()
        {
            if(Initialized)
            {
                var e=Central.WM.KeyboardEventsArgs;
                Input.Catch(e);
                if(
                    OnKeyPressed != null
                    //&& Input.ScanningInProgress
                )
                {
                    OnKeyPressed.Invoke(e);
                }
            }
        }

        //public void Show()
        //{
        //}

        //public void Close()
        //{
        //}

        /// <summary>
        /// отобразить фрейм
        /// </summary>
        public void Show()
        {
            Central.WM.FrameMode = FrameMode;
            var frameName = GetFrameName();
            var frameTitle = FrameTitle;

            if(OnGetFrameTitle!=null)
            {
                frameTitle=OnGetFrameTitle.Invoke();
                frameName = GetFrameName();
            }

            Central.WM.Show(frameName, frameTitle, true, "add", this);
        }

        /// <summary>
        /// скрыть фрейм
        /// </summary>
        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
        }

        /// <summary>
        /// скрыть фрейм
        /// </summary>
        public void Hide()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
        }



        public void ControlBaseInit()
        {
        }

        /// <summary>
        /// создание уникального идентификатора контрола
        /// ControlName+ControlId
        /// </summary>
        public string GetControlName(string id="")
        {
            var result="";
            result=$"{ControlName}";
            if(!id.IsNullOrEmpty())
            {
                result=$"{result}_{id}";
            }
            result = result.MakeSafeName();
            return result;
        }

        public string GetControlName(int id=0)
        {
            var result=GetControlName(id.ToString());
            return result;
        }

        /// <summary>
        /// уникальное имя вкладки
        /// </summary>
        /// <returns></returns>
        public string GetFrameName()
        {
            var a = FrameName.MakeSafeName();
            var b = PrimaryKeyValue.MakeSafeName();
            return $"{a}_{b}";
        }

        //public string GetControlUid()
        //{
        //    //var result = GetControlName(PrimaryKeyValue);

        //    string id = PrimaryKeyValue;
        //    var result = "";
        //    var FrameName = GetFrameName();
        //    result = $"{FrameName}";
        //    if(!id.IsNullOrEmpty())
        //    {
        //        result = $"{result}_{id}";
        //    }
        //    result = result.MakeSafeName();
        //    return result;
        //}


        public void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if(sender.GetType() == typeof(Button))
            {
                var b = (Button)sender;
                if(b != null)
                {
                    if(b.Tag != null)
                    {
                        var t = b.Tag.ToString();
                        if(!t.IsNullOrEmpty())
                        {
                            Commander.ProcessCommand(t);
                        }
                    }
                }
            }

            if(sender.GetType() == typeof(MenuItem))
            {
                var b = (MenuItem)sender;
                if(b != null)
                {
                    if(b.Tag != null)
                    {
                        var t = b.Tag.ToString();
                        if(!t.IsNullOrEmpty())
                        {
                            Commander.ProcessCommand(t);
                        }
                    }
                }
            }

        }

        /// <summary>
        /// добавить стандартные команды для тачскринов
        /// </summary>
        public void AddCommandsStandartTouch()
        {

        }
    }
}
