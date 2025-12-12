using Client.Common;
using DevExpress.ReportServer.ServiceModel.DataContracts;
using DevExpress.Xpf.Grid;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static Client.Common.Msg;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// процессор команд
    /// <see href="http://192.168.3.237/developer/erp2/client/dev/notes/2024-04-05_commander">документация</see>
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-07-11</released>
    /// <changed>2024-07-11</changed>
    public class CommandController
    {
        public CommandController()
        {
            CommandList = new List<CommandItem>();
            LastKeyboardEvent=DateTime.Now;
            KeyboardProcessing = false;
            DoubleClickProcessing = false;
            UserAccessMode = Role.AccessMode.None;
            CurrentGridName = "";
            CurrentGroupName = "";
            Grids = new Dictionary<string, GridBox4>();
            LastGrid = null;
            Message = null;            

            GridContextMenuDo = false;
            GridContextMenuName = "";

            Central.Msg.Register(ProcessMessages);
        }

        private List<CommandItem> CommandList { get; set; }
        private DateTime LastKeyboardEvent { get; set; }
        private bool KeyboardProcessing { get; set; }
        private bool DoubleClickProcessing { get; set; }
        private ControlBase ControlBase { get; set; }
        private Role.AccessMode UserAccessMode { get; set; }
        private string CurrentGridName { get; set; }
        private string CurrentGroupName { get; set; }
        private Dictionary<string, GridBox4> Grids { get; set; }
        private GridBox4 LastGrid { get; set; }
        public ItemMessage Message { get; set; }
        

        public void Init(ControlBase h)
        {
            ControlBase = h;
            foreach (CommandItem c in CommandList)
            {
                if(!c.HotKey.IsNullOrEmpty())
                {
                    KeyboardProcessing = true;

                    var k = c.HotKey;
                    k = k.Trim();
                    k = k.ToLower();
                    if(k.IndexOf("doubleclick") > -1)
                    {
                        DoubleClickProcessing = true;
                    }
                }
            }

            InitRoles();
            InitButtons();
            InitMenus();
            UpdateButtons();
            UpdateMenus();

            try
            {
                UpdateActions();
            }
            catch (Exception)
            {
            }
      
            RenderButtons();
        }

        public void ProcessMessages(ItemMessage m)
        {
            if(m != null)
            {
                if(m.ReceiverName == "Commander")
                {
                    switch(m.Action)
                    {
                        case "SetRoleLevelTest":
                            {
                                if(ControlBase != null)
                                {
                                    Init(ControlBase);
                                }
                            }                            
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// имя текущего грида
        /// если не пустое, все команды будут добавляться к этому гриду (MenuGridName)
        /// GroupGrid
        /// GroupGrid|AccountGrid
        /// </summary>
        /// <param name="gridName"></param>
        public void SetCurrentGridName(string gridName="")
        {
            CurrentGridName = gridName;
        }

        public void SetCurrentGroup(string groupName="")
        {
            CurrentGroupName = groupName;
        }

        public void AddRange(List<CommandItem> list)
        {
            foreach(var c in list)
            {
                AddCommand(c);
            }            
        }
        public void Add(CommandItem c)
        {            
            AddCommand(c);
        }
        public void AddCommand(CommandItem c)
        {
            c.Name = c.Name.ClearCommand();
            c.Group = c.Group.ClearCommand();

            if(c.ButtonTitle.IsNullOrEmpty())
            {
                c.ButtonTitle = c.Title;
            }

            if(c.MenuTitle.IsNullOrEmpty())
            {
                c.MenuTitle = c.Title;
            }

            if(c.Default)
            {
                c.HotKey = "Return|DoubleCLick";
            }

            if(!CurrentGridName.IsNullOrEmpty())
            {
                if(c.MenuGridName.IsNullOrEmpty())
                {
                    c.MenuGridName = CurrentGridName;
                }
            }

            if(!CurrentGroupName.IsNullOrEmpty())
            {
                if(c.Group.IsNullOrEmpty())
                {
                    c.Group = CurrentGroupName;
                }
            }

            var commandExists = FindCommandByName(c.Name);
            if(commandExists == null)
            {
                {
                    if(c.Control!=null)
                    {

                    }
                    else
                    {
                        //button
                        if(c.ButtonControl == null)
                        {
                            c.ButtonControl = FindButtonControl(c.ButtonName);
                        }

                        if(c.ButtonControl == null)
                        {
                            c.ButtonUse = false;
                        }

                        if(c.ButtonControl != null)
                        {
                            c.Control = c.ButtonControl;
                        }
                    }

                    c.EnabledInitial = c.Enabled;
                }

                CommandList.Add(c);
            }
        }

        /// <summary>
        /// обработка горячих клавиш команд
        /// </summary>
        /// <param name="e"></param>
        public void ProcessKeyboard(System.Windows.Input.KeyEventArgs e)
        {
            if(KeyboardProcessing)
            {
                if(!e.Handled)
                {
                    foreach(CommandItem c in CommandList)
                    {
                        if(!c.HotKey.IsNullOrEmpty())
                        {
                            var doAction = ProcessKeys(c.HotKey, e);
                            if(doAction)
                            {
                                c.DoAction();
                                e.Handled = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private bool ProcessKeys(string hotKey, System.Windows.Input.KeyEventArgs e=null, string pressedKey="")
        {
            var doAction = false;

            var dt = (int)((TimeSpan)(DateTime.Now - LastKeyboardEvent)).TotalMilliseconds;
            LastKeyboardEvent = DateTime.Now;

            if(e != null)
            {
                pressedKey = e.Key.ToString();
            }
            pressedKey = pressedKey.Trim();
            pressedKey = pressedKey.ToLower();

            var pressedCtrl = false;
            var pressedShift = false;           

            {
                if(
                    Keyboard.IsKeyDown(Key.LeftCtrl)
                    || Keyboard.IsKeyDown(Key.RightCtrl)
                )
                {
                    pressedCtrl = true;
                }

                if(
                    Keyboard.IsKeyDown(Key.LeftShift)
                    || Keyboard.IsKeyDown(Key.RightShift)
                )
                {
                    pressedShift = true;
                }
            }

            // Central.Dbg($"ProcessKeys pressedKey=[{pressedKey}] ctrl=[{pressedCtrl}] shift=[{pressedShift}]");

            hotKey = hotKey.Trim();
            hotKey = hotKey.ToLower();

            

            var keyList = new List<string>();
            if(hotKey.IndexOf("|") > -1)
            {
                keyList = hotKey.Split('|').ToList();
            }
            else
            {
                keyList.Add(hotKey);
            }

            foreach(string currentKey in keyList)
            {
                doAction = ProcessKey(currentKey, pressedKey, pressedCtrl, pressedShift);
                if(doAction)
                {
                    break;
                }
            }

            return doAction;
        }

        private bool ProcessKey(string currentKey, string pressedKey, bool pressedCtrl, bool pressedShift)
        {
            var doAction = false;
            var useCtrl = false;
            var useShift = false;
            var key = "";

            if(currentKey.IndexOf("ctrl") > -1)
            {
                useCtrl = true;
            }

            if(currentKey.IndexOf("shift") > -1)
            {
                useShift = true;
            }

            key = currentKey;
            key = key.Trim();
            key = key.ToLower();
            key = key.Replace("ctrl+", "");
            key = key.Replace("shift+", "");

            if(!key.IsNullOrEmpty())
            {
                if(useCtrl)
                {
                    if(
                        key == pressedKey
                        && pressedCtrl
                    )
                    {
                        doAction = true;
                    }
                }
                else if(useShift)
                {
                    if(
                        key == pressedKey
                        && pressedShift
                    )
                    {
                        doAction = true;
                    }
                }
                else
                {
                    if(key == pressedKey)
                    {
                        doAction = true;
                    }
                }
            }
            return doAction;
        }

        public void ProcessDoubleClick(string sourceName="")
        {
            if(DoubleClickProcessing)
            {
                var include = false;
                if(sourceName.IsNullOrEmpty())
                {
                    include = true;
                }

                foreach(CommandItem c in CommandList)
                {
                    if(c.MenuEnabled)
                    {
                        if(!c.HotKey.IsNullOrEmpty())
                        {
                            var doAction = ProcessKeys(c.HotKey, null, "DoubleCLick");
                            if(doAction)
                            {
                                include = true;
                            }

                            if(include)
                            {
                                if(!c.MenuGridName.IsNullOrEmpty())
                                {
                                    if(sourceName != c.MenuGridName)
                                    {
                                        include = false;
                                    }
                                }
                            }

                            if(include)
                            {
                                c.DoAction();
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// вызывается при изменении позиции выделения грида
        /// (OnSelectItem)
        /// </summary>
        /// <param name="selectedItem"></param>
        public void ProcessSelectItem(Dictionary<string, string> selectedItem)
        {
            UpdateActions();
            RenderButtons();
        }

        public void UpdateActions()
        {
            if(CommandList.Count > 0)
            {
                //сброс в дефолтное состояние
                foreach(CommandItem c in CommandList)
                {
                    c.Enabled = c.EnabledInitial;
                }

                //вычисление нового состояния
                foreach(CommandItem c in CommandList)
                {
                    c.DoCheck();
                }

                //обновление флагов состояний
                UpdateButtons();
                UpdateMenus();
            }
        }

        /// <summary>
        /// обработка команды
        /// </summary>
        /// <param name="name"></param>
        /// <param name="m"></param>
        public void Process(string name)
        {
            ProcessCommand(name);
        }
        /// <summary>
        /// обработка команды
        /// </summary>
        /// <param name="name"></param>
        /// <param name="m"></param>
        public void ProcessCommand(string name, ItemMessage m)
        {
            Message = m;
            ProcessCommand(name);
        }
        /// <summary>
        /// обработка команды
        /// </summary>
        /// <param name="name"></param>
        /// <param name="m"></param>
        public void ProcessCommand(string name)
        {
            name = name.ClearCommand();
            foreach(CommandItem c in CommandList)
            {
                if(c.Name == name)
                {
                    c.Message = Message;
                    c.DoAction();
                }
            }
        }

        /// <summary>
        /// Присваивает новое название пункту контестного меню
        /// </summary>
        /// <param name="name">имя элемента коммандера</param>
        /// <param name="title">новое название пункта контекстного меню</param>
        public void SetMenuTitle(string name, string title)
        {
            foreach (CommandItem c in CommandList)
            {
                if (c.Name == name)
                {
                    c.MenuTitle = title;
                }
            }
        }

        private void InitRoles()
        {

            var role = "";

            if(ControlBase!=null)
            {
                role = ControlBase.RoleName;
            }

            if(!role.IsNullOrEmpty())
            {
                role = role.Trim();
                role = role.ToLower();
                UserAccessMode = Central.Navigator.GetRoleLevel(role);
            }
        }

        private bool CheckAccess(CommandItem c)
        {
            var result = false;

            if(UserAccessMode >= c.AccessLevel)
            {
                result = true;
            }
            return result;
        }

        private void InitButtons()
        {
            foreach(CommandItem c in CommandList)
            {
                if(c.ButtonUse && CheckAccess(c))
                {
                    c.ButtonVisible = true;

                    if(c.Control.GetType() == typeof(Button))
                    {
                        var b = (System.Windows.Controls.Button)c.Control;
                        b.ToolTip = c.Description;
                        b.Click += (object sender, RoutedEventArgs e) =>
                        {
                            c.DoAction();
                        };
                    }
                    if(c.Control.GetType() == typeof(MenuItem))
                    {
                        var b = (System.Windows.Controls.MenuItem)c.Control;
                        b.ToolTip = c.Description;
                        b.Click += (object sender, RoutedEventArgs e) =>
                        {
                            c.DoAction();
                        };
                    }

                }
                else
                {
                    c.ButtonVisible = false;
                }
            }
        }

        private Button FindButtonControl(string name)
        {
            Button result = null;

            try
            {
                if(ControlBase != null)
                {
                    var o = ControlBase.FindName(name);
                    if(o != null)
                    {
                        var b = (Button)o;
                        if(b != null)
                        {
                            result = b;
                        }
                    }
                }
            }
            catch(Exception e)
            {
            }

            return result;
        }

        private void InitMenus()
        {
            foreach(CommandItem c in CommandList)
            {
                if(c.MenuUse && CheckAccess(c))
                {
                    c.MenuVisible = true;
                }
                else 
                {
                    c.MenuVisible = false;
                }
            }
        }

        private void UpdateButtons()
        {
            foreach(CommandItem c in CommandList)
            {
                if(c.ButtonVisible)
                {
                    if (c.ButtonControl != null)
                    {
                        if(c.Enabled)
                        {
                            c.ButtonEnabled = true;
                            c.ButtonControl.IsEnabled = true;
                        }
                        else
                        {
                            c.ButtonEnabled = false;
                            c.ButtonControl.IsEnabled = false;
                        }
                    }
                }
            }
        }

        private void UpdateMenus()
        {
            foreach(CommandItem c in CommandList)
            {
                if(c.MenuVisible)
                {
                    {
                        if(c.Enabled)
                        {
                            c.MenuEnabled = true;
                        }
                        else
                        {
                            c.MenuEnabled = false;
                        }
                    }
                }
            }
        }

        public void RenderButtons()
        {
            foreach(CommandItem c in CommandList)
            {
                if(c.ButtonControl != null)
                {
                    if(c.ButtonVisible)
                    {
                        c.ButtonControl.Visibility=Visibility.Visible;
                        if(c.ButtonEnabled)
                        {
                            c.ButtonControl.IsEnabled = true;
                        }
                        else
                        {
                            c.ButtonControl.IsEnabled = false;
                        }
                    }
                    else
                    {
                        c.ButtonControl.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private bool CheckMenuGridName(string menuGridName="", string controlGrigName="")
        {
            var result = false;
            var complete = false;

            menuGridName= menuGridName.Trim();
            controlGrigName= controlGrigName.Trim();

            if(!complete)
            {
                if(menuGridName.IsNullOrEmpty())
                {
                    result = true;
                    complete = true;
                }
            }

            if(!complete)
            {
                if(menuGridName.IndexOf("|") > -1)
                {
                    // OneGrid|TwoGrid
                    var list = menuGridName.Split('|').ToList();
                    foreach(string n in list)
                    {
                        if(controlGrigName == n)
                        {
                            result = true;
                            complete = true;
                            break;
                        }
                    }
                }
                else
                {
                    // OneGrid
                    if(controlGrigName == menuGridName)
                    {
                        result = true;
                        complete = true;
                    }
                }
            }

            return result;
        }

        public Dictionary<string, DataGridContextMenuItem> RenderMenu(string gridControlName="")
        {
            var result = new Dictionary<string, DataGridContextMenuItem>();
            var menuGroupOld = "";
            var j = 0;
            foreach(CommandItem c in CommandList)
            {
                var include = false;

                if(c.MenuVisible)
                {
                    include = true;
                }

                if(include)
                {
                    var r = CheckMenuGridName(c.MenuGridName, gridControlName);
                    if(gridControlName=="OrderDocumentGrid")
                    {
                        var rr = 0;
                    }
                    if(!r)
                    {
                        include = false;
                    }
                }

                //if(include)
                //{
                //    if(
                //        !c.MenuGridName.IsNullOrEmpty()
                //        && !gridControlName.IsNullOrEmpty()
                //    )
                //    {
                //        if(c.MenuGridName != gridControlName)
                //        {
                //            include = false;
                //        }
                //    }
                //}

                if(include)
                {
                    if(!menuGroupOld.IsNullOrEmpty())
                    {
                        if(menuGroupOld != c.Group)
                        {
                            j++;
                            var k = $"separator-{j}";
                            var mi = new DataGridContextMenuItem()
                            {
                                Header = "-",
                            };
                            result.Add(k, mi);
                        }
                    }

                    {
                        var mi = new DataGridContextMenuItem()
                        {
                            Header = c.MenuTitle,
                            Enabled = c.Enabled,
                            ToolTip = c.Description,
                            GroupHeader = c.MenuGroupHeader,
                            GroupHeaderName = c.MenuGroupHeaderName,
                            Action = () =>
                            {
                                c.DoAction();
                            },
                        };
                        result.Add(c.Name, mi);
                    }

                    menuGroupOld = c.Group;
                }

            }
            return result;
        }              

        private CommandItem FindCommandByName(string name)
        {
            CommandItem result = null;
            if(CommandList.Count > 0)
            {
                foreach(CommandItem c in CommandList)
                {
                    if(c.Name == name)
                    {
                        result = c;
                        break;
                    }
                }
            }
            return result;
        }

        public void AddGrid(GridBox4 grid)
        {
            var k = grid.Name;
            if(!Grids.ContainsKey(k))
            {
                Grids.Add(k, grid);
            }
            LastGrid = grid;
        }

        private bool GridContextMenuDo { get; set; }
        private string GridContextMenuName { get; set; }
        public void SetGridContextMenu(string name)
        {
            GridContextMenuDo = true;
            GridContextMenuName= name;
        }

        public void DoGridContextMenu(string name)
        {
            Central.Dbg($"DoGridContextMenu {name}");
            if(GridContextMenuDo)
            {
                if(name == LastGrid.Name)
                {
                    foreach(var item in Grids)
                    {
                        var g = item.Value;
                        if(g.Name == name)
                        {
                            g.CellMenuShow();
                        }
                    }
                    GridContextMenuDo = false;
                    
                }
            }
        }
    }

    public class CommandItem
    {
        public CommandItem() 
        {
            Name="";
            Title="";
            Description = "";
            Group = "";

            Enabled = false;
            EnabledInitial = false;
            Action = null;
            ActionMessage = null;
            CheckEnabled = null;
            CheckVisible = null;

            ButtonUse = false;
            ButtonControl = null;
            Control = null;
            ButtonTitle = "";
            ButtonName = "";

            MenuUse = false;
            MenuTitle = "";
            MenuPosition = 0;
            MenuGridName = "";
            MenuGroupHeader = "";
            MenuGroupHeaderName = "";

            ButtonVisible = false;
            ButtonEnabled = false;
            MenuVisible = false;
            MenuEnabled = false;

            HotKey = "";

            AccessLevel= Role.AccessMode.None;
            Default = false;
            Message = new ItemMessage();
        }

        /// <summary>
        /// имя команды, уникальный идентификатор
        /// snake case: task_create
        /// refresh, view, insert, update, delete
        /// export, help
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// заголовок объекта
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// примечание
        /// будет отображаться в блоке всплывающей подсказки (tooltip)
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// группа команды
        /// snake case: task_create
        /// </summary>
        public string Group { get; set; }
        /// <summary>
        /// активность по умолчанию
        /// (активность вычисляется в коллбэке Check)
        /// </summary>
        public bool Enabled { get; set; }
        public bool EnabledInitial { get; set; }

        public delegate void ActionDelegate();
        /// <summary>
        /// действие
        /// будет выполнено при активации команды
        /// (т.е. при клике на объект)
        /// </summary>
        public ActionDelegate Action;

        public delegate void ActionMessageDelegate(ItemMessage message);
        /// <summary>
        /// действие
        /// будет выполнено при активации команды
        /// (т.е. при клике на объект)
        /// </summary>
        public ActionMessageDelegate ActionMessage;

        public delegate bool CheckEnabledDelegate();
        /// <summary>
        /// проверка активности команды
        /// нужно вернуть: true|false
        /// команда может быть выполнена, если она активна
        /// </summary>
        public CheckEnabledDelegate CheckEnabled;

        public delegate bool CheckVisibleDelegate();

        /// <summary>
        /// Проверка видимости кнопки в выпадающем меню 
        /// нужно вернуть true|false
        /// </summary>
        public CheckVisibleDelegate CheckVisible;

        public delegate bool CheckMenuVisibleDelegate();

        /// <summary>
        /// Проверка видимости кнопки в выпадающем меню 
        /// нужно вернуть true|false
        /// </summary>
        public CheckMenuVisibleDelegate CheckMenuVisible;

        /// <summary>
        /// генерация объектов для тулбара
        /// </summary>
        public bool ButtonUse { get; set; }
        /// <summary>
        /// контрол кнопки
        /// </summary>
        public Button ButtonControl { get; set; }
        public Control Control { get; set; }
        public string ButtonTitle { get; set; }
        public string ButtonName { get; set; }

        /// <summary>
        /// генерация объектов для меню
        /// </summary>
        public bool MenuUse { get; set; }
        public string MenuTitle { get; set; }
        public int MenuPosition { get; set; }
        /// <summary>
        /// имя грида, к которому относится команда
        /// (ссылка на команду будет отображена в контекстном меню этого грида)
        /// </summary>
        public string MenuGridName { get; set; }
        public string MenuGroupHeader { get; set; }
        public string MenuGroupHeaderName { get; set; }

        public bool ButtonVisible { get; set; }
        public bool MenuButtonVisible { get; set; }
        public bool ButtonEnabled { get; set; }

        public bool MenuVisible { get; set; }
        public bool MenuEnabled { get; set; }
        /// <summary>
        /// команда по умолчанию
        /// эта команда будет вызывтьася по событиям: Return|DoubleCLick
        /// </summary>
        public bool Default { get; set; }

        /// <summary>
        /// горячая клавиша
        /// Insert
        /// Return
        /// Escape
        /// Ctrl+Insert
        /// Shift+Insert
        /// DoubleCLick
        /// Return|DoubleCLick
        /// </summary>
        public string HotKey { get; set; }

        /// <summary>
        /// уровень доступа
        /// минимальный уровень доступа, которым должен обладать пользователь,
        /// чтобы запустить команду
        /// </summary>
        public Role.AccessMode AccessLevel { get; set; }

        public ItemMessage Message { get; set; }

        public void DoAction()
        {
            if(Enabled)
            {
                if(Action!=null)
                {
                    Action.Invoke();
                }
                else
                {
                    if(ActionMessage != null)
                    {
                        ActionMessage.Invoke(Message);
                    }
                }
            }
        }

        public void DoCheck()
        {
            {
                if(CheckEnabled != null)
                {
                    Enabled = CheckEnabled.Invoke();                    
                }

                if (CheckVisible != null)
                {
                    ButtonVisible = CheckVisible.Invoke();
                }

                if (CheckMenuVisible != null)
                {
                    MenuVisible = CheckMenuVisible.Invoke();
                }
            }
        }
    }


    /// <summary>
    /// структура состояния подсистемы ввода
    /// (клавиатура, сканнер штрихкодов)
    /// </summary>
    public class InputController
    {
        public InputController() 
        {
            KeyPressed = "";
            WordScanned = "";
            ScanningInProgress = false;
            KeyPressedCtrl = false;
            KeyPressedShift = false;
        }

        /// <summary>
        /// lower
        /// </summary>
        public string KeyPressed { get; set; }
        public string WordScanned { get; set; }
        public bool ScanningInProgress { get; set; }
        public bool KeyPressedCtrl { get; set; }
        public bool KeyPressedShift { get; set; }


        public void Catch(System.Windows.Input.KeyEventArgs e)
        {
            if(e != null)
            {
                KeyPressed = e.Key.ToString();
            }
            KeyPressed = KeyPressed.Trim();
            KeyPressed = KeyPressed.ToLower();

            {
                if(
                    Keyboard.IsKeyDown(Key.LeftCtrl)
                    || Keyboard.IsKeyDown(Key.RightCtrl)
                )
                {
                    KeyPressedCtrl = true;
                }

                if(
                    Keyboard.IsKeyDown(Key.LeftShift)
                    || Keyboard.IsKeyDown(Key.RightShift)
                )
                {
                    KeyPressedShift = true;
                }
            }

            WordScanned = Central.WM.GetScannerInput();
            ScanningInProgress=Central.WM.ScanningInProgress();

            {
                if(!WordScanned.IsNullOrEmpty())
                {
                    Central.Dbg($"INPUT:CATCH: scan=[{WordScanned}]");                    
                }
                else
                {
                    Central.Dbg($"INPUT:CATCH: pressedKey=[{KeyPressed}] ctrl=[{KeyPressedCtrl}] shift=[{KeyPressedShift}]");
                }
            }
        }
    }
}
