using Client.Assets.HighLighters;
using Client.Common;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// Форма ввода логина и пароля пользователя
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();

            //если код выполняется в дизайнере, дальше не продолжаем
            if (Central.InDesignMode())
            {
                return;
            }

            //будет вызвана, когда форма отрисуется
            Loaded += FormLoaded;
        }

        private DispatcherTimer StatusTimer { get; set; }

        private string GetTitle()
        {
            //в заголовке окна выведем имя программы и адрес сервера
            var curVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            var titleCustom = $"{Central.ProgramTitle}. Версия:{curVersion}. Сервер:{Central.ServerIP}.";

            return titleCustom;
        }

        private void Password_KeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter))
            {
                DoLogin();
            }
        }

        /// <summary>
        /// будет вызвана при отрисовке формы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormLoaded(object sender, RoutedEventArgs e)
        {
            //установка фокуса на первый элемент формы            
            LoginTextBox.Focus();

            if (!string.IsNullOrEmpty(LoginTextBox.Text))
            {
                PasswordTextBox.Focus();
            }

            //биндинг обработчиков клавиатуры
            var window = Window.GetWindow(this);
            if (window != null) window.KeyDown += KeyDownHandler;
        }

        private void KeyDownHandler(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    DoLogin();
                    break;
                case Key.Escape:
                    Central.Terminate();
                    break;
                case Key.F1:
                    Central.ShowHelp("/doc/l-pack-erp/run");
                    break;
            }
        }


        private void SetStatus(string str, bool error = true)
        {
            FormStatus.Text = str;
            FormStatus.Foreground = (Brush)new BrushConverter().ConvertFrom(error ? HColor.ErrorFG : HColor.NoteFG);


            if (StatusTimer == null)
            {
                StatusTimer = new DispatcherTimer
                {
                    Interval = new TimeSpan(0, 0, 5)
                };
                StatusTimer.Tick += (s, e) =>
                {
                    FormStatus.Text = string.Empty;
                    StatusTimer.Stop();
                };
            }
            else
            {
                StatusTimer.Stop();
            }
            StatusTimer.Start();
        }

        public void AutoLogin(string login, string password)
        {
            var resume = true;

            if (resume)
            {
                var loginResult = Central.LPackClient.DoLogin(login, password);
                if (!loginResult)
                {
                    resume = false;
                    var query = Central.LPackClient.LoginQuery;
                    query.ProcessError();
                }
            }

            if (resume)
            {
                //перейдем ко второй фазе запуска                
                Central.StartUp();
            }
        }

        /// <summary>
        /// отработка логина пользователя
        /// </summary>
        private bool DoLogin()
        {
            var resume = true;

            /*
                валидируем поля на форме вручную (мы не можем использовать
                существующий механизм валидации с полем Password, оно нестандартное
                и его значение не может быть связано со свойством VM)
            */

            if (string.IsNullOrEmpty(LoginTextBox.Text))
            {
                resume = false;
                SetStatus("Пожалуйста, введите логин");
            }

            if (resume)
            {
                if (string.IsNullOrEmpty(PasswordTextBox.Password))
                {
                    resume = false;
                    SetStatus("Пожалуйста, введите пароль");
                }
            }

            if (resume)
            {
                /*
                    До сих пор окно логина было на переднем плане,
                    снимаем флаг. Теперь могут быть диалоговые окна,
                    которые должны иметь более высокий приоритет.
                 */


                if (Window != null)
                {
                    Window.Topmost = false;
                }


                //если все хорошо, проведем логин пользователя
                //var loginResult = Central.Login(Login, Password);
                //resume = loginResult == 0;

            }

            if (resume)
            {
                var loginResult = Central.LPackClient.DoLogin(LoginTextBox.Text, PasswordTextBox.Password);
                if (!loginResult)
                {
                    resume = false;

                    var query = Central.LPackClient.LoginQuery;
                    if ((query.Answer.Error.Code == 40) || (query.Answer.Error.Code == 41))
                    {
                        SetStatus(query.Answer.Error.Message);
                        PasswordTextBox.Password = "";
                    }
                    else if (query.Answer.Error.Code == 7)
                    {
                        Central.LPackClient.DoHop(true);
                        SetStatus("Сервер не отвечает. Повторите вход");
                    }
                    else
                    {
                        query.ProcessError();
                    }
                }
            }

            if (resume)
            {
                SetStatus("Подождите, программа запускается", false);

                //перейдем ко второй фазе запуска                
                Central.StartUp();
                Window?.Close();
            }

            return resume;
        }
        private Window Window { get; set; }

        public void Show()
        {
            //если в конфиге есть логин и пароль, загружаем их в форму
            if (!string.IsNullOrEmpty(Central.Config.Login))
            {
                LoginTextBox.Text = Central.Config.Login;
            }

            //только в отладочном режиме позволяем брать пароль из конфига и автоматически логиниться
            if (Central.DebugMode)
            {
                if (!string.IsNullOrEmpty(Central.Config.Password))
                {
                    PasswordTextBox.Password = Central.Config.Password;
                }

                if (Central.Config.AutoLogin)
                {
                    if (DoLogin())
                    {
                        return;
                    }
                }
            }

            var title = GetTitle();

            var w = 520;
            var h = 94;

            Window = new Window
            {
                Title = title,
                Width = w + 17,
                Height = h + 40,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow,
                Content = new Frame
                {
                    Content = this,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                },
            };

            if (Window != null)
            {
                Window.Topmost = true;
                Window.ShowDialog();
            }
        }

        private void Enter_OnClick(object sender, RoutedEventArgs e)
        {
            DoLogin();
        }
    }
}
