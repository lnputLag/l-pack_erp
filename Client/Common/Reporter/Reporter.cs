using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Client.Common.Lib.Reporter;

namespace Client.Common.Reporter
{
    /// <summary>
    /// Документ, объект-агрегатор, к нему добавляются элементы отчета.
    /// Он содержит массив свойств DocumentOptions.
    /// Он производит рендер внутренних элементов и сохранение на диск.
    /// </summary>
    public class Document
    {
        /// <summary>
        /// Набор свойств
        /// </summary>
        public DocumentOptions Options { get; set; }
        /// <summary>
        /// Стек элементов документа
        /// </summary>
        public List<IBlock> Blocks { get; set; }
        /// <summary>
        /// Флаг рендера, поднимается, когда полотно с элементами отрендерено и готово к выводу в файл
        /// </summary>
        private bool Rendered { get; set; }
        /// <summary>
        /// Координатная сетка, при подъеме будет выведена сетка с координатами на базовое полотно
        /// документа, служит для отладки.
        /// </summary>
        public bool Grid { get; set; }
        /// <summary>
        /// текущая сборка (для извлечения ресурсов)
        /// </summary>
        public Assembly CurrentAssembly { get; set; }

        /// <summary>
        /// Конструктор.
        /// На вход нужно подать размеры полотна в пикселях.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public Document(int width, int height)
        {
            CurrentAssembly = Assembly.GetExecutingAssembly();

            Options = new DocumentOptions { Width = width, Height = height };


            Rendered = false;
            Grid = false;

        }

        /// <summary>
        /// Инициализация. Должна выполняться после выполнения конструктора,
        /// после установки дополнительных свойств, перед рендером.
        /// </summary>
        public void Init()
        {
            //position
            Options.CurrentX = Options.MarginLeft;
            Options.CurrentY = Options.MarginTop;

            //canvas 
            Options.Image = new Image<Rgba32>(Options.Width, Options.Height);

            //canvas bg
            var rect = new Rectangle(0, 0, Options.Width, Options.Height);
            Options.Image.Mutate(x => x.Fill(Options.Background, rect));

            //var c=Color.FromRgba(255,0,0,100);
            //Options.Image.Mutate( x=> x.Fill(c, rect));

            //fonts
            Options.FontCollection = new FontCollection();

            //загрузка шрифтов: один из методов: 1 или 2

            //(1)загрузка из фс
            //Options.BaseFontFamily = Options.FontCollection.Install($"{Options.FontsPath}{Options.BaseFontName}");

            //(2)загрузка из ресурсов сборки            
            var stream = CurrentAssembly.GetManifestResourceStream($"{Options.FontsSrc}.{Options.BaseFontName}");
            Options.BaseFontFamily = Options.FontCollection.Install(stream);


            Options.BaseFont = Options.BaseFontFamily.CreateFont(Options.BaseFontSize, Options.BaseFontStyle);

            Blocks = new List<IBlock>();

        }

        /// <summary>
        /// Добавление блока в стек блоков
        /// </summary>
        /// <param name="block"></param>
        public void AddBlock(IBlock block)
        {
            Blocks.Add(block);
        }

        /// <summary>
        /// Рендер, отрисовка документа
        /// </summary>
        public void Render()
        {

            //отрисовка координатной сетки
            if (Grid)
            {

                var gridStep = 50;
                var borderPen = Pens.Solid(Color.LightGray, 1);
                var textColor = Color.Black;
                var font = Options.BaseFontFamily.CreateFont(10, FontStyle.Regular);

                for (int px = Options.MarginTop; px < Options.Width; px = px + gridStep)
                {
                    Options.Image.Mutate(x => x.Draw(
                       borderPen,
                       new Rectangle(px, 0, gridStep, Options.Height)
                   ));

                    Options.Image.Mutate(x => x.DrawText(
                       $"{px}",
                       font,
                       textColor,
                       new PointF(px + 2, 2)
                   ));
                }

                for (int py = Options.MarginLeft; py < Options.Height; py = py + gridStep)
                {
                    Options.Image.Mutate(x => x.Draw(
                       borderPen,
                       new Rectangle(0, py, Options.Width, gridStep)
                   ));

                    Options.Image.Mutate(x => x.DrawText(
                       $"{py}",
                       font,
                       textColor,
                       new PointF(2, py + 2)
                   ));
                }

            }

            //отрисовка всех блоков
            if (Blocks.Count > 0)
            {
                foreach (IBlock b in Blocks)
                {
                    //передача текущих кординат блоку
                    if (b.PositionType == 1)
                    {
                        //абсолютно
                        b.PositionX = b.CurrentX;
                        b.PositionY = b.CurrentY;
                    }
                    else
                    {
                        //отностиельно
                        b.PositionX = Options.CurrentX;
                        b.PositionY = Options.CurrentY;
                    }

                    //отрисовка
                    b.Render(Options);

                    //возврат текущих координат из блока
                    Options.CurrentX = b.PositionX;
                    Options.CurrentY = b.PositionY;

                    //отступ снизу
                    Options.CurrentY = Options.CurrentY + Options.BlockMarginBottom;
                }
            }

        }

        /// <summary>
        /// Вывод документа на диск в указанный файл
        /// </summary>
        /// <param name="filename"></param>
        public void Save(string filename)
        {
            //если полотно до сих пор не отрендерено, рендерим
            if (!Rendered)
            {
                Render();
                Rendered = true;
            }

            Options.Image.Save(filename);

            //Options.Image.
        }

    }

 
    /// <summary>
    /// Базовый класс элемента блока
    /// </summary>
    public class Block : IBlock
    {
        /// <summary>
        /// Текущая координата Х
        /// </summary>
        public int CurrentX { get; set; }
        /// <summary>
        /// Текущая координата У
        /// </summary>
        public int CurrentY { get; set; }

        /*
            Блоки будут рендериться последовательно.
            Каждый следующий блок берет текущие координаты из документа,
            отрисовывает свои внутренности, и возвращает текущие координаты
            обратно в документ.
            При отрисовке своих внутренних элементов, блок инкрементирует
            эти координаты. Они всегда содержат позицию текущего элемента,
            который сейчас отрисовывается.
         */

        /// <summary>
        /// Тип позиционирования, внутренняя структура
        /// 0--rel,1--abs
        /// </summary>
        public int PositionType { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int PositionX { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int PositionY { get; set; }

        /// <summary>
        /// конструктор
        /// </summary>
        public Block()
        {
            CurrentX = 0;
            CurrentY = 0;
            PositionType = 0;
        }

        /// <summary>
        /// Установка абсолютных координат.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SetAbsPosition(int x, int y)
        {
            CurrentX = x;
            CurrentY = y;
            PositionType = 1;
        }

        /// <summary>
        /// Возврат к относительным координатам (относительно предыдущего блока).
        /// </summary>
        public void SetRelPosition()
        {
            CurrentX = 0;
            CurrentY = 0;
            PositionType = 0;
        }

        /// <summary>
        /// Инициализация. Должна выполняться после выполнения конструктора,
        /// после установки дополнительных свойств, перед рендером.
        /// </summary>
        /// <param name="options"></param>
        public virtual void Init(DocumentOptions options)
        {

        }

        /// <summary>
        /// Отрисовка.
        /// </summary>
        /// <param name="options"></param>
        public virtual void Render(DocumentOptions options)
        {

        }
    }

    /// <summary>
    /// Таблица с данными.
    /// Отрисовывает полноценную таблицу с заголовками колонок и строками.
    /// </summary>
    public class Table : Block
    {
        /// <summary>
        /// Толщина границ
        /// </summary>
        public int BorderWidth { get; set; }
        /// <summary>
        /// Стек колонок
        /// </summary>
        private Dictionary<string, TableColumn> Columns { get; set; }
        /// <summary>
        /// Стек строк
        /// </summary>
        private List<Dictionary<string, string>> Rows { get; set; }
        /// <summary>
        /// Перо для отрисовки границ
        /// </summary>
        private IPen BorderPen { get; set; }
        /// <summary>
        /// Цвет текста
        /// </summary>
        private Color TextColor { get; set; }
        /// <summary>
        /// Ширина колонки по умолчанию
        /// </summary>
        private int CellWidth { get; set; }
        /// <summary>
        /// Высота колонки по умолчанию
        /// </summary>
        private int CellHeight { get; set; }
        /// <summary>
        /// Отступ текста сверху внутри ячейки
        /// </summary>
        private int CellTextMarginTop { get; set; }
        /// <summary>
        /// Отступ текста слева внутри ячейки
        /// </summary>
        private int CellTextMarginLeft { get; set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        public Table()
        {
            //consts
            BorderPen = Pens.Solid(Color.Black, 1);
            TextColor = Color.Black;
            CellWidth = 40;
            CellHeight = 20;
            CellTextMarginTop = 4;
            CellTextMarginLeft = 4;
            BorderWidth = 1;

            //registers
            Columns = new Dictionary<string, TableColumn>();
            Rows = new List<Dictionary<string, string>>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="column"></param>
        public void AddColumn(TableColumn column)
        {
            Columns.Add(column.Name, column);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        public void AddRow(Dictionary<string, string> row)
        {
            Rows.Add(row);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        public override void Render(DocumentOptions options)
        {
            var o = options;

            //border
            if (BorderWidth > 0)
            {
                BorderPen = Pens.Solid(Color.Black, BorderWidth);
            }
            else
            {
                BorderPen = Pens.Solid(Color.Black, 0);
            }


            //check columns
            bool renderColumns = false;

            if (Columns.Count > 0)
            {
                bool oneColHasTitle = false;
                foreach (KeyValuePair<string, TableColumn> column in Columns)
                {
                    var c = column.Value;
                    if (!string.IsNullOrEmpty(c.Title))
                    {
                        oneColHasTitle = true;
                    }
                }

                if (oneColHasTitle)
                {
                    renderColumns = true;
                }

            }


            //render columns    
            if (renderColumns)
            {
                int px = PositionX;
                int py = PositionY;

                foreach (KeyValuePair<string, TableColumn> column in Columns)
                {
                    var c = column.Value;

                    int w = CellWidth;
                    if (c.Width != 0)
                    {
                        w = c.Width;
                    }

                    o.Image.Mutate(x => x.Draw(
                       BorderPen,
                       new Rectangle(px, py, w, CellHeight)
                   ));


                    var font = o.BaseFontFamily.CreateFont(c.FontSize, c.FontStyle);

                    o.Image.Mutate(x => x.DrawText(
                       c.Title,
                       font,
                       TextColor,
                       new PointF(px + CellTextMarginLeft, py + CellTextMarginTop)
                   ));

                    px = px + c.Width;

                }

                PositionY = PositionY + CellHeight;
            }

            //render rows
            if (Rows.Count > 0)
            {
                foreach (Dictionary<string, string> row in Rows)
                {
                    if (row.Count > 0)
                    {

                        int px = PositionX;
                        int py = PositionY;

                        foreach (KeyValuePair<string, TableColumn> column in Columns)
                        {
                            var c = column.Value;
                            var k = column.Key;

                            var v = "";
                            if (row.ContainsKey(k))
                            {
                                v = row[k];
                            }

                            int w = CellWidth;
                            if (c.Width != 0)
                            {
                                w = c.Width;
                            }

                            o.Image.Mutate(x => x.Draw(
                               BorderPen,
                               new Rectangle(px, py, w, CellHeight)
                           ));


                            var font = o.BaseFontFamily.CreateFont(c.FontSize, c.FontStyle);

                            o.Image.Mutate(x => x.DrawText(
                               v,
                               font,
                               TextColor,
                               new PointF(px + CellTextMarginLeft, py + CellTextMarginTop)
                           ));

                            px = px + c.Width;

                        }

                        PositionY = PositionY + CellHeight;
                    }
                }
            }

        }
    }

    /// <summary>
    /// Структура колонки таблицы (вспомогательная структура)
    /// </summary>
    public class TableColumn
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public int Width { get; set; }
        public FontStyle FontStyle { get; set; }
        public int FontSize { get; set; }
        public TableColumn()
        {
            Name = "";
            Title = "";
            Width = 40;
            FontStyle = FontStyle.Regular;
            FontSize = 12;
        }
    }

    /// <summary>
    /// Текстовый блок, отрисовывает текст внутри контейнера заданного размера.
    /// </summary>
    public class Paragraph : Block
    {
        /// <summary>
        /// Текст
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Стиль текста
        /// </summary>
        public FontStyle FontStyle { get; set; }
        /// <summary>
        /// Размер текста
        /// </summary>
        public int FontSize { get; set; }
        /// <summary>
        /// Цвет текста
        /// </summary>
        public Color TextColor { get; set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        public Paragraph()
        {
            Text = "";
            FontStyle = FontStyle.Regular;
            FontSize = 12;
            TextColor = Color.Black;
        }

        /// <summary>
        /// Отрисовка
        /// </summary>
        /// <param name="options"></param>
        public override void Render(DocumentOptions options)
        {
            var o = options;

            {
                int px = PositionX;
                int py = PositionY;

                var font = o.BaseFontFamily.CreateFont(FontSize, FontStyle);

                o.Image.Mutate(x => x.DrawText(
                   Text,
                   font,
                   TextColor,
                   new PointF(px, py)
               ));
            }

        }
    }

    /// <summary>
    /// Пустой блок, отрисовывает прямоугольник с заданными размерами.
    /// </summary>
    public class Gizmo : Block
    {
        /// <summary>
        /// Ширина
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// Высота
        /// </summary>
        public int Height { get; set; }
        /// <summary>
        /// Флаг наличия границы
        /// </summary>
        public bool Border { get; set; }
        /// <summary>
        /// Ширина границы (при ее наличии)
        /// </summary>
        public int BorderWidth { get; set; }

        /// <summary>
        /// Внутренняя структура пера отрисовки границ
        /// </summary>
        private IPen BorderPen { get; set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        public Gizmo()
        {
            Width = 100;
            Height = 100;
            Border = true;
            BorderWidth = 1;
        }

        /// <summary>
        /// Отрисовка
        /// </summary>
        /// <param name="options"></param>
        public override void Render(DocumentOptions options)
        {
            var o = options;

            //граница
            if (Border && BorderWidth > 0)
            {
                BorderPen = Pens.Solid(Color.Black, BorderWidth);
            }
            else
            {
                BorderPen = Pens.Solid(Color.Black, 0);
            }

            {
                int px = PositionX;
                int py = PositionY;

                o.Image.Mutate(x => x.Draw(
                   BorderPen,
                   new Rectangle(px, py, Width, Height)
               ));

                PositionY = PositionY + Height;
            }

        }
    }
}
