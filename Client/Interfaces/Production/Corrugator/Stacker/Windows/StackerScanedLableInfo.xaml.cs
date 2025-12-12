using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
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

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Всплывающее самозакрывающееся цветное информационное окно для стекеров гофроагрегата
    /// </summary>
    public partial class StackerScanedLableInfo : UserControl
    {
        /// <summary>
        /// Конструктор всплывающего самозакрывающегося цветного информационного окна для стекеров гофроагрегата.
        /// Отобразит переданную информацию об ошибке 
        /// </summary>
        /// <param name="informationText">Информация, которая будет отображаться на окне</param>
        /// <param name="scanedStatus">
        /// Статус сканирования ярлыка:
        /// 2 -- Ярлык успешно отсканирован -> залёный цвет фона окна;
        /// 1 -- Ошибка сканирования ярлыка -> красный цвет фона окна.
        /// 0 -- Информация -> белый цвет фона окна.
        /// </param>
        public StackerScanedLableInfo(string informationText, int scanedStatus)
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            FrameName = "StackerScanedLableInfo";

            ScanedLableInfoText = informationText;
            ScanedLableInfoStatus = scanedStatus;
        }

        /// <summary>
        /// Конструктор всплывающего самозакрывающегося цветного информационного окна для стекеров гофроагрегата.
        /// Отобразит переданную информацию об ошибке и информацию по отсканированным поддонам.
        /// </summary>
        /// <param name="informationText">Информация, которая будет отображаться на окне (не считая информации по отсканированным поддонам)</param>
        /// <param name="scanedStatus">
        /// Статус сканирования ярлыка:
        /// 2 -- Ярлык успешно отсканирован -> залёный цвет фона окна;
        /// 1 -- Ошибка сканирования ярлыка -> красный цвет фона окна
        /// 0 -- Информация -> белый цвет фона окна.
        /// </param>
        /// <param name="palletId">Идентификатор отсканированного поддона</param>
        public StackerScanedLableInfo(string informationText, int scanedStatus, string palletId)
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            FrameName = "StackerScanedLableInfo";

            ScanedLableInfoText = informationText;
            ScanedLableInfoStatus = scanedStatus;
            PalletId = palletId;

            GetQuantityDataByPalletId();
        }

        /// <summary>
        /// Техническое имя фрейма
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// Информация, которая будет отображаться на окне
        /// </summary>
        public string ScanedLableInfoText { get; set; }

        /// <summary>
        /// Статус сканирования ярлыка:
        /// 2 -- Ярлык успешно отсканирован -> залёный цвет фона окна;
        /// 1 -- Ошибка сканирования ярлыка -> красный цвет фона окна;
        /// 0 -- Информация -> белый цвет фона окна.
        /// </summary>
        public int ScanedLableInfoStatus { get; set; }

        /// <summary>
        /// Идентификатор поддона
        /// Для получения данных по количеству
        /// </summary>
        public string PalletId { get; set; }

        /// <summary>
        /// Количество картона по заданию
        /// </summary>
        public int QuantityByTask { get; set; }

        /// <summary>
        /// Количество поддона на отсканированных поддонах
        /// </summary>
        public int QuantityScaned { get; set; }

        /// <summary>
        /// Оставшееся количество картона
        /// </summary>
        public int QuantityLeft { get; set; }

        public DispatcherTimer AutoCloseTimer { get; set; }

        /// <summary>
        /// Флаг того, что окно нужно открывать в полноэкранном режиме
        /// </summary>
        public bool WindowMaxSizeFlag { get; set; }

        /// <summary>
        /// Получает данные по количеству для заполнения информационных полей
        /// </summary>
        public void GetQuantityDataByPalletId()
        {
            if (!string.IsNullOrEmpty(PalletId))
            {
                var p = new Dictionary<string, string>();
                p.Add("ID_PODDON", PalletId);

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "CorrugatingLabel");
                q.Request.SetParam("Action", "GetQuantityData");
                q.Request.SetParams(p);

                q.Request.Timeout = 5000;
                q.Request.Attempts = 1;

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        var quantityInfoDataSet = ListDataSet.Create(result, "QUANTIY_INFO");
                        if (quantityInfoDataSet != null)
                        {
                            if (quantityInfoDataSet.Items != null)
                            {
                                if (quantityInfoDataSet.Items.Count > 0)
                                {
                                    QuantityByTask = quantityInfoDataSet.Items.First().CheckGet("QUANTITY_BY_TASK").ToInt();
                                    QuantityScaned = quantityInfoDataSet.Items.First().CheckGet("QUANTITY_SCANED").ToInt();
                                    QuantityLeft = quantityInfoDataSet.Items.First().CheckGet("QUANTITY_LEFT").ToInt();

                                    // Если есть информация по количеству по заданию
                                    // то увеличиваем окно по высоте, включаем отображение доболнительной информации по количесту и заполняем эту информацию
                                    if (QuantityByTask > 0)
                                    {
                                        //this.Height = 265;
                                        StackerScanedLabelQuantityInfoBorder.Visibility = Visibility.Visible;

                                        StackerScanedLableQuantityByTaskInfoLabel.Text = QuantityByTask.ToString();
                                        StackerScanedLableQuantityScanedInfoLabel.Text = QuantityScaned.ToString();
                                        StackerScanedLableQuantityLeftInfoLabel.Text = QuantityLeft.ToString();

                                        // Если отскарировали больше, чем должно быть по заданию,
                                        // то выделяем область с информацией по количеству в красный цвет
                                        if (QuantityLeft < 0)
                                        {
                                            // Красный
                                            var color = "#ffee0000";
                                            var bc = new BrushConverter();
                                            var brush = (Brush)bc.ConvertFrom(color);

                                            StackerScanedLabelQuantityInfoBorder.Background = brush;
                                        }
                                        else
                                        {
                                            StackerScanedLabelQuantityInfoBorder.Background = null;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            if (!string.IsNullOrEmpty(ScanedLableInfoText))
            {
                StackerScanedLableInfoLabel.Text = ScanedLableInfoText;

                if (ScanedLableInfoStatus == 1)
                {
                    // Красный
                    var color = "#ffee0000";
                    var bc = new BrushConverter();
                    var brush = (Brush)bc.ConvertFrom(color);

                    StackerScanedLableInfoBorder.Background = brush;
                }
                else if (ScanedLableInfoStatus == 2)
                {
                    // Зелёный
                    var color = "#cc289744";
                    var bc = new BrushConverter();
                    var brush = (Brush)bc.ConvertFrom(color);

                    StackerScanedLableInfoBorder.Background = brush;
                }
                else if (ScanedLableInfoStatus == 0)
                {
                    // белый
                    var color = "#ffffff";
                    var bc = new BrushConverter();
                    var brush = (Brush)bc.ConvertFrom(color);

                    StackerScanedLableInfoBorder.Background = brush;
                }

                // режим отображения новых фреймов
                //     0=по умолчанию
                //     1=новая вкладка
                //     2=новое окно
                Central.WM.FrameMode = 2;

                Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
                windowParametrs.Add("no_resize", "1");
                windowParametrs.Add("center_screen", "1");
                if (WindowMaxSizeFlag)
                {
                    windowParametrs.Add("maximized_size", "1");
                }
                Central.WM.Show(FrameName, "Статус сканирования ярлыка", true, "add", this, "top", windowParametrs);
            }
        }

        /// <summary>
        /// Отображение и автоматическое закрытие через заданное количество секунд
        /// </summary>
        /// <param name="seconds"></param>
        public void ShowAndAutoClose(int seconds = 1)
        {
            if (!string.IsNullOrEmpty(ScanedLableInfoText))
            {
                StackerScanedLableInfoLabel.Text = ScanedLableInfoText;

                if (ScanedLableInfoStatus == 1)
                {
                    // Красный
                    var color = "#ffee0000";
                    var bc = new BrushConverter();
                    var brush = (Brush)bc.ConvertFrom(color);

                    StackerScanedLableInfoBorder.Background = brush;
                }
                else if (ScanedLableInfoStatus == 2)
                {
                    // Зелёный
                    var color = "#cc289744";
                    var bc = new BrushConverter();
                    var brush = (Brush)bc.ConvertFrom(color);

                    StackerScanedLableInfoBorder.Background = brush;
                }
                else if (ScanedLableInfoStatus == 0)
                {
                    // белый
                    var color = "#ffffff";
                    var bc = new BrushConverter();
                    var brush = (Brush)bc.ConvertFrom(color);

                    StackerScanedLableInfoBorder.Background = brush;
                }

                // режим отображения новых фреймов
                //     0=по умолчанию
                //     1=новая вкладка
                //     2=новое окно
                Central.WM.FrameMode = 2;
                AutoClose(seconds);

                Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
                windowParametrs.Add("no_resize", "1");
                windowParametrs.Add("center_screen", "1");
                if (WindowMaxSizeFlag)
                {
                    windowParametrs.Add("maximized_size", "1");
                }
                Central.WM.Show(FrameName, "Статус сканирования ярлыка", true, "add", this, "top", windowParametrs);
            }
        }

        /// <summary>
        /// автоматически скрывает окно через заданное время
        /// P.S. Для срабатывания этой функции вместе с функцией Central.WM.Show нужно вызывать AutoClose() перед Show(), 
        /// т.к. Central.WM.Show не отдаёт управление до тех пор, пока не закроешь окно
        /// </summary>
        /// <param name="seconds"></param>
        public void AutoClose(int seconds = 1)
        {
            if (AutoCloseTimer == null)
            {
                AutoCloseTimer = new DispatcherTimer
                {
                    Interval = new TimeSpan(0, 0, seconds)
                };

                {
                    var row = new Dictionary<string, string>();
                    row.CheckAdd("TIMEOUT", seconds.ToString());
                    row.CheckAdd("DESCRIPTION", "");
                    Central.Stat.TimerAdd("StackerScanedLableInfo_AutoClose", row);
                }

                AutoCloseTimer.Tick += (s, e) =>
                {
                    AutoCloseTimer.Stop();
                    Close();
                };
            }
            else
            {
                AutoCloseTimer.Stop();
            }
            AutoCloseTimer.Start();
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close(FrameName);

            //вся работа по утилизации ресурсов происходит в Destroy
            //он будет вызван при закрытии фрейма
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "StackerCM",
                ReceiverName = "",
                SenderName = "StackerScanedLableInfo",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

        }
    }
}