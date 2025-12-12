using Client.Common;
using Client.Common.Reporter;
using Client.Interfaces.Main;
using DevExpress.Xpf.Core.Serialization;
using Newtonsoft.Json;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Threading;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using static NPOI.HSSF.Util.HSSFColor;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Логика взаимодействия для CellVisualizationBuffer.xaml
    /// </summary>
    public partial class CellVisualizationBuffer : ControlBase
    {
        // TODO
        // расчитывать размер минимального уменьшения изображения (зум или шаг сетки) через проверку на то, что ни один элемент не получит значение вирины или высоты = 0
        // баг очистки существующих элементов при отмене создания поддона

        public CellVisualizationBuffer()
        {
            ControlTitle = "Визуализация буфера";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]cell_visualization";
            InitializeComponent();

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            //конструктор, будет вызван, когда объект создается
            //здесь создаются все внутренние структуры
            //впервые этот коллбэк будет вызван, когда данный таб станет активным
            //впервые (до этих пор, никакая работа внутри не происходит, что экономит ресурсы)
            OnLoad = () =>
            {
                SetDefaults();
                PreLoadItems();
                RenderItems();
                RunAutoUpdateTimer();
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
                if (AutoUpdateTimer != null)
                {
                    AutoUpdateTimer.Stop();
                }
            };

            // получение фокуса, таб стал активным
            //включаются механизмы автообновления данных
            OnFocusGot = () =>
            {
            };

            //потеря фокуса
            //отключаются все механизмы
            OnFocusLost = () =>
            {
            };
        }

        public WriteableBitmap GridImgWB { get; set; }

        public WriteableBitmap wb { get; set; }

        public List<Block> Blocks { get; set; }

        public static Random Random = new Random();

        public ToolTip ImageToolTip { get; set; }

        private int _imgWidth { get; set; }
        public int ImgWidth 
        {
            get { return _imgWidth; }
            set 
            { 
                _imgWidth = value; 
                Img.Width = value; 
                //ImgBorder.Width = value; 
                //BackgorundImgBorder.Width = value; 
                BackgorundImg.Width = value; 
                //GridImgBorder.Width = value;
                GridImg.Width = value;
            }
        }

        private int _imgHeight { get; set; }
        public int ImgHeight
        {
            get { return _imgHeight; }
            set 
            {
                _imgHeight = value; 
                Img.Height = value; 
                //ImgBorder.Height = value; 
                //BackgorundImgBorder.Height = value; 
                BackgorundImg.Height = value; 
                //GridImgBorder.Height = value;
                GridImg.Height = value;
            }
        }

        public bool ShowGridFlag = false;


        /// <summary>
        /// Размер сетки грида
        /// </summary>
        public static int GridStep = 11;

        public bool AddCellMode {  get; set; }

        /// <summary>
        /// Таймер получения данных по текущему производственному заданию
        /// </summary>
        public DispatcherTimer AutoUpdateTimer { get; set; }

        /// <summary>
        /// интервал обновления таймера (сек)
        /// </summary>
        public int AutoUpdateTimerInterval { get; set; }

        public static string GetEntranceSideName(int entranceSide)
        {
            string entranceSideName = "";

            // 1 -- Слева;
            // 2 -- Сверху;
            // 3 -- Справа;
            // 4 -- Снизу;
            switch (entranceSide)
            {
                case 1:
                    entranceSideName = "Слева";
                    break;

                case 2:
                    entranceSideName = "Сверху";
                    break;

                case 3:
                    entranceSideName = "Справа";
                    break;

                case 4:
                    entranceSideName = "Снизу";
                    break;

                default:
                    break;
            }

            return entranceSideName;
        }

        public static Dictionary<int, Rgba32> ColorDictionary;

        public static void CreateColorDictionary()
        {
            ColorDictionary = new Dictionary<int, Rgba32>();
            ColorDictionary.Add(0, new Rgba32(214, 252, 213, 255)); //(c/уп)салатовый

            //// синие
            //ColorDictionary.Add(1, new Rgba32(181, 242, 234, 255)); //Аквамарин
            //ColorDictionary.Add(5, new Rgba32(231, 236, 255, 255)); //Лиловый
            //ColorDictionary.Add(9, new Rgba32(222, 247, 254, 255)); //Голубой 
            //ColorDictionary.Add(13, new Rgba32(161, 133, 148, 255)); //Пастельно-фиолетовый
            //ColorDictionary.Add(17, new Rgba32(175, 218, 252, 255)); //Синий-синий иней

            //// красные
            //ColorDictionary.Add(2, new Rgba32(254, 214, 188, 255)); //Персиковый
            //ColorDictionary.Add(6, new Rgba32(250, 218, 221, 255)); //Бледно-розовый
            //ColorDictionary.Add(10, new Rgba32(255, 189, 136, 255)); //Макароны и сыр
            //ColorDictionary.Add(14, new Rgba32(255, 155, 170, 255)); //Лососевый Крайола
            //ColorDictionary.Add(18, new Rgba32(228, 113, 122, 255)); //Карамельно-розовый

            //// зелёные
            //ColorDictionary.Add(3, new Rgba32(62, 180, 137, 255)); //Мятный
            //ColorDictionary.Add(7, new Rgba32(218, 216, 113, 255)); //Вердепешевый
            //ColorDictionary.Add(11, new Rgba32(172, 183, 142, 255)); //Болотный
            //ColorDictionary.Add(15, new Rgba32(198, 223, 144, 255)); //Очень светлый желто-зеленый
            //ColorDictionary.Add(19, new Rgba32(246, 255, 248, 255)); //Лаймовый

            //// желтые
            //ColorDictionary.Add(4, new Rgba32(255, 250, 221, 255)); //Светло-жёлтый
            //ColorDictionary.Add(8, new Rgba32(179, 159, 122, 255)); //Кофе с молоком
            //ColorDictionary.Add(12, new Rgba32(255, 207, 72, 255)); //Восход солнца
            //ColorDictionary.Add(16, new Rgba32(239, 169, 74, 255)); //Пастельно-желтый
            //ColorDictionary.Add(20, new Rgba32(230, 214, 144, 255)); //Светлая слоновая кость




            ColorDictionary.Add(1, new Rgba32(95, 134, 112, 255));
            ColorDictionary.Add(2, new Rgba32(255, 152, 0, 255));
            ColorDictionary.Add(3, new Rgba32(184, 0, 0, 255));
            //ColorDictionary.Add(4, new Rgba32(161, 0, 53, 255));
            //ColorDictionary.Add(1, new Rgba32(42, 9, 68, 255));
            //ColorDictionary.Add(2, new Rgba32(254, 194, 96, 255));
            //ColorDictionary.Add(3, new Rgba32(63, 167, 150, 255));
            //ColorDictionary.Add(4, new Rgba32(161, 0, 53, 255));



            //ColorDictionary.Add(1, new Rgba32(255, 189, 136, 255));
            //ColorDictionary.Add(2, new Rgba32(161, 133, 14, 255));
            //ColorDictionary.Add(3, new Rgba32(255, 155, 170, 255));
            //ColorDictionary.Add(4, new Rgba32(228, 113, 122, 255));
            //ColorDictionary.Add(5, new Rgba32(175, 218, 252, 255));
        }

        public static Rgba32 GetColor(int _number, int colProduction)
        {
            Rgba32 result = ColorDictionary[0];

            decimal colCollors = ColorDictionary.Count() - 1;

            int maxKoef = (int)Math.Ceiling(colProduction / colCollors);

            for (int j = 0; j < maxKoef; j++)
            {
                for (int i = 1; i < colCollors + 1; i++)
                {
                    if (i + colCollors * j == _number.ToInt())
                    {
                        result = ColorDictionary[i];
                    }
                }
            }

            return result;
        }

        public bool Initialized = false;

        public void SetDefaults()
        {
            GridStapTextBox.Text = $"{GridStep}";

            Cells = new List<Dictionary<string, string>>();
            Pallets = new List<Dictionary<string, string>>();
            AutoUpdateTimerInterval = 60;

            AddCellMode = false;
            ImageToolTip = new ToolTip();
            Blocks = new List<Block>();

            CreateColorDictionary();

            int countCellByWidth = 192;
            int countCellByHeight = 108;

            ImgWidth = countCellByWidth * GridStep;
            ImgHeight = countCellByHeight * GridStep;

            {
                // Create the bitmap, with the dimensions of the image placeholder.
                wb = new WriteableBitmap(ImgWidth, ImgHeight, 96, 96, PixelFormats.Bgra32, null);

                // Show the bitmap in an Image element.
                Img.Source = wb;
            }

            GridImgWB = new WriteableBitmap(ImgWidth, ImgHeight, 96, 96, PixelFormats.Bgra32, null);
            GridImg.Source = GridImgWB;
            RenderGrid();

            SetGridVisible();

            ImgScrollViewer.ScrollToTop();
            ImgScrollViewer.ScrollToRightEnd();

            Initialized = true;
        }

        public void RunAutoUpdateTimer()
        {
            if (AutoUpdateTimerInterval != 0)
            {
                if (AutoUpdateTimer == null)
                {
                    AutoUpdateTimer = new DispatcherTimer
                    {
                        Interval = new TimeSpan(0, 0, AutoUpdateTimerInterval)
                    };

                    AutoUpdateTimer.Tick += (s, e) =>
                    {
                        Refresh();
                    };
                }

                if (AutoUpdateTimer.IsEnabled)
                {
                    AutoUpdateTimer.Stop();
                }

                AutoUpdateTimer.Start();
            }
        }

        public void SetGridVisible()
        {
            if (ShowGridFlag)
            {
                GridImg.Visibility = Visibility.Visible;
            }
            else
            {
                GridImg.Visibility = Visibility.Hidden;
            }
        }

        public void RenderGrid()
        {
            {
                for (int i = 0; i < (int)Math.Floor((double)ImgWidth / GridStep); i++)
                {
                    SystemBlock systemBlock = new SystemBlock();
                    systemBlock.X = i;
                    systemBlock.Y = 0;
                    systemBlock.PixelX = systemBlock.X * GridStep + GridStep;
                    systemBlock.PixelY = systemBlock.Y * GridStep;
                    systemBlock.PixelWidth = 1;
                    systemBlock.PixelHeight = ImgHeight;

                    if (systemBlock.PixelX < ImgWidth)
                    {
                        RenderGridFillObject(systemBlock.PixelX, systemBlock.PixelY, systemBlock.PixelWidth, systemBlock.PixelHeight, systemBlock.Red, systemBlock.Green, systemBlock.Blue, systemBlock.Alpha);
                        systemBlock.Rendered = true;
                    }
                    else
                    {
                        systemBlock = null;
                    }
                }

                for (int i = 0; i < (int)Math.Floor((double)ImgHeight / GridStep); i++)
                {
                    SystemBlock systemBlock = new SystemBlock();
                    systemBlock.X = 0;
                    systemBlock.Y = i;
                    systemBlock.PixelX = systemBlock.X * GridStep;
                    systemBlock.PixelY = systemBlock.Y * GridStep + GridStep;
                    systemBlock.PixelWidth = ImgWidth;
                    systemBlock.PixelHeight = 1;

                    if (systemBlock.PixelY < ImgHeight)
                    {
                        RenderGridFillObject(systemBlock.PixelX, systemBlock.PixelY, systemBlock.PixelWidth, systemBlock.PixelHeight, systemBlock.Red, systemBlock.Green, systemBlock.Blue, systemBlock.Alpha);
                        systemBlock.Rendered = true;
                    }
                    else
                    {
                        systemBlock = null;
                    }
                }
            }
        }

        public void RenderGridFillObject(int startX, int startY, int width, int heigth, int red, int green, int blue, int alpha)
        {
            // Define the update square (which is as big as the entire image).
            Int32Rect rect = new Int32Rect(startX, startY, width, heigth);

            byte[] pixels = new byte[width * heigth * GridImgWB.Format.BitsPerPixel / 8];

            for (int y = 0; y < heigth; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelOffset = (x + y * width) * GridImgWB.Format.BitsPerPixel / 8;
                    pixels[pixelOffset] = (byte)blue;
                    pixels[pixelOffset + 1] = (byte)green;
                    pixels[pixelOffset + 2] = (byte)red;
                    pixels[pixelOffset + 3] = (byte)alpha;
                }

                int stride = (width * GridImgWB.Format.BitsPerPixel) / 8;

                GridImgWB.WritePixels(rect, pixels, stride, 0);
            }
        }

        public List<Dictionary<string, string>> Cells { get; set; }

        public List<Dictionary<string, string>> Pallets { get; set; }

        /// <summary>
        /// Пременная функция, чтобы не ловить ошибки из-за того, что локальный сервер сбоит
        /// </summary>
        public void PreLoadItems()
        {
            var p = new Dictionary<string, string>();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "CellVisualization");
            q.Request.SetParam("Action", "ListCell");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            Cells = new List<Dictionary<string, string>>();
            Pallets = new List<Dictionary<string, string>>();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dsCell = ListDataSet.Create(result, "CELL");
                    Cells = dsCell.Items;

                    var dsPallets = ListDataSet.Create(result, "PALLET");
                    Pallets = dsPallets.Items;
                }
            }
            else
            {
                q.ProcessError();
            }

            if (Cells != null && Cells.Count > 0)
            {
                LoadItems(Cells, Pallets);
            }
            else
            {
                PreLoadItems();
            }
        }

        public void LoadItems(List<Dictionary<string, string>> Cells, List<Dictionary<string, string>> Pallets)
        {
            foreach (var cellData in Cells)
            {
                Cell cell = new Cell();
                cell.Data = cellData;
                cell.X = cellData["LEFT"].ToInt();
                cell.Y = cellData["TOP"].ToInt();
                cell.Width = cellData["WIDTH"].ToInt();
                cell.Height = cellData["LENGTH"].ToInt();
                cell.EntranceSide = cellData["ALIGN"].ToInt();
                cell.CellId = cellData["WMVI_ID"].ToInt();
                cell.CellName = $"{cellData["SKLAD"]}{cellData["NUM_PLACE"].ToInt()}";
                cell.PixelX = cell.X * GridStep;
                cell.PixelY = cell.Y * GridStep;
                cell.PixelWidth = cell.Width * GridStep;
                cell.PixelHeight = cell.Height * GridStep;

                List<Dictionary<string, string>> palletInCellList = Pallets.Where(x => x["WMVI_ID"].ToInt() == cell.CellId).ToList();
                foreach (var palletData in palletInCellList)
                {
                    Pallet pallet = new Pallet();
                    pallet.Data = palletData;
                    pallet.PixelWidth = pallet.Width * GridStep;
                    pallet.PixelHeight = pallet.Height * GridStep;

                    cell.AddPallet(pallet);
                    Blocks.Add(pallet);
                }

                cell.PlacePallets();

                Blocks.Add(cell);
            }
        }

        public void Refresh()
        {
            Blocks.Clear();
            //RenderFillObject(0, 0, ImgWidth, ImgHeight, 0, 0, 0, 0);
            wb = null;
            wb = new WriteableBitmap(ImgWidth, ImgHeight, 96, 96, PixelFormats.Bgra32, null);
            PreLoadItems();
            RenderItems();
            Img.Source = wb;
        }

        public void RenderItems()
        {
            foreach (var block in Blocks)
            {
                if (block.BlockType == BlockType.Cell)
                {
                    RenderFillObject(block.PixelX + 1, block.PixelY + 1, block.PixelWidth - 1, block.PixelHeight - 1, block.Red, block.Green, block.Blue, block.Alpha);
                    block.Rendered = true;
                }
            }

            foreach (var block in Blocks)
            {
                if (block.BlockType == BlockType.Pallet)
                {
                    RenderFillObject(block.PixelX + 1, block.PixelY + 1, block.PixelWidth - 2, block.PixelHeight - 2, block.Red, block.Green, block.Blue, block.Alpha);
                    block.Rendered = true;

                    if (((Pallet)block).StackFlag)
                    {
                        RenderFillObject(
                              block.PixelX + 1
                            , block.PixelY + (int)Math.Ceiling((double)block.PixelHeight / 2)
                            , block.PixelWidth - 2
                            , 1
                            , 220
                            , 238
                            , 238
                            , 255
                            );
                    }
                }
            }
        }

        public void RenderItem(Block block)
        {
            if (block.BlockType == BlockType.Cell)
            {
                RenderFillObject(block.PixelX + 1, block.PixelY + 1, block.PixelWidth - 1, block.PixelHeight - 1, block.Red, block.Green, block.Blue, block.Alpha);
                block.Rendered = true;
            }
            else if (block.BlockType == BlockType.Pallet)
            {
                RenderFillObject(block.PixelX + 1, block.PixelY + 1, block.PixelWidth - 2, block.PixelHeight - 2, block.Red, block.Green, block.Blue, block.Alpha);
                block.Rendered = true;
            }
        }

        /// <summary>
        /// Отрисовка объекта, залитого одним цветом
        /// </summary>
        /// <param name="startX"></param>
        /// <param name="startY"></param>
        /// <param name="width"></param>
        /// <param name="heigth"></param>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        /// <param name="alpha"></param>
        public void RenderFillObject(int startX, int startY, int width, int heigth, int red, int green, int blue, int alpha)
        {
            // Define the update square (which is as big as the entire image).
            Int32Rect rect = new Int32Rect(startX, startY, width, heigth);

            byte[] pixels = new byte[width * heigth * wb.Format.BitsPerPixel / 8];

            for (int y = 0; y < heigth; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelOffset = (x + y * width) * wb.Format.BitsPerPixel / 8;
                    pixels[pixelOffset] = (byte)blue;
                    pixels[pixelOffset + 1] = (byte)green;
                    pixels[pixelOffset + 2] = (byte)red;
                    pixels[pixelOffset + 3] = (byte)alpha;
                }

                int stride = (width * wb.Format.BitsPerPixel) / 8;

                wb.WritePixels(rect, pixels, stride, 0);
            }
        }

        public enum BlockType
        {
            Cell,
            Pallet,
            SystemBlock
        }

        public abstract class Block
        {
            public Block()
            {
                Rendered = false;
                ObjectId = Random.Next();
                Data = new Dictionary<string, string>();
            }

            /// <summary>
            /// Текущее положение X координаты в пикселях
            /// </summary>
            public int PixelX;

            /// <summary>
            /// Текущее положение Y координаты в пикселях
            /// </summary>
            public int PixelY;

            /// <summary>
            /// Ширина объекта в пикселях на картинке
            /// </summary>
            public int PixelWidth;

            /// <summary>
            /// Длина объекта в пикселях на картинке
            /// </summary>
            public int PixelHeight;

            /// <summary>
            /// Текущее положение X координаты в у.е.
            /// </summary>
            public int X;

            /// <summary>
            /// Текущее положение Y координаты в у.е.
            /// </summary>
            public int Y;

            /// <summary>
            /// Ширина объекта в у.е. на картинке
            /// </summary>
            public int Width;

            /// <summary>
            /// Длина объекта в у.е. на картинке
            /// </summary>
            public int Height;

            /// <summary>
            /// Условный индекс объекта по оси Z. Используется для определения приоритета при клике мышкой на карту
            /// </summary>
            public int ZIndex;

            /// <summary>
            /// Вид объекта
            /// </summary>
            public BlockType BlockType;

            /// <summary>
            /// Флаг того, что объект отрисован
            /// </summary>
            public bool Rendered;

            /// <summary>
            /// Значение красного цвета при отрисовке
            /// </summary>
            public int Red;

            /// <summary>
            /// Значение зелёного цвета при отрисовке
            /// </summary>
            public int Green;

            /// <summary>
            /// Значение синего цвета при отрисовке
            /// </summary>
            public int Blue;

            /// <summary>
            /// Значение прозрачности при отрисовке
            /// </summary>
            public int Alpha;

            /// <summary>
            /// Случайное число для идентификации объекта
            /// </summary>
            public int ObjectId;

            /// <summary>
            /// Данные по объекту из БД
            /// </summary>
            public Dictionary<string, string> Data;

            public abstract string GetDataText();
        }

        public class Cell : Block
        {
            public Cell() : base()
            {
                this.BlockType = BlockType.Cell;
                this.ZIndex = 0;
                this.Alpha = 205;

                // ++
                // this.Red = 58;
                // this.Green = 96;
                // this.Blue = 123;

                //this.Red = 255;
                //this.Green = 237;
                //this.Blue = 202;

                //this.Red = 255; 
                //this.Green = 236; 
                //this.Blue = 255;

                //this.Red = 153;
                //this.Green = 204;
                //this.Blue = 204;

                //this.Red = 153;
                //this.Green = 204;
                //this.Blue = 204;

                //this.Red = 255;
                //this.Green = 191;
                //this.Blue = 64;

                //this.Red = 223;
                //this.Green = 201;
                //this.Blue = 154;

                this.Red = 220;
                this.Green = 238;
                this.Blue = 238;


                this.Pallets = new List<Pallet>();
            }

            /// <summary>
            /// Ид ячейки из БД
            /// </summary>
            public int CellId;

            /// <summary>
            /// Наименование ячейки из БД
            /// </summary>
            public string CellName;

            /// <summary>
            /// 1 -- Слева;
            /// 2 -- Сверху;
            /// 3 -- Справа;
            /// 4 -- Снизу;
            /// </summary>
            public int EntranceSide;

            /// <summary>
            /// Список поддонов в ячейке
            /// </summary>
            public List<Pallet> Pallets;

            public override string GetDataText()
            {
                string text = $"Ячейка: {CellName}{Environment.NewLine}" +
                    $"Положение поддонов: {GetEntranceSideName(this.EntranceSide)}{Environment.NewLine}" +
                    $"ID: {this.CellId}";

                if (Central.DebugMode)
                {
                    text = $"{text}{Environment.NewLine}" +
                        $"X {this.X} Y {this.Y}{Environment.NewLine}" +
                        $"WIDTH {this.Width} HEIGHT {this.Height}{Environment.NewLine}" +
                        $"Объект {ObjectId}";
                }

                return text;
            }

            public void AddPallet(Pallet pallet)
            {
                pallet.CellId = this.CellId;
                pallet.CellName = this.CellName;
                Pallets.Add(pallet);
            }

            public void PlacePallets()
            {
                this.Pallets = Pallets.OrderBy(x => x.Data.CheckGet("PRODUCT_NAME")).ToList();

                List<string> productList = new List<string>();
                foreach (var pallet in Pallets)
                {
                    if (!productList.Contains(pallet.Data.CheckGet("PRODUCT_NAME")))
                    {
                        productList.Add(pallet.Data.CheckGet("PRODUCT_NAME"));
                    }
                }

                // Проверяем, если все поддоны помещаются в один слой, то распологаем поддоны в один слой
                if (this.Width * this.Height >= Pallets.Count)
                {
                    Pallet lastPlacedPallet = null;

                    int colorIterator = 0;
                    foreach (var pallet in Pallets)
                    {
                        if (lastPlacedPallet == null
                            || pallet.Data["PRODUCT_NAME"] != lastPlacedPallet.Data["PRODUCT_NAME"])
                        {
                            colorIterator++;
                        }

                        var color = GetColor(colorIterator, productList.Count);
                        pallet.SetColor(color);

                        switch (EntranceSide)
                        {
                            // снизу
                            case 4:
                                {
                                    if (lastPlacedPallet == null)
                                    {
                                        pallet.X = this.X;
                                        pallet.Y = this.Y;
                                    }
                                    else
                                    {
                                        // Если размещаемый поддон помещается справа от последнего размещённого поддона
                                        if (lastPlacedPallet.X + lastPlacedPallet.Width + pallet.Width <= this.X + this.Width)
                                        {
                                            // то размещаем новый поддон на том же уровне по Y, что и предыдущий, правее от него
                                            pallet.Y = lastPlacedPallet.Y;
                                            pallet.X = lastPlacedPallet.X + lastPlacedPallet.Width;
                                        }
                                        // Если размещаемый поддон не помещается справа и помещается снизу, то  начинаем размещать поддоны ниже последнего
                                        else if (lastPlacedPallet.Y + lastPlacedPallet.Height + pallet.Height <= this.Y + this.Height)
                                        {
                                            // то размещаем новый поддон у левого края ячейки ниже текущего последнего поддона
                                            pallet.X = this.X;
                                            pallet.Y = lastPlacedPallet.Y + lastPlacedPallet.Height;
                                        }
                                    }
                                }
                                break;

                            // слева
                            case 1:
                                {
                                    if (lastPlacedPallet == null)
                                    {
                                        pallet.X = this.X;
                                        pallet.Y = this.Y + this.Height - pallet.Height;
                                    }
                                    else
                                    {
                                        // Если поддон помещается над последним размещённым поддоном
                                        if (lastPlacedPallet.Y - pallet.Height >= this.Y)
                                        {
                                            // то размещаем новый поддон на том же уровне по X, что и предыдущий, выше от него
                                            pallet.Y = lastPlacedPallet.Y - pallet.Height;
                                            pallet.X = lastPlacedPallet.X;
                                        }
                                        // Если размещаемый поддон не помещается сверху и помещается справа, то  начинаем размещать поддоны правее последнего
                                        else if (lastPlacedPallet.X + lastPlacedPallet.Width + pallet.Width <= this.X + this.Width)
                                        {
                                            // то размещаем новый поддон у левого края ячейки ниже текущего последнего поддона
                                            pallet.X = lastPlacedPallet.X + lastPlacedPallet.Width;
                                            pallet.Y = this.Y + this.Height - pallet.Height;
                                        }
                                    }
                                }
                                break;

                            // сверху
                            case 2:
                                {
                                    if (lastPlacedPallet == null)
                                    {
                                        pallet.X = this.X + this.Width - pallet.Width;
                                        pallet.Y = this.Y + this.Height - pallet.Height;
                                    }
                                    else
                                    {
                                        // если новый поддон помещается слева от последнего размещённого поддона
                                        if (lastPlacedPallet.X - pallet.Width >= this.X)
                                        {
                                            // то размещаем новый поддон на том же уровне по Y, что и предыдущий, левее от него
                                            pallet.Y = lastPlacedPallet.Y;
                                            pallet.X = lastPlacedPallet.X - pallet.Width;
                                        }
                                        // если новый поддон не помещается левее последнего размещённого и помещается выше него
                                        else if (lastPlacedPallet.Y - pallet.Height >= this.Y)
                                        {
                                            // то размещаем новый поддон у правого края ячейки выше текущего последнего поддона
                                            pallet.X = this.X + this.Width - pallet.Width;
                                            pallet.Y = lastPlacedPallet.Y - pallet.Height;
                                        }
                                    }
                                }
                                break;

                            // справа
                            case 3:
                                {
                                    if (lastPlacedPallet == null)
                                    {
                                        pallet.X = this.X + this.Width - pallet.Width;
                                        pallet.Y = this.Y;
                                    }
                                    else
                                    {
                                        // Если размещаемый поддон помещается ниже от последнего размещённого поддона
                                        if (lastPlacedPallet.Y + lastPlacedPallet.Height + pallet.Height <= this.Y + this.Height)
                                        {
                                            // то размещаем новый поддон на том же уровне по X, что и предыдущий, ниже от него
                                            pallet.Y = lastPlacedPallet.Y + lastPlacedPallet.Height;
                                            pallet.X = lastPlacedPallet.X;
                                        }
                                        // поддон не помещается под последним размещённым и помещается левее от него
                                        else if (lastPlacedPallet.X - pallet.Width >= this.X)
                                        {
                                            // то размещаем новый поддон у верхнего края ячейки левее текущего последнего поддона
                                            pallet.X = lastPlacedPallet.X - pallet.Width;
                                            pallet.Y = this.Y;
                                        }
                                    }
                                }
                                break;

                            default:
                                break;
                        }

                        pallet.PixelX = pallet.X * GridStep;
                        pallet.PixelY = pallet.Y * GridStep;

                        lastPlacedPallet = pallet;
                    }
                }
                // Если поддоны не помещаются в один слой
                else
                {
                    // В зависимости от положения загрузки ячейки высчитываем, сколько поддонов в один ряд не помещается

                    // количество не вместившихся в один слой поддонов
                    int nonPlacedPalletCount = Pallets.Count - (this.Width * this.Height);

                    // Количество не вместившихся рядов поддонов
                    int nonPlacedPalletRowCount = 0;

                    // Количество слоёв, которым будем укладывать не вместившиеся ряды
                    int levelCount = 1;

                    switch (EntranceSide)
                    {
                        // снизу
                        case 4:
                            {
                                nonPlacedPalletRowCount = (int)Math.Ceiling((double)nonPlacedPalletCount / this.Width);
                                levelCount = levelCount + (int)Math.Ceiling((double)nonPlacedPalletRowCount / this.Height);
                            }
                            break;

                        // слева
                        case 1:
                            {
                                nonPlacedPalletRowCount = (int)Math.Ceiling((double)nonPlacedPalletCount / this.Height);
                                levelCount = levelCount + (int)Math.Ceiling((double)nonPlacedPalletRowCount / this.Width);
                            }
                            break;

                        // сверху
                        case 2:
                            {
                                nonPlacedPalletRowCount = (int)Math.Ceiling((double)nonPlacedPalletCount / this.Width);
                                levelCount = levelCount + (int)Math.Ceiling((double)nonPlacedPalletRowCount / this.Height);
                            }
                            break;

                        // справа
                        case 3:
                            {
                                nonPlacedPalletRowCount = (int)Math.Ceiling((double)nonPlacedPalletCount / this.Height);
                                levelCount = levelCount + (int)Math.Ceiling((double)nonPlacedPalletRowCount / this.Width);
                            }
                            break;

                        default:
                            break;
                    }

                    Pallet lastPlacedPallet = null;

                    // количество поддонов с одинаковыми координатами
                    int palletInCoordinateCount = 0;

                    int colorIterator = 0;
                    foreach (var pallet in Pallets)
                    {
                        if (lastPlacedPallet == null
                            || pallet.Data["PRODUCT_NAME"] != lastPlacedPallet.Data["PRODUCT_NAME"])
                        {
                            colorIterator++;
                        }

                        var color = GetColor(colorIterator, productList.Count);
                        pallet.SetColor(color);

                        bool continueFlag = false;

                        switch (EntranceSide)
                        {
                            // снизу
                            case 4:
                                {
                                    // Если не размещён ни один поддон, то ставим в начальное положение
                                    if (lastPlacedPallet == null)
                                    {
                                        pallet.X = this.X;
                                        pallet.Y = this.Y;

                                        palletInCoordinateCount = 1;
                                    }
                                    // если есть размещённые поддоны
                                    else
                                    {
                                        // если есть поддоны, которые не помещаются при расположении в один слой
                                        if (nonPlacedPalletCount > 0)
                                        {
                                            // если количество поддонов в этой координате меньше, чем количество поддонов, которое мы можем поставить в один ряд
                                            if (palletInCoordinateCount < levelCount)
                                            {
                                                // друг на друга ставим поддоны только с одинаковой продукцией
                                                if (pallet.Data["PRODUCT_NAME"] == lastPlacedPallet.Data["PRODUCT_NAME"])
                                                {
                                                    // ставим поддон в эту же коррдинату
                                                    pallet.Y = lastPlacedPallet.Y;
                                                    pallet.X = lastPlacedPallet.X;

                                                    pallet.StackFlag = true;
                                                    lastPlacedPallet.StackFlag = true;

                                                    palletInCoordinateCount++;
                                                    nonPlacedPalletCount--;
                                                    continueFlag = true;
                                                }
                                            }
                                        }

                                        if (!continueFlag)
                                        {
                                            // Если размещаемый поддон помещается справа от последнего размещённого поддона
                                            if (lastPlacedPallet.X + lastPlacedPallet.Width + pallet.Width <= this.X + this.Width)
                                            {
                                                // то размещаем новый поддон на том же уровне по Y, что и предыдущий, правее от него
                                                pallet.Y = lastPlacedPallet.Y;
                                                pallet.X = lastPlacedPallet.X + lastPlacedPallet.Width;

                                                palletInCoordinateCount = 1;
                                            }
                                            // Если размещаемый поддон не помещается справа и помещается снизу, то  начинаем размещать поддоны ниже последнего
                                            else if (lastPlacedPallet.Y + lastPlacedPallet.Height + pallet.Height <= this.Y + this.Height)
                                            {
                                                // то размещаем новый поддон у левого края ячейки ниже текущего последнего поддона
                                                pallet.X = this.X;
                                                pallet.Y = lastPlacedPallet.Y + lastPlacedPallet.Height;

                                                palletInCoordinateCount = 1;
                                            }
                                        }
                                    }
                                }
                                break;

                            // слева
                            case 1:
                                {
                                    if (lastPlacedPallet == null)
                                    {
                                        pallet.X = this.X;
                                        pallet.Y = this.Y + this.Height - pallet.Height;

                                        palletInCoordinateCount = 1;
                                    }
                                    else
                                    {
                                        // если есть поддоны, которые не помещаются при расположении в один слой
                                        if (nonPlacedPalletCount > 0)
                                        {
                                            // если количество поддонов в этой координате меньше, чем количество поддонов, которое мы можем поставить в один ряд
                                            if (palletInCoordinateCount < levelCount)
                                            {
                                                // друг на друга ставим поддоны только с одинаковой продукцией
                                                if (pallet.Data["PRODUCT_NAME"] == lastPlacedPallet.Data["PRODUCT_NAME"])
                                                {
                                                    // ставим поддон в эту же коррдинату
                                                    pallet.Y = lastPlacedPallet.Y;
                                                    pallet.X = lastPlacedPallet.X;

                                                    pallet.StackFlag = true;
                                                    lastPlacedPallet.StackFlag = true;

                                                    palletInCoordinateCount++;
                                                    nonPlacedPalletCount--;
                                                    continueFlag = true;
                                                }
                                            }
                                        }

                                        if (!continueFlag)
                                        {
                                            // Если поддон помещается над последним размещённым поддоном
                                            if (lastPlacedPallet.Y - pallet.Height >= this.Y)
                                            {
                                                // то размещаем новый поддон на том же уровне по X, что и предыдущий, выше от него
                                                pallet.Y = lastPlacedPallet.Y - pallet.Height;
                                                pallet.X = lastPlacedPallet.X;

                                                palletInCoordinateCount = 1;
                                            }
                                            // Если размещаемый поддон не помещается сверху и помещается справа, то  начинаем размещать поддоны правее последнего
                                            else if (lastPlacedPallet.X + lastPlacedPallet.Width + pallet.Width <= this.X + this.Width)
                                            {
                                                // то размещаем новый поддон у левого края ячейки ниже текущего последнего поддона
                                                pallet.X = lastPlacedPallet.X + lastPlacedPallet.Width;
                                                pallet.Y = this.Y + this.Height - pallet.Height;

                                                palletInCoordinateCount = 1;
                                            }
                                        }
                                    }
                                }
                                break;

                            // сверху
                            case 2:
                                {
                                    if (lastPlacedPallet == null)
                                    {
                                        pallet.X = this.X + this.Width - pallet.Width;
                                        pallet.Y = this.Y + this.Height - pallet.Height;

                                        palletInCoordinateCount = 1;
                                    }
                                    else
                                    {
                                        // если есть поддоны, которые не помещаются при расположении в один слой
                                        if (nonPlacedPalletCount > 0)
                                        {
                                            // если количество поддонов в этой координате меньше, чем количество поддонов, которое мы можем поставить в один ряд
                                            if (palletInCoordinateCount < levelCount)
                                            {
                                                // друг на друга ставим поддоны только с одинаковой продукцией
                                                if (pallet.Data["PRODUCT_NAME"] == lastPlacedPallet.Data["PRODUCT_NAME"])
                                                {
                                                    // ставим поддон в эту же коррдинату
                                                    pallet.Y = lastPlacedPallet.Y;
                                                    pallet.X = lastPlacedPallet.X;

                                                    pallet.StackFlag = true;
                                                    lastPlacedPallet.StackFlag = true;

                                                    palletInCoordinateCount++;
                                                    nonPlacedPalletCount--;
                                                    continueFlag = true;
                                                }
                                            }
                                        }

                                        if (!continueFlag)
                                        {
                                            // если новый поддон помещается слева от последнего размещённого поддона
                                            if (lastPlacedPallet.X - pallet.Width >= this.X)
                                            {
                                                // то размещаем новый поддон на том же уровне по Y, что и предыдущий, левее от него
                                                pallet.Y = lastPlacedPallet.Y;
                                                pallet.X = lastPlacedPallet.X - pallet.Width;

                                                palletInCoordinateCount = 1;
                                            }
                                            // если новый поддон не помещается левее последнего размещённого и помещается выше него
                                            else if (lastPlacedPallet.Y - pallet.Height >= this.Y)
                                            {
                                                // то размещаем новый поддон у правого края ячейки выше текущего последнего поддона
                                                pallet.X = this.X + this.Width - pallet.Width;
                                                pallet.Y = lastPlacedPallet.Y - pallet.Height;

                                                palletInCoordinateCount = 1;
                                            }
                                        }
                                    }
                                }
                                break;

                            // справа
                            case 3:
                                {
                                    if (lastPlacedPallet == null)
                                    {
                                        pallet.X = this.X + this.Width - pallet.Width;
                                        pallet.Y = this.Y;

                                        palletInCoordinateCount = 1;
                                    }
                                    else
                                    {
                                        // если есть поддоны, которые не помещаются при расположении в один слой
                                        if (nonPlacedPalletCount > 0)
                                        {
                                            // если количество поддонов в этой координате меньше, чем количество поддонов, которое мы можем поставить в один ряд
                                            if (palletInCoordinateCount < levelCount)
                                            {
                                                // друг на друга ставим поддоны только с одинаковой продукцией
                                                if (pallet.Data["PRODUCT_NAME"] == lastPlacedPallet.Data["PRODUCT_NAME"])
                                                {
                                                    // ставим поддон в эту же коррдинату
                                                    pallet.Y = lastPlacedPallet.Y;
                                                    pallet.X = lastPlacedPallet.X;

                                                    pallet.StackFlag = true;
                                                    lastPlacedPallet.StackFlag = true;

                                                    palletInCoordinateCount++;
                                                    nonPlacedPalletCount--;
                                                    continueFlag = true;
                                                }
                                            }
                                        }

                                        if (!continueFlag)
                                        {
                                            // Если размещаемый поддон помещается ниже от последнего размещённого поддона
                                            if (lastPlacedPallet.Y + lastPlacedPallet.Height + pallet.Height <= this.Y + this.Height)
                                            {
                                                // то размещаем новый поддон на том же уровне по X, что и предыдущий, ниже от него
                                                pallet.Y = lastPlacedPallet.Y + lastPlacedPallet.Height;
                                                pallet.X = lastPlacedPallet.X;

                                                palletInCoordinateCount = 1;
                                            }
                                            // поддон не помещается под последним размещённым и помещается левее от него
                                            else if (lastPlacedPallet.X - pallet.Width >= this.X)
                                            {
                                                // то размещаем новый поддон у верхнего края ячейки левее текущего последнего поддона
                                                pallet.X = lastPlacedPallet.X - pallet.Width;
                                                pallet.Y = this.Y;

                                                palletInCoordinateCount = 1;
                                            }
                                        }
                                    }
                                }
                                break;

                            default:
                                break;
                        }

                        pallet.PixelX = pallet.X * GridStep;
                        pallet.PixelY = pallet.Y * GridStep;

                        lastPlacedPallet = pallet;
                    }
                }
            }
        }

        public class Pallet : Block
        {
            public Pallet() : base()
            {
                this.BlockType = BlockType.Pallet;
                this.ZIndex = 1;
                this.Alpha = 255;
                this.Red = 122;
                this.Green = 65;
                this.Blue = 0;
                this.Width = 1;
                this.Height = 1;
            }

            /// <summary>
            /// Ид ячейки из БД, в которой стоит этот поддон
            /// </summary>
            public int CellId;

            /// <summary>
            /// Наименование ячейки из БД, в которой стоит этот поддон
            /// </summary>
            public string CellName;

            /// <summary>
            /// Флаг того, что эти поддоны размещены друг на друге
            /// </summary>
            public bool StackFlag { get; set; }

            public override string GetDataText()
            {
                string text = $"Поддон: {Data["PALLET_NAME"]}{Environment.NewLine}" +
                    $"Продукция: {Data["PRODUCT_NAME"]}{Environment.NewLine}" +
                    $"ID поддона: {Data["PALLET_ID"].ToInt()}{Environment.NewLine}" +
                    $"Ячейка: {this.CellName}";

                if (Central.DebugMode)
                {
                    text = $"{text}{Environment.NewLine}" +
                        $"X {this.X} Y {this.Y}{Environment.NewLine}" +
                        $"WIDTH {this.Width} HEIGHT {this.Height}{Environment.NewLine}" +
                        $"ID ячейки {this.CellId}{Environment.NewLine}" +
                        $"Объект {ObjectId}";
                }

                return text;
            }

            public void SetColor(Rgba32 color)
            {
                this.Red = color.R;
                this.Green = color.G;
                this.Blue = color.B;
                this.Alpha = color.A;
            }
        }

        public class SystemBlock : Block
        {
            public SystemBlock() : base()
            {
                this.BlockType = BlockType.SystemBlock;
                this.ZIndex = 0;
                this.Alpha = 255;
                this.Red = 0;
                this.Green = 0;
                this.Blue = 0;
            }

            public override string GetDataText()
            {
                string text = "";
                return text;
            }
        }

        public Cell NewCell { get; set; }

        private void img_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ImageToolTip.IsOpen = false;

            var positionByImage = e.GetPosition(Img);
            var globalPosition = e.GetPosition(Central.MainWindow);

            int imageX = (int)Math.Floor(positionByImage.X / GridStep);
            int imageY = (int)Math.Floor(positionByImage.Y / GridStep);

            if (!AddCellMode)
            {
                bool succes = false;
                string toolTipContent = "";

                var blocks = Blocks.Where(x =>
                    x.X <= imageX && x.X + x.Width - 1 >= imageX
                    && x.Y <= imageY && x.Y + x.Height - 1 >= imageY).ToList();

                if (blocks != null && blocks.Count > 0)
                {
                    int maxZIndex = blocks.Max(x => x.ZIndex);
                    var toolTipBlocks = blocks.Where(x => x.ZIndex == maxZIndex).ToList();

                    if (toolTipBlocks.Count == 1) 
                    {
                        toolTipContent = toolTipBlocks[0].GetDataText();
                        succes = true;
                    }
                    else
                    {
                        foreach (var toolTipBlock in toolTipBlocks)
                        {
                            if (string.IsNullOrEmpty(toolTipContent))
                            {
                                toolTipContent = $"{toolTipBlock.GetDataText()}";
                            }
                            else
                            {
                                toolTipContent = $"{toolTipContent}" +
                                    $"{Environment.NewLine}-------{Environment.NewLine}" +
                                    $"{toolTipBlock.GetDataText()}";
                            }
                        }

                        succes = true;
                    }
                }

                if (succes)
                {
                    ImageToolTip.Content = toolTipContent;
                    ImageToolTip.PlacementRectangle = new Rect(globalPosition.X + 20, globalPosition.Y + 20, 0, 0);
                    ImageToolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                    ImageToolTip.IsOpen = true;
                }
            }
            else
            {
                if (NewCell == null)
                {
                    NewCell = new Cell();
                    NewCell.X = imageX;
                    NewCell.Y = imageY;
                    NewCell.Width = 1;
                    NewCell.Height = 1;
                    NewCell.PixelX = NewCell.X * GridStep;
                    NewCell.PixelY = NewCell.Y * GridStep;
                    NewCell.PixelWidth = NewCell.Width * GridStep;
                    NewCell.PixelHeight = NewCell.Height * GridStep;
                    NewCell.Red = 0;
                    NewCell.Green = 0;
                    NewCell.Blue = 255;
                    RenderItem(NewCell);
                }
                else
                {
                    if (imageX - NewCell.X + 1 >= 1
                        && imageY - NewCell.Y + 1 >= 1)
                    {
                        NewCell.Width = imageX - NewCell.X + 1;
                        NewCell.Height = imageY - NewCell.Y + 1;
                        NewCell.PixelWidth = NewCell.Width * GridStep;
                        NewCell.PixelHeight = NewCell.Height * GridStep;
                        RenderItem(NewCell);

                        AddCell();
                    }
                }
            }
        }

        public void ClearNewCell()
        {
            NewCell.Alpha = 0;
            RenderItem(NewCell);

            NewCell = null;

            AddCellMode = false;
            AddCellButton.IsEnabled = true;
        }

        public void AddCell()
        {
            Dictionary<string, string> formData = new Dictionary<string, string>()
            {
                {"LEFT",    $"{NewCell.X}"},
                {"TOP",     $"{NewCell.Y}"},
                {"WIDTH",   $"{NewCell.Width}"},
                {"LENGTH",  $"{NewCell.Height}"},
            };

            var w = new AddCell();
            w.FormData = formData;
            w.ParentFrame = this.FrameName;
            w.OnClose = (bool saveFlag) => 
            {
                ClearNewCell();

                if (saveFlag)
                {
                    Refresh();
                }
            };
            w.Show();
        }

        private void AddCellButton_Click(object sender, RoutedEventArgs e)
        {
            AddCellMode = true;
            AddCellButton.IsEnabled = false;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void ImgScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            GridImgScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
            GridImgScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);

            BackgorundImgScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
            BackgorundImgScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
        }

        private void ShowGridCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ShowGridFlag = true;
            SetGridVisible();
        }

        private void ShowGridCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ShowGridFlag = false;
            SetGridVisible();
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            if (GridStep < 20)
            {
                GridStep++;
                GridStapTextBox.Text = $"{GridStep}";
                int countCellByWidth = 192;
                int countCellByHeight = 108;

                ImgWidth = countCellByWidth * GridStep;
                ImgHeight = countCellByHeight * GridStep;

                wb = null;
                wb = new WriteableBitmap(ImgWidth, ImgHeight, 96, 96, PixelFormats.Bgra32, null);
                GridImgWB = new WriteableBitmap(ImgWidth, ImgHeight, 96, 96, PixelFormats.Bgra32, null);

                Blocks.Clear();
                LoadItems(Cells, Pallets);
                RenderItems();
                Img.Source = wb;
            }

            e.Handled = true;
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            if (GridStep > 3)
            {
                GridStep--;
                GridStapTextBox.Text = $"{GridStep}";
                int countCellByWidth = 192;
                int countCellByHeight = 108;

                ImgWidth = countCellByWidth * GridStep;
                ImgHeight = countCellByHeight * GridStep;

                wb = null;
                wb = new WriteableBitmap(ImgWidth, ImgHeight, 96, 96, PixelFormats.Bgra32, null);
                GridImgWB = new WriteableBitmap(ImgWidth, ImgHeight, 96, 96, PixelFormats.Bgra32, null);

                Blocks.Clear();
                LoadItems(Cells, Pallets);
                RenderItems();
                Img.Source = wb;
            }

            e.Handled = true;
        }
    }
}
