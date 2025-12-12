using Client.Common;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// сплеш-скрин
    /// показывается во время процедур первоначального запуска приложения
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2024-03-07</released>
    /// <changed>2024-03-07</changed>
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
            CenterWindowOnScreen();
            Hide();
            Central.MessageBus.AddProcessor(new MessageProcessor(ProcessMessage));

            StatusStringTimeout = new Common.Timeout(
                1,
                () => {
                    UpdateStatusString();
                },
                true,
                false
            );
            //StatusStringTimeout.SetIntervalMs(1000);
            //StatusStringTimeout.Run();

        }

        private ActionTimer MessageProcessorTimer { get; set; }
        private Timeout StatusStringTimeout { get; set; }

        private void UpdateStatusString()
        {
            var connection = Central.LPackClient.CurrentConnection;
            if(connection!=null)
            {
                var s=connection.DebugStatusString;
                TextStatus.Text = s;
            }
        }

        private void CenterWindowOnScreen()
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 2) - (windowHeight / 2);
        }

        public void ShowText(string text="")
        {
            TextDescription.Text = text;
        }

        /// <summary>
        /// Показать прогресс-бар обновления
        /// </summary>
        public void ShowProgress()
        {
            Dispatcher.Invoke(() =>
            {
                ProgressContainer.Visibility = Visibility.Visible;
                UpdateProgressBar.Value = 0;
            });
        }

        /// <summary>
        /// Скрыть прогресс-бар обновления
        /// </summary>
        public void HideProgress()
        {
            Dispatcher.Invoke(() =>
            {
                ProgressContainer.Visibility = Visibility.Collapsed;
                UpdateProgressBar.Value = 0;
                TextProgress.Text = "";
            });
        }

        /// <summary>
        /// Обновить значение прогресс-бара
        /// </summary>
        /// <param name="percentage">Процент выполнения (0-100)</param>
        /// <param name="currentFile">Текущий файл</param>
        /// <param name="fileNumber">Номер текущего файла</param>
        /// <param name="totalFiles">Общее количество файлов</param>
        public void UpdateProgress(int percentage, string currentFile = "", int fileNumber = 0, int totalFiles = 0)
        {
            Dispatcher.Invoke(() =>
            {
                if (ProgressContainer.Visibility != System.Windows.Visibility.Visible)
                {
                    ProgressContainer.Visibility = System.Windows.Visibility.Visible;
                }

                UpdateProgressBar.Value = percentage;

                if (!string.IsNullOrEmpty(currentFile))
                {
                    if (totalFiles > 0)
                    {
                        TextProgress.Text = $"Файл {fileNumber}/{totalFiles}: {currentFile} - {percentage}%";
                    }
                    else
                    {
                        TextProgress.Text = $"{currentFile} - {percentage}%";
                    }
                }
                else
                {
                    TextProgress.Text = $"{percentage}%";
                }
            });
        }

        public void ProcessMessage(MessageItem m)
        {
            var mode = 0;
            if(m != null)
            {
                if(m.ReceiverName == this.GetType().Name)
                {
                    switch(m.Action)
                    {
                        case "ShowWait":
                            {
                                TextDescription.Text = m.Message;
                                ProcessTimer(1);
                                Show();
                                if(Visibility == Visibility.Hidden)
                                {
                                    Show();
                                    mode = 1;
                                }
                            }
                            break;

                        case "Show":
                            {
                                TextDescription.Text = m.Message;
                                ProcessTimer(1);
                                if(Visibility == Visibility.Hidden)
                                {
                                    ShowDialog();
                                    mode = 1;
                                }
                            }
                            break;

                        case "Hide":
                            {
                                ProcessTimer(0);
                                Hide();
                                mode = 0;
                            }
                            break;

                    }
                }
            }
        }

        private void ProcessTimer(int mode = 0)
        {
            if(Central.DebugMode)
            {
                if(mode == 1)
                {
                    //StatusStringTimeout.Restart();
                }
                else
                {
                    StatusStringTimeout.Finish();
                }
            }
        }
    }
}
