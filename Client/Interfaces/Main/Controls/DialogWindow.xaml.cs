using Client.Common;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Client.Interfaces.Main
{
    public enum DialogWindowButtons
    {
        OK = 0,
        OKCancel = 1,
        YesNoCancel = 3,
        YesNo = 4,
        RetryCancel = 5,
        NoYes = 11,
        None=12,
        //только кнопка ОК, автоскрытие через 3 сек
        OKAutohide=12,
    }

    /// <summary>
    /// Перечисление для определения кнопки, которая была нажата в диалоговом окне
    /// </summary>
    public enum DialogResultButton
    {
        None = 0,
        Yes = 1,
        No  = 2,
        Cancel = 3,
    }

    public partial class DialogWindow : Window
    {
        private DialogWindowButtons _buttons;

        /// <summary>
        /// Диалоговое окно с сообщением и вариантами кнопок
        /// </summary>
        /// <param name="message">суть, краткое сообщение</param>
        /// <param name="title">заголовок окна</param>
        /// <param name="description">развернутое описание</param>
        /// <param name="buttons">набор кнопок</param>
        public DialogWindow(string message, string title = "", string description = "", DialogWindowButtons buttons = DialogWindowButtons.OK)
        {
            _buttons = buttons;

            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();

            YesButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Collapsed;
            NoButton.Visibility = Visibility.Collapsed;

            switch (buttons)
            {
                case DialogWindowButtons.OK:
                    YesButton.Content = "OK";
                    YesButton.Visibility = Visibility.Visible;
                    break;

                case DialogWindowButtons.YesNo:
                    YesButton.Content = "Да";
                    CancelButton.Content = "Нет";
                    YesButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                    break;

                case DialogWindowButtons.OKCancel:
                    YesButton.Content = "OK";
                    CancelButton.Content = "Отмена";
                    YesButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                    break;

                case DialogWindowButtons.NoYes:
                    YesButton.Content = "Да";
                    CancelButton.Content = "Нет";
                    YesButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;

                    CancelButton.Style= (Style)CancelButton.FindResource("FButtonPrimary");
                    YesButton.Style = (Style)YesButton.FindResource("FButtonCancel");
                    break;

                case DialogWindowButtons.YesNoCancel:
                    YesButton.Content = "Да";
                    NoButton.Content = "Нет";
                    CancelButton.Content = "Отмена";
                    YesButton.Visibility = Visibility.Visible;
                    NoButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                    break;

                case DialogWindowButtons.RetryCancel:
                    YesButton.Content = "Повторить";
                    CancelButton.Content = "Отмена";
                    YesButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                    break;

                case DialogWindowButtons.None:
                    break;
            }


            Title = title;
            Message.Text = message;
            Description.Text = description;

            Message.MaxWidth = 350;
            Description.MaxWidth = 350;

            if (message.Length > 120 || description.Length > 80)
            {
                Width = 600;
                Height = 300;
                Message.MaxWidth = 450;
                Description.MaxWidth = 450;
            }

            SetIcon("info");
            ResultButton = 0;
        }

        // Нажатая кнопка
        public DialogResultButton ResultButton;

        public static bool? ShowDialog(string message, string title = "", string description = "", DialogWindowButtons buttons = DialogWindowButtons.OK)
        {
            var dialog=new DialogWindow(message, title, description, buttons);
            dialog.Topmost=true;
            var result= dialog.ShowDialog();            
            return result;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            AnswerOk();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            AnswerCancel();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            AnswerNo();
        }

        public void SetIcon(string type = "info")
        {
            IconAlert.Visibility    = Visibility.Collapsed;
            IconInfo.Visibility     = Visibility.Collapsed;
            IconServer.Visibility   = Visibility.Collapsed;

            switch (type.ToLower())
            {
                case "alert":
                    IconAlert.Visibility = Visibility.Visible;
                    break;

                case "server":
                    IconServer.Visibility = Visibility.Visible;
                    break;

                default:
                    IconInfo.Visibility = Visibility.Visible;
                    break;
            }
        }

       

        private void AnswerOk()
        {
            DialogResult = true;
            ResultButton = DialogResultButton.Yes;
            Close();
        }

        private void AnswerCancel()
        {
            DialogResult = false;
            ResultButton = DialogResultButton.Cancel;
            Close();
        }

        private void AnswerNo()
        {
            DialogResult = true;
            ResultButton = DialogResultButton.No;
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var window = GetWindow(this);
            if (window != null) window.KeyDown += KeyDownHandler;
        }

        private void KeyDownHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {

            switch( _buttons )
            {
                case DialogWindowButtons.NoYes:

                    if( e.Key == Key.Enter )
                    {
                        AnswerCancel();
                    }

                    if( e.Key == Key.Escape )
                    {
                        AnswerCancel();
                    }

                    break;

                default:

                    if( e.Key == Key.Enter )
                    {
                        AnswerOk();
                    }

                    if( e.Key == Key.Escape )
                    {
                        AnswerCancel();
                    }

                    break;

            }
           
        }

        public DispatcherTimer AutoCloseTimer { get; set; }
        /// <summary>
        /// автоматически скрывает окно через заданное время
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
                    Central.Stat.TimerAdd("DialogWindow_AutoClose", row);
                }

                AutoCloseTimer.Tick += (s, e) =>
                {
                    Close();
                    AutoCloseTimer.Stop();
                };
            }
            else
            {
                AutoCloseTimer.Stop();
            }
            AutoCloseTimer.Start();
        }


        /// <summary>
        /// FIXME: иногда случается что диалог показывается на заднем плане
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Deactivated(object sender, EventArgs e)
        {
            //https://stackoverflow.com/questions/3729369/topmost-is-not-topmost-always-wpf
            // This should do the trick in most cases
            // The Window was deactivated 
            Topmost = false; // set topmost false first
            Topmost = true; // then set topmost true again.
        }
    }
}
