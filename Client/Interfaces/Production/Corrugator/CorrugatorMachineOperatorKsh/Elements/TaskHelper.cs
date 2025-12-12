using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh
{
    /// <summary>
    /// Вспомогательный класс для раскраски ячеек грида и добавления иконок
    /// </summary>
    public static class TaskHelper
    {
        /// <summary>
        /// Раскраска каждого сырья
        /// </summary>
        public static Dictionary<string, int> PaperCodesForPlanning { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Возвращает цвет для ячейки списка заданий
        /// </summary>
        public static object GetColor(string fieldName, Dictionary<string, string> row)
        {
            var result = DependencyProperty.UnsetValue;
            var color = "";


            if(fieldName == "ON_MACHINE")
            {
                if (row.CheckGet("ON_MACHINE").ToInt() == 1)
                {
                    color = HColor.Gray;
                }
            }

            // Начать до
            else if (fieldName == "START_BEFORE")
            {
                if (!row.CheckGet("START_BEFORE").IsNullOrEmpty())
                {
                    var bwDateTime = row.CheckGet("START_BEFORE").ToDateTime();
                    double hours = (bwDateTime - DateTime.Now).TotalHours;
                    // задание просрочено
                    if (hours < 0)
                    {
                        if (hours > -8000)
                        {
                            color = HColor.Red;
                        }
                    }
                    // до планового начала задания меньше 2х часов
                    else if (hours < 2)
                    {
                        color = HColor.Orange;
                    }
                    // до планового начала задания меньше 4х часов
                    else if (hours < 4)
                    {
                        color = HColor.YellowOrange;
                    }
                }
            }

            // профиль
            else if (fieldName == "PROFIL_NAME")
            {
                var profil = row.CheckGet("PROFIL_NAME").Trim();

                // латиница ~ кириллица 
                if (profil == "B" || profil == "В")
                {
                    color = HColor.Violet;
                }
                if (profil == "C" || profil == "С")
                {
                    color = HColor.BlueFG;
                }
                if (profil == "E" || profil == "Е")
                {
                    color = HColor.PinkOrange;
                }
                if (profil == "BB" || profil == "ВВ")
                {
                    color = HColor.Green;
                }
                if (profil == "BC" || profil == "ВС")
                {
                    color = HColor.Yellow;
                }
                if (profil == "BE" || profil == "ВЕ")
                {
                    color = HColor.Olive;
                }
                if (profil == "EB" || profil == "ЕВ")
                {
                    color = HColor.Blue;
                }
                if (profil == "EC" || profil == "ЕС")
                {
                    color = HColor.Orange;
                }
            }

            //формат
            else if (fieldName == "WIDTH")
            {
                int width = row.CheckGet("WIDTH").ToInt();
                int offset = (width - 1400) / 8;
                offset = offset.Clamp(0, 255);
                //отенки жёлтый - зелёный - бирюзовый
                color = $"#{255 - offset:x2}{255 - offset / 4:x2}{127 + offset/2:x2}";
            }

            // станок
            else if (fieldName == "SNAME")
            {
                // задание на фенфолд
                if (row.CheckGet("KOL_PAK").ToInt() > 0)
                {
                    color = HColor.YellowOrange;
                }
                
            }

            // сырьё (рулоны)
            else if (fieldName == "LAYER_1"
                || fieldName == "LAYER_2"
                || fieldName == "LAYER_3"
                || fieldName == "LAYER_4"
                || fieldName == "LAYER_5")
            {
                //Образец
                if (row.CheckGet("LEN").ToInt() > 0
                    && row.CheckGet("IS_SAMPLE").ToInt() > 0)
                {
                    color = HColor.Green;
                }

                // используется на раскате крашенный картон
                int layerNumber = fieldName.Substring(6).ToInt();
                if (row.CheckGet($"PAINTED_FLAG_{layerNumber}").ToInt() == 1)
                {
                    color = HColor.Pink;
                }

                // Позиция заблокирована, берется из tovar
                if (row.CheckGet("BLOCKING").ToInt() == 1)
                {
                    color = HColor.BlackFG;
                }

                // раскраска различного сырья для планирования
                if (row.CheckGet("DURATION").ToDouble() > 0)
                {
                    string paperName = row.CheckGet($"LAYER_{layerNumber}");
                    if (!paperName.IsNullOrEmpty())
                    {
                        int code = PaperCodesForPlanning.CheckGet(paperName);
                        if (code == 0)
                        {
                            int nextCode = (PaperCodesForPlanning.Count + 1).Clamp(1, 32);

                            PaperCodesForPlanning.CheckAdd(paperName, nextCode);
                            code = nextCode;
                        }
                        //отенки жёлтый - зелёный - бирюзовый
                        color = $"#{256 - code * 8:x2}{255 - code * 2:x2}{127 + code * 2:x2}";
                    }
                }
                else
                    ;
            }

            // обрезь
            else if (fieldName == "OBREZ")
            {
                // слишком маленькая или слишком широкая обрезь
                if (row.CheckGet("OBREZ").ToInt() < 50
                    || row.CheckGet("OBREZ").ToInt() > 200)
                {
                    color = HColor.Red;
                }
            }

            // качество
            else if (fieldName == "CHECK_QID")
            {
                int num = row.CheckGet("CHECK_QID").ToInt();

                // первый раз на этом станке
                // если сейчас на БХС, на соседнем БХС данной композиции также не было 
                if (num == 1)
                {
                    color = HColor.BlueDark;
                }
                // первый раз на этом станке
                // если сейчас на БХС, на соседнем БХС данная композиция уже была, можно посмотреть у них настройки станка 
                if (num == 2)
                {
                    color = HColor.Green;
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
            { "!",  ""},
            { "СПЛ",  "Включить сплайсер вручную."},
            { "НРП", "Заготовка с несимметричной рилевкой, необходимо обмотать пленкой." },
            { "ДНК", "Допускается необрезная кромка" },
            { "ПП", "Использование пластиковых поддонов." },
            { "ЗП", "Заливная печать" },
            { "ББ", "Белый с двух сторон" },
            { "СР", "Сверьте рилевки с техкартой" },
            { "ПЛ", "Плоские рилевки" },
            { "Л", "Лаборатория" },
            { "Z", "Z Картон" }
        };

        /// <summary>
        /// Создание лейблов адаптация под грид4
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static List<DataGridHelperColumnLabel> CreateLabel(string columnName)
        {
            var result = new List<DataGridHelperColumnLabel>();

            // формат (ширина)
            if (columnName == "WEB_WIDTH")
            {
                result.Add(new DataGridHelperColumnLabel()
                {
                    Construct = () =>
                    {
                        var label = DataGridHelperColumnLabel.MakeElement("НРП", HColor.GreenFG, HColor.Yellow, fontSize: 6, toolTip: "Заготовка с несимметричной рилевкой, необходимо обмотать пленкой.");
                        return label;
                    },
                    Update = (Dictionary<string, string> row) =>
                    {
                        var visibility = Visibility.Collapsed;
                        if (row.CheckGet("NOTE").Contains(DecodeIconTexts.CheckGet("НРП")))
                        {
                            visibility = Visibility.Visible;
                        }
                        return visibility;
                    },
                    Position = LabelPosition.Left
                });
            }

            // станок
            if (columnName == "MACHINE")
            {
                // Пластиковые поддоны
                result.Add(new DataGridHelperColumnLabel()
                {
                    Construct = () =>
                    {
                        var label = DataGridHelperColumnLabel.MakeElement("ПП", HColor.BlueFG, HColor.Yellow, fontSize: 6, toolTip: "Использование пластиковых поддонов.");
                        return label;
                    },
                    Update = (Dictionary<string, string> row) =>
                    {
                        var visibility = Visibility.Collapsed;
                        string note = row.CheckGet("NOTE");
                        if (!note.IsNullOrEmpty() && note.Contains(DecodeIconTexts.CheckGet("ПП")))
                        {
                            visibility = Visibility.Visible;
                        }
                        return visibility;
                    },
                    Position = LabelPosition.Right
                });

                // Уникальное примечание
                result.Add(new DataGridHelperColumnLabel()
                {
                    Construct = () =>
                    {
                        var label = DataGridHelperColumnLabel.MakeElement("!", HColor.RedFG, HColor.Yellow, fontSize: 8);
                        return label;
                    },
                    Update = (Dictionary<string, string> row) =>
                    {
                        var visibility = Visibility.Collapsed;
                        string note = row.CheckGet("NOTE");
                        if (!note.IsNullOrEmpty())
                        {
                            note = note.Replace(DecodeIconTexts.CheckGet("НРП"), "");
                            note = note.Replace(DecodeIconTexts.CheckGet("ДНК"), "");
                            note = note.Replace(DecodeIconTexts.CheckGet("ПП"), "");
                            note = note.Trim(new char[] { '.', ',', ' ' });

                            if (!note.IsNullOrEmpty())
                            {
                                visibility = Visibility.Visible;
                            }
                        }
                        return visibility;
                    },
                    Position = LabelPosition.Right
                });

                // Лаборатория
                result.Add(new DataGridHelperColumnLabel()
                {
                    Construct = () =>
                    {
                        var label = DataGridHelperColumnLabel.MakeElement("Л", HColor.BlueFG, HColor.Yellow, fontSize: 6, toolTip: "Лаборатория");
                        return label;
                    },
                    Update = (Dictionary<string, string> row) =>
                    {
                        var visibility = Visibility.Collapsed;
                        if (row.CheckGet("TESTING_FLAG").ToInt() > 0 && row.CheckGet("MACHINE").Contains("СГП"))
                        {
                            visibility = Visibility.Visible;
                        }
                        return visibility;
                    },
                    Position = LabelPosition.Right
                });

                // Плоские рилевки
                result.Add(new DataGridHelperColumnLabel()
                {
                    Construct = () =>
                    {
                        var label = DataGridHelperColumnLabel.MakeElement("ПЛ", HColor.RedFG, HColor.Yellow, fontSize: 6, toolTip: "Плоская рилевка");
                        return label;
                    },
                    Update = (Dictionary<string, string> row) =>
                    {
                        var visibility = Visibility.Collapsed;
                        if (row.CheckGet("STYPE").ToInt() == 2)
                        {
                            visibility = Visibility.Visible;
                        }
                        return visibility;
                    },
                    Position = LabelPosition.Right
                });

                // Сверьте рилевки
                result.Add(new DataGridHelperColumnLabel()
                {
                    Construct = () =>
                    {
                        var label = DataGridHelperColumnLabel.MakeElement("СР", HColor.BlueFG, HColor.Yellow, fontSize: 6, toolTip: "Сверьте рилевки с техкартой");
                        return label;
                    },
                    Update = (Dictionary<string, string> row) =>
                    {
                        var visibility = Visibility.Collapsed;
                        if (row.CheckGet("RILEVKI").ToInt() > 0)
                        {
                            visibility = Visibility.Visible;
                        }
                        return visibility;
                    },
                    Position = LabelPosition.Right
                });

                // Z-картон
                result.Add(new DataGridHelperColumnLabel()
                {
                    Construct = () =>
                    {
                        var label = DataGridHelperColumnLabel.MakeElement("Z", HColor.Green, HColor.BlackFG, fontSize: 6, toolTip: "Z-картон");
                        return label;
                    },
                    Update = (Dictionary<string, string> row) =>
                    {
                        var visibility = Visibility.Collapsed;
                        if (row.CheckGet("KOL_PAK").ToInt() > 0)
                        {
                            visibility = Visibility.Visible;
                        }
                        return visibility;
                    },
                    Position = LabelPosition.Right
                });
            }

            // слои сырья (LAYER_1, LAYER_2, LAYER_3, LAYER_4, LAYER_5)
            if (columnName == "LAYER_1" || columnName == "LAYER_2" || columnName == "LAYER_3" ||
                columnName == "LAYER_4" || columnName == "LAYER_5")
            {
                result.Add(new DataGridHelperColumnLabel()
                {
                    Construct = () =>
                    {
                        var label = DataGridHelperColumnLabel.MakeElement("СПЛ", HColor.RedFG, HColor.Yellow, fontSize: 6, toolTip: "Включить сплайсер вручную");
                        return label;
                    },
                    Update = (Dictionary<string, string> row) =>
                    {
                        var visibility = Visibility.Collapsed;
                        string layerNumber = columnName.Substring(6);
                        if (row.CheckGet("RAWS_TO_START_SPLICER").Contains(layerNumber))
                        {
                            visibility = Visibility.Visible;
                        }
                        return visibility;
                    },
                    Position = LabelPosition.Right
                });
            }

            // обрезь
            if (columnName == "OBREZ")
            {
                result.Add(new DataGridHelperColumnLabel()
                {
                    Construct = () =>
                    {
                        var label = DataGridHelperColumnLabel.MakeElement("ДНК", HColor.GreenFG, HColor.Yellow, fontSize: 6, toolTip: "Допускается необрезная кромка");
                        return label;
                    },
                    Update = (Dictionary<string, string> row) =>
                    {
                        var visibility = Visibility.Collapsed;
                        if (row.CheckGet("NOTE").Contains(DecodeIconTexts.CheckGet("ДНК")))
                        {
                            visibility = Visibility.Visible;
                        }
                        return visibility;
                    },
                    Position = LabelPosition.Left
                });
            }

            // композиция, качество
            if (columnName == "QID")
            {
                // Заливная печать
                result.Add(new DataGridHelperColumnLabel()
                {
                    Construct = () =>
                    {
                        var label = DataGridHelperColumnLabel.MakeElement("ЗП", HColor.GreenFG, HColor.Yellow, fontSize: 6, toolTip: "Заливная печать");
                        return label;
                    },
                    Update = (Dictionary<string, string> row) =>
                    {
                        var visibility = Visibility.Collapsed;
                        if (row.CheckGet("FILL_PRINTING_FLAG").ToInt() == 1)
                        {
                            visibility = Visibility.Visible;
                        }
                        return visibility;
                    },
                    Position = LabelPosition.Right
                });

                // Белый с двух сторон
                result.Add(new DataGridHelperColumnLabel()
                {
                    Construct = () =>
                    {
                        var label = DataGridHelperColumnLabel.MakeElement("ББ", HColor.BlueFG, HColor.White, fontSize: 6, toolTip: "Белый с двух сторон");
                        return label;
                    },
                    Update = (Dictionary<string, string> row) =>
                    {
                        var visibility = Visibility.Collapsed;
                        if (row.CheckGet("TWICE_WHITE_RAW").ToInt() == 1)
                        {
                            visibility = Visibility.Visible;
                        }
                        return visibility;
                    },
                    Position = LabelPosition.Right
                });
            }

            return result;
        }

        /// <summary>
        /// Возвращает необходимые иконки для списка заданий
        /// </summary>
        [Obsolete("Использовать вместо этого CreateLabel")]
        public static List<Border> GetIcons(string fieldName, Dictionary<string, string> row)
        {
            var list = new List<Border>();

            var stackIcons = new StackPanel();
            stackIcons.Orientation = Orientation.Horizontal;
            stackIcons.HorizontalAlignment = HorizontalAlignment.Left;

            // формат (ширина)
            if (fieldName == "WEB_WIDTH")
            {
                if (row.CheckGet("NOTE").Contains(DecodeIconTexts.CheckGet("НРП")))
                {
                    var b = MakeBorder(HColor.GreenFG, HColor.Yellow, "НРП");
                    stackIcons.Children.Add(b);
                }
            }

            // станок
            if (fieldName == "MACHINE")
            {
                stackIcons.HorizontalAlignment = HorizontalAlignment.Right;
                string note = "";
                if (!row.CheckGet("NOTE").IsNullOrEmpty())
                {
                    note = row.CheckGet("NOTE");

                    note = note.Replace(DecodeIconTexts.CheckGet("НРП"), "");
                    note = note.Replace(DecodeIconTexts.CheckGet("ДНК"), "");

                    if (note.Contains(DecodeIconTexts.CheckGet("ПП")))
                    {
                        note = note.Replace(DecodeIconTexts.CheckGet("ПП"), "");
                        var b = MakeBorder(HColor.BlueFG, HColor.Yellow, "ПП");
                        stackIcons.Children.Add(b);
                    }

                    note = note.Trim(new char[] { '.', ',', ' ' });

                    // есть уникальное примечание
                    if (!note.IsNullOrEmpty())
                    {
                        DecodeIconTexts["!"] = note;
                        var b = MakeBorder(HColor.RedFG, HColor.Yellow, "!");
                        stackIcons.Children.Add(b);
                    }
                }

                if (row.CheckGet("TESTING_FLAG").ToInt() > 0)
                {
                    if (row.CheckGet("MACHINE").Contains("СГП"))
                    {
                        var b = MakeBorder(HColor.BlueFG, HColor.Yellow, "Л");
                        stackIcons.Children.Add(b);
                    }
                }



                if (row.CheckGet("STYPE").ToInt()==2)
                {
                    var b = MakeBorder(HColor.RedFG, HColor.Yellow, "ПЛ");
                    stackIcons.Children.Add(b);
                }

                if (row.CheckGet("RILEVKI").ToInt() > 0)
                {
                    var b = MakeBorder(HColor.BlueFG, HColor.Yellow, "СР");
                    stackIcons.Children.Add(b);
                }

                // Признак Z-картона
                if (row.CheckGet("KOL_PAK").ToInt() > 0)
                {
                    var b = MakeBorder(HColor.Green, HColor.BlackFG, "Z");
                    stackIcons.Children.Add(b);
                }


            }

            // Если формат изменён и сплайсер надо включить вручную
            if (fieldName == "LAYER_1"
                || fieldName == "LAYER_2"
                || fieldName == "LAYER_3"
                || fieldName == "LAYER_4"
                || fieldName == "LAYER_5")
            {
                stackIcons.HorizontalAlignment = HorizontalAlignment.Right;

                string layerNumber = fieldName.Substring(6);
                // Если номер текущего слоя есть в строке, содержащей номера слоёв сырья, для которых надо включить сплайсер вручную
                if (row.CheckGet("RAWS_TO_START_SPLICER").Contains(layerNumber))
                {
                    var b = MakeBorder(HColor.RedFG, HColor.Yellow, "СПЛ", 14, 9, 7);
                    stackIcons.Children.Add(b);
                }
            }

            // обрезь
            if (fieldName == "OBREZ")
            {
                if (row.CheckGet("NOTE").Contains(DecodeIconTexts.CheckGet("ДНК")))
                {
                    var b = MakeBorder(HColor.GreenFG, HColor.Yellow, "ДНК");
                    stackIcons.Children.Add(b);
                }
            }

            // композиция, качество
            if (fieldName == "QID")
            {
                stackIcons.HorizontalAlignment = HorizontalAlignment.Right;
                // заливная печать на переработке 
                if (row.CheckGet("FILL_PRINTING_FLAG").ToInt() == 1)
                {
                    var b = MakeBorder(HColor.GreenFG, HColor.Yellow, "ЗП");
                    stackIcons.Children.Add(b);
                }

                // белый с двух сторон
                if (row.CheckGet("TWICE_WHITE_RAW").ToInt() == 1)
                {
                    var b = MakeBorder(HColor.BlueFG, HColor.White, "ББ");
                    stackIcons.Children.Add(b);
                }
            }

            //красная линия сверху всего ряда, если формат изменён и сплайсер надо включить вручную
            if (!row.CheckGet("RAWS_TO_START_SPLICER").IsNullOrEmpty())
            {
                var b = new Border();
                b.Width = 500;
                b.Height = 3;
                b.Background = HColor.RedFG.ToBrush();
                b.VerticalAlignment = VerticalAlignment.Top;

                var tooltip = new ToolTip();
                tooltip.Content = "Необходимо включить сплайсер вручную";
                b.ToolTip = tooltip;

                list.Add(b);
            }
            
            {
                var b = new Border();
                b.Child = stackIcons;
                list.Add(b);
            }

            return list;
        }

        private static Border MakeBorder(string borderColor, string textColor, string iconText, int width = 20, int height = 15, int fontSize = 10)
        {
            var b = new Border();
            b.Width = width;
            b.Height = height;
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
            textBlock.FontSize = fontSize;
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
