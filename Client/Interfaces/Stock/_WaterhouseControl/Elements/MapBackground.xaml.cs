using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Контрол для фонового изображения
    /// Поведение схоже с ячейкой Cell
    /// <author>eletkikh_ya</author>
    /// </summary>
    public partial class MapBackground : UserControl
    {
        private MapZone OwnerMap;

        public MapBackground(MapZone map)
        {
            InitializeComponent();

            OwnerMap = map;
          
            SizeChanged += RowButton_SizeChanged;

            PreviewMouseDown += Row_PreviewMouseDown;
            PreviewMouseUp += Row_PreviewMouseUp;
            PreviewMouseMove += Row_PreviewMouseMove;
            MouseLeave += RowButton_MouseLeave;
            MouseEnter += RowButton_MouseEnter;

            MouseDoubleClick += MapBackground_MouseDoubleClick;
        }

        private void MapBackground_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //throw new NotImplementedException();

            OpenFileDialog dlg = new OpenFileDialog();
            if(dlg.ShowDialog()==true)
            {
                try
                {
                    BodyImage.Source = new BitmapImage(new Uri(dlg.FileName));
                }
                catch
                {
                    // Возможно это даже не картинка, поэтому для пердотвращения остановки программы, необходимо обработать все варианты неправильной загрузки данных
                    var d = new DialogWindow($"Данный файл не подходит", "Ошибка загрузки картинки", "", DialogWindowButtons.OK);
                    d.ShowDialog();
                }
            }
        }

        public delegate void MoveBackground(double x, double y);
        public event MoveBackground OnMoveBackground;

        public event MoveBackground OnSizeBackground;

        private bool EnableMove = false;

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

        public event RoutedEventHandler Click;
        public event RoutedEventHandler ReallocComplete;


        private System.Windows.Shapes.Rectangle SelectionBox
        {
            get { 
                return OwnerMap.GetSelectBox(); 
            }    
           
        }

        private bool _isMouseDown = false;
        private bool isMouseDown
        {
            get
            {
                return _isMouseDown;
            }
            set
            {
                if(value)
                {
                    // Initial placement of the drag selection box.         
                    Canvas.SetLeft(SelectionBox, mouseDown.X);
                    Canvas.SetTop(SelectionBox, mouseDown.Y);
                    SelectionBox.Width = 0;
                    SelectionBox.Height = 0;

                    // Make the drag selection box visible.
                    SelectionBox.Visibility = Visibility.Visible;
                }
                else
                {
                    OwnerMap.ClearSelectBox(mouseDown);
                }

                _isMouseDown = value;
            }
        }

        private double TextCountFontSize = 0;
        private double TextNameFontSize = 0;

        private System.Windows.Point _positionInBlock;
        private bool _IsMouseCaptured = false;


        private System.Windows.Point? _buttonPosition;
        private double deltaX;
        private double deltaY;
        private TranslateTransform _currentTT;

        private Canvas RowOwner;
        private System.Windows.Point mouseDown;
        private System.Windows.Point oldSize = new System.Windows.Point();


        public double offsetX = 0.0;
        public double offsetY = 0.0;

        public bool ResizeFlag = false;

        private bool _isMoving = false;
        private bool IsMoving
        {
            get { return _isMoving; }
            set
            {
                _isMoving = value;
                if (_isMoving)
                {
                    //Button.Background = HColor.Gray.ToBrush();
                }
                else
                {
                    //Button.Background = "#FFECF0F1".ToBrush();
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

            isMouseDown = false;

            ResizeFlag = false;
            Mouse.OverrideCursor = null;
            _currentTT = Button.RenderTransform as TranslateTransform;
            IsMoving = false;
        }


        private void RowButton_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }

        private void Row_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // get the parent container
            RowOwner = VisualTreeHelper.GetParent(this) as Canvas;

            if (_buttonPosition == null)
                _buttonPosition = Button.TransformToAncestor(RowOwner).Transform(new System.Windows.Point(0, 0));

            mouseDown = Mouse.GetPosition(RowOwner);
            oldSize.X = Width;
            oldSize.Y = Height;
            deltaX = mouseDown.X - _buttonPosition.Value.X;
            deltaY = mouseDown.Y - _buttonPosition.Value.Y;
            IsMoving = true;
            isMouseDown = true;
        }

        private void Row_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = false;

            if (RowOwner != null)
            {
                if (IsMoving || ResizeFlag) ReallocComplete?.Invoke(this, e);

                _currentTT = Button.RenderTransform as TranslateTransform;
                IsMoving = false;

                if (_buttonPosition == null)
                    _buttonPosition = Button.TransformToAncestor(RowOwner).Transform(new System.Windows.Point(0, 0));

                var mousePoint = Mouse.GetPosition(RowOwner);

                mouseDown = Mouse.GetPosition(RowOwner);
                deltaX = mouseDown.X - _buttonPosition.Value.X;
                deltaY = mouseDown.Y - _buttonPosition.Value.Y;

                offsetX = (_currentTT == null ? _buttonPosition.Value.X : _buttonPosition.Value.X - _currentTT.X) + deltaX - mousePoint.X;
                offsetY = (_currentTT == null ? _buttonPosition.Value.Y : _buttonPosition.Value.Y - _currentTT.Y) + deltaY - mousePoint.Y;
            }
        }

        private static BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        public void SetImageData(string base64)
        {
            BodyImage.Source = LoadImage(Convert.FromBase64String(base64));
        }

        public string GetImageData()
        {
            string res = string.Empty;

            if (BodyImage.Source is BitmapImage bitmapImage)
            {
                ImageConverter converter = new ImageConverter();
                byte[] uncompressedFile;// = (byte[])converter.ConvertTo(img, typeof(byte[]));

                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));

                using (MemoryStream ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    uncompressedFile = ms.ToArray();

                    if (uncompressedFile != null)
                    {
                        res = Convert.ToBase64String(uncompressedFile);
                    }
                }
            }

            return res;
        }


        private void Row_PreviewMouseMove(object sender, MouseEventArgs e)
        {
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

                if (Dx < 200 && Dy < 200)
                {

                    Width = oldSize.X - Dx;
                    Height = oldSize.Y - Dy;

                    OnSizeBackground?.Invoke(Width, Height);
                }
            }
            else
            {
                if (EnableMove)
                {
                    this.Button.RenderTransform = new TranslateTransform(-offsetX, -offsetY);

                    OnMoveBackground?.Invoke(-offsetX, -offsetY);
                }
            }

            if(isMouseDown)
            {
                if (mouseDown.X < mousePoint.X)
                {
                    Canvas.SetLeft(SelectionBox, mouseDown.X);
                    SelectionBox.Width = mousePoint.X - mouseDown.X;
                }
                else
                {
                    Canvas.SetLeft(SelectionBox, mousePoint.X);
                    SelectionBox.Width = mouseDown.X - mousePoint.X;
                }

                if (mouseDown.Y < mousePoint.Y)
                {
                    Canvas.SetTop(SelectionBox, mouseDown.Y);
                    SelectionBox.Height = mousePoint.Y - mouseDown.Y;
                }
                else
                {
                    Canvas.SetTop(SelectionBox, mousePoint.Y);
                    SelectionBox.Height = mouseDown.Y - mousePoint.Y;
                }
            }
        }

        internal void Clear()
        {
            if(BodyImage!=null)
            {
                BodyImage.Source = null;
            }
        }
    }
}
