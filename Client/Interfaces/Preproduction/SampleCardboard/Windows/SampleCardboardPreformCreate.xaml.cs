using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Создание нового формата заготовки картона для образцов
    /// </summary>
    /// <author>Рясной П.В.</author>
    public partial class SampleCardboardPreformCreate : UserControl
    {
        public SampleCardboardPreformCreate()
        {
            InitializeComponent();

            PreformLength.Text = "";
            PreformWidth.Text = "";

            PreviewKeyDown += OnKeyDown;
        }

        /// <summary>
        /// ID картона
        /// </summary>
        public int Idc;

        /// <summary>
        /// Окно формы ввода данных
        /// </summary>
        public Window Window { get; set; }

        /// <summary>
        /// Обработчики нажатий клавиш
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;

                case Key.Enter:
                    Save();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Отображение окна с полями ввода
        /// </summary>
        private void ShowWin()
        {
            Window = new Window
            {
                Title = "Создание заготовки",
                Width = 280,
                Height = 130,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,
            };
            Window.Content = new Frame
            {
                Content = this,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            if (Window != null)
            {
                Window.Topmost = true;
                Window.ShowDialog();
            }
        }

        /// <summary>
        /// Вызов окна ввода данных дя новой заготовки
        /// </summary>
        /// <param name="idc"></param>
        public void Show(int idc)
        {
            Idc = idc;
            ShowWin();
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        public void Close()
        {
            var window = this.Window;
            if (window != null)
            {
                window.Close();
            }
        }

        /// <summary>
        /// Сохранение данных для создания формата заготовки
        /// </summary>
        private async void Save()
        {
            int bLength = PreformLength.Text.ToInt();
            int bWidth = PreformWidth.Text.ToInt();
            if ((bLength > 0) && (bWidth > 0))
            {
                var qt = new LPackClientQuery();
                var pt = new Dictionary<string, string>()
                {
                    { "IDC", Idc.ToString() },
                    { "LENGTH", bLength.ToString() },
                    { "WIDTH", bWidth.ToString() },
                };

                await Task.Run(() =>
                {
                    qt = _LPackClientDataProvider.DoQueryGetResult("Preproduction", "SampleCardboards", "CreateTovar", "", pt);
                }
                );
                if (qt.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(qt.Answer.Data);
                    if (result.Count > 0)
                    {
                        //отправляем сообщение Гриду о необходимости обновить данные
                        Messenger.Default.Send(new ItemMessage()
                        {
                            ReceiverGroup = "Preproduction",
                            ReceiverName = "SampleCardboardCreateTask",
                            SenderName = "SampleCardboardPerformCreate",
                            Action = "Refresh",
                        });
                    }
                }
                else
                {
                    qt.ProcessError();
                }

                Close();
            }
            else
            {
                DialogWindow.ShowDialog("Заполните данные для заготовки", "Добавление заготовки");
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку отмены
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку сохранения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }
    }
}
