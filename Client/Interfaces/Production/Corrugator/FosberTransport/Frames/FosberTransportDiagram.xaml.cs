using Client.Assets.HighLighters;
using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Мнемосхема транспортной системы
    /// </summary>
    /// <author>zelenskiy_sv</author>
    public partial class FosberTransportDiagram : UserControl
    {
        public FosberTransportDiagram()
        {
            InitializeComponent();
            FrameName = "FosberTransportDiagram";
            posList = new List<int>() {2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17};
        }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// перечень позиций, на которых указывается доп. информация (раскат, сторона)
        /// </summary>
        private List<int> posList;

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Production",
                ReceiverName = "",
                SenderName = "FosberTransport",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// установка цвета позиции
        /// </summary>
        /// <param name="position"></param>
        /// <param name="color"></param>
        public void SetPositionColor(int position, string color, int reel, int side)
        {
            EnumVisual(this, position, color, reel, side);
        }

        /// <summary>
        /// Сброс цветов позиций ТС в исходное состояние
        /// </summary>
        public void ClearPositionColors()
        {
            var mColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(HColor.Gray);

            for (int i = 1; i <= 54; i++)
            {
                ClearVisual(this, mColor, i);
            }

            GetPrinterData();
        }

        /// <summary>
        /// Заполнение информации о данных, переданных для печати ярлыка
        /// </summary>
        public void GetPrinterData()
        {
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "FosberTransport");
                q.Request.SetParam("Action", "GetPrinterData");

                q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            var first = ds.GetFirstItem();

                            // данные для печати ярлыка
                            var toolTip = new Border();
                            {
                                var g = new StackPanel();
                                g.Orientation = Orientation.Vertical;
                                {
                                    var label = new TextBlock();
                                    label.Text = $"Данные для печати ярлыка:";
                                    label.FontWeight = FontWeights.Bold;
                                    g.Children.Add(label);
                                }
                                {
                                    var label = new TextBlock();
                                    label.Text = $"Время: {first.CheckGet("DTTM").ToDateTime().ToString("dd.MM.yyyy HH:mm:ss")}";
                                    g.Children.Add(label);
                                }
                                {
                                    var label = new TextBlock();
                                    label.Text = $"Сырье + формат: {first.CheckGet("NAME")}";
                                    g.Children.Add(label);
                                }
                                {
                                    var label = new TextBlock();
                                    label.Text = $"Остаток в рулоне, м: {first.CheckGet("LENGTH")}";
                                    g.Children.Add(label);
                                }
                                {
                                    var label = new TextBlock();
                                    label.Text = $"Остаток в рулоне, кг: {first.CheckGet("WEIGHT")}";
                                    g.Children.Add(label);
                                }
                                {
                                    var label = new TextBlock();
                                    label.Text = $"Номер раската: {first.CheckGet("REEL_NUM")}";
                                    g.Children.Add(label);
                                }
                                {
                                    var label = new TextBlock();
                                    label.Text = $"Сторона раската: {first.CheckGet("REEL_SIDE")}";
                                    g.Children.Add(label);
                                }
                                {
                                    var label = new TextBlock();
                                    label.Text = $"Смена: {first.CheckGet("WORK_TEAM")}";
                                    g.Children.Add(label);
                                }
                                {
                                    var label = new TextBlock();
                                    label.Text = $"Показатели влажности: {first.CheckGet("WET_LIST")}";
                                    g.Children.Add(label);
                                }
                                {
                                    var label = new TextBlock();
                                    label.Text = $"Номер рулона (barcode): {first.CheckGet("BARCODE")}";
                                    g.Children.Add(label);
                                }
                                {
                                    var label = new TextBlock();
                                    label.Text = $"Номер рулона (roll_num): {first.CheckGet("ROLL_NUM")}";
                                    g.Children.Add(label);
                                }
                                {
                                    var label = new TextBlock();
                                    label.Text = $"Сырье: {first.CheckGet("MARK")}";
                                    g.Children.Add(label);
                                }
                                {
                                    var label = new TextBlock();
                                    label.Text = $"Формат: {first.CheckGet("PARAMETER")}";
                                    g.Children.Add(label);
                                }
                                {
                                    var label = new TextBlock();
                                    label.Text = $"ИД рулона (idp): {first.CheckGet("IDP")}";
                                    g.Children.Add(label);
                                }

                                toolTip.Child = g;
                            }

                            Print53.ToolTip = toolTip;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Сброс состояния позиций ТС в исходное состояние
        /// </summary>
        /// <param name="myVisual"></param>
        /// <param name="color"></param>
        /// <param name="position"></param>
        public void ClearVisual(Visual myVisual, System.Windows.Media.Color color, int position)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(myVisual); i++)
            {
                Visual childVisual = (Visual)VisualTreeHelper.GetChild(myVisual, i);

                if (childVisual != null && childVisual is Ellipse)
                {
                    var pos = (Ellipse)childVisual;
                    pos.Fill = new SolidColorBrush(color);
                }

                if (childVisual != null && childVisual is System.Windows.Shapes.Rectangle)
                {
                    var pos = (System.Windows.Shapes.Rectangle)childVisual;
                    pos.Fill = new SolidColorBrush(color);
                }

                if (childVisual != null && childVisual is TextBlock)
                {
                    var pos = (TextBlock)childVisual;

                    if (pos.Name == $"Label{position}")
                    {
                        //pos.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                    }
                    else if (pos.Name == $"ReelLabel{position}")
                    {
                        pos.Visibility = System.Windows.Visibility.Hidden;
                    }
                    else if (pos.Name == $"SideLabel{position}")
                    {
                        pos.Visibility = System.Windows.Visibility.Hidden;
                    }

                }

                ClearVisual(childVisual, color, position);
            }
        }

        /// <summary>
        /// Раскраска позиций ТС в цвета в зависимости от состояния
        /// </summary>
        /// <param name="myVisual"></param>
        /// <param name="position"></param>
        /// <param name="color"></param>
        /// <param name="reel"></param>
        /// <param name="side"></param>
        public void EnumVisual(Visual myVisual, int position, string color, int reel, int side)
        {
            var mColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color);
            
            string sideLabel = side == 0 ? "А" : (side == 1 ? "Л" : "П");

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(myVisual); i++)
            {
                Visual childVisual = (Visual)VisualTreeHelper.GetChild(myVisual, i);

                if (childVisual != null && childVisual is Ellipse)
                {
                    var pos = (Ellipse)childVisual;
                    if (pos.Name == $"Position{position}")
                    {
                        pos.Fill = new SolidColorBrush(mColor);
                    }
                }

                if (childVisual != null && childVisual is System.Windows.Shapes.Rectangle)
                {
                    var pos = (System.Windows.Shapes.Rectangle)childVisual;
                    if (pos.Name == $"Position{position}")
                    {
                        pos.Fill = new SolidColorBrush(mColor);
                    }
                }

                if (childVisual != null && childVisual is TextBlock)
                {
                    var pos = (TextBlock)childVisual;

                    if (posList.Contains(position))
                    {
                        if (color != HColor.Gray)
                        {
                            if (pos.Name == $"Label{position}")
                            {
                                //pos.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                            }
                            else if (pos.Name == $"ReelLabel{position}")
                            {
                                pos.Visibility = System.Windows.Visibility.Visible;
                                pos.Text = reel.ToString();
                            }
                            else if (pos.Name == $"SideLabel{position}")
                            {
                                pos.Visibility = System.Windows.Visibility.Visible;
                                pos.Text = sideLabel;
                            }
                        }
                    }
                }

                EnumVisual(childVisual, position, color, reel, side);
            }
        }
    }
}
