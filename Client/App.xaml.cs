using Client.Common;
using DevExpress.Xpf.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;


namespace Client
{
    /// <summary>
    /// Главный класс приложения
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            ApplicationThemeHelper.ApplicationThemeName = MyThemeName;

            //в режиме отладки эта функция не работает (иначе она перебивает встроенный отладчик)
            #if DEBUG

            #else
                DispatcherUnhandledException += App_DispatcherUnhandledException;
            #endif

            //отрабатываем проверку копий приложения
            CheckInstance();

            //стартуем главный процесс
            Central.Init();
        }

        const string MyThemeName = "Office2019Colorful";

        [STAThread]
        public static void Main()
        {
            var app = new App();
            app.Run();
        }

        private Mutex _mutex;

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr handle);
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr handle, int nCmdShow);
        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool IsIconic(IntPtr handle);

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //в режиме отладки эта функция не работает (иначе она перебивает встроенный отладчик)
            #if DEBUG
        
            #else
                DispatcherUnhandledException += App_DispatcherUnhandledException;
            #endif
        }

        private void CheckInstance()
        {
            /*
                //нельзя запускать только 2 инстанса одной версии
                //иначе не будет работать рестарт после обновления
                //подмешиваем в идентификатор номер версии
            */
            Version curVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            var name = $"L-PACK_ERP-{curVersion}";

            /*
                в отладочном режиме еще подмешаем суффикс, т.о. мы сможем
                заустить один отладочный процесс вместе с рабочим
            */
            #if DEBUG
                name = $"{name}_DBG";
            #endif

            _mutex = new Mutex(true, name, out var createdNew);

            if (!createdNew)
            {
                var current = Process.GetCurrentProcess();

                //среди процессов найдем процесс с таким же именем, но с отличным ID
                foreach (var process in Process.GetProcessesByName(current.ProcessName))
                {
                    if (process.Id != current.Id)
                    {

                        /*
                        string[] args = Environment.GetCommandLineArgs();
                        if(args.Length > 0)
                        {
                            UnsafeNative.SendMessage(process.MainWindowHandle,string.Join(" ",args));
                        }
                        */

                        IntPtr handle = process.MainWindowHandle;

                        //если окно свернуто, размернем
                        if (IsIconic(handle))
                        {
                            ShowWindow(handle, 9);
                        }

                        //перенесем на передний план
                        UnsafeNative.SetForegroundWindow(handle);

                        //передадим параметры командной строки в виде одной строки
                        string[] args = Environment.GetCommandLineArgs();
                        var s = $"{string.Join("|", args)}";
                        UnsafeNative.SendMessage(handle, s);

                        break;
                    }
                }

                Current.Shutdown();
            }
            else
            {
                Exit += CloseMutexHandler;
            }

        }

        private void CloseMutexHandler(object sender, EventArgs e)
        {
            _mutex?.Close();
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Central.SendReport("", e, false, true);
        }
    }
}