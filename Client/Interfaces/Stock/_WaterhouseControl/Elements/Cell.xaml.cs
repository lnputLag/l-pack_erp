using Client.Assets.HighLighters;
using Client.Common;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Interaction logic for Cell.xaml
    /// Контрол для отображения и управления ячейками
    /// <author>eletskikh_ya</author>
    /// </summary>
    public partial class Cell : UserControl
    {
        public Cell(MapZone owner, Task<Dictionary<int, Dictionary<string, string>>> zoneInformation)
        {
            InitializeComponent();

            ZoneInformation = zoneInformation;

            MapZone = owner;

            TextCountFontSize = TextCount.FontSize;
            TextNameFontSize = TextName.FontSize;

            SizeChanged += RowButton_SizeChanged;

            PreviewMouseDown += Row_PreviewMouseDown;
            PreviewMouseUp += Row_PreviewMouseUp;
            PreviewMouseMove += Row_PreviewMouseMove;
            MouseLeave += RowButton_MouseLeave;
            MouseEnter += RowButton_MouseEnter;

            Opacity = 0.7;

        }

        /// <summary>
        /// Состояние ячейки из БД
        /// </summary>
        private int CellState
        {
            get; set;
        }


        public static double DefaultWidth = 80;
        public static double DefaultHeight = 80;

        private Task<Dictionary<int, Dictionary<string, string>>> ZoneInformation
        {
            get; set;
        }

        private MapZone MapZone { get; set; }

        private string _Id;
        public string ID
        {
            get
            {
                return _Id;
            }
            set
            {
                _Id = value;
           
            }
        }

        public string Num
        {
            get;set;
        }

        private Point? _buttonPosition;
        private double deltaX;
        private double deltaY;
        private TranslateTransform _currentTT;

        /// <summary>
        /// контейнер владелец ячейки
        /// </summary>
        private Canvas RowOwner;

        /// <summary>
        /// Переменные для вычисления перемещения и изменения размеров ячейки
        /// </summary>
        private Point mouseDown;
        private Point oldSize = new Point();

        public double offsetX = 0.0;
        public double offsetY = 0.0;

        public bool ResizeFlag = false;

        public event RoutedEventHandler Click;
        public event RoutedEventHandler ReallocComplete;

        private double TextCountFontSize = 0;
        private double TextNameFontSize = 0;

        private Point _positionInBlock;
        private bool _IsMouseCaptured = false;

        /// <summary>
        /// флаг перемещения ячейки
        /// </summary>
        private bool _isMoving = false;
        private bool IsMoving
        {
            get { return _isMoving; }
            set
            {
                bool prev = _isMoving;
                _isMoving = value;

                if(_isMoving==false && prev)
                {
                    MoveDone();
                }

                if (_isMoving)
                {
                    //Button.Background = HColor.Gray.ToBrush();
                }
                else
                {
                    //Button.Background = "#FFECF0F1".ToBrush();

                    //MoveDone();
                }
            }
        }

        
        /// <summary>
        /// Признак того что ячейка выделенна
        /// </summary>
        private bool _isSelected = false;
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }

            set
            {
                _isSelected = value;
                if(_isSelected)
                {
                    Opacity = 1.0;
                }
                else
                {
                    Opacity = 0.7;
                }
            }
        }
       

        private void RowButton_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.SizeAll;
        }

        private void RowButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (IsMoving || ResizeFlag) ReallocComplete?.Invoke(this, e);

            ResizeFlag = false;
            Mouse.OverrideCursor = null;
            _currentTT = Button.RenderTransform as TranslateTransform;
            IsMoving = false;
        }


        /// <summary>
        /// Изменение размера ячейки, корректирует размеры шрифта
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RowButton_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            GridContainer.Width = Button.ActualWidth > 40 ? Button.ActualWidth - 20 : 20;
            GridContainer.Height = Button.ActualHeight > 40 ? Button.ActualHeight - 20 : 20;

            if (GridContainer.Height < 85)
            {
                TextCount.FontSize = TextCountFontSize * GridContainer.Height / 85;
                TextName.FontSize = TextNameFontSize * GridContainer.Height / 85;
            }
            else
            {
                TextCount.FontSize = TextCountFontSize;
                TextName.FontSize = TextNameFontSize;

            }
        }


      
        private void Row_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // get the parent container
            RowOwner = VisualTreeHelper.GetParent(this) as Canvas;

            if (RowOwner != null)
            {
                RowOwner.Children.Remove(this);
                RowOwner.Children.Add(this);
            }

            if (_buttonPosition == null)
                _buttonPosition = Button.TransformToAncestor(RowOwner).Transform(new Point(0, 0));

            mouseDown = Mouse.GetPosition(RowOwner);
            oldSize.X = Width;
            oldSize.Y = Height;
            deltaX = mouseDown.X - _buttonPosition.Value.X;
            deltaY = mouseDown.Y - _buttonPosition.Value.Y;
            IsMoving = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Row_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (RowOwner != null)
            {
                if (IsMoving || ResizeFlag) ReallocComplete?.Invoke(this, e);

                _currentTT = Button.RenderTransform as TranslateTransform;
                IsMoving = false;

                if (_buttonPosition == null)
                    _buttonPosition = Button.TransformToAncestor(RowOwner).Transform(new Point(0, 0));

                var mousePoint = Mouse.GetPosition(RowOwner);

                mouseDown = Mouse.GetPosition(RowOwner);
                deltaX = mouseDown.X - _buttonPosition.Value.X;
                deltaY = mouseDown.Y - _buttonPosition.Value.Y;

                offsetX = (_currentTT == null ? _buttonPosition.Value.X : _buttonPosition.Value.X - _currentTT.X) + deltaX - mousePoint.X;
                offsetY = (_currentTT == null ? _buttonPosition.Value.Y : _buttonPosition.Value.Y - _currentTT.Y) + deltaY - mousePoint.Y;
            }
        }

        /// <summary>
        /// собыьтие перетаскивания ячейки мышкой
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Row_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if(ToolTip==string.Empty)
            {
                LoadStorageData();
            }

            var mousePoint = Mouse.GetPosition(RowOwner);

            if (!IsMoving)
            {
                var mousePoint2 = Mouse.GetPosition(this);

                if ((mousePoint2.X + offsetX) > (Width - 20) && (mousePoint2.Y + offsetY) > (Height - 20))
                {
                    Mouse.OverrideCursor = Cursors.SizeNWSE;
                    ResizeFlag = true;
                }
                else
                {
                    Mouse.OverrideCursor = null;
                    ResizeFlag = false;
                }

                return;
            }

            offsetX = (_currentTT == null ? _buttonPosition.Value.X : _buttonPosition.Value.X - _currentTT.X) + deltaX - mousePoint.X;
            offsetY = (_currentTT == null ? _buttonPosition.Value.Y : _buttonPosition.Value.Y - _currentTT.Y) + deltaY - mousePoint.Y;

            if (ResizeFlag)
            {
                double Dx = mouseDown.X - mousePoint.X;
                double Dy = mouseDown.Y - mousePoint.Y;


                var w = oldSize.X - Dx;
                var h = oldSize.Y - Dy;

                if (w < 40) w = 40;
                if (h < 40) h = 40;

                Width = w;
                Height = h;

                DefaultWidth = Width;
                DefaultHeight = Height;

                if(IsSelected)
                {
                    MapZone.ResizeSelected(this, Width, Height);
                }
            }
            else
            {
                Move(-offsetX, -offsetY);

                if(IsSelected)
                {
                    MapZone.MoveSelected(this, offsetX, offsetY);
                }
            }


        }

        /// <summary>
        /// Движение ячейки
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Move(double x, double y)
        {
            this.Button.RenderTransform = new TranslateTransform(x, y);
        }

        /// <summary>
        /// Вызывается при групповом перемещении ячеек
        /// </summary>
        /// <param name="_offsetX"></param>
        /// <param name="_offsetY"></param>
        internal void MoveSelected(double _offsetX, double _offsetY)
        {
            Move(-_offsetX, -_offsetY);

            _buttonPosition = null;

            if (this.Button.RenderTransform != null)
            {
                offsetX = this.Button.RenderTransform.Value.OffsetX;
                offsetY = this.Button.RenderTransform.Value.OffsetY;

                _currentTT = null;
            }
        }

        /// <summary>
        /// Порлучение дополниетльных данны ячейки
        /// </summary>
        /// <param name="task_id"></param>
        /// <returns></returns>
        private Task<string> GetTaskInformation(int task_id)
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                string res = string.Empty;

                var p = new Dictionary<string, string>() { { "WMTA_ID", task_id.ToString() } };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Task");
                q.Request.SetParam("Action", "GetExtendedInformation");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;


                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");

                        if(ds.Items.Count>0)
                        {
                            res += ds.Items[0].CheckGet("FULLNAME") + Environment.NewLine;
                            res += ds.Items[0].CheckGet("DESCRIPTION") + Environment.NewLine;
                            res += "Дата разгрузки:" + ds.Items[0].CheckGet("ACCEPTED_DTTM") + Environment.NewLine;
                        }
                    }
                }

                res += "Код задачи = " + task_id + Environment.NewLine;
                res += "----------------------------" + Environment.NewLine;
                return res;
            }
            );
        }

        /// <summary>
        /// Получения дополнительной информации по операции
        /// </summary>
        /// <param name="operation_id"></param>
        /// <returns></returns>
        private Task<string> GetOperationInformation(int operation_id)
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                string res = string.Empty;

                var p = new Dictionary<string, string>() { { "WMOP_ID", operation_id.ToString() } };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Operation");
                q.Request.SetParam("Action", "GetExtendedInformation");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;


                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");

                        if (ds.Items.Count > 0)
                        {
                            res += ds.Items[0].CheckGet("FULLNAME") + Environment.NewLine;
                            res += "Дата разгрузки:" + ds.Items[0].CheckGet("ACCEPTED_DTTM") + Environment.NewLine;
                        }
                    }
                }

                res += "Код операции = " + operation_id + Environment.NewLine;
                res += "----------------------------" + Environment.NewLine;
                return res;
            }
            );
        }

        /// <summary>
        /// Загрузка данных для tooltip ячеейки
        /// </summary>
        private async void LoadStorageData()
        {
            ToolTip = "Данные загружаются";

            string message = string.Empty;

            if (CellState == 0 || CellState == 2)
            {
                message = "Свободна";
            }
            else if (CellState == 3)
            {
                message = "Забронирована";
            }
            else if (CellState == 1)
            {
                message = "Заблокирована";
            }
            else
            {
                var p = new Dictionary<string, string>() { { "WMST_ID", ID.ToString() } };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Item");
                q.Request.SetParam("Action", "ListByStorage");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                await Task.Run(() =>
                {
                    q.DoQuery();
                });

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");

                        Dictionary<int, Dictionary<string, int>> task_items = new Dictionary<int, Dictionary<string, int>>();
                        Dictionary<int, Dictionary<string, int>> operations_items = new Dictionary<int, Dictionary<string, int>>();

                        if (ds != null)
                        {
                            if (ds.Items != null)
                            {
                                foreach (var item in ds.Items)
                                {
                                    var task_id = item.CheckGet("WMTA_ID").ToInt();
                                    var operation_id = item.CheckGet("WMOP_ID").ToInt();

                                    string itemName = item.CheckGet("NAME") + " " + item.CheckGet("QTY") + " " + item.CheckGet("UNIT_NAME");

                                    if (task_id != 0)
                                    {
                                        if (!task_items.ContainsKey(task_id))
                                        {
                                            task_items.Add(task_id, new Dictionary<string, int>());
                                        }

                                        if (task_items[task_id].ContainsKey(itemName))
                                        {
                                            task_items[task_id][itemName]++;
                                        }
                                        else
                                        {
                                            task_items[task_id][itemName] = 1;
                                        }
                                    }
                                    else if (operation_id != 0)
                                    {
                                        if (!operations_items.ContainsKey(operation_id))
                                        {
                                            operations_items.Add(operation_id, new Dictionary<string, int>());
                                        }

                                        if (operations_items[operation_id].ContainsKey(itemName))
                                        {
                                            operations_items[operation_id][itemName]++;
                                        }
                                        else
                                        {
                                            operations_items[operation_id][itemName] = 1;
                                        }
                                    }
                                    else
                                    {
                                        // FIXME: не должно быть такого но есть, возможно оприходованные еще до добавления отметки в приход операции или задачи
                                        if (!task_items.ContainsKey(task_id))
                                        {
                                            task_items.Add(task_id, new Dictionary<string, int>());
                                        }

                                        if (task_items[task_id].ContainsKey(itemName))
                                        {
                                            task_items[task_id][itemName]++;
                                        }
                                        else
                                        {
                                            task_items[task_id][itemName] = 1;
                                        }
                                    }
                                }
                            }
                        }

                        foreach (var task_id in task_items.Keys)
                        {
                            var taskInfo = GetTaskInformation(task_id);

                            foreach (string item in task_items[task_id].Keys)
                            {
                                message += item + " x " + task_items[task_id][item] + Environment.NewLine;
                            }

                            message += await taskInfo;
                        }

                        foreach (var operation_id in operations_items.Keys)
                        {
                            var oprationInf = GetOperationInformation(operation_id);

                            foreach (string item in operations_items[operation_id].Keys)
                            {
                                message += item + " x " + operations_items[operation_id][item] + Environment.NewLine;
                            }

                            message += await oprationInf;
                        }
                    }
                }
            }

            ToolTip = message;
        }

        /// <summary>
        /// кстановка данных ячейки
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        public async void SetData(string name, int id)
        {
            Num = name;
            string toolTip = string.Empty;
            ID = id.ToString();
            TextName.Text = name;
            TextCount.Text = string.Empty;

            var itemsInfo = await ZoneInformation;

            if(itemsInfo != null)
            {
                if(itemsInfo.ContainsKey(id))
                {
                    CellState = itemsInfo[id].CheckGet("WMSS_ID").ToInt();
                }
                else
                {
                    CellState = 0;
                }
            }

            /*var p = new Dictionary<string, string>() { { "WMST_ID", id.ToString() } };

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Warehouse");
            q.Request.SetParam("Object", "Item");
            q.Request.SetParam("Action", "ListByCell");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestTimeoutGrid;
            q.Request.Attempts = Central.Parameters.RequestAttemptsGrid;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");

                    Dictionary<int, Dictionary<string, int>> items = new Dictionary<int, Dictionary<string, int>>();
                    Dictionary<int, string> additionalInformation = new Dictionary<int, string>();

                    if (ds != null)
                    {
                        if(ds.Items!=null)
                        {
                            foreach( var item in ds.Items)
                            {
                                state = 4;
                                var task_id = item.CheckGet("WMTA_ID").ToInt();

                                if(!items.ContainsKey(task_id))
                                {
                                    items.Add(task_id, new Dictionary<string, int>());
                                    additionalInformation[task_id] = item.CheckGet("TASK_NAME") + Environment.NewLine + item.CheckGet("ACCEPTED_DTTM") + Environment.NewLine + item.CheckGet("DESCRIPTION") + Environment.NewLine + item.CheckGet("FULL_NAME");
                                }

                                string itemName = item.CheckGet("NAME") + " " + item.CheckGet("QTY") + " " + item.CheckGet("UNIT_NAME");

                                if (items[task_id].ContainsKey(itemName))
                                {
                                    items[task_id][itemName]++;
                                }
                                else
                                {
                                    items[task_id][itemName] = 1;
                                }

                                //toolTip += item.CheckGet("NAME") + " " + item.CheckGet("QTY") + " " + item.CheckGet("UNIT_NAME") + Environment.NewLine;
                            }
                        }
                    }

                    string message = string.Empty;

                    foreach( var task_id in additionalInformation.Keys )
                    {
                        foreach(string item in items[task_id].Keys)
                        {
                            message += item + " x " + items[task_id][item] + Environment.NewLine;
                        }

                        message += additionalInformation[task_id] + Environment.NewLine + "----------------------------" + Environment.NewLine;
                    }

                    toolTip = message;
                }
            }*/



            string color = string.Empty;

            //ToolTip = "Ячейка " + name + Environment.NewLine + "Проверка подсказки" + Environment.NewLine + "_______________" + Environment.NewLine + " Вот так " + Environment.NewLine + "Водитель погрузчика: Ларшин И.Г\r\n";

            ToolTip = toolTip;

            switch (CellState)
            {
                case 1: // Заблокирована
                    color = HColor.Red;
                    break;
                case 6: // Недоступна
                    color = HColor.Yellow;
                    break;
                case 0:
                case 2: // Свободна
                    //color = HColor.Blue;
                    break;
                case 3: // Забронирована
                    color = HColor.Green;
                    break;
                case 4: // Частично занята
                    color = HColor.Yellow;
                    break;
                case 5: // Занята
                    color = HColor.Green;
                    break;
            }

            if (!string.IsNullOrEmpty(color))
            {
                Button.Background = color.ToBrush();
            }
        }

        /// <summary>
        /// Вызывается при заверщении перемещения ячейки
        /// </summary>
        public void MoveDone()
        {
            if (this.Button.RenderTransform != Transform.Identity)
            {
                double top = Canvas.GetTop(this) + this.Button.RenderTransform.Value.OffsetY;
                double left = Canvas.GetLeft(this) + this.Button.RenderTransform.Value.OffsetX;

                offsetY = 0;
                offsetY = 0;

                _currentTT = null;

                Canvas.SetLeft(this, left);
                Canvas.SetTop(this, top);

                this.Button.RenderTransform = Transform.Identity;

                _buttonPosition = null;
            }

            MapZone.MoveDone(this);
        }

        /// <summary>
        /// Установка координат и ширины с высотой для ячейки
        /// </summary>
        /// <param name="cX"></param>
        /// <param name="cY"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        internal void SetCoord(double cX, double cY, double w, double h)
        {
            double top = cY;
            double left = cX;

            offsetY = 0;
            offsetY = 0;

            _currentTT = null;

            Canvas.SetLeft(this, left);
            Canvas.SetTop(this, top);

            Width = w;
            Height = h;

            this.Button.RenderTransform = Transform.Identity;

            _buttonPosition = null;
        }
    }
}
