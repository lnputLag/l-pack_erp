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

namespace Client.Interfaces.Production.Corrugator.TaskPlanningKashira
{
    internal class TaskColors
    {
        /// <summary>
        /// Раскраска каждого сырья
        /// </summary>
        public static Dictionary<string, int> PaperCodesForPlanning { get; set; } = new Dictionary<string, int>();


        public static object GetColor(string fieldName, Dictionary<string, string> row)
        {
            var result = DependencyProperty.UnsetValue;
            var color = "";

            if(fieldName== TaskPlaningDataSet.Dictionary.StartBeforeTime)
            {
                var lastDateTime = row.CheckGet(TaskPlaningDataSet.Dictionary.LastDate);
                var startBefore = row.CheckGet(TaskPlaningDataSet.Dictionary.StartBeforeTime);
                if (!string.IsNullOrEmpty(lastDateTime) && !string.IsNullOrEmpty(startBefore))
                {
                    DateTime dt;
                    if(DateTime.TryParse(lastDateTime, out dt))
                    {
                        DateTime dt1 = startBefore.ToDateTime();
                            if (dt > dt1)
                            {
                                color = HColor.Red;
                            }

                    }
                }
            }
            

            if (fieldName == "PRODUCTION_TASK_NUMBER")
            {
                // если печать + FF - синим
                if (row.CheckGet("FANFOLD").ToInt() == 1 && row.CheckGet("COLOR").ToInt() == 1)
                {
                    color = HColor.Blue;
                }
            }
            else
            if (fieldName == "ON_MACHINE")
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
                    //FIXME: кажется ошибка, нужно считать до расчетной даты, а не до текущей!
                    // но так как эта функция вызыватеся еще и из модуля каунтера, то возможно нужно преоверить этот модуль
                    // double hours = (bwDateTime - DateTime.Now).TotalHours;

                    var CalculatedTime = DateTime.Now;

                    if (!row.CheckGet("CALCULATED_START_PLANNED").IsNullOrEmpty())
                    {
                        CalculatedTime = row.CheckGet("CALCULATED_START_PLANNED").ToDateTime();
                    }

                    double hours = (bwDateTime - CalculatedTime).TotalHours;



                    // нужно сделать красным выделение, если расчетное время идет с опозданием более 2,5ч от «начать до».
                    // И оранжевым-если опоздание от 1мин до 2,5часов. Если нет опоздания, то выделять вообще не нужно.

                    if (hours < 0)
                    {
                        if (hours < -2.5)
                        {
                            if (hours > -8000)
                            {
                                color = HColor.Red;
                            }
                        }
                        else
                        {
                            color = HColor.YellowOrange;
                        }
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
                else
                if (profil == "C" || profil == "С")
                {
                    color = HColor.BlueFG;
                }
                else
                if (profil == "E" || profil == "Е")
                {
                    color = HColor.PinkOrange;
                }
                else
                if (profil == "BB" || profil == "ВВ")
                {
                    color = HColor.Green;
                }
                else
                if (profil == "BC" || profil == "ВС")
                {
                    color = HColor.Yellow;
                }
                else
                if (profil == "BE" || profil == "ВЕ")
                {
                    color = HColor.Olive;
                }
                else
                if (profil == "EB" || profil == "ЕВ")
                {
                    color = HColor.Blue;
                }
                else
                if (profil == "EC" || profil == "ЕС")
                {
                    color = HColor.Orange;
                }
            }

            //формат
            else if (fieldName == "WIDTH")
            {
                int width = row.CheckGet("WIDTH").ToInt();

                // жалуются на то что цаета получаются схожими у близких значений
                // пока не знаю лучшего способа чем задание палитры
                // кажется длинн не так и много

                if (width == 2800)
                {
                    color = "#b0d4d6";
                }
                else
                {
                    int offset = (width - 1400) / 8;
                    offset = offset.Clamp(0, 255);
                    //отенки жёлтый - зелёный - бирюзовый
                    color = $"#{255 - offset:x2}{255 - offset / 4:x2}{127 + offset / 2:x2}";
                }
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
                    color = HColor.Blue;
                }
                // первый раз на этом станке
                // если сейчас на БХС, на соседнем БХС данная композиция уже была, можно посмотреть у них настройки станка 
                if (num == 2)
                {
                    color = HColor.Green;
                }
            }
            // длина
            else if (fieldName == "LENGTH")
            {
                //if (row.CheckGet(TaskPlaningDataSet.Dictionary.StanokId).ToInt() == 0)
                {
                    if (row.CheckGet(TaskPlaningDataSet.Dictionary.Length).ToInt() < 300)
                    {
                        if (row.CheckGet(TaskPlaningDataSet.Dictionary.DropdownId).ToInt() == 0)
                        {
                            color = HColor.Yellow;
                        }
                    }
                }
            }
            else if (fieldName == "MACHINE")
            {
                // http://911.l-pak.ru/Task/Edit/7491
                //var machine = row.CheckGet("MACHINE");

                //if(machine.Contains("Eterna") || row.CheckGet("FANFOLD").ToBool())
                //{
                //    color = HColor.Red;
                //}

            }
            else if (fieldName == TaskPlaningDataSet.Dictionary.OtherMachine)
            {
                int machine = row.CheckGet(TaskPlaningDataSet.Dictionary.OtherMachine).ToInt();

                if (machine == (int)TaskPlaningDataSet.TypeStanok.Js)
                {
                    color = HColor.Yellow;
                }
                // else if (machine == (int)TaskPlaningDataSet.TypeStanok.Gofra5)
                // {
                //     color = HColor.Red;
                // }
                // else
                // if (machine == (int)TaskPlaningDataSet.TypeStanok.Fosber)
                // {
                //     color = HColor.Green;
                // }


            }
            else if (fieldName == TaskPlaningDataSet.Dictionary.CalculatedDuration)
            {
                var time = row.CheckGet(TaskPlaningDataSet.Dictionary.CalculatedDuration);// row.CheckGet(TaskPlaningDataSet.Dictionary.BlockTimeLength);
                if (!string.IsNullOrEmpty(time))
                {
                    double TotalMinutes = time.ToDouble();

                    //TimeSpan span = TimeSpan.Parse(time);
                    if (TotalMinutes >= 20)
                    {
                        color = HColor.LightSelection;
                    }
                }
            }

            // сырьё (рулоны)
            if (fieldName == "LAYER_1"
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
                //if (row.CheckGet("DURATION").ToDouble() > 0)
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
        };

        /// <summary>
        /// Возвращает необходимые иконки для списка заданий
        /// </summary>
        public static List<Border> GetIcons(string fieldName, Dictionary<string, string> row)
        {
            var list = new List<Border>();

            var stackIcons = new StackPanel();
            stackIcons.Orientation = Orientation.Horizontal;
            stackIcons.HorizontalAlignment = HorizontalAlignment.Left;

            // формат (ширина)
            //if (fieldName == "WEB_WIDTH")
            //{
            //    if (row.CheckGet("NOTE").Contains(DecodeIconTexts.CheckGet("НРП")))
            //    {
            //        var b = MakeBorder(HColor.GreenFG, HColor.Yellow, "НРП");
            //        stackIcons.Children.Add(b);
            //    }
            //}

            // станок
            if (fieldName == "ERRORS")
            {
            
                //if (row.CheckGet("NOTE").Contains(DecodeIconTexts.CheckGet("ДНК")))
                {
                    var b = MakeBorder(HColor.GreenFG, HColor.Yellow, "ДНК");
                    stackIcons.Children.Add(b);
                }
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
