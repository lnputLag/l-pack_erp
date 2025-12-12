using Client.Assets.Converters;
using Client.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using static Client.Common.FormHelperField;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Main
{
    /*
        <lpctl:SelectBox  
            Name="MySelector" 
            Autocomplete="True" 
            ListResizeable="true"
            Items="{Binding ElementName = VM, Path = Customers, Mode = OneWay, UpdateSourceTrigger = PropertyChanged}" 
            SelectedItem="{Binding ElementName = VM, Path = Customer, Mode = TwoWay, UpdateSourceTrigger = PropertyChanged, ValidatesOnDataErrors=True}" 
            />
      
     
        Селектбокс с автокомплитом.
        Если автокомплит включен (Autocomplete="True"), то позволяет в поле ввода вводить текстовое значение.
        При начале ввода значения, последовательно начинает фильтровать список значений в списке выбора,
        оставляя только те, что соответствуют введенному.
        Соответствие проверяется в нескольких режимах:
            CompareMode="StartsWith" -- остаются значения, начинающиеся на введенную фразу (default)
            CompareMode="Contains"   -- остаются значения, содержащие введенную фразу

        ListResizeable="true|false" -- дает возможность сделать контейнер со списком значений изменяемого размера (true)
        (если да, можно тянуть контенйер за правый нижний уголок)


        Механика реализации
        если свойство Autocomplete установлено, то 
        по клику на поле ввода включаем режим с автодополнением (AutocompleteMode)
            - запоминаем выбранное до этого значение
            - очищаем поле ввода
            - при вводе текста в поле делаем подстановку:
                если введено более 1 символа, фильтруем набор данных в списке выбора
                загружаем в список выбора фильтрованный список
            - при выборе значения из списка:
                подставляем его в поле ввода, очищаем фильтрованный список, снимаем флажок (AutocompleteMode)
        иначе селектбокс ведет себя как простой список выбора
     */


    public partial class SelectBox:UserControl, IDataErrorInfo
    {

        public SelectBox()
        {
            InitializeComponent();


            if(Central.InDesignMode()) return;

            DataContext = this;

            ValueTextBox.Text = "";

            ValueTextBox.PreviewMouseDown += ValueTextBox_PreviewMouseDown;
            ValueTextBox.MouseUp += ValueTextBox_MouseUp;
            ValueTextBox.KeyUp += ValueTextBox_KeyUp;

            ShowDropdownButton.Click += ShowDropdownButton_Click;

            DropDownListBox.SelectionChanged += DropDownListBox_SelectionChanged;
            DropDownBlock.Closed += DropDownBlock_Closed;
            DropDownGridBox.SelectionChanged+=DropDownGridBox_SelectionChanged;

            Items = new Dictionary<string,string>();
            SelectedItem = new KeyValuePair<string,string>();
            SelectedItemOld=new KeyValuePair<string, string>();
            SelectedRow = new Dictionary<string, string>();

            DebugMode = false;
            AutocompleteMode = false;

            //SetMode(ReadOnlyMode);

            ListBoxMinHeight = 0;
            ListBoxMinWidth = 0;
            IsEnabled=true;
            IsReadOnly=false;
            DataType=DataTypeRef.Default;
            GridColumns=new List<DataGridHelperColumn>();
            SelectedItemValue="";
            GridDataSet=new ListDataSet();
            ColIndexes=new Dictionary<string,int>();
            GridSelectedItemFormat="";
            OnSelectItem=OnSelectItemAction;

        }

        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
            "Items",
            typeof(Dictionary<string,string>),
            typeof(SelectBox),
            new FrameworkPropertyMetadata(default(Dictionary<string,string>),OnItemsChanged
            )
        );

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            "SelectedItem",
            typeof(KeyValuePair<string,string>),
            typeof(SelectBox),
            new FrameworkPropertyMetadata(
                default(KeyValuePair<string,string>),
                //OnSelectedItemChanged
                new PropertyChangedCallback(OnSelectedItemChanged)
            )
        );

        public static readonly DependencyProperty AutocompleteProperty = DependencyProperty.Register(
            "Autocomplete",
            typeof(bool),
            typeof(SelectBox),
            new FrameworkPropertyMetadata(false)
        );

        public static readonly DependencyProperty ListResizeableProperty = DependencyProperty.Register(
            "ListResizeable",
            typeof(bool),
            typeof(SelectBox),
            new FrameworkPropertyMetadata(true)
        );

        public static readonly DependencyProperty CompareModeProperty = DependencyProperty.Register(
             "CompareMode",
             typeof(CompareModeRef),
             typeof(SelectBox),
             new FrameworkPropertyMetadata(CompareModeRef.Default)
        );

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
            "IsReadOnly",
            typeof(bool),
            typeof(SelectBox),
            new FrameworkPropertyMetadata(false,OnReadOnlyChanged)
        );

        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(
            "IsEnabled",
            typeof(bool),
            typeof(SelectBox),
            new FrameworkPropertyMetadata(false,OnEnabledChanged)
        );

        public static readonly DependencyProperty DataTypeProperty = DependencyProperty.Register(
            "DataType",
            typeof(DataTypeRef),
            typeof(SelectBox),
            new FrameworkPropertyMetadata(DataTypeRef.Default,OnDataTypeChanged)
        );

        public static readonly DependencyProperty GridColumnsProperty = DependencyProperty.Register(
            "GridColumns",
            typeof(List<DataGridHelperColumn>),
            typeof(SelectBox),
            new FrameworkPropertyMetadata(new List<DataGridHelperColumn>(),OnGridColumnsChanged)
        );

        


        public static readonly DependencyProperty GridDataSetProperty = DependencyProperty.Register(
            "GridDataSet",
            typeof(ListDataSet),
            typeof(SelectBox),
            new FrameworkPropertyMetadata(new ListDataSet(),OnGridDataSetChanged)
        );

        public event PropertyChangedCallback SelectedItemChanged;

        private void RaiseValueChangedEvent(DependencyPropertyChangedEventArgs e)
        {
            SelectedItemChanged?.Invoke(this,e);
        }


        //отладочный флажок
        private bool DebugMode { get; set; }

        //флажок режима "автокомплит"
        private bool AutocompleteMode { get; set; }

        //количество введенных символов с которых начинает работать автокомплит
        private static int AutocompleteMinLen { get; } = 1;

        //номинальная высота (начальная) блока списка выбора
        //private static int ListBoxNominalHeight { get; } = 120;

        // path-index (zero based)
        private Dictionary<string,int> ColIndexes { get; set; }

        /// <summary>
        /// шаблон текстового значения
        /// </summary>
        public string SelectedItemValue { get; set; }


        /// <summary>
        /// формула для вычисления значения выбранного поля
        /// имена полей через запятую, значения которых будут взяты, чтобы сформировать 
        /// (главное поле или несколько полей)
        /// строчку с выбранным пунктом
        /// </summary>
        public string GridSelectedItemFormat { get; set;}
        public delegate bool OnSelectItemDelegate(Dictionary<string,string> selectedItem);
        public OnSelectItemDelegate OnSelectItem;
        public virtual bool OnSelectItemAction(Dictionary<string,string> selectedItem)
        {
            return true;
        }

        public FormHelperField FieldControl { get; set; }=null;
        public delegate bool OnSelectItemCompleteDelegate(FormHelperField f, Dictionary<string,string> selectedItem);
        /// <summary>
        /// Вызывается после установки значения в SelectedItem
        /// </summary>
        public OnSelectItemCompleteDelegate OnSelectItemComplete=null;

        public static readonly DependencyProperty ListBoxMinHeightProperty = DependencyProperty.Register(
            "ListBoxMinHeight",
            typeof(int),
            typeof(SelectBox),
            new FrameworkPropertyMetadata(0,ListBoxMinHeightChanged)
        );

        private static void ListBoxMinHeightChanged(DependencyObject depObj,DependencyPropertyChangedEventArgs args)
        {
            SelectBox s = (SelectBox)depObj;

            if(args.NewValue != null)
            {
                s.ListBoxMinHeight = (int)args.NewValue;
            }
        }

        public static readonly DependencyProperty ListBoxMinWidthProperty = DependencyProperty.Register(
            "ListBoxMinWidth",
            typeof(int),
            typeof(SelectBox),
            new FrameworkPropertyMetadata(0,ListBoxMinWidthChanged)
        );

        private static void ListBoxMinWidthChanged(DependencyObject depObj,DependencyPropertyChangedEventArgs args)
        {
            var s = (SelectBox)depObj;

            if(args.NewValue != null)
            {
                s.ListBoxMinWidth = (int)args.NewValue;
            }
        }


        public enum CompareModeRef
        {
            Default = 0,
            StartsWith = 1,
            Contains = 2,
            ContainsInner = 3,
        }

        public enum DataTypeRef
        {
            Default = 0,
            List = 1,
            Grid = 2,
        }

        public Dictionary<string,string> Items
        {
            get => (Dictionary<string,string>)GetValue(ItemsProperty);
            set
            {
                UpdateListItems(value);
                SetValue(ItemsProperty,value);
            }
        }

        public KeyValuePair<string,string> SelectedItemOld { get;set;}
        public KeyValuePair<string,string> SelectedItem
        {
            get => (KeyValuePair<string,string>)GetValue(SelectedItemProperty);
            set
            {
                UpdateSelectedItem(value);
                SetValue(SelectedItemProperty,value);
            }
        }

        public int ListBoxMinHeight
        {
            get => (int)GetValue(ListBoxMinHeightProperty);
            set
            {
                SetValue(ListBoxMinHeightProperty,value);
                UpdateDropDownSize();
            }
        }

        public int ListBoxMinWidth
        {
            get => (int)GetValue(ListBoxMinWidthProperty);
            set
            {
                SetValue(ListBoxMinWidthProperty,value);
                UpdateDropDownSize();
            }
        }

        public bool Autocomplete
        {
            get => (bool)GetValue(AutocompleteProperty);
            set => SetValue(AutocompleteProperty,value);
        }

        public bool ListResizeable
        {
            get => (bool)GetValue(ListResizeableProperty);
            set => SetValue(ListResizeableProperty,value);
        }


        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set
            {
                SetValue(IsReadOnlyProperty,value);
                CheckMode();
            }
        }

        public bool IsEnabled
        {
            get => (bool)GetValue(IsEnabledProperty);
            set
            {
                SetValue(IsEnabledProperty,value);
                CheckMode();
            }
        }

        public DataTypeRef DataType
        {
            get => (DataTypeRef)GetValue(DataTypeProperty);
            set
            {
                SetValue(DataTypeProperty,value);
                CheckMode();
            }
        }

        public List<DataGridHelperColumn> GridColumns
        {
            get => (List<DataGridHelperColumn>)GetValue(GridColumnsProperty);
            set
            {
                SetValue(GridColumnsProperty,value);
                CheckMode();
                UpdateGridColumns();
            }
        }

      

        public ListDataSet GridDataSet
        {
            get => (ListDataSet)GetValue(GridDataSetProperty);
            set
            {
                UpdateGridItems(value);
                SetValue(GridDataSetProperty,value);
            }
        }

        public CompareModeRef CompareMode
        {
            get => (CompareModeRef)GetValue(CompareModeProperty);
            set => SetValue(CompareModeProperty,value);
        }

        public string Error => throw new NotImplementedException();

        public string this[string columnName]
        {
            get
            {
                string result = null;
                //result = "validation error";
                return result;
            }
        }


        private static void OnItemsChanged(DependencyObject depObj,DependencyPropertyChangedEventArgs args)
        {
            SelectBox s = (SelectBox)depObj;

            if(args.NewValue != null)
            {
                s.Items = (Dictionary<string,string>)args.NewValue;
            }
        }

        private static void OnSelectedItemChanged(DependencyObject depObj,DependencyPropertyChangedEventArgs args)
        {
            SelectBox s = (SelectBox)depObj;

            if(args.NewValue != null)
            {
                s.SelectedItem = (KeyValuePair<string,string>)args.NewValue;
                s.RaiseValueChangedEvent(args);

            }
        }

        private static void OnReadOnlyChanged(DependencyObject depObj,DependencyPropertyChangedEventArgs args)
        {
            SelectBox s = (SelectBox)depObj;

            if(args.NewValue != null)
            {
                s.IsReadOnly = (bool)args.NewValue;
            }
        }


        private static void OnEnabledChanged(DependencyObject depObj,DependencyPropertyChangedEventArgs args)
        {
            SelectBox s = (SelectBox)depObj;

            if(args.NewValue != null)
            {
                s.IsEnabled = (bool)args.NewValue;
            }
        }

        private static void OnDataTypeChanged(DependencyObject depObj,DependencyPropertyChangedEventArgs args)
        {
            SelectBox s = (SelectBox)depObj;

            if(args.NewValue != null)
            {
                s.DataType = (DataTypeRef)args.NewValue;
            }
        }

        private static void OnGridColumnsChanged(DependencyObject depObj,DependencyPropertyChangedEventArgs args)
        {
            SelectBox s = (SelectBox)depObj;

            if(args.NewValue != null)
            {
                s.GridColumns = (List<DataGridHelperColumn>)args.NewValue;
            }
        }

      

        private static void OnGridDataSetChanged(DependencyObject depObj,DependencyPropertyChangedEventArgs args)
        {
            SelectBox s = (SelectBox)depObj;

            if(args.NewValue != null)
            {
                s.GridDataSet = (ListDataSet)args.NewValue;
            }
        }

       

        private KeyValuePair<string,string> FindItem(string value = "")
        {
            var result = default(KeyValuePair<string,string>);
            if(!string.IsNullOrEmpty(value))
            {
                if(Items != null && Items.Count > 0)
                {
                    foreach(var i in Items)
                    {
                        if(i.Value == value)
                        {
                            result = i;
                        }
                    }
                }
            }
            return result;
        }

        private ListBoxItem FindListBoxItem(string value = "")
        {
            var result = new ListBoxItem();
            if(!string.IsNullOrEmpty(value))
            {
                if(DropDownListBox.Items.Count > 0)
                {
                    foreach(ListBoxItem i in DropDownListBox.Items)
                    {
                        if(i.Content.ToString() == value)
                        {
                            result = i;
                        }
                    }
                }
            }
            return result;
        }
        
        private object FindGridBoxItem(string value = "")
        {
            var result = new object();
            if(!string.IsNullOrEmpty(value))
            {
                if(DropDownGridBox.Items.Count > 0)
                {
                    foreach (var i in DropDownGridBox.ItemsSource)
                    {
                        var selectedItem = (Dictionary<string, string>)i;
                        foreach (var j in selectedItem)
                        {
                            if (j.Key == "ID" && j.Value==value)
                            {
                                result = i;
                                DropDownGridBox.ScrollIntoView(selectedItem);

                            }
                        }
                        
                    }
                }
            }
            return result;
        }


        private void ValueTextBox_PreviewMouseDown(object sender,MouseButtonEventArgs e)
        {
            e.Handled = true;
            ShowDropDown();
        }

        private void ValueTextBox_MouseUp(object sender,MouseButtonEventArgs e)
        {
            e.Handled = true;
            AfterShowDropDown();
        }

        private void ShowDropdownButton_Click(object sender,RoutedEventArgs e)
        {
            e.Handled = true;
            ShowDropDown();
            AfterShowDropDown();
        }

        public void Show()
        {
            ShowDropDown();
            AfterShowDropDown();
        }

        private void ValueTextBox_KeyUp(object sender,KeyEventArgs e)
        {
            //ShowDropDown();
            switch (DataType)
            {
                case DataTypeRef.List:
                    if (Items != null && Items.Count > 0)
                    {
                        if (AutocompleteMode)
                        {
                            SelectedItem = new KeyValuePair<string, string>();

                            string v = ValueTextBox.Text;
                            v = v.Trim();
                            if (v.Length >= AutocompleteMinLen)
                            {
                                var culture = System.Globalization.CultureInfo.InvariantCulture;
                                //var filteredItems = Items.Where(p => p.Value.StartsWith(v, true, culture)).ToDictionary(p => p.Key, p => p.Value);
                                var filteredItems = new Dictionary<string, string>();

                                switch (CompareMode)
                                {
                                    case CompareModeRef.Default:
                                    case CompareModeRef.StartsWith:
                                        filteredItems = Items.Where(p => p.Value.StartsWith(v, true, culture)).ToDictionary(p => p.Key, p => p.Value);
                                        break;

                                    case CompareModeRef.Contains:
                                        filteredItems = Items.Where(p => p.Value != null && p.Value.ToLower().Contains(v.ToLower())).ToDictionary(p => p.Key, p => p.Value);
                                        break;

                                    case CompareModeRef.ContainsInner:
                                        var items = new Dictionary<string, string>();
                                        foreach (KeyValuePair<string, string> i in Items)
                                        {
                                            var include = false;

                                            var v2 = i.Value;
                                            v2 = v2.ToLower();

                                            if (v2.IndexOf(v) > -1)
                                            {
                                                include = true;
                                            }

                                            if (include)
                                            {
                                                items.Add(i.Key, i.Value);
                                            }
                                        }
                                        filteredItems = items;
                                        break;
                                }

                                if (filteredItems.Count > 0)
                                {
                                    UpdateListItems(filteredItems);
                                }
                                else
                                {
                                    DropDownListBox.Items.Clear();
                                }

                            }
                            else
                            {
                                UpdateListItems(Items);
                            }
                        }
                    }
                    break;

                case DataTypeRef.Grid:
                    if (AutocompleteMode)
                    {
                        if (ValueTextBox.Text.IsNullOrEmpty())
                        {
                            DropDownGridBox.SelectedItem = null;
                            SelectedItem = new KeyValuePair<string, string>();
                        }

                        //Сделать автокомплит для грида
                    }
                    break;
            }
        }

        private void ShowDropDown()
        {
            if(IsReadOnly || !IsEnabled)
            {
                return;
            }

            //ресайзинг
            DropDownThumb.Visibility = ListResizeable ? Visibility.Visible : Visibility.Collapsed;


            // В режиме автокомплита по умолчанию полный список
            if(Autocomplete)
            {
                UpdateListItems(Items);
            }

            //подкрутка
            switch (DataType)
            {
                case DataTypeRef.List:
                    {
                        DropDownListBox.SelectedItem = FindListBoxItem(SelectedItem.Value);
                        DropDownListBox.ScrollIntoView(DropDownListBox.SelectedItem);
                    }
                    break;

                case DataTypeRef.Grid:
                    {
                        DropDownGridBox.SelectedItem = FindGridBoxItem(SelectedItem.Key);
                    }
                    break;
            }
            

            SetDropDownSize(ListBoxMinWidth,ListBoxMinHeight);

            //появляе
            DropDownBlock.StaysOpen = true;
            DropDownBlock.IsOpen = true;
        }

        private void AfterShowDropDown()
        {
            if(IsReadOnly || !IsEnabled)
            {
                return;
            }

            if(Autocomplete)
            {
                AutocompleteMode = true;
                ValueTextBox.SelectAll();
                ValueTextBox.Focusable = true;
                ValueTextBox.Focus();

            }

            //режим автосокрытия popup при потере фокуса
            if(!DebugMode)
            {
                DropDownBlock.StaysOpen = false;
            }
        }

        private void UpdateDropDownSize()
        {
            SetDropDownSize(ListBoxMinWidth,ListBoxMinHeight);
        }

        private void SetDropDownSize(int w,int h)
        {
            //минимальная ширина -- оптимальная ширина
            int minWidth = GetNominalWidth();
            //if (w < minWidth || double.IsNaN(DropDownBlock.Width))
            if(w < minWidth)
            {
                w=minWidth;
            }

            if (h == 0)
            {
                h = 120;
            }

            int minHeight = 50;
            //if (h < minHeight || double.IsNaN(DropDownBlock.Height))
            if(h < minHeight)
            {
                h=minHeight;
            }

            Central.Dbg($"SelectBox DropDown size: w={w} h={h} mode=[{DataType.ToString()}]");

            DropDownBlock.Width=w;
            DropDownBlock.Height=h;

            DropDownListBox.Width=w;
            DropDownListBox.Height=h;

            DropDownGridBox.Width=w;
            DropDownGridBox.Height=h;

        }

        private int GetNominalWidth()
        {
            //номинальная ширина контейнера списка -- это поле ввода + ширина кнопки [V]
            var result = (int)ValueTextBox.ActualWidth + 25;
            return result;
        }

        private int GetNominalHeight()
        {
            //номинальная высота контейнера списка
            int result = ListBoxMinHeight;
            return result;
        }

        private void HideDropDown()
        {
            DropDownBlock.StaysOpen = true;
            DropDownBlock.IsOpen = false;
        }

        private void DropDownBlock_Closed(object sender,EventArgs e)
        {

            if(AutocompleteMode)
            {
                ValueTextBox.Text = SelectedItem.Value;
            }

            /*
            switch(DataType)
            {
                case DataTypeRef.List:
                    { 
                        if (AutocompleteMode)
                        {
                            ValueTextBox.Text = SelectedItem.Value;
                        }
                    }
                    break;

                case DataTypeRef.Grid:
                    { 
                        
                    }
                    break;
            }
            */


        }

        public void UpdateListItems(Dictionary<string,string> items)
        {
            if(items != null && items.Count > 0)
            {
                //очистим список элементов для выбора
                //и сформируем новый список из полученных значений

                if(DataType==DataType)
                {
                    DropDownListBox.Items.Clear();
                    foreach(var i in items)
                    {
                        DropDownListBox.Items.Add(new ListBoxItem
                        {
                            Content = $"{i.Value}",
                            Style = (Style)DropDownListBox.FindResource("DropDownListItem"),
                        });
                    }
                }

                ClearSelectedItem(SelectedItem);

            }

            CheckMode();
        }

        public void UpdateSelectedItem(KeyValuePair<string,string> item)
        {
            if(Items != null && Items.Count > 0)
            {
                if(!string.IsNullOrEmpty(item.Key))
                {
                    if(Items.ContainsKey(item.Key))
                    {
                        DropDownListBox.SelectedItem = Items[item.Key];
                        ValueTextBox.Text = Items[item.Key];
                    }
                    else
                    {
                        DropDownListBox.SelectedItem = null;
                        ValueTextBox.Text = "";
                    }
                }
            }
        }

        /// <summary>
        /// Очищаем наполнение селектбокса
        /// </summary>
        public void Clear()
        {
            DropDownListBox.Items.Clear();
            DropDownListBox.SelectedItem = null;
            ValueTextBox.Text = "";
            Items = new Dictionary<string, string>();
            SelectedItem = new KeyValuePair<string, string>();
        }

        public void ClearSelectedItem(KeyValuePair<string,string> item)
        {
            if(Items != null && Items.Count > 0)
            {
                if(!string.IsNullOrEmpty(item.Key))
                {
                    if(Items.ContainsKey(item.Key))
                    {

                    }
                    else
                    {
                        // в режиме RO нам неважно, есть ли значение в списке
                        if(!IsReadOnly)
                        {
                            DropDownListBox.SelectedItem = null;
                            ValueTextBox.Text = "";
                        }
                    }
                }
            }
        }


        public void SetValuePrimary(Dictionary<string, string> row)
        {
            if(!GridPrimaryKey.IsNullOrEmpty())
            {
                var v=row.CheckGet(GridPrimaryKey);
                v = v.ToInt().ToString();
                SetValue(v, GridPrimaryKey);
            }
        }

        // ID
        public void SetValue(object v,string key = "ID")
        {
            var itemSelected = false;

            var value=v.ToString();

            switch(DataType)
            {
                case DataTypeRef.List:
                {

                }
                break;

                case DataTypeRef.Grid:
                {
                    if(GridDataSet != null)
                    {
                        if(GridDataSet.Items.Count>0)
                        {
                            foreach(Dictionary<string,string> row in GridDataSet.Items)
                            {
                                if(!itemSelected)
                                {
                                    if(row.ContainsKey(key))
                                    {
                                        //FIXME
                                        if(row[key].ToInt().ToString()==value)
                                        {
                                            itemSelected=true;
                                            var itemValue="";
                                            var itemKey=row[key];

                                            if(ColIndexes.Count>0)
                                            {
                                                foreach(KeyValuePair<string,int> c in ColIndexes)
                                                {
                                                    var k=c.Key;
                                                    var col=FindColumnByName(k);

                                                    var include=false;
                                                    if(!string.IsNullOrEmpty(GridSelectedItemFormat))
                                                    {
                                                        if(GridSelectedItemFormat.IndexOf(c.Key)>-1)
                                                        {
                                                            if (col.Enabled && !col.Hidden)
                                                            {
                                                                include=true;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        include=true;
                                                    }

                                                    

                                                    if(
                                                        col!=null 
                                                        && !string.IsNullOrEmpty(col.Name) 
                                                        && row.ContainsKey(k)
                                                        && include
                                                    )
                                                    {
                                                        var a="";

                                                        switch(col.ColumnType)
                                                        {
                                                            case DataGridHelperColumn.ColumnTypeRef.Integer:
                                                                a=row[k].ToInt().ToString();
                                                                break;
                                                            case DataGridHelperColumn.ColumnTypeRef.Double:
                                                                a=row[k].ToDouble().ToString();
                                                                break;
                                                            default:
                                                                a=row[k];
                                                                break;
                                                        }

                                                        itemValue=$"{itemValue} {a}";
                                                    }

                                                }
                                            }
                                            ValueTextBox.Text = itemValue;
                                            SelectedItem=new KeyValuePair<string,string>(itemKey,itemValue);


                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                break;
            }
            
        }

        public string GetValue(string key = "ID")
        {
            var result = "";
            result=SelectedItem.Key;
            return result;
        }

        // обслуживание ресайза контейнера
        private void OnDragStarted(object sender,DragStartedEventArgs e)
        {
            if(ListResizeable)
            {
                var t = (Thumb)sender;
                t.Cursor = Cursors.Hand;
            }
        }

        private void OnDragDelta(object sender,DragDeltaEventArgs e)
        {
            if(ListResizeable)
            {
                //int minWidth = GetNominalWidth();
                //int minHeight = 120;

                int verticalChange = 0;
                int horizontalChange = 0;
                int height = (int)DropDownBlock.Height;
                int width = (int)DropDownBlock.Width;

                horizontalChange = (int)e.HorizontalChange;
                width += horizontalChange;

                verticalChange = (int)e.VerticalChange;
                height += verticalChange;


                SetDropDownSize(width,height);
            }
        }

        private void OnDragCompleted(object sender,DragCompletedEventArgs e)
        {
            if(ListResizeable)
            {
                var t = (Thumb)sender;
                t.Cursor = null;
            }
        }

        public void CheckMode()
        {
            ValueTextBox.IsEnabled=IsEnabled;
            ValueTextBox.IsReadOnly=IsReadOnly;

            if(DataType==DataTypeRef.Default)
            {
                DataType=DataTypeRef.List;
            }


            switch(DataType)
            {
                case DataTypeRef.List:
                {
                    DropDownListBox.Visibility=Visibility.Visible;
                    DropDownGridBox.Visibility=Visibility.Collapsed;
                }
                break;

                case DataTypeRef.Grid:
                {
                    DropDownGridBox.Visibility=Visibility.Visible;
                    DropDownListBox.Visibility=Visibility.Collapsed;
                }
                break;
            }
        }

        public void UpdateGridColumns()
        {
            var resume = true;

            //инициализируемся лишь однажды
            if(ColIndexes!=null)
            {
                if(ColIndexes.Count>0)
                {
                    resume=false;
                }
            }


            if(resume)
            {
                if(DropDownGridBox!=null)
                {

                    var Grid = DropDownGridBox;
                    var Columns = GridColumns;

                    /*
                        ячейкам будут программно назначены стили:
                        (в зависимости от типа данных)
                            DataGridColumn
                            DataGridColumnDigit
                            DataGridColumnBool
                     */
                    var styles=new Dictionary<string,string>();

                    {
                        Grid.Style=(Style)Grid.TryFindResource("DataGridMain");

                        styles.CheckAdd("base",      "DataGridColumn");
                        styles.CheckAdd("digit",     "DataGridColumnDigit");
                        styles.CheckAdd("bool",      "DataGridColumnBool");
                        styles.CheckAdd("selectbox", "DataGridColumnSelectBox");
                    }


                    if(Grid.Columns.Count>0)
                    {
                        Grid.Columns.Clear();
                    }

                    if(ColIndexes!=null)
                    {
                        if(ColIndexes.Count>0)
                        {
                            ColIndexes.Clear();
                        }
                    }


                    if(Columns.Count > 0)
                    {
                        int columnCounter = 0;

                        foreach(DataGridHelperColumn c in Columns)
                        {
                            
                            if (c.Enabled && !c.Hidden)
                            {

                                var h = $"column{columnCounter}";
                                if(!string.IsNullOrEmpty(c.Header))
                                {
                                    h=c.Header.Trim();
                                }
                                else
                                {
                                    c.Header=h;
                                }

                                var p = "";
                                if(!string.IsNullOrEmpty(c.Path))
                                {
                                    p=$"{c.Path.Trim()}";
                                }
                                else
                                {
                                    c.Path=p;
                                }

                                var n = $"{p}";
                                if(!string.IsNullOrEmpty(c.Name))
                                {
                                    n=c.Name.Trim();
                                }
                                else
                                {
                                    c.Name=n;
                                }
                                var v= Visibility.Visible;
                                if (!c.Visible)
                                {
                                    v= Visibility.Collapsed;
                                }


                                switch(c.ColumnType)
                                {

                                    case DataGridHelperColumn.ColumnTypeRef.String:
                                    {
                                        var col = new DataGridTextColumn();

                                        var b = new System.Windows.Data.Binding();
                                        b.Path=new PropertyPath($"[{p}]");

                                        if(c.Formatter!=null)
                                        {
                                            b.Converter=new ProxyConverter();
                                            b.ConverterParameter=c.Formatter;
                                        }
                                        col.Visibility = v;

                                        col.Binding=b;

                                        col.CellStyle=(Style)Grid.TryFindResource(styles.CheckGet("base"));

                                        col.Header=h;

                                        if(c.Width > 0)
                                        {
                                            col.Width=c.Width;
                                        }

                                        if(c.MinWidth > 0)
                                        {
                                            col.MinWidth=c.MinWidth;
                                        }

                                        if(c.Width2 > 0)
                                        {
                                            var w= c.Width2 * 8;
                                            col.Width = w;
                                            col.MinWidth = w;
                                        }
                                        
                                        if(!string.IsNullOrEmpty(c.Style))
                                        {
                                            col.CellStyle=(Style)Grid.TryFindResource($"{c.Style}");
                                        }

                                        if (c.Stylers != null && c.Stylers.Count > 0)
                                        {
                                            col.CellStyle = ProcessStylers(c.Stylers);
                                        }

                                        Grid.Columns.Add(col);
                                    }
                                    break;

                                    case DataGridHelperColumn.ColumnTypeRef.Integer:
                                    {
                                        var col = new DataGridTextColumn();

                                        var b = new System.Windows.Data.Binding();
                                        b.Path=new PropertyPath($"[{p}]");
                                        b.Converter=new Float0();
                                        col.Binding=b;
                                        col.Visibility = v;
                                        //col.CellStyle=(Style)Grid.TryFindResource("DataGridColumnDigit");
                                        col.CellStyle=(Style)Grid.TryFindResource(styles.CheckGet("digit"));

                                        col.Header=h;

                                        if(c.Width > 0)
                                        {
                                            col.Width=c.Width;
                                        }

                                        if(c.MinWidth > 0)
                                        {
                                            col.MinWidth=c.MinWidth;
                                        }

                                        if(c.Width2 > 0)
                                        {
                                            var w = c.Width2 * 8;
                                            col.Width = w;
                                            col.MinWidth = w;
                                        }

                                        if(!string.IsNullOrEmpty(c.Style))
                                        {
                                            col.CellStyle=(Style)Grid.TryFindResource($"{c.Style}");
                                        }

                                        if (c.Stylers != null && c.Stylers.Count > 0)
                                        {
                                            col.CellStyle = ProcessStylers(c.Stylers);
                                        }

                                        Grid.Columns.Add(col);
                                    }
                                    break;

                                    case DataGridHelperColumn.ColumnTypeRef.Double:
                                    {
                                        var col = new DataGridTextColumn();

                                        var b = new System.Windows.Data.Binding();
                                        b.Path=new PropertyPath($"[{p}]");
                                        b.Converter=new DataGridHelperDouble();

                                        if(!string.IsNullOrEmpty(c.Format))
                                        {
                                            b.ConverterParameter=c.Format;
                                        }
                                        col.Visibility = v;

                                        col.Binding=b;

                                        //col.CellStyle=(Style)Grid.TryFindResource("DataGridColumnDigit");
                                        col.CellStyle=(Style)Grid.TryFindResource(styles.CheckGet("digit"));

                                        col.Header=h;

                                        if(c.Width > 0)
                                        {
                                            col.Width=c.Width;
                                        }

                                        if(c.MinWidth > 0)
                                        {
                                            col.MinWidth=c.MinWidth;
                                        }

                                        if(c.Width2 > 0)
                                        {
                                            var w = c.Width2 * 8;
                                            col.Width = w;
                                            col.MinWidth = w;
                                        }

                                        if(!string.IsNullOrEmpty(c.Style))
                                        {
                                            col.CellStyle=(Style)Grid.TryFindResource($"{c.Style}");
                                        }

                                        if (c.Stylers != null && c.Stylers.Count > 0)
                                        {
                                            col.CellStyle = ProcessStylers(c.Stylers);
                                        }

                                        Grid.Columns.Add(col);
                                    }
                                    break;

                                    case DataGridHelperColumn.ColumnTypeRef.DateTime:
                                    {
                                        var col = new DataGridTextColumn();

                                        var b = new System.Windows.Data.Binding();
                                        b.Path=new PropertyPath($"[{p}]");
                                        col.Binding=b;
                                        col.Visibility = v;
                                        //col.CellStyle=(Style)Grid.TryFindResource("DataGridColumnString");
                                        col.CellStyle=(Style)Grid.TryFindResource(styles.CheckGet("base"));

                                        col.Header=h;

                                        if(c.Width > 0)
                                        {
                                            col.Width=c.Width;
                                        }

                                        if(c.MinWidth > 0)
                                        {
                                            col.MinWidth=c.MinWidth;
                                        }

                                        if(c.Width2 > 0)
                                        {
                                            var w = c.Width2 * 8;
                                            col.Width = w;
                                            col.MinWidth = w;
                                        }

                                        if(!string.IsNullOrEmpty(c.Style))
                                        {
                                            col.CellStyle=(Style)Grid.TryFindResource($"{c.Style}");
                                        }

                                        if (c.Stylers != null && c.Stylers.Count > 0)
                                        {
                                            col.CellStyle = ProcessStylers(c.Stylers);
                                        }

                                        Grid.Columns.Add(col);
                                    }
                                    break;

                                    case DataGridHelperColumn.ColumnTypeRef.Boolean:
                                    {
                                        var col = new DataGridTemplateColumn();

                                        var b = new System.Windows.Data.Binding();
                                        b.Path=new PropertyPath($"[{p}]");
                                        b.Mode = BindingMode.OneWay;
                                        b.Converter=new ToBool();


                                        /*
                                        // TextBlock
                                        var factory = new FrameworkElementFactory(typeof(TextBlock));
                                        factory.SetBinding(TextBlock.TextProperty, b);
                                        var tpl = new DataTemplate();
                                        tpl.VisualTree = factory;
                                        */

                                        // CheckBox
                                        var factory = new FrameworkElementFactory(typeof(CheckBox));
                                        factory.SetBinding(CheckBox.IsCheckedProperty,b);
                                        factory.SetValue(CheckBox.IsEnabledProperty,false);
                                        var tpl = new DataTemplate();
                                        tpl.VisualTree = factory;

                                        col.CellTemplate = tpl;

                                        col.Visibility = v;
                                        //col.CellStyle=(Style)Grid.TryFindResource("DataGridColumnBool");
                                        col.CellStyle=(Style)Grid.TryFindResource(styles.CheckGet("bool"));

                                        col.Header=h;

                                        if(c.Width > 0)
                                        {
                                            col.Width=c.Width;
                                        }

                                        if(c.MinWidth > 0)
                                        {
                                            col.MinWidth=c.MinWidth;
                                        }

                                        if(c.Width2 > 0)
                                        {
                                            var w = c.Width2 * 8;
                                            col.Width = w;
                                            col.MinWidth = w;
                                        }

                                        if(!string.IsNullOrEmpty(c.Style))
                                        {
                                            col.CellStyle=(Style)Grid.TryFindResource($"{c.Style}");
                                        }

                                        Grid.Columns.Add(col);

                                    }
                                    break;

                                }

                                ColIndexes.Add(n,columnCounter);
                                columnCounter++;
                            }
                        }

                        int columnCounter2 = 0;

                        foreach(DataGridHelperColumn c in Columns)
                        {
                            if(c.Enabled && !c.Hidden)
                            {
                                if(Grid.Columns[columnCounter2] != null)
                                {
                                    var column = Grid.Columns[columnCounter2];
                                    DataGridUtil.SetName(column,c.Name);

                                }
                                columnCounter2++;
                            }
                        }
                    }

                }
            }

        }

        /// <summary>
        /// обработка стилей
        /// </summary>
        /// <param name="stylers">массив стилей</param>
        /// <param name="type">1=ячейка,2=строка</param>
        /// <returns></returns>
        private Style ProcessStylers(Dictionary<StylerTypeRef, StylerDelegate> stylers, int type = 1, DataGridHelperColumn.ColumnTypeRef columnType = DataGridHelperColumn.ColumnTypeRef.String)
        {
            if (type == 1)
            {
                stylers = PrepareStylers(stylers);
            }

            string s = "";
            Binding binding = new Binding();
            binding.Source = stylers;

            var style = new Style();
            if (type == 1)
            {
                //cell
                style = new Style(typeof(DataGridCell));

            }
            else if (type == 2)
            {
                //row
                style = new Style(typeof(DataGridRow));
            }

            if (type == 1)
            {
                var styleName = "";
                switch (columnType)
                {
                    case ColumnTypeRef.Integer:
                    case ColumnTypeRef.Double:
                        styleName = "DataGridColumnDigit";
                        break;

                    default:
                    case ColumnTypeRef.String:
                        styleName = "DataGridColumn";
                        break;
                }

                style.BasedOn = (Style)DropDownGridBox.TryFindResource(styleName);
            }

            foreach (KeyValuePair<StylerTypeRef, StylerDelegate> styler in stylers)
            {
                switch (styler.Key)
                {
                    case StylerTypeRef.BackgroundColor:
                        {
                            var setter = new Setter(
                                ContentControl.BackgroundProperty,
                                new Binding(s)
                                {
                                    Converter = new ProxyHightlighter(),
                                    ConverterParameter = styler.Value,
                                }
                            );
                            style.Setters.Add(setter);

                            var setter2 = new Setter(
                                ContentControl.BorderBrushProperty,
                                new Binding(s)
                                {
                                    Converter = new ProxyHightlighter(),
                                    ConverterParameter = styler.Value,
                                }
                            );
                            style.Setters.Add(setter2);
                        }
                        break;

                    case StylerTypeRef.BorderColor:
                        {
                            var setter2 = new Setter(
                                ContentControl.BorderBrushProperty,
                                new Binding(s)
                                {
                                    Converter = new ProxyHightlighter(),
                                    ConverterParameter = styler.Value,
                                }
                            );
                            style.Setters.Add(setter2);
                        }
                        break;

                    case StylerTypeRef.ForegroundColor:
                        {
                            var setter2 = new Setter(
                                ContentControl.ForegroundProperty,
                                new Binding(s)
                                {
                                    Converter = new ProxyHightlighter(),
                                    ConverterParameter = styler.Value,
                                }
                            );
                            style.Setters.Add(setter2);
                        }
                        break;


                    case StylerTypeRef.FontWeight:
                        {
                            var setter2 = new Setter(
                                ContentControl.FontWeightProperty,
                                new Binding(s)
                                {
                                    Converter = new ProxyFontWight(),
                                    ConverterParameter = styler.Value,
                                }
                            );
                            style.Setters.Add(setter2);
                        }
                        break;
                }
            }
            return style;
        }

        private Dictionary<StylerTypeRef, StylerDelegate> PrepareStylers(Dictionary<StylerTypeRef, StylerDelegate> stylers)
        {
            /*
                подготовка стайлеров
                если есть хотя бы один стайлер для бэкграунда -- все хорошо
                если нет, добавим дефолтный
             */

            bool backgroundColorAddDefault = false;
            bool foregroundColorAddDefault = false;

            if (stylers.Count == 0)
            {
                backgroundColorAddDefault = true;
                foregroundColorAddDefault = true;
            }
            else
            {
                int backgroundColorCount = 0;
                int foregroundColorCount = 0;

                foreach (KeyValuePair<StylerTypeRef, StylerDelegate> s in stylers)
                {
                    if (s.Key == StylerTypeRef.BackgroundColor)
                    {
                        backgroundColorCount++;
                    }
                    if (s.Key == StylerTypeRef.ForegroundColor)
                    {
                        foregroundColorCount++;
                    }
                }

                if (backgroundColorCount == 0)
                {
                    backgroundColorAddDefault = true;
                }

                if (foregroundColorCount == 0)
                {
                    foregroundColorAddDefault = true;
                }
            }

            if (backgroundColorAddDefault)
            {
                stylers.Add(
                    StylerTypeRef.BackgroundColor,
                    (Dictionary<string, string> row) =>
                    {
                        var result = DependencyProperty.UnsetValue;
                        return result;
                    }
                );
            }

            return stylers;
        }

        public DataGridHelperColumn FindColumnByName(string name)
        {
            var result=new DataGridHelperColumn();

            if(DropDownGridBox!=null)
            {
                var Columns = GridColumns;

                if(Columns.Count > 0)
                {
                    foreach(DataGridHelperColumn c in Columns)
                    {
                        if(c.Name==name)
                        {
                            result=c;
                        }
                    }
                }
            }

            return result;
        }

        public void UpdateGridItems(ListDataSet ds)
        {
            if(ds != null)
            {
                if(DropDownGridBox!=null)
                {
                    var items = new List<Dictionary<string,string>>();

                    if(ds.Items.Count>0)
                    {

                        if(ColIndexes.Count>0)
                        {
                            //перегрузим только определенные для нас поля
                            //var items = new List<Dictionary<string,string>>();

                            foreach(Dictionary<string,string> row in ds.Items)
                            {
                                var item = new Dictionary<string,string>();
                                foreach(KeyValuePair<string,int> col in ColIndexes)
                                {
                                    var k = col.Key;
                                    if(row.ContainsKey(k))
                                    {
                                        var v = row[k];
                                        item.Add(k,v);
                                    }
                                }
                                items.Add(item);
                            }

                            if(items.Count>0)
                            {
                                //DropDownGridBox.ItemsSource=items;
                            }
                        }
                    }

                    DropDownGridBox.ItemsSource=items;

                }

            }

        }

        private void ValueTextBox_LostFocus(object sender,RoutedEventArgs e)
        {
            HideDropDown();
        }

        private void DropDownListBox_SelectionChanged(object sender,SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            var listItem = listBox.SelectedItem as ListBoxItem;

            if(listBox.SelectedIndex != -1)
            {
                if(Items != null && Items.Count > 0)
                {
                    //SelectedItem = Items.ElementAt(listBox.SelectedIndex);
                    SelectedItemOld=SelectedItem;
                    SelectedItem = FindItem(listItem.Content.ToString());
                    {
                        var selectedItem=new Dictionary<string,string>();
                        selectedItem.Add("ID",SelectedItem.Key);
                        selectedItem.Add("KEY",SelectedItem.Key);
                        selectedItem.Add("VALUE",SelectedItem.Value);

                        Central.Logger.Debug($"OnSelectItem (1)");

                        //предотвращение двойного срабатывания 2022-03-17_F11
                        //var doSelect=OnSelectItem.Invoke(selectedItem);
                        if (!SelectedItemOld.Equals(default(KeyValuePair<string, string>)) || SelectedItemOld.Equals(new KeyValuePair<string, string>(null, null)))
                        {
                            if (!SelectedItemOld.Equals(SelectedItem))
                            {
                                var doSelect =OnSelectItem.Invoke(selectedItem);
                                if(OnSelectItemComplete!=null && FieldControl!=null)
                                {
                                    OnSelectItemComplete.Invoke(FieldControl, selectedItem);
                                }
                            }
                        }
                    }
                    

                    ValueTextBox.Text = SelectedItem.Value;
                    HideDropDown();
                }
            }
        }

        private void DropDownGridBox_SelectionChanged(object sender,RoutedEventArgs e)
        {
            
            if (sender != null)
            {
                var dg = sender as DataGrid;
                if(dg != null)
                {
                    if(dg.SelectedItem != null)
                    {
                       var selectedItem = dg.SelectedItem as Dictionary<string,string>;
                       ProcessSelectedItem(selectedItem);
                    }
                }
            }

            //var grid = sender as DataGrid;

            //var item = grid.SelectedItem as DataGridI;
            /*

            if (listBox.SelectedIndex != -1)
            {
                if (Items != null && Items.Count > 0)
                {
                    SelectedItem = FindItem(listItem.Content.ToString());
                    ValueTextBox.Text = SelectedItem.Value;
                    HideDropDown();
                }
            }
            */
        }

        /*
            сначала нужно сделать SET, затем вызвать коллбэк
            отработку блокировки выбора нужно вынести в отдельное событие: beforeSet
         
         */

        public void SetSelectedItem(KeyValuePair<string,string> item)
        {
            switch(DataType)
            {
                case DataTypeRef.List:
                {
                   
                    SelectedItem=item;
                    {
                        var selectedItem=new Dictionary<string,string>();
                        selectedItem.Add("ID",SelectedItem.Key);
                        selectedItem.Add("KEY",SelectedItem.Key);
                        selectedItem.Add("VALUE",SelectedItem.Value);

                        Central.Logger.Debug($"OnSelectItem (2)");
                        var doSelect=OnSelectItem.Invoke(selectedItem);

                        if(OnSelectItemComplete!=null && FieldControl!=null)
                        {
                            OnSelectItemComplete.Invoke(FieldControl, selectedItem);
                        }
                        
                            //if (doSelect)
                            //{
                            //    
                            //}

                        }
                }
                break;

                case DataTypeRef.Grid:
                {
                        SelectedItem = item;
                        var selectedItem = new Dictionary<string, string>();
                        selectedItem.Add("ID", SelectedItem.Key);
                        selectedItem.Add("KEY", SelectedItem.Key);
                        selectedItem.Add("VALUE", SelectedItem.Value);

                        Central.Logger.Debug($"OnSelectItem (2)");
                        var doSelect = OnSelectItem.Invoke(selectedItem);

                        if (OnSelectItemComplete != null && FieldControl != null)
                        {
                            OnSelectItemComplete.Invoke(FieldControl, selectedItem);
                        }
                        ProcessSelectedItem(selectedItem);
                    }
                break;
            }
            
        }

        public void SetSelectedItem(Dictionary<string,string> item)
        {
            switch(DataType)
            {
                case DataTypeRef.List:
                {
                    
                }
                break;
                case DataTypeRef.Grid:
                {
                    ProcessSelectedItem(item);
                }
                break;
            }            
        }
        /// <summary>
        /// установка выбора на первый элемент в коллекции
        /// </summary>
        public void SetSelectedItemFirst()
        {
            switch(DataType)
            {
                case DataTypeRef.List:
                {
                    if(Items!=null)
                    {
                        if(Items.Count>0)
                        {
                            var first=Items.First();

                            if (!first.Equals(default(KeyValuePair<string, string>)))
                            {
                                SetSelectedItem(first);
                            }
                        }
                    }
                }
                break;

                case DataTypeRef.Grid:
                {
                    
                }
                break;
            }
            
        }

        /// <summary>
        /// установка выбора на первый элемент в коллекции
        /// </summary>
        public void SetSelectedItemByKey(string key)
        {
            switch(DataType)
            {
                case DataTypeRef.List:
                {
                    if(Items!=null)
                    {
                        if(Items.Count>0)
                        {
                            var first=default(KeyValuePair<string, string>);
                            foreach(KeyValuePair<string,string> item in Items)
                            {
                                if(item.Key==key)
                                {
                                    first=item;
                                }
                            }
                            
                            if (!first.Equals(default(KeyValuePair<string, string>)))
                            {
                                SetSelectedItem(first);
                            }
                        }
                    }
                }
                break;

                case DataTypeRef.Grid:
                {
                    if (GridDataSet != null)
                    {
                        if (GridDataSet.Items.Count > 0)
                        {
                            var first = default(KeyValuePair<string, string>);
                            foreach (Dictionary<string, string> item in GridDataSet.Items)
                            {
                                if (item.CheckGet("ID") == key)
                                {
                                    DropDownGridBox.SelectedItem = item;
                                    first = new KeyValuePair<string,string>(item.CheckGet("ID"), item.CheckGet("NAME"));
                                    break;
                                }
                            }

                            if (!first.Equals(default(KeyValuePair<string, string>)))
                            {
                                SetSelectedItem(first);
                            }
                        }
                    }
                }
                break;
            }
            
        }

        public string GridPrimaryKey { get; set; }
        /// <summary>
        /// выделенная строка таблицы
        /// </summary>
        public Dictionary<string, string> SelectedRow { get; set; }
        private void ProcessSelectedItem( Dictionary<string,string> selectedItem)
        {
            if(selectedItem!=null)
            {
                var k = "";
                var itemValue = "";
                var doSelect=true;

                if(selectedItem.Count>0)
                {
                    SelectedRow = selectedItem;
                    var tpl = SelectedItemValue;
                    
                    foreach(KeyValuePair<string,string> c in selectedItem)
                    {
                        if(!GridPrimaryKey.IsNullOrEmpty())
                        {
                            if(c.Key == GridPrimaryKey)
                            {
                                k = c.Value;
                            }
                        }
                        else
                        {
                            if(c.Key == "ID")
                            {
                                k = c.Value;
                            }
                        }

                        
                                   
                        var include=false;
                        if(!string.IsNullOrEmpty(GridSelectedItemFormat))
                        {
                            if(GridSelectedItemFormat.IndexOf(c.Key)>-1)
                            {
                                include=true;
                            }
                        }
                        else
                        {
                            include=true;
                        }

                        if(!string.IsNullOrEmpty(SelectedItemValue))
                        {
                            if(SelectedItemValue.IndexOf(c.Key)>-1)
                            {
                                include=true;
                            }
                            else
                            {
                                include=false;
                            }
                        }

                        var col=FindColumnByName(c.Key);
                        if(
                            col!=null 
                            && !string.IsNullOrEmpty(col.Name) 
                            && include
                        )
                        {
                            var a="";

                            switch(col.ColumnType)
                            {
                                case DataGridHelperColumn.ColumnTypeRef.Integer:
                                    //a=c.Value.ToInt().ToString();
                                    var emptyMode=false;
                                    if(col.Options.IndexOf("zeroempty")>-1)
                                    {
                                        emptyMode=true;
                                    }
                                    if(emptyMode)
                                    {
                                        if(c.Value.ToInt()==0)
                                        {
                                            a="";
                                        }
                                        else
                                        {
                                            a=c.Value.ToInt().ToString();
                                        }
                                    }
                                    else
                                    {
                                        a=c.Value.ToInt().ToString();
                                    }
                                    break;
                                
                                case DataGridHelperColumn.ColumnTypeRef.Double:
                                    a=c.Value.ToDouble().ToString();
                                    break;
                                
                                default:
                                    a=c.Value;
                                    break;
                            }

                            if (!string.IsNullOrEmpty(SelectedItemValue))
                            {
                                var f = col.Path;
                                var t = a;                               
                                tpl = tpl.Replace(f, t);
                            }
                            else
                            {
                                itemValue=$"{itemValue} {a}";    
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(SelectedItemValue))
                    {
                        itemValue = tpl;
                    }
                }

                Central.Logger.Debug($"OnSelectItem (3)");
                doSelect=OnSelectItem.Invoke(selectedItem);

                if(doSelect)
                {
                    ValueTextBox.Text = itemValue;

                    if(!string.IsNullOrEmpty(k))
                    {
                        SelectedItem=new KeyValuePair<string,string>(k,itemValue);
                    }

                    HideDropDown();
                }

                if (OnSelectItemComplete != null && FieldControl != null)
                {
                    OnSelectItemComplete.Invoke(FieldControl, selectedItem);
                }
            }
        }

        public void SetDS(ListDataSet ds)
        {
            GridDataSet=ds;
        }

        public void SetItems(Dictionary<string,string> items)
        {
            
            switch(DataType)
            {
                case DataTypeRef.List:
                {
                    Items=items;        
                }
                break;

                case DataTypeRef.Grid:
                {
                    
                }
                break;
            }     
        }

        public void SetItems(ListDataSet ds)
        {
            
            switch(DataType)
            {
                case DataTypeRef.List:
                {
                    if(ds!=null)
                    {
                        var items=ds.GetItemsList();       
                        Items=items;      
                    }                    
                }
                break;

                case DataTypeRef.Grid:
                {
                    GridDataSet=ds;
                }
                break;
            }     
        }

        public void SetItems(ListDataSet ds, string k="KEY", string v="VALUE")
        {
            
            switch(DataType)
            {
                case DataTypeRef.List:
                {
                    if(ds!=null)
                    {
                        var items=ds.GetItemsList(k,v);       
                        Items=items;      
                    }                    
                }
                break;

                case DataTypeRef.Grid:
                {
                    GridDataSet=ds;
                }
                break;
            }     
        }

        public void AddItems(Dictionary<string, string> newItems)
        {
            if (newItems == null || newItems.Count == 0)
                return;

            if (Items == null)
                Items = new Dictionary<string, string>();

            foreach (var item in newItems)
            {
                if (!Items.ContainsKey(item.Key))
                    Items.Add(item.Key, item.Value);
            }

            UpdateListItems(Items);
        }

        public void AddItems(ListDataSet ds, string k = "KEY", string v = "VALUE")
        {
            if (ds == null)
                return;

            var newItems = ds.GetItemsList(k, v);
            AddItems(newItems);
        }


        public void SetItems(ListDataSet ds, FieldTypeRef keyTypeRef, string k = "KEY", string v = "VALUE")
        {
            switch (DataType)
            {
                case DataTypeRef.List:
                    {
                        if (ds != null)
                        {
                            Dictionary<string, string> tempItems = ds.GetItemsList(k, v);
                            Dictionary<string, string> items = new Dictionary<string, string>();

                            switch (keyTypeRef)
                            {
                                case FieldTypeRef.Integer:
                                    if (tempItems != null && tempItems.Count > 0)
                                    {
                                        foreach (var item in tempItems)
                                        {
                                            items.Add(item.Key.ToInt().ToString(), item.Value);
                                        }
                                    }
                                    else
                                    {
                                        items = tempItems;
                                    }
                                    break;

                                case FieldTypeRef.String:
                                case FieldTypeRef.Double:
                                case FieldTypeRef.DateTime:
                                case FieldTypeRef.Boolean:
                                default:
                                    items = tempItems;
                                    break;
                            }

                            Items = items;
                        }
                    }
                    break;

                case DataTypeRef.Grid:
                    {
                        GridDataSet = ds;
                    }
                    break;
            }
        }

        /// <summary>
        /// Очищаем наполнение селектбокса
        /// </summary>
        /// <param name="selectBox"></param>
        public static void ClearSelectBoxItems(SelectBox selectBox)
        {
            selectBox.DropDownListBox.Items.Clear();
            selectBox.DropDownListBox.SelectedItem = null;
            selectBox.ValueTextBox.Text = "";
            selectBox.Items = new Dictionary<string, string>();
            selectBox.SelectedItem = new KeyValuePair<string, string>();
        }
    }
}
