using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Gu.Wpf.DataGrid2D;
using Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.Word;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using SharpVectors.Dom.Resources;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Контролл реализует редактор карты склада
    /// Позволяет добавлять, удалять, перемещать как отдельные ячейки так и работать с группами
    /// <author>eletskikh_ya</author>
    /// </summary>
    public partial class MapZone : UserControl
    {
        

        public MapZone()
        {
            InitializeComponent();

            SizeChanged += Map_SizeChanged;

            PanelCanvas.MouseWheel += UIElement_OnMouseWheel;

            AllowDrop = true;

            mapBackground = new MapBackground(this);

            mapBackground.Width = 200;
            mapBackground.Height = 200;
            Canvas.SetLeft(mapBackground, 0);
            Canvas.SetTop(mapBackground, 0);

            PanelCanvas.Children.Add(mapBackground);

            mapBackground.OnMoveBackground += MapBackground_OnMoveBackground;
            mapBackground.OnSizeBackground += MapBackground_OnSizeBackground;

            ContextMenu menu = new ContextMenu();

            

            var openFileMenu = new System.Windows.Controls.MenuItem() {  Header  = "Загрузить из файла" };
            var saveFileMenu = new System.Windows.Controls.MenuItem() { Header = "Сохранить в файл" };

            menu.Items.Add(openFileMenu);
            menu.Items.Add(saveFileMenu);

            //openFileMenu.Click += OpenFileMenu_Click;
            //saveFileMenu.Click += SaveFileMenu_Click;

            //PanelCanvas.ContextMenu = menu;
        }

        //private void SaveFileMenu_Click(object sender, RoutedEventArgs e)
        //{
        //    throw new NotImplementedException();
        //}

        //private void OpenFileMenu_Click(object sender, RoutedEventArgs e)
        //{
        //    throw new NotImplementedException();
        //}

       

        /// <summary>
        /// Информация о хранилище, может быть загружена асинхронно
        /// </summary>
        public Task<Dictionary<int, Dictionary<string, string>>> ZoneInfo
        {
            get; set;
        }

        public string Id { get; set; }

        private void MapBackground_OnSizeBackground(double x, double y)
        {
            Width = x;
            Height = y;
        }

        private void MapBackground_OnMoveBackground(double x, double y)
        {
        }

        private MapBackground mapBackground;

        public delegate void OnDropItem(object sender, Dictionary<string, string> item);
        public event OnDropItem DropItem;

        public delegate List<Dictionary<string,string>> GetCellForRow(int row);
        public GetCellForRow GetCellRow;

        public delegate void DeleteItems(List<Cell> items);
        public event DeleteItems OnDeleteItems;

        public event RoutedEventHandler OnChangeMap;

        private double _zoomValue = 1.0;

        /// <summary>
        /// реализация масштабирования карты с использованием колеса мыши
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIElement_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                _zoomValue += 0.1;
            }
            else
            {
                if(_zoomValue>0.2)
                    _zoomValue -= 0.1;
            }

            ScaleTransform scale = new ScaleTransform(_zoomValue, _zoomValue);

            PanelCanvas.LayoutTransform = scale;

            e.Handled = true;
        }

        private void Map_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PanelCanvas.Width = ActualWidth;
            PanelCanvas.Height = ActualHeight;
        }

        
        //protected override void OnDragOver(DragEventArgs e)
        //{
        //    base.OnDragOver(e);
        //}

        private bool ContainsCell(string storageId)
        {
            bool result = false;

            foreach(var cell in PanelCanvas.Children)
            {
                if(cell is Cell Cell)
                {
                    if(Cell.ID == storageId)
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Реализация перемещения данных из грида в редактор
        /// позволяет перетаскивать ячейку в редактор
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            ClearSelectStatus();

            if (e.Data.GetDataPresent("DragSource"))
            {
                object data = e.Data.GetData("DragSource");

                if (data is System.Windows.Controls.DataGridRow row)
                {
                    if (row.Item is Dictionary<string, string> selectedItem)
                    {
                        var position = e.GetPosition(PanelCanvas);

                        if (!selectedItem.ContainsKey("WMST_ID"))
                        {
                            /// добавляется целый ряд, необходимо получить все ячейки в этом ряде 
                            /// сравнить с уже добавленными
                            int rowId = selectedItem.CheckGet("WMRO_ID").ToInt();

                            if (rowId > 0)
                            {
                                if (GetCellRow != null)
                                {
                                    var list = GetCellRow(rowId);

                                    int sqrt = (int)Math.Sqrt(list.Count);

                                    int i = 0;
                                    int j = 0;

                                    foreach (var cell in list)
                                    {
                                        if (!ContainsCell(cell.CheckGet("WMST_ID").ToInt().ToString()))
                                        {
                                            if (i > sqrt)
                                            {
                                                j++;
                                                i = 0;
                                            }

                                            AddCell(cell, position.X + j * Cell.DefaultWidth, position.Y + i * Cell.DefaultHeight, true);

                                            i++;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            AddCell(selectedItem, position.X, position.Y);
                        }
                    }
                }
            }

            OnChangeMap?.Invoke(this, e);
        }



        /// <summary>
        /// асинхронная загрузка данных о хранилище для зоны
        /// </summary>
        /// <param name="zone"></param>
        /// <returns></returns>
        private static Task<Dictionary<int, Dictionary<string, string>>> LoadZoneInformarion(int zone)
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                var mapZoneData = new Dictionary<int, Dictionary<string, string>>();

                var p = new Dictionary<string, string>() { { "WMZO_ID", zone.ToString() } };

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Warehouse");
                q.Request.SetParam("Object", "Storage");
                q.Request.SetParam("Action", "ListByZone");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var data = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (data != null)
                    {
                        var ds = ListDataSet.Create(data, "ITEMS");

                        foreach (var item in ds.Items)
                        {
                            int storageId = item.CheckGet("WMST_ID").ToInt();
                            mapZoneData[storageId] = item;
                        }
                    }
                }

                return mapZoneData;
            }
            );
        }

     
        /// <summary>
        /// Добавление ячейки
        /// </summary>
        /// <param name="selectedItem">данные ячейки</param>
        /// <param name="x">координаты</param>
        /// <param name="y">координаты</param>
        /// <param name="selected">сделать ее выбранной</param>
        private void AddCell(Dictionary<string,string> selectedItem, double x, double y, bool selected = false)
        {
            var cell = new Cell(this, ZoneInfo);
            cell.SetData(selectedItem.CheckGet("NUM"), selectedItem.CheckGet("WMST_ID").ToInt());
            cell.Width = Cell.DefaultWidth;
            cell.Height = Cell.DefaultHeight;
            Canvas.SetLeft(cell, x);
            Canvas.SetTop(cell, y);

            PanelCanvas.Children.Add(cell);

            if(selected)
            {
                cell.IsSelected = true;
            }

            DropItem?.Invoke(this, selectedItem);
        }

        /// <summary>
        /// Получение данных редактора для последующего сохранения
        /// </summary>
        /// <returns></returns>
        public string GetData()
        {
            XmlDocument document = new XmlDocument();

            XmlNode root = document.AppendChild(document.CreateElement("Cells"));

            foreach (var item in PanelCanvas.Children)
            {
                if (item is Cell cell)
                {
                    XmlNode xitem = document.CreateElement("Item");
                    root.AppendChild(xitem);

                    double y = Canvas.GetTop(cell);
                    double x = Canvas.GetLeft(cell);

                    double top = y + cell.offsetY;
                    double left = x + cell.offsetX;

                    xitem.Attributes.Append(document.CreateAttribute("ID")).Value = cell.ID;
                    xitem.Attributes.Append(document.CreateAttribute("NUM")).Value = cell.Num;

                    xitem.Attributes.Append(document.CreateAttribute("X")).Value = ((int)left).ToString();
                    xitem.Attributes.Append(document.CreateAttribute("Y")).Value = ((int)top).ToString();

                    //xitem.Attributes.Append(document.CreateAttribute("X")).Value = ((int)-relativePoint.X).ToString();
                    //xitem.Attributes.Append(document.CreateAttribute("Y")).Value = ((int)-relativePoint.Y).ToString();

                    xitem.Attributes.Append(document.CreateAttribute("W")).Value = ((int)cell.Width).ToString();
                    xitem.Attributes.Append(document.CreateAttribute("H")).Value = ((int)cell.Height).ToString();
                }
                else if(item is MapBackground background)
                {
                    XmlNode xbackground = document.CreateElement("Background");
                    root.AppendChild(xbackground);

                    xbackground.Attributes.Append(document.CreateAttribute("Data")).Value = background.GetImageData();
                    xbackground.Attributes.Append(document.CreateAttribute("W")).Value = ((int)background.Width).ToString();
                    xbackground.Attributes.Append(document.CreateAttribute("H")).Value = ((int)background.Height).ToString();
                }
            }

            return document.OuterXml;
        }

        /// <summary>
        /// загрузка данных редактора из xml
        /// </summary>
        /// <param name="data">xml данных</param>
        /// <param name="zoneId">id зоны</param>
        public void SetData(string data, int zoneId)
        {
            ZoneInfo = LoadZoneInformarion(zoneId);

            if (data != string.Empty)
            {
                PanelCanvas.Children.Clear();

                XmlDocument document = new XmlDocument();

                document.LoadXml(data);

                XmlNodeList background = document.SelectNodes("/Cells/Background");
                foreach (XmlNode node in background)
                {
                    mapBackground = new MapBackground(this);

                    int W = Convert.ToInt32(node.Attributes["W"].Value);
                    int H = Convert.ToInt32(node.Attributes["H"].Value);

                    mapBackground.Width = W;
                    mapBackground.Height = H;

                    Width = W;
                    Height = H;

                    Canvas.SetLeft(mapBackground, 0);
                    Canvas.SetTop(mapBackground, 0);

                    mapBackground.SetImageData(node.Attributes["Data"].Value);

                    PanelCanvas.Children.Add(mapBackground);

                    mapBackground.OnMoveBackground += MapBackground_OnMoveBackground;
                    mapBackground.OnSizeBackground += MapBackground_OnSizeBackground;

                }

                XmlNodeList cells = document.SelectNodes("/Cells/Item");

                foreach (XmlNode node in cells)
                {
                    try
                    {
                        string id = node.Attributes["ID"].Value;
                        string Num = node.Attributes["NUM"].Value;
                        //if (Cells.ContainsKey(key))
                        {
                            int X = Convert.ToInt32(node.Attributes["X"].Value);
                            int Y = Convert.ToInt32(node.Attributes["Y"].Value);

                            int W = Convert.ToInt32(node.Attributes["W"].Value);
                            int H = Convert.ToInt32(node.Attributes["H"].Value);

                            if (X < 0) X = 0;
                            if(Y<0) Y = 0;
                            if (X > Width) X = (int)Width;
                            if(Y> Height) Y = (int)Height;

                            var cell = new Cell(this, ZoneInfo);
                            //cell.ID = id;
                            cell.SetData(Num, Convert.ToInt32(id));

                            Canvas.SetLeft(cell, X);
                            Canvas.SetTop(cell, Y);

                            if (W > 0) cell.Width = W;
                            if (H > 0) cell.Height = H;

                            PanelCanvas.Children.Add(cell);
                        }
                    }
                    catch
                    {

                    }

                }
            }
        }

        /// <summary>
        /// реализация выделения нескольких ячеек, попадаюзих в рамку выделения
        /// </summary>
        private System.Windows.Shapes.Rectangle _selectBox;
        internal System.Windows.Shapes.Rectangle GetSelectBox()
        {
            if(_selectBox == null )
            {
                _selectBox = new System.Windows.Shapes.Rectangle();
                _selectBox.StrokeThickness = 1;
                _selectBox.StrokeDashArray = new DoubleCollection { 1, 0, 1 };
                _selectBox.Stroke = HColor.BlackFG.ToBrush();
                PanelCanvas.Children.Add(_selectBox);

                _selectBox.SizeChanged += _selectBox_SizeChanged;
            }

            return _selectBox;
        }

        /// <summary>
        /// Изменени размеров рамки выделения приводит к пересчету ячеек порпдпющих внутрь выделенной области
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _selectBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // тут необходимо выбрать все ячейки которые попадают в область выделения

            double btop = Canvas.GetTop(_selectBox);
            double bleft = Canvas.GetLeft(_selectBox);

            double bbottom = btop + _selectBox.Height;
            double bright = bleft + _selectBox.Width;

            if(Math.Abs(_selectBox.Height)>100)
            {
                Console.WriteLine();
            }

            foreach (var child in PanelCanvas.Children)
            {
                if(child is Cell)
                {
                    var cell = (Cell)child;

                    double top = Canvas.GetTop(cell) + cell.offsetY;
                    double left = Canvas.GetLeft(cell) + cell.offsetX;

                    double bottom = top + cell.Height;
                    double right = left + cell.Width;

                    if(btop<=top && bleft<= left &&
                        bbottom>=bottom && bright>=right)
                    {
                        cell.IsSelected = true;
                    }
                    else
                    {
                        cell.IsSelected = false;
                    }
                }
            }
        }

        /// <summary>
        /// удаление рамки выделения
        /// </summary>
        /// <param name="mouseDown"></param>
        internal void ClearSelectBox(System.Windows.Point mouseDown)
        {
            if(_selectBox != null)
            {

                //foreach (var item in PanelCanvas.Children)
                //{
                //    if (item is Cell cell)
                //    {
                //        double top = Canvas.GetTop(cell) + cell.offsetY;
                //        double left = Canvas.GetLeft(cell) + cell.offsetX;
                //    }
                //}

                PanelCanvas.Children.Remove( _selectBox );
                _selectBox = null;
            }
        }

        /// <summary>
        /// Операция перемещения выделенных ячеек 
        /// Если двигаем ячейку у которой был статус выделена
        /// то если есть ячейки с тем же статусом
        /// их необходимо двигать на тоже смещение
        /// </summary>
        /// <param name="instigator">выделенная ячейка которую двигает пользователь</param>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        internal void MoveSelected(Cell instigator, double offsetX, double offsetY)
        {
            if (Math.Abs(offsetX) > 0 && Math.Abs(offsetY) > 0)
            {
                foreach (var item in PanelCanvas.Children)
                {
                    if (item is Cell cell)
                    {
                        if (cell.IsSelected)
                        {
                            if (cell != instigator)
                            {
                                cell.MoveSelected(offsetX, offsetY);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Функуия группового изменения размера ячеек
        /// </summary>
        /// <param name="instigator"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        internal void ResizeSelected(Cell instigator, double width, double height)
        {
            bool resume = false;
            foreach (var item in PanelCanvas.Children)
            {
                if (item is Cell cell)
                {
                    if (cell.IsSelected)
                    {
                        if (cell != instigator)
                        {
                            //cell.MoveSelected(offsetX, offsetY);
                            cell.Width = width;
                            cell.Height = height;
                            resume = true;
                        }
                    }
                }
            }

            if (resume)
            {
                OnChangeMap?.Invoke(this, null);
            }
        }

        /// <summary>
        /// Событие по заверщению движения ячейки
        /// </summary>
        /// <param name="instigator"></param>
        internal void MoveDone(Cell instigator)
        {
            OnChangeMap?.Invoke(this, null);
        }

        /// <summary>
        /// Удалить статус выделения на всех выделенных ячейках
        /// </summary>
        public void ClearSelectStatus()
        {
            foreach (var child in PanelCanvas.Children)
            {
                if (child is Cell)
                {
                    var cell = (Cell)child;

                    if (cell.IsSelected)
                    {
                        cell.IsSelected = false;
                    }
                }
            }
        }

        /// <summary>
        /// Удалить выделенные ячейки
        /// </summary>
        internal void DeleteSelected()
        {
            List<Cell> selectedList = new List<Cell>();
            foreach (var child in PanelCanvas.Children)
            {
                if (child is Cell)
                {
                    var cell = (Cell)child;

                    if(cell.IsSelected)
                    {
                        selectedList.Add(cell);
                    }
                }
            }

            foreach(var item in selectedList)
            {
                PanelCanvas.Children.Remove(item);
            }

            OnDeleteItems?.Invoke(selectedList);
        }

        /// <summary>
        /// Тестовая фкнкция позволяет выровнять выделенные ячейки в квадрат
        /// </summary>
        internal void LineSelected()
        {
            double minimalX = double.NaN;
            double minimalY = double.NaN;
            double maximalX = double.NaN;
            double maximalY = double.NaN;

            double MaxWidth = 0.0;
            double MaxHeight = 0.0;

            List<Cell> selectedList = new List<Cell>();

            foreach (var item in PanelCanvas.Children)
            {
                if (item is Cell cell)
                {
                    if (cell.IsSelected)
                    {
                        selectedList.Add(cell);

                        double y = Canvas.GetTop(cell);
                        double x = Canvas.GetLeft(cell);

                        double top = y + cell.offsetY;
                        double left = x + cell.offsetX;

                        MaxWidth += cell.Width;
                        MaxHeight += cell.Height;

                        if (double.IsNaN(minimalX))
                        {
                            minimalX = left;
                            minimalY = top;
                            maximalX = left;
                            maximalY = top;
                        }
                        else
                        {
                            if (minimalX > left) minimalX = left;
                            if (minimalY > top) minimalY = top;
                            if (maximalX < left) maximalX = left;
                            if (maximalY < top) maximalY = top;
                        }
                    }
                }
            }

            if (selectedList.Count > 0)
            {
                double AvgWidth = MaxWidth / selectedList.Count;
                double AvgHeight = MaxHeight / selectedList.Count;

                selectedList.Sort((x,y)=>x.Num.CompareTo(y.Num));

                int square = (int)Math.Sqrt(selectedList.Count);

                int i, j;

                i = 0; j = 0;

                foreach (var item in selectedList)
                {
                    if(i>square)
                    {
                        i = 0; j++;
                    }

                    double cX = minimalX + AvgWidth * i;
                    double cY = minimalY + AvgHeight * j;

                    item.SetCoord(cX, cY, AvgWidth, AvgHeight);

                    i++;

                    
                }
            }
        }

        internal void DeleteBackground()
        {
            if(mapBackground!=null)
            {
                mapBackground.Clear();
            }
        }
    }
}
