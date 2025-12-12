using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;

namespace Client.Common
{
    /// <summary>
    /// Преобразует текст в изображение. Доступно сохранение в виде файла в форматах JPG, PNG, BMP
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public class TextImageGenerator
    {
        private Color TextColor { get; set; }
        private Color BackgroundColor { get; set; }
        private Font Font { get; set; }
        private int Padding { get; set; }
        private int FontSize { get; set; }

        public TextImageGenerator(int fontSize=20, string font="Arial", int padding=10)
        {
            TextColor = Color.Black;
            BackgroundColor = Color.White;
            Font = new Font(font, fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            Padding = padding;
            FontSize = fontSize;
        }

        /// <summary>
        /// Создание пиксельного рисунка из заданного текста
        /// </summary>
        /// <param name="text">текст</param>
        /// <returns></returns>
        public Bitmap CreateBitmap(string text)
        {
            // Create graphics for rendering 
            Graphics retBitmapGraphics = Graphics.FromImage(new Bitmap(1, 1));
            // measure needed width for the image
            var bitmapWidth = (int)retBitmapGraphics.MeasureString(text, Font).Width;
            // measure needed height for the image
            var bitmapHeight = (int)retBitmapGraphics.MeasureString(text, Font).Height;
            // Create the bitmap with the correct size and add padding
            Bitmap retBitmap = new Bitmap(bitmapWidth + Padding, bitmapHeight + Padding);
            // Add the colors to the new bitmap.
            retBitmapGraphics = Graphics.FromImage(retBitmap);
            // Set Background color
            retBitmapGraphics.Clear(BackgroundColor);
            retBitmapGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            retBitmapGraphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            retBitmapGraphics.DrawString(text, Font, new SolidBrush(TextColor), Padding / 2, Padding / 2);
            // flush the changes
            retBitmapGraphics.Flush();

            return (retBitmap);
        }

        /// <summary>
        /// Изображение, полученное из текста, преобразуется в формат Base64
        /// </summary>
        /// <param name="text"></param>
        /// <param name="imageFormat"></param>
        /// <returns></returns>
        public string CreateBase64Image(string text, ImageFormat imageFormat)
        {
            var bitmap = CreateBitmap(text);
            var stream = new System.IO.MemoryStream();
            // save into stream
            bitmap.Save(stream, imageFormat);
            // convert to byte array
            var imageBytes = stream.ToArray();
            // convert to base64 string
            return Convert.ToBase64String(imageBytes);
        }

        /// <summary>
        /// Сохранение файла в формете JPG заданного текста
        /// </summary>
        /// <param name="filename">путь к файлу</param>
        /// <param name="text">текст</param>
        public void SaveAsJpg(string filename, string text)
        {
            var bitmap = CreateBitmap(text);
            bitmap.Save(filename, ImageFormat.Jpeg);
        }

        /// <summary>
        /// Сохранение файла в формете PNG заданного текста
        /// </summary>
        /// <param name="filename">путь к файлу</param>
        /// <param name="text">текст</param>
        public void SaveAsPng(string filename, string text)
        {
            var bitmap = CreateBitmap(text);
            bitmap.Save(filename, ImageFormat.Png);
        }

        /// <summary>
        /// Сохранение файла в формете BMP заданного текста
        /// </summary>
        /// <param name="filename">путь к файлу</param>
        /// <param name="text">текст</param>
        public void SaveAsBmp(string filename, string text)
        {
            var bitmap = CreateBitmap(text);
            bitmap.Save(filename, ImageFormat.Bmp);
        }

    }
}
