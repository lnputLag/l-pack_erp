using Client.Assets.HighLighters;
using Client.Common;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// форма авторизации
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2019-10-17</released>
    /// <changed>2024-04-18</changed>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            CenterWindowOnScreen();
            Loaded += OnLoad;
            Closed += OnClose;
            KeyDown += OnKeyDown;
            Hide();

            Login = "";
            Password = "";
            AutoLogin = false;

            Central.MessageBus.AddProcessor(new MessageProcessor(ProcessMessage));
        }

        private DispatcherTimer StatusTimer { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public bool  AutoLogin { get; set; }

        private void CenterWindowOnScreen()
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 2) - (windowHeight / 2);
        }

        private string GetTitle()
        {
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

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            SetValues();
        }

        private void SetValues()
        {
            LoginTextBox.Text = Login;
            PasswordTextBox.Password = Password;

            LoginTextBox.Focus();
            if(!Login.IsNullOrEmpty())
            {
                PasswordTextBox.Focus();
            }

            Title = GetTitle();
            SetStatus(" ");
        }

        private void OnClose(object sender, EventArgs e)
        {
            Central.Terminate();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
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

                {
                    var row = new Dictionary<string, string>();
                    row.CheckAdd("TIMEOUT", "5");
                    row.CheckAdd("DESCRIPTION", "");
                    Central.Stat.TimerAdd("LoginWindow_SetStatus", row);
                }

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

        public void DoAutoLogin()
        {
            var resume = true;

            if (resume)
            {
                var loginResult = Central.LPackClient.DoLogin(Login, Password);
                if (!loginResult)
                {
                    resume = false;

                    SetValues();

                    var query = Central.LPackClient.LoginQuery;
                    if(query.Answer.Status != 0)
                    {
                        SetStatus(query.Answer.Error.Message);
                    }
                }
            }

            if(!resume)
            {
                ShowDialog();
            }

            if (resume)
            {
                Topmost = false;
                DoReturn();
            }
        }

        private void DoReturn()
        {
            Central.StartUp();            
        }

        private bool DoLogin()
        {
            var resume = true;

            var Login = LoginTextBox.Text;
            var Password = PasswordTextBox.Password;
            bool? _saveLogin = SaveLogin.IsChecked;

            if(resume)
            {
                if(Login.IsNullOrEmpty())
                {
                    resume = false;
                    SetStatus("Пожалуйста, введите логин");
                }
                else
                {
                    Login = Login.ToLower();
                }
            }

            if (resume)
            {
                if(Password.IsNullOrEmpty())
                {
                    resume = false;
                    SetStatus("Пожалуйста, введите пароль");
                }
            }

            if (resume)
            {
                SetStatus("Подождите, выполняется вход", false);
                var loginResult = Central.LPackClient.DoLogin(Login, Password, _saveLogin);
                if (!loginResult)
                {
                    resume = false;
                    var query = Central.LPackClient.LoginQuery;
                    if(query.Answer.Status != 0)
                    {
                        SetStatus(query.Answer.Error.Message);
                    }
                }
            }

            if (resume)
            {
                SetStatus("Подождите, программа запускается", false);
                DoReturn();
            }

            return resume;
        }

        public void Init()
        {
            if(AutoLogin)
            {
                DoAutoLogin();
            }
            else
            {
                ShowDialog();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Central.Terminate();
        }

        private void Enter_OnClick(object sender, RoutedEventArgs e)
        {
            DoLogin();
        }

        public void ProcessMessage(MessageItem m)
        {
            if(m != null)
            {
                if(m.ReceiverName == this.GetType().Name)
                {
                    switch(m.Action)
                    {
                        case "Show":
                            {
                                Show();
                            }
                            break;

                        case "Hide":
                            {
                                Hide();
                            }
                            break;
                    }
                }
            }
        }
    }
}
