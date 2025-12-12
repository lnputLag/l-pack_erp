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
using static NPOI.HSSF.Util.HSSFColor;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Логика взаимодействия для CellVisualizationBuffer.xaml
    /// </summary>
    public partial class CellVisualizationBuffer2 : ControlBase
    {
        // TODO
        // размещение поддона внутри ячейки
        // поддон на поддон

        public CellVisualizationBuffer2()
        {
            ControlTitle = "Визуализация буфера";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]debug";
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
            };

            //деструктор, вызовется когда таб закрывается
            //утилизация ресурсов
            //ранее эта работа была в методе Destroy
            OnUnload = () =>
            {
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

        public WriteableBitmap wb { get; set; }

        public List<Block> Blocks { get; set; }

        public static Random Random = new Random();

        public static Dictionary<int, Rgba32> ColorDictionary;

        public static void CreateColorDictionary()
        {
            ColorDictionary = new Dictionary<int, Rgba32>();
            ColorDictionary.Add(-1, new Rgba32(255, 255, 153, 255)); //(б/уп)песочный
            ColorDictionary.Add(0, new Rgba32(214, 252, 213, 255)); //(c/уп)салатовый

            // синие
            ColorDictionary.Add(1, new Rgba32(181, 242, 234, 255)); //Аквамарин
            ColorDictionary.Add(5, new Rgba32(231, 236, 255, 255)); //Лиловый
            ColorDictionary.Add(9, new Rgba32(222, 247, 254, 255)); //Голубой 
            ColorDictionary.Add(13, new Rgba32(161, 133, 148, 255)); //Пастельно-фиолетовый
            ColorDictionary.Add(17, new Rgba32(175, 218, 252, 255)); //Синий-синий иней

            // красные
            ColorDictionary.Add(2, new Rgba32(254, 214, 188, 255)); //Персиковый
            ColorDictionary.Add(6, new Rgba32(250, 218, 221, 255)); //Бледно-розовый
            ColorDictionary.Add(10, new Rgba32(255, 189, 136, 255)); //Макароны и сыр
            ColorDictionary.Add(14, new Rgba32(255, 155, 170, 255)); //Лососевый Крайола
            ColorDictionary.Add(18, new Rgba32(228, 113, 122, 255)); //Карамельно-розовый

            // зелёные
            ColorDictionary.Add(3, new Rgba32(62, 180, 137, 255)); //Мятный
            ColorDictionary.Add(7, new Rgba32(218, 216, 113, 255)); //Вердепешевый
            ColorDictionary.Add(11, new Rgba32(172, 183, 142, 255)); //Болотный
            ColorDictionary.Add(15, new Rgba32(198, 223, 144, 255)); //Очень светлый желто-зеленый
            ColorDictionary.Add(19, new Rgba32(246, 255, 248, 255)); //Лаймовый

            // желтые
            ColorDictionary.Add(4, new Rgba32(255, 250, 221, 255)); //Светло-жёлтый
            ColorDictionary.Add(8, new Rgba32(179, 159, 122, 255)); //Кофе с молоком
            ColorDictionary.Add(12, new Rgba32(255, 207, 72, 255)); //Восход солнца
            ColorDictionary.Add(16, new Rgba32(239, 169, 74, 255)); //Пастельно-желтый
            ColorDictionary.Add(20, new Rgba32(230, 214, 144, 255)); //Светлая слоновая кость

            ColorDictionary.Add(99, new Rgba32(238, 238, 238, 255)); //(транспорт)серый
        }

        public static Rgba32 GetColor(int _number, int colProduction)
        {
            Rgba32 result = ColorDictionary[0];

            decimal colCollors = ColorDictionary.Count() - 3;

            int maxKoef = (int)Math.Ceiling(colProduction / colCollors);

            if (_number == 99 || _number == 0 || _number == -1)
            {
                result = ColorDictionary[_number];
            }
            else
            {
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
            }

            return result;
        }

        public int DefaultImgWidth;
        public int DefaultImgHeight;


        public static int koef = 100;
        public static int defaultPalletWhdth = 1000;
        public static int defaultPalletHeight = 1000;


        public void SetDefaults()
        {
            CreateColorDictionary();

            Blocks = new List<Block>();

            img.Width = (int)BackgroundImage.ActualWidth;//ImgBorder.ActualWidth;
            img.Height = (int)BackgroundImage.ActualHeight;// ImgBorder.ActualHeight;

            DefaultImgWidth = (int)img.Width;
            DefaultImgHeight = (int)img.Height;

            // Create the bitmap, with the dimensions of the image placeholder.
            wb = new WriteableBitmap((int)img.Width,
                (int)img.Height, 96, 96, PixelFormats.Bgra32, null);

            // Show the bitmap in an Image element.
            img.Source = wb;
        }

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

            List<Dictionary<string, string>> Cells = new List<Dictionary<string, string>>();
            List<Dictionary<string, string>> Pallets = new List<Dictionary<string, string>>();

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

            if (Cells != null && Cells.Count > 0
                && Pallets != null && Pallets.Count > 0)
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

            int iterator = 0;
            int margin = 10;

            foreach (var cellData in Cells)
            {
                Cell cell = new Cell();
                cell.Data = cellData;
                cell.RealWidth = cell.Data.CheckGet("CELL_WIDTH").ToInt();
                cell.RealHeight = cell.Data.CheckGet("CELL_LENGTH").ToInt();
                cell.RealSquare = cell.RealWidth * cell.RealHeight;
                cell.Width = cell.RealWidth / koef;
                cell.Height = cell.RealHeight / koef;
                cell.Square = cell.Width * cell.Height;
                cell.EntranceSide = Random.Next(1, 5);

                cell.X = ((cell.Width + margin) * iterator);
                cell.Y = 0;

                var d = Pallets.Where(x => x.CheckGet("CELL_NAME") == cellData.CheckGet("CELL_NAME")).ToList();
                foreach (var palletData in d)
                {
                    Pallet pallet = new Pallet();
                    pallet.Data = palletData;
                    pallet.RealWidth = pallet.Data.CheckGet("PALLET_WIDTH").ToInt();
                    pallet.RealHeight = pallet.Data.CheckGet("PALLET_LENGTH").ToInt();
                    pallet.RealSquare = pallet.RealWidth * pallet.RealHeight;

                    pallet.Width = defaultPalletWhdth / koef;
                    pallet.Height = defaultPalletHeight / koef;
                    pallet.Square = pallet.Width * pallet.Height;

                    cell.AddPallet(pallet);

                    pallet.SaveDefaults();
                    Blocks.Add(pallet);
                }

                cell.PlacePallets();

                cell.SaveDefaults();
                Blocks.Add(cell);
                iterator++;
            }


            //for (int i = 0; i <= img.Width - 50; i = i + 60)
            //{
            //    for (int j = 0; j <= img.Height - 50; j = j + 60)
            //    {
            //        Block cell = new Block();
            //        cell.X = i;
            //        cell.Y = j;
            //        cell.Width = 50;
            //        cell.Height = 50;
            //        cell.ZIndex = 0;
            //        cell.Rendered = false;
            //        cell.BlockType = BlockType.Cell;
            //        cell.Alpha = 250;
            //        cell.Red = 100;
            //        cell.Green = 100;
            //        cell.Blue = 100;

            //        Blocks.Add(cell);

            //        {
            //            for (int x2 = cell.X; x2 <= cell.X + cell.Width - 10; x2 = x2 + 20)
            //            {
            //                for (int y2 = cell.Y; y2 <= cell.Y + cell.Height - 10; y2 = y2 + 20)
            //                {
            //                    Block pallet = new Block();
            //                    pallet.X = x2;
            //                    pallet.Y = y2;
            //                    pallet.Width = 10;
            //                    pallet.Height = 10;
            //                    pallet.ZIndex = 1;
            //                    pallet.Rendered = false;
            //                    pallet.BlockType = BlockType.Pallet;
            //                    pallet.Alpha = 255;
            //                    pallet.Red = 122;
            //                    pallet.Green = 65;
            //                    pallet.Blue = 0;

            //                    Blocks.Add(pallet);
            //                }
            //            }
            //        }
            //    }
            //}

            //Blocks = Blocks.OrderByDescending(x => x.ZIndex).ToList();

            //for (int i = 0; i <= img.Width - 50; i = i + 100)
            //{
            //    for (int j = 0; j <= img.Height - 50; j = j + 100)
            //    {
            //        Cell cell = new Cell();
            //        cell.X = i;
            //        cell.Y = j;
            //        cell.Width = 50;
            //        cell.Height = 50;
            //        cell.Square = cell.Width * cell.Height;

            //        Blocks.Add(cell);

            //        for (int k = 0; k < 3; k ++) 
            //        {
            //            Pallet pallet = new Pallet();
            //            pallet.Width = 10;
            //            pallet.Height = 10;
            //            cell.AddPallet(pallet);

            //            Blocks.Add(pallet);
            //        }

            //        //{
            //        //    for (int x2 = cell.X; x2 <= cell.X + cell.Width - 10; x2 = x2 + 20)
            //        //    {
            //        //        for (int y2 = cell.Y; y2 <= cell.Y + cell.Height - 10; y2 = y2 + 20)
            //        //        {
            //        //            Pallet pallet = new Pallet();
            //        //            pallet.X = x2;
            //        //            pallet.Y = y2;
            //        //            pallet.Width = 10;
            //        //            pallet.Height = 10;
            //        //            cell.AddPallet(pallet);

            //        //            Blocks.Add(pallet);
            //        //        }
            //        //    }
            //        //}
            //    }
            //}

            //Blocks = Blocks.OrderByDescending(x => x.ZIndex).ToList();
        }

        public void Zoom(int z, int operation)
        {
            if (operation == 1)
            {
                for (int i = 0; i < Blocks.Count; i++)
                {
                    var block = Blocks[i];
                    block.X = block.DefaultX * z;
                    block.Y = block.DefaultY * z;
                    block.Width = block.DefaultWidth * z;
                    block.Height = block.DefaultHeight * z;
                    Blocks[i] = block;
                }

                img.Width = DefaultImgWidth * z;
                img.Height = DefaultImgHeight * z;

                // FIXME возможно ActualWidth
                BackgroundImage.Width = img.Width;
                BackgroundImage.Height = img.Height;
            }
            else if (operation == 2)
            {
                for (int i = 0; i < Blocks.Count; i++)
                {
                    var block = Blocks[i];
                    block.X = block.DefaultX / z;
                    block.Y = block.DefaultY / z;
                    block.Width = block.DefaultWidth / z;
                    block.Height = block.DefaultHeight / z;
                    Blocks[i] = block;
                }

                img.Width = DefaultImgWidth / z;
                img.Height = DefaultImgHeight / z;

                // FIXME возможно ActualWidth
                BackgroundImage.Width = img.Width;
                BackgroundImage.Height = img.Height;
            }

            ClearImage();
            RenderItems();
        }

        // FIXME оптимизировать + не всегда работает как нужно
        public void ClearImage()
        {
            wb = new WriteableBitmap((int)img.Width,
                (int)img.Height, 96, 96, PixelFormats.Bgra32, null);
        }

        public void RenderItems()
        {
            foreach (var block in Blocks)
            {
                if (block.BlockType == BlockType.Cell)
                {
                    RenderBlock(block);
                }
            }

            foreach (var block in Blocks)
            {
                if (block.BlockType == BlockType.Pallet)
                {
                    RenderBlock(block);
                }
            }
        }

        /// <summary>
        /// Отрисовка ячейки склада
        /// </summary>
        public void RenderBlock(Block block)
        {
            RenderFillObject(block.X, block.Y, block.Width, block.Height, block.Red, block.Green, block.Blue, block.Alpha);
            block.Rendered = true;
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
            Pallet
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
            /// Начальное (до зума) положение X координаты
            /// </summary>
            public int DefaultX;

            /// <summary>
            /// Начальное (до зума)  положение Y координаты
            /// </summary>
            public int DefaultY;

            /// <summary>
            /// Начальное (до зума) Ширина объекта в пикселях на картинке
            /// </summary>
            public int DefaultWidth;

            /// <summary>
            /// Начальное (до зума) Длина объекта в пикселях на картинке
            /// </summary>
            public int DefaultHeight;

            /// <summary>
            /// Текущее положение X координаты
            /// </summary>
            public int X;

            /// <summary>
            /// Текущее положение Y координаты
            /// </summary>
            public int Y;

            /// <summary>
            /// Ширина объекта в пикселях на картинке
            /// </summary>
            public int Width;

            /// <summary>
            /// Длина объекта в пикселях на картинке
            /// </summary>
            public int Height;

            /// <summary>
            /// Площадь объекта в пикселях на картинке
            /// </summary>
            public int Square;

            public int ZIndex;

            public BlockType BlockType;

            public bool Rendered;

            public int Red;

            public int Green;

            public int Blue;

            public int Alpha;

            public int ObjectId;

            public Dictionary<string, string> Data;

            /// <summary>
            /// Реальная ширина объекта в мм.
            /// </summary>
            public int RealWidth;

            /// <summary>
            /// Реальная длина объекта в мм.
            /// </summary>
            public int RealHeight;

            /// <summary>
            /// Реальная площадь объекта в мм.
            /// </summary>
            public int RealSquare;

            public abstract string GetDataText();

            public abstract void SaveDefaults();
        }

        public class Cell : Block
        {
            public Cell() : base()
            {
                this.BlockType = BlockType.Cell;
                this.ZIndex = 0;
                this.Alpha = 250;
                this.Red = 100;
                this.Green = 100;
                this.Blue = 100;
                this.Pallets = new List<Pallet>();
            }

            public int EntranceSide;

            public List<Pallet> Pallets;

            public override void SaveDefaults()
            {
                this.DefaultX = this.X;
                this.DefaultY = this.Y;
                this.DefaultWidth = this.Width;
                this.DefaultHeight = this.Height;
            }

            //public Pallet LastPlacedPallet;

            //// FIXME реализовать логику расположения поддонов в ячейке
            //public void AddPallet(Pallet pallet)
            //{
            //    pallet.CellObjectId = this.ObjectId;

            //    var y = this.Y + 5 + Pallets.Sum(x => x.Height) + Pallets.Count() * 5;
            //    pallet.Y = y;
            //    pallet.X = this.X + 5;
            //    pallet.Width = this.Width - 10;
            //    pallet.Height = 10;

            //    Pallets.Add(pallet);
            //}

            /// <summary>
            /// Рисуем все поддоны одного размера
            /// </summary>
            /// <param name="pallet"></param>
            public void AddPallet(Pallet pallet)
            {
                pallet.CellObjectId = this.ObjectId;

                //PlacePallet(pallet);

                Pallets.Add(pallet);
                //LastPlacedPallet = pallet;
            }

            //public void PlacePallet(Pallet pallet)
            //{
            //    switch (EntranceSide)
            //    {
            //        case 1:
            //            {
            //                if (LastPlacedPallet == null)
            //                {
            //                    pallet.X = this.X;
            //                    pallet.Y = this.Y;
            //                }
            //                else
            //                {
            //                    // Если размещаемый поддон помещается справа от последнего размещённого поддона
            //                    if (LastPlacedPallet.X + LastPlacedPallet.Width + pallet.Width <= this.X + this.Width)
            //                    {
            //                        // то размещаем новый поддон на том же уровне по Y, что и предыдущий, правее от него

            //                        pallet.Y = LastPlacedPallet.Y;
            //                        pallet.X = LastPlacedPallet.X + LastPlacedPallet.Width;
            //                    }
            //                    // Если размещаемый поддон не помещается справа и помещается снизу, то  начинаем размещать поддоны ниже последнего
            //                    else if (LastPlacedPallet.Y + LastPlacedPallet.Height + pallet.Height <= this.Y + this.Height)
            //                    {
            //                        pallet.X = this.X;
            //                        pallet.Y = LastPlacedPallet.Y + LastPlacedPallet.Height;
            //                    }
            //                    // Если не помещвется ни один из вариантом, то размещаем вторым рядом
            //                    else
            //                    {

            //                    }
            //                }
            //            }
            //            break;

            //        default:
            //            break;
            //    }
            //}

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

                Pallet LastPlacedPallet = null;

                int i = 1;
                foreach (var pallet in Pallets)
                {
                    switch (EntranceSide)
                    {
                        // снизу
                        case 1:
                            {
                                if (LastPlacedPallet == null)
                                {
                                    pallet.X = this.X;
                                    pallet.Y = this.Y;

                                    var color = GetColor(i, productList.Count);
                                    pallet.SetColor(color);
                                }
                                else
                                {
                                    if (pallet.Data.CheckGet("PRODUCT_NAME") != LastPlacedPallet.Data.CheckGet("PRODUCT_NAME"))
                                    {
                                        i++;
                                    }

                                    var color = GetColor(i, productList.Count);

                                    // Если размещаемый поддон помещается справа от последнего размещённого поддона
                                    if (LastPlacedPallet.X + LastPlacedPallet.Width + pallet.Width <= this.X + this.Width)
                                    {
                                        // то размещаем новый поддон на том же уровне по Y, что и предыдущий, правее от него
                                        pallet.Y = LastPlacedPallet.Y;
                                        pallet.X = LastPlacedPallet.X + LastPlacedPallet.Width;

                                        pallet.SetColor(color);
                                    }
                                    // Если размещаемый поддон не помещается справа и помещается снизу, то  начинаем размещать поддоны ниже последнего
                                    else if (LastPlacedPallet.Y + LastPlacedPallet.Height + pallet.Height <= this.Y + this.Height)
                                    {
                                        // то размещаем новый поддон у левого края ячейки ниже текущего последнего поддона
                                        pallet.X = this.X;
                                        pallet.Y = LastPlacedPallet.Y + LastPlacedPallet.Height;

                                        pallet.SetColor(color);
                                    }
                                    // Если не помещвется ни один из вариантом, то размещаем вторым рядом
                                    else
                                    {

                                    }
                                }
                            }
                            break;

                        // слева
                        case 2:
                            {
                                if (LastPlacedPallet == null)
                                {
                                    pallet.X = this.X;
                                    pallet.Y = this.Y + this.Height - pallet.Height;

                                    var color = GetColor(i, productList.Count);
                                    pallet.SetColor(color);
                                }
                                else
                                {
                                    if (pallet.Data.CheckGet("PRODUCT_NAME") != LastPlacedPallet.Data.CheckGet("PRODUCT_NAME"))
                                    {
                                        i++;
                                    }

                                    var color = GetColor(i, productList.Count);

                                    // Если поддон помещается над последним размещённым поддоном
                                    if (LastPlacedPallet.Y - pallet.Height >= this.Y)
                                    {
                                        // то размещаем новый поддон на том же уровне по X, что и предыдущий, выше от него
                                        pallet.Y = LastPlacedPallet.Y - pallet.Height;
                                        pallet.X = LastPlacedPallet.X;

                                        pallet.SetColor(color);
                                    }
                                    // Если размещаемый поддон не помещается сверху и помещается справа, то  начинаем размещать поддоны правее последнего
                                    else if (LastPlacedPallet.X + LastPlacedPallet.Width + pallet.Width <= this.X + this.Width)
                                    {
                                        // то размещаем новый поддон у левого края ячейки ниже текущего последнего поддона
                                        pallet.X = LastPlacedPallet.X + LastPlacedPallet.Width;
                                        pallet.Y = this.Y + this.Height - pallet.Height;

                                        pallet.SetColor(color);
                                    }
                                    // Если не помещвется ни один из вариантом, то размещаем вторым рядом
                                    else
                                    {

                                    }
                                }
                            }
                            break;

                        // сверху
                        case 3:
                            {
                                if (LastPlacedPallet == null)
                                {
                                    pallet.X = this.X + this.Width - pallet.Width;
                                    pallet.Y = this.Y + this.Height - pallet.Height;

                                    var color = GetColor(i, productList.Count);
                                    pallet.SetColor(color);
                                }
                                else
                                {
                                    if (pallet.Data.CheckGet("PRODUCT_NAME") != LastPlacedPallet.Data.CheckGet("PRODUCT_NAME"))
                                    {
                                        i++;
                                    }

                                    var color = GetColor(i, productList.Count);

                                    // если новый поддон помещается слева от последнего размещённого поддона
                                    if (LastPlacedPallet.X - pallet.Width >= this.X)
                                    {
                                        // то размещаем новый поддон на том же уровне по Y, что и предыдущий, левее от него
                                        pallet.Y = LastPlacedPallet.Y;
                                        pallet.X = LastPlacedPallet.X - pallet.Width;

                                        pallet.SetColor(color);
                                    }
                                    // если новый поддон не помещается левее последнего размещённого и помещается выше него
                                    else if (LastPlacedPallet.Y - pallet.Height >= this.Y)
                                    {
                                        // то размещаем новый поддон у правого края ячейки выше текущего последнего поддона
                                        pallet.X = this.X + this.Width - pallet.Width;
                                        pallet.Y = LastPlacedPallet.Y - pallet.Height;

                                        pallet.SetColor(color);
                                    }
                                    // Если не помещвется ни один из вариантом, то размещаем вторым рядом
                                    else
                                    {

                                    }
                                }
                            }
                            break;

                        case 4:
                            {
                                if (LastPlacedPallet == null)
                                {
                                    pallet.X = this.X + this.Width - pallet.Width;
                                    pallet.Y = this.Y;

                                    var color = GetColor(i, productList.Count);
                                    pallet.SetColor(color);
                                }
                                else
                                {
                                    if (pallet.Data.CheckGet("PRODUCT_NAME") != LastPlacedPallet.Data.CheckGet("PRODUCT_NAME"))
                                    {
                                        i++;
                                    }

                                    var color = GetColor(i, productList.Count);

                                    // Если размещаемый поддон помещается ниже от последнего размещённого поддона
                                    if (LastPlacedPallet.Y + LastPlacedPallet.Height + pallet.Height <= this.Y + this.Height)
                                    {
                                        // то размещаем новый поддон на том же уровне по X, что и предыдущий, ниже от него
                                        pallet.Y = LastPlacedPallet.Y + LastPlacedPallet.Height;
                                        pallet.X = LastPlacedPallet.X;

                                        pallet.SetColor(color);
                                    }
                                    // поддон не помещается под последним размещённым и помещается левее от него
                                    else if (LastPlacedPallet.X - pallet.Width >= this.X)
                                    {
                                        // то размещаем новый поддон у верхнего края ячейки левее текущего последнего поддона
                                        pallet.X = LastPlacedPallet.X - pallet.Width;
                                        pallet.Y = this.Y;

                                        pallet.SetColor(color);
                                    }
                                    // Если не помещвется ни один из вариантом, то размещаем вторым рядом
                                    else
                                    {

                                    }
                                }
                            }
                            break;

                        default:
                            break;
                    }

                    LastPlacedPallet = pallet;
                }
               
            }

            // рисуем поддон в процентном отношении площади занимаемой поддоном к площади ячейки
            //public void AddPallet(Pallet pallet)
            //{
            //    pallet.CellObjectId = this.ObjectId;


            //    //FIXME доделать расчёт поддона в ячейке (нужно перобразование из реальных размеров в экранные перед тем, как делать расчёт)

            //    int palletHeightNew = (int)((pallet.Square / this.Square) * this.Height);
            //    pallet.Height = palletHeightNew;
            //    pallet.Width = this.Width;
            //    pallet.X = this.X;
            //    pallet.Y = this.Y + Pallets.Sum(x => x.Height);

            //    Pallets.Add(pallet);
            //}

            public override string GetDataText()
            {
                string text = $"Ячейка {Data["CELL_NAME"]}.{Environment.NewLine}" +
                    $"Размеры {Data["CELL_WIDTH"]} {Data["CELL_LENGTH"]}{Environment.NewLine}" +
                    $"-------{Environment.NewLine}" +
                    $"Ид {ObjectId}{Environment.NewLine}" +
                    $"Координаты {X} {Y}{Environment.NewLine}" +
                    $"Размеры {Width} {Height}{Environment.NewLine}" +
                    $"Сторона загрузки {EntranceSide}{Environment.NewLine}" +
                    $"Поддонов {Pallets.Count}{Environment.NewLine}";

                return text;
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
            }

            public int CellObjectId;

            public override void SaveDefaults()
            {
                this.DefaultX = this.X;
                this.DefaultY = this.Y;
                this.DefaultWidth = this.Width;
                this.DefaultHeight = this.Height;
            }

            public override string GetDataText()
            {
                string text = $"Поддон {Data["PALLET_ID"]} [{Data["PALLET_NAME"]}].{Environment.NewLine}" +
                    $"Продукция {Data["PRODUCT_NAME"]}{Environment.NewLine}" +
                    $"Ячейка {Data["CELL_NAME"]}{Environment.NewLine}" +
                    $"Размеры {Data["PALLET_WIDTH"]} {Data["PALLET_LENGTH"]}{Environment.NewLine}" +
                    $"-------{Environment.NewLine}" +
                    $"Ид {ObjectId}{Environment.NewLine}" +
                    $"Размеры {Width} {Height}{Environment.NewLine}" +
                    $"Координаты {X} {Y}{Environment.NewLine}" +
                    $"Находится в ячейке {CellObjectId}";

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

        private void ZoomMinusButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ZoomTextBox.Text))
            {
                Zoom(ZoomTextBox.Text.ToInt(), 2);
            }
        }

        private void ZoomPlusButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ZoomTextBox.Text))
            {
                Zoom(ZoomTextBox.Text.ToInt(), 1);
            }
        }

        public ToolTip tt { get; set; }

        private void img_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (tt != null)
            {
                tt.IsOpen = false;
            }

            bool succes = false;

            var d = e.GetPosition(img);

            var dd = e.GetPosition(null);

            var x = d.X;
            var y = d.Y;

            //Block block = new Block();
            for (int i = 0; i < Blocks.Count; i++)
            {
                if (Blocks[i].X <= x
                    && Blocks[i].X + Blocks[i].Width >= x)
                {
                    if (Blocks[i].Y <= y
                        && Blocks[i].Y + Blocks[i].Height >= y)
                    {
                        var block = Blocks[i];
                        //block.Red = 255;
                        //block.Green = 0;
                        //block.Blue = 0;

                        tt = new ToolTip();
                        tt.PlacementRectangle = new Rect(dd.X, dd.Y, block.Width, block.Height);
                        tt.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                        tt.Content = block.GetDataText();
                        tt.IsOpen = true;

                        succes = true;

                        //Blocks[i] = block;

                        break;
                    }
                }
            }

            //RenderItems();

            if (!succes)
            {
                //tt = new ToolTip();
                //tt.PlacementRectangle = new Rect(dd.X, dd.Y, 100, 100);
                //tt.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                //tt.Content = $"x:{d.X} y:{d.Y}";
                //tt.IsOpen = true;

                Cell cell = new Cell();
                cell.X = (int)d.X;
                cell.Y = (int)d.Y;
                cell.Width = 10;
                cell.Height = 10;
                cell.Red = 255;
                cell.Green = 0;
                cell.Blue = 0;
                cell.Alpha = 255;



                RenderBlock(cell);
            }
        }

        private void imgScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            BackgroundImageScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
            BackgroundImageScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
        }

        public int currentSliderValue;

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int value = (int)e.NewValue;

            if (value != currentSliderValue)
            {
                currentSliderValue = value;

                if (value >= 0)
                {
                    value++;
                    Zoom(value, 1);
                }
                else
                {
                    value--;
                    value = Math.Abs(value);
                    Zoom(value, 2);
                }
            }
        }
    }
}
