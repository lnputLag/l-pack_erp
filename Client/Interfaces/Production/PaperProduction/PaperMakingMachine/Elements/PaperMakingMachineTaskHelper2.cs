using Client.Assets.HighLighters;
using Client.Common;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Вспомогательный класс для раскраски ячеек грида и добавления иконок
    /// </summary>
    public static class PaperMakingMachineTaskHelper2
    {
        /// <summary>
        /// Возвращает цвет для ячейки списка заданий
        /// </summary>
        public static object GetColor(string fieldName, Dictionary<string, string> row)
        {
            var result = DependencyProperty.UnsetValue;
            var color = "";

            if (fieldName == "NAME")
            {
                if (row.CheckGet("GLUED_FLAG").ToInt() == 1)
                {
                    //   R  G   B
                    // 221-215-172   Оливковый
                    color = $"#ddd7ac";
                }
                else if ((row.CheckGet("NAME").ToString() == "Б0") || (row.CheckGet("NAME").ToString() == "К0"))
                {
                    //   R  G   B
                    // 127-255-127   Зеленый
                    color = $"#7fff7f";
                }
            }
            else if ((fieldName == "BALANCE1") || (fieldName == "B1"))
            {
                if (row.CheckGet("SALE_FLAG1").ToInt() == 1)
                {
                    //   R  G   B
                    // 172-237-247   морская волна
                    color = $"#acedf7";
                }
            }
            else if ((fieldName == "BALANCE2") || (fieldName == "B2"))
            {
                if (row.CheckGet("SALE_FLAG2").ToInt() == 1)
                {
                    //   R  G   B
                    // 172-237-247   морская волна
                    color = $"#acedf7";
                }
            }
            else if ((fieldName == "BALANCE3") || (fieldName == "B3"))
            {
                if (row.CheckGet("SALE_FLAG3").ToInt() == 1)
                {
                    //   R  G   B
                    // 172-237-247   морская волна
                    color = $"#acedf7";
                }
            }

            if (!color.IsNullOrEmpty())
            {
                result = color.ToBrush();
            }

            return result;
        }

        /// <summary>
        /// Аббревиатура на значке и её расшифровка
        /// </summary>
        public static Dictionary<string, string> DecodeIconTexts { get; set; } = new Dictionary<string, string>()
        {
            { "!",  "11111111"},
            { "ПЛ", "Плоские рилевки" },
        };

        /// <summary>
        /// Возвращает необходимые иконки для списка заданий
        /// </summary>
        public static List<Border> GetIcons1(string fieldName, Dictionary<string, string> row)
        {
            var list = new List<Border>();

            var stackIcons = new StackPanel();
            stackIcons.Orientation = Orientation.Horizontal;
            stackIcons.HorizontalAlignment = HorizontalAlignment.Left;

            // Изделие
            if (fieldName == "NAME")
            {
                stackIcons.HorizontalAlignment = HorizontalAlignment.Right;
                string note = "";
                if (!row.CheckGet("NOTE").IsNullOrEmpty())
                {
                    note = row.CheckGet("NOTE");
                    DecodeIconTexts["!"] = note;
                    var b = MakeBorder(HColor.RedFG, HColor.Yellow, "!");
                    stackIcons.Children.Add(b);
                }
            }

            {
                var b = new Border();
                b.Child = stackIcons;
                list.Add(b);
            }

            return list;
        }

        private static Border MakeBorder(string borderColor, string textColor, string iconText)
        {
            var b = new Border();
            b.Width = 20;
            b.Height = 15;
            b.Background = borderColor.ToBrush();
            b.HorizontalAlignment = HorizontalAlignment.Left;
            b.VerticalAlignment = VerticalAlignment.Top;
            b.Margin = new Thickness(2, 0, 2, 0);
            b.CornerRadius = new CornerRadius(5);

            var textBlock = new TextBlock();
            textBlock.Text = iconText;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.Foreground = textColor.ToBrush();
            textBlock.FontSize = 10;
            b.Child = textBlock;

            var tooltipText = DecodeIconTexts.CheckGet(iconText);
            if (!tooltipText.IsNullOrEmpty())
            {
                var tooltip = new ToolTip();
                tooltip.Content = tooltipText;
                b.ToolTip = tooltip;
            }

            return (b);
        }
    }
}
