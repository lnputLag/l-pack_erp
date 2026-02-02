using Client.Interfaces.Main;
using Client.Interfaces.Main.Controls.Tabs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Common
{
    /// <summary>
    /// управление окнами и вкладками приложения
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public class WindowManager
    {
        public WindowManager()
        {
            TabItems = new Dictionary<string, ExtTabItem>();
            LastSelectedTab="";
            SelectedTab="";
            Windows=new Dictionary<string, Window>();
            FrameMode=0;          
            FrameTypes=new Dictionary<string, int>();
            KeyboardProfiler=new Profiler("kbd");
            KeyboardPatternBuffer="";
            KeyboardPatternBufferLen = 0;
            ScannerInputProgress = false;
            KeyboardInputBuffer ="";
            ScannerInput=false;
            TabObjects=new Dictionary<string, object>();
            TabParents = new Dictionary<string, string>();
            Label ="WM";
            InnerLog="";
            ActivesLog="";
            TabMainSelected1="";
            TabSelected1="";
            TabSelected2="";
            TabSelected3="";
            FocusStartTimeout=new Timeout(
                1,
                ()=>{
                    ProcessFocusStart();
                }
            );
            FocusStartTimeout.SetIntervalMs(300);
            ProcessFocusTabName="";
        }

        /*
            WindowManager

                реестры
                    TabItems    (old)
                    TabObjects  (new)
                    TabParents  (new)

                добавление таба в реестр
                    AddTab
                        TabItems
                        TabObjects
                    CheckAddTab<T>
                        AddTab
                    AddTab<T>
                        AddTab
                        TabObjects
                        TabParents
         
         */

        public TabControl MainTabsContainer { get; set; }
        public TabControl AddTabsContainer { get; set; }
        /// <summary>
        /// реестр табов (старый)
        /// </summary>
        public Dictionary<string, ExtTabItem> TabItems { get; }
        /// <summary>
        /// реестр табов (новый)
        /// object -- инстанс интерфейса (прототип ControlBase)
        /// </summary>
        private Dictionary<string, object> TabObjects { get; }
        /// <summary>
        /// иерархия табов
        /// </summary>
        private Dictionary<string, string> TabParents { get; }
        public string LastSelectedTab { get; set; }
        public string SelectedTab { get; set; }
        private string Label { get; set; }
        private string InnerLog { get; set; }
        private string ActivesLog { get; set; }
        public string TabMainSelected1 { get; set; }
        public string TabSelected1 { get; set; }
        public string TabSelected2 { get; set; }
        public string TabSelected3 { get; set; }
        private Timeout FocusStartTimeout {get;set;}
        private string ProcessFocusTabName {get;set;}
        private string ProcessFocusPrev{get;set; }="";
        public System.Windows.Input.KeyEventArgs KeyboardEventsArgs { get;set;}
        public Profiler KeyboardProfiler { get;set;}
        public string KeyboardPatternBuffer { get;set;}
        public int KeyboardPatternBufferLen { get; set; }
        public bool ScannerInput { get;set;}
        public bool ScannerInputProgress { get; set; }
        public string KeyboardInputBuffer { get;set;}
        public bool KeyboardEventHandled { get; set; }
        public int FrameMode { get;set;}
        public Dictionary<string,int> FrameTypes { get;set;}
        private Dictionary<string,Window> Windows { get;set;}
        public enum FrameModeRef
        {
            Default=0,
            NewTab=1,
            NewWindow=2,
        }
        public enum TabBarRef
        {
            Top=1,
            Bottom=2,
        }

        /// <summary>
        /// </summary>
        /// <param name="name">уникальный идентификатор таба</param>
        /// <param name="title">заголовок таба (можно вместо текста задать глиф: glyph:menu)</param>
        /// <param name="closeable">возможность закрыть таб</param>
        /// <param name="parentName">идентификатор родительского таба</param>
        /// <param name="content">контрол, который будет отрисовываться внутри рабочего фрейма таба</param>
        /// <param name="type">расположение внутреннего блока вкладок top|bottom</param>
        [Obsolete]
        public void AddTab(string name, string title = "tab", bool closeable = true, string parentName = "main", object content = null, string innerTabsPosition = "top")
        {
            bool createNewTab = true;
            var container = MainTabsContainer;
            ExtTabItem parentTab = null;

            if( parentName == "main" )
            {
                container = MainTabsContainer;
            }
            else if( parentName == "add" ){
                container = AddTabsContainer;                
            }
            else
            {
                if( TabItems.ContainsKey(parentName) )
                {
                    container = TabItems[parentName].Tabs;
                    parentTab = TabItems[parentName];                    
                }
            }

            //если в контейнере есть вкладки, производится поиск среди них
            if (container != null)
            {
                foreach (var item in container.Items)
                {
                    var tab = (ExtTabItem)item;
                    if (tab != null)
                    {
                        if (tab.Name == name)
                        {
                            //если в контейнере была найдена вкладка с указанныи именем
                            //будет открыта найденная вкладка (вместо создания новой вкладки)
                            SetActiveTab(container, tab);
                            createNewTab = false;
                            break;
                        }
                    }
                }
            }

            //иначе будет создана новая вкладка
            if( createNewTab )
            {
                var newTab = new ExtTabItem(name, title, closeable, content, innerTabsPosition) {ParentName = parentName};
                if(parentTab != null)
                {
                    parentTab.ChildNames.Add(name);

                    //вкладка, которая будет открываться по умолчанию -- это первая вкладка
                    if(parentTab.ChildNames.Count == 1)
                    {
                        parentTab.LastSelectedChildTabName=name;
                    }                    
                }

                //добавляем в набор табов контейнера
                container?.Items?.Add(newTab);

                //добавляем в реестр табов
                TabItems.Add(name, newTab);
                lock(TabObjects)
                {
                    if(!TabObjects.ContainsKey(name))
                    {
                        TabObjects.Add(name, newTab.Content);
                    }
                }                
            }
                         
            SetLayer(parentName);

            if( parentName!="add" )
            {
                LastSelectedTab=name;
            }   
            
            SelectedTab=name;

            if(parentName == "add")
            {
                SetActive(name, false, "1_AddTab0_parent_add");
                ProcessFocus(name, 1, "1_AddTab0_parent_add");
            }
        }

        [Obsolete]
        public T CheckAddTab<T>(string name, string title = "tab", bool closeable = true, string parent = "main", string innerTabsPosition = "top")
        where T:new()
        {
            try
            {
                if(TabItems.ContainsKey(name))
                {
                    if(TabObjects.ContainsKey(name))
                    {
                        return (T)Convert.ChangeType(TabObjects[name], typeof(T));
                    }
                }
                else
                {
                    T v=new T();
                    AddTab(name, title, closeable, parent, v, innerTabsPosition);
                    return (T)Convert.ChangeType(v, typeof(T));
                }
            }
            catch(Exception e)
            {
            }

            return default(T);
        }

        /// <summary>
        /// добавление нового таба в систему навигации
        /// (интерфейс должен быть унаследован от ControlBase)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent"></param>
        /// <param name="closeable"></param>
        /// <param name="innerTabsPosition"></param>
        public void AddTab<T>(string parent = "main", bool closeable = false,  string innerTabsPosition = "bottom")
        where T:new()
        {
            try
            {
                T refObject=new T();
                var tab=(ControlBase)Convert.ChangeType(refObject, typeof(T));;
                var name=tab.ControlName;
                var title=tab.ControlTitle;

                if(!TabItems.ContainsKey(name))
                {
                    if(
                        !name.IsNullOrEmpty()
                        && !title.IsNullOrEmpty()
                    )
                    {
                        AddTab(name, title, closeable, parent, refObject, innerTabsPosition);
                        lock(TabObjects)
                        {
                            if(!TabObjects.ContainsKey(name))
                            {
                                TabObjects.Add(name, refObject);
                            }                            
                        }

                        if(!TabParents.ContainsKey(name))
                        {
                            TabParents.Add(name, parent);
                        }
                    }
                    else
                    {
                        var msg="";
                        
                        msg=msg.Append($"Объект должен быть унаследован от ControlBase");
                        msg=msg.Append($"У объекта должны быть задать свойства:");
                        msg=msg.Append($"    ControlName");
                        msg=msg.Append($"    ControlSection");                        
                        msg=msg.Append($"    ControlTitle");
                        var d = new LogWindow($"{msg}", "Создание вкладки" );
                        d.ShowDialog();
                    }
                }
            }
            catch(Exception e)
            {
            }
        }

        /// <summary>
        /// удаление таба из стека
        /// для внутреннего использования
        /// для закрытия фрейма используйте Close
        /// </summary>
        /// <param name="tabName"></param>
        public void RemoveTab(string tabName)
        {
            if(
                TabItems.ContainsKey(tabName)
                || TabObjects.ContainsKey(tabName)
            )
            {
                var parentName="";

                var controlBaseInterface = GetControl(tabName);
                if (controlBaseInterface != null)
                {
                    if (controlBaseInterface.OnUnload != null)
                    {
                        controlBaseInterface.OnUnload.Invoke();
                    }
                }

                if(TabItems.ContainsKey(tabName))
                {
                    parentName=TabItems[tabName].ParentName;
                    DestroyChilds(tabName);
                }

                DebugLogActives($"9 RemoveTab    name=[{tabName.ToString().SPadLeft(24)}] parentName=[{parentName}]");

                if(TabObjects.ContainsKey(tabName))
                {
                    TabObjects.Remove(tabName);
                }

                if (FrameTypes.ContainsKey(tabName))
                {
                    FrameTypes.Remove(tabName);
                }

                //if(parentName != "add")
                {
                    NavigateBack();
                }        
            }
        }

        /// <summary>
        /// переключение таба на указанный
        /// (таб должен сущестсовать в системе навигации)
        /// </summary>
        /// <param name="tabName"></param>
        public void SetActive(string tabName, bool ignoreParent=false, string source="")
        {
            DebugLog($"SetActive tabName=[{tabName}] source=[{source}]");
            if(!tabName.IsNullOrEmpty())
            {
                if(tabName.ToLower()=="gohome")
                {
                    NavigateBack();
                }
                else
                {
                    SelectTabInner(tabName, "SetActive");
                }
            }            
        }

        public void SetActive2(string tabName)
        {
            DebugLog($"SetActive2 tabName=[{tabName}] ");
            if(!string.IsNullOrEmpty(tabName))
            {
                if(tabName.ToLower()=="gohome")
                {
                    SetLayer("main");
                    if(!TabMainSelected1.IsNullOrEmpty())
                    {
                        SelectTabInner(TabMainSelected1, "SetActive2_1");
                    }
                }
                else
                {
                    SelectTabInner(tabName, "SetActive2_2");
                }
            }
        }

        /// <summary>
        /// переключение планов
        /// </summary>
        /// <param name="n"></param>
        /// <param name="currentName"></param>
        [Obsolete]
        public void SetLayer(string n="main")
        {
            string parent ="";

            if( TabItems.ContainsKey(n) )
            {
                parent = TabItems[n].ParentName;
            }

            if(  n == "add" || parent == "add" )
            {
                var m=(Border)MainTabsContainer.Parent;                           
                m.SetValue( Canvas.ZIndexProperty, 10 );

                var a=(Border)AddTabsContainer.Parent;                           
                a.SetValue( Canvas.ZIndexProperty, 20 );

                if( TabItems.ContainsKey("GoHome") )
                { 
                    TabItems["GoHome"].Visibility=Visibility.Visible;
                }
            }
            else
            {
                var m=(Border)MainTabsContainer.Parent;                           
                m.SetValue( Canvas.ZIndexProperty, 20 );

                var a=(Border)AddTabsContainer.Parent;                           
                a.SetValue( Canvas.ZIndexProperty, 10 );

                if( TabItems.ContainsKey("GoHome") )
                { 
                    TabItems["GoHome"].Visibility=Visibility.Hidden;
                }
            }

           
            SelectedTab=n;
        }

        /// <summary>
        /// получение имени таба-родителя относительно указанного таба
        /// </summary>
        /// <param name="tabName"></param>
        /// <returns></returns>
        public string GetParentTabName(string tabName)
        {
            var result = "";
            if(TabParents.Count>0)
            {
                if(TabParents.ContainsKey(tabName))
                {
                    result = TabParents[tabName];
                }
            }
            return result;
        }

        /// <summary>
        /// отработка второй фазы навигации
        /// установка фокуса на указанную вкладку
        /// </summary>
        /// <param name="parentTabName"></param>
        public void ProcNavigation(string parentTabName, string activeTabName="")
        {
            var resume = true;

            var doNavigation = false;
            var section = "";
            var address = Central.Navigator.Address;
            var activeTabNameLast = "";
            var activeTabNameSet = "";

            if(resume)
            {
                if(address.AddressInner.Count > 0)
                {
                    if(address.AddressInner[0] != null)
                    {
                        section = address.AddressInner[0].ToString();
                    }                    
                }
                if(section.IsNullOrEmpty())
                {
                    resume = false;
                }
                else
                {
                    doNavigation = true;
                }
            }

            {
                foreach(KeyValuePair<string, object> item in TabObjects)
                {
                    var currentTabName = item.Key;
                    var currentParentTabName = "";
                    if(TabParents.ContainsKey(currentTabName))
                    {
                        currentParentTabName = TabParents[currentTabName];
                    }

                    var tab = TryGetBaseObject(item.Value);
                    if(tab != null)
                    {
                        {
                            if(currentParentTabName == parentTabName)
                            {
                                activeTabNameLast = currentTabName;

                                if(doNavigation)
                                {
                                    if(!tab.ControlSection.IsNullOrEmpty())
                                    {
                                        if(tab.ControlSection == section)
                                        {
                                            if(address.Params.Count > 0)
                                            {
                                                tab.Parameters = address.Params;
                                            }

                                            tab.OnNavigateInner();
                                            activeTabNameSet = currentTabName;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            {
                if(!activeTabNameSet.IsNullOrEmpty())
                {
                    // по навигации
                    SetActive(activeTabNameSet, false, "ProcNavigation_1_activeTabNameSet");
                }
                else
                {
                    if(!activeTabName.IsNullOrEmpty())
                    {
                        // активная вкладка по умолчанию
                        // (прописанная в интерфейсе)
                        SetActive(activeTabName, false, "ProcNavigation_2_activeTabName");
                    }
                    else
                    {
                        if(!activeTabNameLast.IsNullOrEmpty())
                        {
                            // последняя вкладка в этом разделе
                            SetActive(activeTabNameLast,false, "ProcNavigation_3_activeTabNameLast");
                        }
                    }
                }
            }
        }

        public void ProcessKeyboard(System.Windows.Input.KeyEventArgs e)
        {
            KeyboardEventHandled = false;
            KeyboardEventsArgs=e;

            {
                {
                    var t=(int)KeyboardProfiler.GetDelta();
                    if(t > Central.Parameters.KeyboardInputBufferClearTimeout)
                    {
                        KeyboardPatternBuffer="";
                        KeyboardPatternBufferLen = 0;
                    }

                    var x=e.Key.ToString();
                    x=x.ToLower();

                    if(!string.IsNullOrEmpty(x))
                    {
                        switch (x)
                        {
                            case "d0":
                            case "0":
                                x ="0";
                                break;

                            case "d1":
                            case "1":
                                x ="1";
                                break;

                            case "d2":
                            case "2":
                                x ="2";
                                break;

                            case "d3":
                            case "3":
                                x ="3";
                                break;

                            case "d4":
                            case "4":
                                x ="4";
                                break;

                            case "d5":
                            case "5":
                                x ="5";
                                break;

                            case "d6":
                            case "6":
                                x ="6";
                                break;

                            case "d7":
                            case "7":
                                x ="7";
                                break;

                            case "d8":
                            case "8":
                                x ="8";
                                break;

                            case "d9":
                            case "9":
                                x ="9";
                                break;

                            case "down":
                                x="<DN>";
                                break;

                            case "return":
                                x="<CR>";
                                break;
                        }
                    }

                    KeyboardPatternBuffer=$"{KeyboardPatternBuffer}{x}";
                    KeyboardPatternBufferLen++;

                    if(KeyboardPatternBufferLen > 32)
                    {
                        KeyboardPatternBuffer="";
                        KeyboardPatternBufferLen = 0;
                    }

                    if(!string.IsNullOrEmpty(KeyboardPatternBuffer))
                    {
                        if (KeyboardPatternBuffer.IndexOf("<CR>") > -1)
                        {
                            ScannerInput=true;
                            KeyboardInputBuffer=KeyboardPatternBuffer;
                            KeyboardInputBuffer = KeyboardInputBuffer.Replace("<CR>", "");
                            // Из-за этого при ручном вводе после нажатия return возвращает в WordScanned "0"
                            KeyboardInputBuffer = KeyboardInputBuffer.ToDouble().ToString();
                        }
                    }
                }

                //obsolete
                if(TabItems.ContainsKey(SelectedTab))
                {
                    var type = TabItems[SelectedTab].Content.GetType();
                    if( type.GetMethod("ProcessKeyboard2") != null )
                    {
                        try{              
                            object ret=type.InvokeMember("ProcessKeyboard2", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, TabItems[SelectedTab].Content, null);
                            if (KeyboardEventHandled)
                            {
                                e.Handled = true;
                            }
                        }
                        catch(Exception)
                        {
                        }
                    }
                }    
                
                if(!TabSelected1.IsNullOrEmpty())
                {
                    if(TabObjects.Count > 0)
                    {
                        lock(TabObjects)
                        {
                            var list = new Dictionary<string, object>(TabObjects);                            
                            foreach(KeyValuePair<string, object> item in list)
                            {
                                var obj = item.Value;
                                var tab = TryGetBaseObject(obj);
                                if(tab != null)
                                {
                                    var n = tab.GetFrameName();

                                    if(tab.ControlName == TabSelected1)
                                    {
                                        tab.OnKeyPressInner();
                                    }

                                    if(n == TabSelected1)
                                    {
                                        tab.OnKeyPressInner();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public string GetScannerInput()
        {
            string result="";

            if(ScannerInput)
            {
                result=KeyboardInputBuffer;
                KeyboardInputBuffer="";
                KeyboardPatternBuffer = "";
                KeyboardPatternBufferLen = 0;
                ScannerInput =false;
            }
            
            return result;
        }

        public bool ScanningInProgress()
        {
            var result = false;
            if(KeyboardPatternBufferLen > 1)
            {
                result = true;
            }
            return result;
        }

        /// <summary>
        /// открыть фрейм
        /// добавление фрейма (таб или окно) в систему навигации
        /// отображение интерфейса во фрейме
        /// </summary>
        /// <param name="name"></param>
        /// <param name="title"></param>
        /// <param name="closeable"></param>
        /// <param name="parent"></param>
        /// <param name="content"></param>
        /// <param name="innerTabsPosition"></param>
        /// <param name="p"></param>
        public void Show(string name, string title = "tab", bool closeable = true, string parent = "main", object content = null, string innerTabsPosition = "top", Dictionary<string,string> p=null)
        {
            bool resume=true;

            if (p == null)
            {
                p = new Dictionary<string, string>();
            }

            if(resume)
            {
                if(FrameTypes.ContainsKey(name))
                {
                    //frame exists
                    resume=false;
                    SetActive(name, false, "show_1_frame_exists");
                }
            }

            if(FrameMode==0)
            {
                FrameMode=1;
            }

            if(resume)
            {
                //новое окно
                if(FrameMode==2)
                {
                    FrameTypes.Add(name,2);

                    if (p == null)
                    {
                        p = new Dictionary<string, string>();
                        p.CheckAdd("no_modal","1");
                    }
                    CreateWindow(name, (UserControl)content, title, p);                    
                }

                //новая вкладка
                if(FrameMode==1)
                {
                    FrameTypes.Add(name,1);
                    AddTab(name,title,closeable,parent,content,innerTabsPosition);                
                    SetActive(name, false, "show_1_frame_added");   
                }
                resume=false;
            }

            FrameMode=0;
        }

        /// <summary>
        /// скрыть фрейм
        /// </summary>
        /// <param name="name"></param>
        public void Close(string name)
        {
            var type=1;
            if(FrameTypes.ContainsKey(name))
            {
                type=FrameTypes[name];
                FrameTypes.Remove(name);
            }

            switch(type)
            {
                //новое окно
                case 2:
                    CloseWindow(name);
                    break;

                //новая вкладка
                case 1:
                default:
                    RemoveTab(name);
                    break;
            }
        }

        public void DebugLog(string message)
        {
            Central.Dbg($"{Label}: {message}");
            InnerLog=InnerLog.Append(message,true);
            InnerLog=InnerLog.Crop(8000);
        }

        private string DebugLogActivesPrev {get;set; } = Tools.GetToday();
        public void DebugLogActives(string message)
        {            
            var today=DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_ffffff");
            
            {
                var dt=Tools.TimeOffset(DebugLogActivesPrev);
                if(dt>=1200){
                    ActivesLog=ActivesLog.Append($" ",true);
                }
                DebugLogActivesPrev=Tools.GetToday();
            }

            ActivesLog=ActivesLog.Append($"{today} {message}",true);
            ActivesLog=ActivesLog.Crop(8000);           
        }

        public Dictionary<string, Dictionary<string,string>> GetTabItemsList()
        {
            var result= new Dictionary<string, Dictionary<string,string>>();
            if(TabItems.Count > 0)
            {
                foreach(KeyValuePair<string, ExtTabItem> item in TabItems)
                {
                    var k = item.Key;
                    var tab=item.Value;
                    var row = new Dictionary<string, string>();
                    row.CheckAdd("NAME", tab.Name.ToString());
                    row.CheckAdd("PARENT_NAME", tab.ParentName.ToString());
                    row.CheckAdd("IN_FOCUS", tab.InFocus.ToString().ToInt().ToString());
                    row.CheckAdd("ACTIVE", tab.Active.ToString().ToInt().ToString());
                    row.CheckAdd("LEVEL", tab.Level.ToString());
                    result.Add(k,row);
                }
            }
            return result;
        }

        public void EditWindowSettings(string name, Dictionary<string, string> p)
        {
            if (Windows != null)
            {
                if (Windows.ContainsKey(name))
                {
                    Window ctlWindow = Windows[name];

                    if (p.Count > 0)
                    {
                        foreach (KeyValuePair<string, string> item in p)
                        {
                            var k = item.Key;
                            var v = item.Value;

                            k = k.ClearCommand();
                            switch (k) 
                            {
                                case "no_resize":
                                    {
                                        if (v.ToInt() > 0)
                                        {
                                            ctlWindow.ResizeMode = ResizeMode.NoResize;
                                        }
                                    }
                                    break;

                                case "maximized_size":
                                    {
                                        if (v.ToInt() > 0)
                                        {
                                            ctlWindow.WindowState = WindowState.Maximized;
                                            ctlWindow.VerticalContentAlignment = VerticalAlignment.Stretch;
                                            ctlWindow.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                                        }
                                    }
                                    break;

                                case "center_screen":
                                    {
                                        if (v.ToInt() > 0)
                                        {
                                            ctlWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                                        }
                                    }
                                    break;

                                case "position_left":
                                    {
                                        if (!string.IsNullOrEmpty(v))
                                        {
                                            ctlWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                                            ctlWindow.Left = v.ToDouble();
                                        }
                                    }
                                    break;

                                case "position_top":
                                    {
                                        if (!string.IsNullOrEmpty(v))
                                        {
                                            ctlWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                                            ctlWindow.Top = v.ToDouble();
                                        }
                                    }
                                    break;

                                case "width":
                                    {
                                        if (v.ToInt() > 0)
                                        {
                                            ctlWindow.Width = v.ToInt() + 17;
                                        }
                                    }
                                    break;

                                case "height":
                                    {
                                        if (v.ToInt() > 0)
                                        {
                                            ctlWindow.Height = v.ToInt() + 40;
                                        }
                                    }
                                    break;

                                case "window_style":
                                    {
                                        if (!string.IsNullOrEmpty(v))
                                        {
                                            WindowStyle windowStyle = WindowStyle.SingleBorderWindow;
                                            switch (v.ToInt())
                                            {
                                                case 0:
                                                    windowStyle = WindowStyle.None;
                                                    break;

                                                case 1:
                                                    windowStyle = WindowStyle.SingleBorderWindow;
                                                    break;

                                                case 2:
                                                    windowStyle = WindowStyle.ThreeDBorderWindow;
                                                    break;

                                                case 3:
                                                    windowStyle = WindowStyle.ToolWindow;
                                                    break;

                                                default:
                                                    windowStyle = WindowStyle.SingleBorderWindow;
                                                    break;
                                            }
                                            ctlWindow.WindowStyle = windowStyle;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }

        public void DebugShow(int mode=1)
        {
            switch(mode)
            {
                case 1:
                    DebugShowTabsLog();
                    break;

                case 2:
                    DebugShowActLog();
                    break;

            }
        }



        private void DebugShowTabsLog()
        {
            var d = new LogWindow("", "Конфигурация вкладок" );
            d.AutoUpdateInterval=1;
            d.SetSize(1360,900);
            d.Show();
            d.SetOnUpdate(()=>
            {
                var s = "";
                s=$"{s}WINDOW_MANAGER";   

                {
                    s=$"{s}\n";   
                    s=$"{s}\n -2=[{TabSelected3}]";   
                    s=$"{s}\n -1=[{TabSelected2}]";   
                    s=$"{s}\n  0=[{TabSelected1}]  [{TabMainSelected1}] ";   
                    s=$"{s}\n             LastSelectedTab=[{LastSelectedTab}] ";   
                    s=$"{s}\n                 SelectedTab=[{SelectedTab}] ";   
                }

                {
                    s=$"{s}\n";                  
                    s=$"{s} {"#".ToString().SPadLeft(2)} | ";
                    s=$"{s} {"PARENT".ToString().SPadLeft(32)} | ";
                    s=$"{s} {"NAME".ToString().SPadLeft(32)} | ";
                    s=$"{s} {"HEADER".ToString().SPadLeft(16)} | ";
                    s=$"{s} {"T".ToString().SPadLeft(2)} | ";
                    s=$"{s} {"F".ToString().SPadLeft(2)} | ";
                }

                {
                    var j = 0;
                    foreach(KeyValuePair<string,ExtTabItem> item in TabItems)
                    {
                        j++;
                        var k=item.Key;
                        var tab=item.Value;

                        var type=1;
                        var title=tab.Title2;

                        var tab2=GetControl(k);
                        if(tab2 != null)
                        {
                            type=2;    
                            title=tab2.ControlTitle;
                        }

                        if(tab != null)
                        {
                            s=$"{s}\n";                  
                            s=$"{s} {j.ToString().SPadLeft(2)} | ";
                            s=$"{s} {tab.ParentName.ToString().SPadLeft(32)} | ";
                            s=$"{s} {tab.Name.ToString().SPadLeft(32)} | ";
                            s=$"{s} {title.ToString().SPadLeft(16)} | ";
                            s=$"{s} {type.ToString().SPadLeft(2)} | ";
                            s=$"{s} {tab.InFocus.ToInt().ToString().SPadLeft(2)} | ";
                        }
                    }
                } 

                {
                    s=$"{s}\n────[InnerLog]──────────────────────────────────";   
                    s=$"{s}\n{InnerLog.Crop(1000)}";   
                }

                {
                    s=$"{s}\n────[ActivesLog]────────────────────────────────";   
                    s=$"{s}\n{ActivesLog.Crop(1000)}";   
                }

                return s;
            });
        }

        private void DebugShowActLog()
        {
            var d = new LogWindow("", "Вкладки, события" );
            d.AutoUpdateInterval=1;
            d.SetSize(1360,900);
            d.Show();
            d.SetOnUpdate(()=>
            {
                var s = "";
                s=$"{s}WINDOW_MANAGER";   

                {
                    s=$"{s}\n";   
                    s=$"{s}\n 3=[{TabSelected3}]";   
                    s=$"{s}\n 2=[{TabSelected2}]";   
                    s=$"{s}\n 1=[{TabSelected1}]    [{TabMainSelected1}] ";   
                    //s=$"{s}\n             LastSelectedTab=[{LastSelectedTab}] ";   
                    //s=$"{s}\n                 SelectedTab=[{SelectedTab}] ";   
                }

                {
                    s=$"{s}\n────[ActivesLog]────────────────────────────────";   
                    s=$"{s}\n{ActivesLog.Crop(4000)}";   
                }

                return s;
            });
        }

        private void SetActiveTab(TabControl tabContainer, ExtTabItem tab)
        {
            DebugLog($"    SetActiveTab [{tab.Name}]");
            tabContainer.SelectedItem = tab;

            {
                var parentTabName = tab.ParentName;
                if(parentTabName != "add")
                {
                    ProcessFocus(tab.Name, 2, "2_SetActiveTab");
                }
            }
        }

        private void NavigateBack()
        {
            var level=0;
            var complete=false;
            var n="";

            if(!complete)
            {
                if(!TabSelected2.IsNullOrEmpty())
                {
                    level=1;
                    n=TabSelected2;
                    complete=true;
                }                
            }

            if(!complete)
            {
                if(!TabSelected3.IsNullOrEmpty())
                {
                    level=2;
                    n=TabSelected3;
                    complete=true;
                }
            }
            DebugLog($"  NavigateBack {level}->{n} ");
            DebugLogActives($"8 NavigateBack name=[{n.ToString().SPadLeft(24)}] level=[{level}]");
            SelectTabInner(n, "NavigateBack");
        }

        private void DestroyChilds(string name)
        {
            bool doReturn=false;

            if( TabItems.ContainsKey(name) )
            {
                var container = MainTabsContainer;
                string parent = TabItems[name].ParentName;

                if( parent == "main" )
                {
                    container = MainTabsContainer;
                }
                else if( parent == "add" )
                {
                    container = AddTabsContainer;
                    doReturn=true;
                }
                else
                {
                    if( TabItems.ContainsKey(parent) )
                    {
                        container = TabItems[parent].Tabs;
                    }
                }

                var type = TabItems[name].Content.GetType();

                //если таб содержит контрол, то будет вызван метода BeforeDestroy этого контрола (если существует)
                if( type.GetMethod("BeforeDestroy") != null )
                {
                    var beforeDestroyResult=true;
                    try{                        
                        object ret=type.InvokeMember("BeforeDestroy", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, TabItems[name].Content, null);
                        beforeDestroyResult=(bool)ret;
                    }
                    catch(Exception)
                    {

                    }

                    if( !beforeDestroyResult )
                    {
                        return;
                    }
                }

                //удаление из набора табов
                container.Items.Remove(TabItems[name]);

                if(TabObjects.ContainsKey(name))
                {
                    var controlBaseInterface = GetControl(name);
                    if (controlBaseInterface != null)
                    {
                        if (controlBaseInterface.OnUnload != null)
                        {
                            controlBaseInterface.OnUnload.Invoke();
                        }
                    }
                    TabObjects.Remove(name);
                }

                //если таб содержит контрол, то будет вызван метода Destroy этого контрола (если существует)
                if( type.GetMethod("Destroy") != null )
                {
                    try{
                        type.InvokeMember("Destroy", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, TabItems[name].Content, null);
                    }
                    catch(Exception)
                    {

                    }
                }

                //удаление из реестра табов
                var cn = TabItems[name].ChildNames;
                TabItems.Remove(name);

                //находим потомков
                if( cn.Count > 0 )
                {
                    foreach( var n in cn )
                    {
                        DestroyChilds(n.ToString());
                    }
                }               
            }
        }
        
        private void ProcessFocus(string tabName, int mode=0, string source="")
        {
            if(mode==4 || mode==3)
            {
                 DebugLog($"        * focus: [{tabName}]");
                DebugLogActives($"2 ProcessFocus name=[{tabName.ToString().SPadLeft(24)}] src=[{source.ToString().SPadLeft(24)}]");

                //new tabs
                {
                    if(TabObjects.Count > 0)
                    {
                        foreach(KeyValuePair<string,object> item in TabObjects)
                        {
                            var tab=TryGetBaseObject(item.Value);
                            if(tab != null)
                            {
                                if(tab.ControlName != tabName)
                                {
                                    tab.OnFocusLostInner();
                                    tab.InFocus=false;
                                }
                            }
                        }

                        foreach(KeyValuePair<string,object> item in TabObjects)
                        {
                            var tab=TryGetBaseObject(item.Value);
                            if(tab != null)
                            {
                                if(tab.ControlName == tabName)
                                {
                                    tab.OnFocusGotInner();
                                    tab.InFocus=true;
                                }
                            }
                        }
                    }
                }

                //old tabs
                {
                    if(TabItems.Count > 0)
                    {
                        foreach(KeyValuePair<string,ExtTabItem> item in TabItems)
                        {
                            var k=item.Key;
                            var t=item.Value;

                            if(
                                t.ParentName != "add"
                                && t.Name != tabName
                            )
                            {
                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverGroup="All",
                                    ReceiverName = t.Name,
                                    SenderName = "WindowManager",
                                    Action = "FocusLost",
                                    Message=""
                                });
                                t.InFocus=false;
                            }
                        }

                        foreach(KeyValuePair<string,ExtTabItem> item in TabItems)
                        {
                            var k=item.Key;
                            var t=item.Value;

                            if(
                                t.ParentName != "add"
                                && t.Name == tabName
                            )
                            {
                                Central.Msg.SendMessage(new ItemMessage()
                                {
                                    ReceiverGroup="All",
                                    ReceiverName = t.Name,
                                    SenderName = "WindowManager",
                                    Action = "FocusGot",
                                    Message=""
                                });
                                t.InFocus=true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private ExtTabItem TabFindByName(string tabName)
        {
            ExtTabItem result=null;
            if(TabItems.ContainsKey(tabName))
            {
                result=TabItems[tabName];
            }
            return result;
        }

        private ExtTabItem TabFindParent(ExtTabItem tab)
        {
            ExtTabItem result=null;
            var tabName=tab.ParentName;
            if(!tabName.IsNullOrEmpty())
            {
                var parent=TabFindByName(tabName);
                if(parent != null)
                {
                    result=parent;
                }
            }
            return result;
        }

        private List<ExtTabItem> FindParents(string tabName)
        { 
            var result=new List<ExtTabItem>();

            var list=new List<string>();
            list.Add("main");
            list.Add("add");

            var tab1=TabFindByName(tabName);
            if(tab1 != null)
            {
                if(!list.Contains(tab1.Name))
                {
                    //DebugLog($"    {tab1.Name}");                    
                    result.Add(tab1);

                    var tab2=TabFindParent(tab1);
                    if(tab2 != null)
                    {
                        if(!list.Contains(tab2.Name))
                        {
                            //DebugLog($"    {tab2.Name}");
                            result.Add(tab2);

                            var tab3=TabFindParent(tab2);
                            if(tab3 != null)
                            {
                                if(!list.Contains(tab3.Name))
                                {
                                    //DebugLog($"    {tab3.Name}");
                                    result.Add(tab3);
                                }
                            }
                        }
                    }                    
                }
            }

            if(result.Count > 0)
            {
                int j=0;
                foreach(ExtTabItem tab in result)
                {
                    j++;
                    if(j == result.Count)
                    {
                        tab.Root=true;
                    }
                }
            }

            return result;
        }

        private TabControl GetTabContainer(string tabName)
        {
            var result=MainTabsContainer;
            {
                switch(tabName)
                {
                    case "main":
                        result=MainTabsContainer;
                        break;

                    case "add":
                        result=AddTabsContainer;
                        break;

                    default:
                    {
                        var tab=TabFindByName(tabName);
                        if(tab != null)
                        {
                            result=tab.Tabs;
                        }
                    }
                        break;
                }
            }
            return result;
        }

        private void TabContainerSetActive(string parentTabName, ExtTabItem tab)
        {
            var container=GetTabContainer(parentTabName);

            if(tab != null && container != null)
            {
                container.SelectedItem=tab;
                {
                    var parentTab=TabFindByName(parentTabName);
                    if(parentTab != null)
                    {
                        parentTab.LastSelectedChildTabName=tab.Name;
                    }
                }
                DebugLog($"        * select: {parentTabName}->{tab.Name}");
            }
        }

        private void SetActiveLayer(string parentTabName)
        {
            DebugLog($"        * layer: [{parentTabName}]");
            string parent ="";

            if(  parentTabName == "add" )
            {
                var m=(Border)MainTabsContainer.Parent;                           
                m.SetValue( Canvas.ZIndexProperty, 10 );

                var a=(Border)AddTabsContainer.Parent;                           
                a.SetValue( Canvas.ZIndexProperty, 20 );

                if( TabItems.ContainsKey("GoHome") )
                { 
                    TabItems["GoHome"].Visibility=Visibility.Visible;
                }
            }
            else
            {
                var m=(Border)MainTabsContainer.Parent;                           
                m.SetValue( Canvas.ZIndexProperty, 20 );

                var a=(Border)AddTabsContainer.Parent;                           
                a.SetValue( Canvas.ZIndexProperty, 10 );

                if( TabItems.ContainsKey("GoHome") )
                { 
                    TabItems["GoHome"].Visibility=Visibility.Hidden;
                }
            }
        }

        private string FindLastNode(ExtTabItem tab)
        {
            var result="";
            {
                var tab1=tab;
                if(tab1 != null)
                {
                    if(tab1.ChildNames.Count > 0)
                    {
                        var tab2Name=tab1.LastSelectedChildTabName;
                        if(!tab2Name.IsNullOrEmpty())
                        {
                            var tab2=TabFindByName(tab2Name);
                            if(tab2 != null)
                            {
                                if(tab2.ChildNames.Count > 0)
                                {
                                    var tab3Name=tab2.LastSelectedChildTabName;
                                     if(!tab3Name.IsNullOrEmpty())
                                     {
                                         var tab3=TabFindByName(tab3Name);
                                         if(tab3 != null)
                                         {
                                            if(tab3.ChildNames.Count > 0)
                                            {
                                            }
                                            else
                                            {
                                                result=tab3.Name;
                                            }
                                         }
                                     }
                                }
                                else
                                {
                                    result=tab2.Name;
                                }
                            }
                        }
                    }
                    else
                    {
                        result=tab1.Name;
                    }
                }
            }
            return result;
        }

        private string SelectTabInnerPrev{get;set; }="";

        /// <summary>
        /// проверка: эта вкладка единственная в системе в данный момент
        /// </summary>
        /// <returns></returns>
        private bool CheckSingleTab(string tabName)
        {
            var result=false;
            if(TabObjects.ContainsKey(tabName))
            {
                var x=0;
                foreach(var item in TabObjects)
                {
                    if(
                        item.Key!="GoHome"
                        && item.Key!=tabName
                    )
                    {
                        x++;
                    }
                }
                if(x==0)
                {
                    result=true;
                }
            }
            return result;
        }

        private void SelectTabInner(string tabName, string source="")
        {
            var checkSingleTabResult=CheckSingleTab(tabName);
            if(
                (
                    SelectTabInnerPrev!=tabName
                    || checkSingleTabResult
                )
                && (
                    TabItems.ContainsKey(tabName)
                    || TabObjects.ContainsKey(tabName)
                )
            )
            {
                DebugLog($"  SelectTabInner [{tabName}]");
                DebugLogActives($"0 SelectTabInn name=[{tabName.ToString().SPadLeft(24)}] source=[{source}]");
                SelectTabInnerPrev=tabName;
                

                var list=FindParents(tabName);            
                int j=0;
            
                var processRootTab=false;
                var rootTabName="";
                var processFocusTab=false;
                var focusTabName="";
                ExtTabItem prevTab=null;
                ExtTabItem curTab=null;
                ExtTabItem firstTab=null;

                var typeString="node";
                var root=" ";

                var ss=$"{TabSelected3} {TabSelected2} {TabSelected1}";
                DebugLog($"    {j} {ss}");

                foreach(ExtTabItem tab in list)
                {
                    j++;
                    curTab=tab;

                    if(firstTab == null)
                    {
                        firstTab=curTab;
                    }
                
                    //0=node,1=section
                    var type=0;
                    typeString="node";
                    root=" ";
                
                    if(curTab.ChildNames.Count > 0)
                    {
                        type=1;
                        typeString="sect";
                    }
                
                    if(curTab.Root)
                    {   
                        root="/";
                        processRootTab=true;
                        rootTabName=curTab.ParentName;
                    }

                    DebugLog($"    {j} [{root}] {typeString} [{curTab.Name.SPadLeft(32)}]");
                    if(type == 1)
                    {
                        //section
                        TabContainerSetActive(curTab.Name,prevTab);
                    }
                    else
                    {
                        //node                    
                        if(curTab.ParentName != "add")
                        {
                            processFocusTab=true;
                            focusTabName=curTab.Name;
                        }                   
                    }

                    prevTab=curTab;
                }

                if(processRootTab)
                {
                    j++;
                    root="^";
                    typeString="bloc";
                    DebugLog($"    {j} [{root}] {typeString} [{rootTabName.SPadLeft(32)}]");
                    TabContainerSetActive(rootTabName,prevTab);
                    SetActiveLayer(rootTabName);
                }

                if(!processFocusTab)
                {
                    var s=FindLastNode(firstTab);
                    if(!s.IsNullOrEmpty())
                    {
                        processFocusTab=true;
                        focusTabName=s;
                    }    
                }

                if(processFocusTab)
                {
                    StackPullTab(focusTabName, curTab);
                    ProcessFocus(TabSelected1, 3, "3_OnSelectTabInner");

                    //ProcessFocus(..., "4_ProcessFocusStart");
                    ProcessFocusTabName=focusTabName;                    
                    FocusStartTimeout.Restart();
                }
            }
        }
        
        private void StackPullTab(string tabName, ExtTabItem tab)
        {
            DebugLogActives($"1 StackPullTab name=[{tabName.ToString().SPadLeft(24)}] parent=[{tab.ParentName}]");
            TabSelected3 = TabSelected2;
            TabSelected2 = TabSelected1;
            TabSelected1 = tabName;

            if(tab.ParentName!="add")
            {
                TabMainSelected1=tabName;
            }            
        }

        private void ProcessFocusStart()
        {
            if(!ProcessFocusTabName.IsNullOrEmpty())
            {
                ProcessFocus(ProcessFocusTabName, 4, "4_ProcessFocusStart");                
            }
        }

        private ControlBase GetControl(string tabName)
        {
            ControlBase c=null;
            if(TabObjects.ContainsKey(tabName))
            {
                c=TryGetBaseObject(TabObjects[tabName]);
            }
            return c;
        }

        private ControlBase TryGetBaseObject (object o)
        {
            ControlBase c=null;
            try
            {
                var type = o.GetType();
                if (type.GetMethod("ControlBaseInit") != null)
                {
                    c=(ControlBase)o;
                }                
            }
            catch(Exception e)
            {
            }
            return c;
        }
        
        private void CreateWindow(string name, UserControl control, string title, Dictionary<string,string> p)
        {
            int w=800;
            int h=600;
            bool modal = true;

            if(!double.IsNaN(control.MinWidth))
            {
                w=(int)control.MinWidth;
            }

            if(!double.IsNaN(control.MinHeight))
            {
                h=(int)control.MinHeight;
            }

            if(!double.IsNaN(control.Width))
            {
                w=(int)control.Width;
            }

            if(!double.IsNaN(control.Height))
            {
                h=(int)control.Height;
            }

            if(p.Count > 0)
            {
                foreach(KeyValuePair<string, string> item in p)
                {
                    var k = item.Key;
                    var v = item.Value;

                    k = k.ClearCommand();
                    switch(k)
                    {
                        case "width":
                            {
                                if(v.ToInt() > 0)
                                {
                                    w = v.ToInt();
                                }
                            }
                            break;

                        case "height":
                            {
                                if(v.ToInt() > 0)
                                {
                                    h = v.ToInt();
                                }
                            }
                            break;

                        case "no_modal":
                            {
                                if(v.ToInt() > 0)
                                {
                                    modal = false;
                                }
                            }
                            break;
                    }
                }
            }

            var ctlWindow = new Window
            {
                Title = title,
                Width = w + 17,
                Height = h + 40,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.SingleBorderWindow,
                Name=name,
            };
            
            if (p.Count > 0)
            {
                foreach (KeyValuePair<string, string> item in p)
                {
                    var k = item.Key;
                    var v = item.Value;

                    k = k.ClearCommand();
                    switch (k)
                    {
                        case "no_resize":
                            {
                                if (v.ToInt() > 0)
                                {
                                    ctlWindow.ResizeMode = ResizeMode.NoResize;
                                }
                            }
                            break;

                        case "maximized_size":
                            {
                                if (v.ToInt() > 0)
                                {
                                    ctlWindow.WindowState = WindowState.Maximized;
                                    ctlWindow.VerticalContentAlignment = VerticalAlignment.Stretch;
                                    ctlWindow.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                                }
                            }
                            break;

                        case "center_screen":
                            {
                                if (v.ToInt() > 0)
                                {
                                    ctlWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                                }
                            }
                            break;

                        case "position_left":
                            {
                                if (!string.IsNullOrEmpty(v))
                                {
                                    ctlWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                                    ctlWindow.Left = v.ToDouble();
                                }
                            }
                            break;

                        case "position_top":
                            {                 
                                if (!string.IsNullOrEmpty(v))
                                {
                                    ctlWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                                    ctlWindow.Top = v.ToDouble();
                                }
                            }
                            break;
                    }
                }
            }
            
            ctlWindow.Content = new Frame
            {
                Content = control,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            ctlWindow.Closed += WindowOnClose;

            if(Windows!=null)
            {
                if(!Windows.ContainsKey(name))
                {
                    Windows.Add(name,ctlWindow);
                }
            }

            if (TabObjects != null)
            {
                lock (TabObjects)
                {
                    if (!TabObjects.ContainsKey(name))
                    {
                        TabObjects.Add(name, control);
                    }
                }
            }

            if ( ctlWindow != null )
            {
                if(modal)
                {
                    ctlWindow.ShowDialog();
                }
                else
                {
                    ctlWindow.Show();
                }
            }
        }

        private void WindowOnClose(object sender, EventArgs e)
        {
            var w=sender as Window;
            var name=w.Name;

            if (!string.IsNullOrEmpty(name))
            {
               CloseWindow(name);
            }
        }

        private void CloseWindow(string name)
        {
            DebugLogActives($"CloseWindow {name}");

            var controlBaseInterface = GetControl(name);
            if (controlBaseInterface != null)
            {
                if (controlBaseInterface.OnUnload != null)
                {
                    controlBaseInterface.OnUnload.Invoke();
                }
            }

            if (Windows!=null)
            {
                if(Windows.ContainsKey(name))
                {
                    Windows[name].Close();
                    Windows.Remove(name);
                }
            }

            if (FrameTypes!=null)
            {
                if(FrameTypes.ContainsKey(name))
                {
                    FrameTypes.Remove(name);
                }
            }
        }

        internal object CheckAddTab<T>()
        {
            throw new NotImplementedException();
        }
    }
}

