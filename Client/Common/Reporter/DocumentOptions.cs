using SixLabors.Fonts;
using SixLabors.ImageSharp;

namespace Client.Common.Lib.Reporter
{
    /// <summary>
    /// структура параметров документа
    /// </summary>
    public class DocumentOptions
    {
        /// <summary>
        /// полотно для формирования изображения
        /// </summary>
        public Image Image { get; set; }
        /// <summary>
        /// фон полотна, по умолчанию белый
        /// </summary>
        public Color Background { get; set; }

        /// <summary>
        /// текущая позиция Х
        /// </summary>
        public int CurrentX { get; set; }
        /// <summary>
        /// Текущая позиция У
        /// </summary>        
        public int CurrentY { get; set; }

        /*
            При формировании документа работает поточный алгоритм:
            первый блок позиционируется от опорной точки документа 
            (верхний левый угол + отступы: MarginLeft+MarginTop).
            Следующий блок располагается ниже предыдущего,
            с отступом от него в BlockMarginBottom
         */

        /// <summary>
        /// Отступ слева от края полотна документа
        /// </summary>
        public int MarginLeft { get; set; }
        /// <summary>
        /// Отступ сверху от края полотна документа
        /// </summary>
        public int MarginTop { get; set; }
        /// <summary>
        /// ширина документа
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// Высота документа
        /// </summary>
        public int Height { get; set; }
        /// <summary>
        /// Вертикальный отступ между блоками
        /// </summary>
        public int BlockMarginBottom { get; set; }

        /// <summary>
        /// Путь хранения шрифтов по умолчанию
        /// </summary>
        public string FontsPath { get; set; }
        /// <summary>
        /// Локатор ресурсов шрифтов в сборке
        /// </summary>
        public string FontsSrc { get; set; }
        /// <summary>
        /// Имя базового шрифта
        /// </summary>
        public string BaseFontName { get; set; }
        /// <summary>
        /// Размер базового шрифта
        /// </summary>
        public int BaseFontSize { get; set; }
        /// <summary>
        /// Стиль базового шрифта
        /// </summary>
        public FontStyle BaseFontStyle { get; set; }

        /*
           Должен быть хотя бы один шрифт TTF, лежащий в папке FontsPath.
           Будет загружен базовый шрифт по умолчанию.
        */

        /// <summary>
        /// Внутренняя структура коллекции шрифтов
        /// </summary>
        public FontCollection FontCollection { get; set; }
        /// <summary>
        /// Внутренняя структура семейства шрифтов
        /// </summary>
        public FontFamily BaseFontFamily { get; set; }
        /// <summary>
        /// Внутренняя структура базового шрифта
        /// </summary>
        public Font BaseFont { get; set; }

        /// <summary>
        /// конструктор
        /// </summary>
        public DocumentOptions()
        {
            Background = Color.White;
            CurrentX = 0;
            CurrentY = 0;
            MarginLeft = 20;
            MarginTop = 20;
            Width = 640;
            Height = 480;
            BlockMarginBottom = 20;
            FontsPath = "./../../../Assets/Fonts/";
            FontsSrc = "Client.Assets.Fonts";
            BaseFontName = "SEGOEUI.TTF";
            BaseFontSize = 12;
            BaseFontStyle = FontStyle.Regular;

        }

    }
}
