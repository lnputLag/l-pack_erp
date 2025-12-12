
using Client.Interfaces.Debug;
using Client.Interfaces.Main;
using Client.Interfaces.Production;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Printing;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;
using static Client.Common.LPackClientRequest;

namespace Client.Common
{
    /// <summary>
    /// главный объект приложения
    /// (синглтон-объект, организующий инфраструктуру)
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public static class Central
    {
        /*  
            Центральный класс приложения клиента, бэкбон.
            (https://en.wikipedia.org/wiki/Grand_Central_Terminal)
            Содержит точки входа и выхода, основные структуры для работы клиента.
            Является синглтоном, доступен в любом месте клиентского приложения.

            Точка входа: Init()

                Init() 
                ConnectionCheck()
                InitContinue()
                    CheckUpdates()
                <login>
                StartUp()
                ProcessCmdArgs()
                    
            Точка выхода: Terminate()
         
            Здесь также проходит основной процесс запуска программы.
            Сначала показывается форма логина: Init()
            Производится процедура обновления: CheckUpdates()
            Показывается главное окно: StartUp()
            Завершение программы: Terminate()          
         */


         /// <summary>
        /// точка входа в приложение
        /// </summary>
        public static void Init(bool reloadConfigOnly = false)
        {
            if (!reloadConfigOnly)
            {
                StartDate = DateTime.Now;
                ConfigContent = "";
                Uptime = 0;
                Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                Logger = new Logger();
                Stat = new Stat();

                State = "INIT";
                ConnectingInProgress = false;
                ConnectingHopping = false;
                ServerIP = "";
                ProgramVersion = "";

                EmbdedMode = false;
                DeveloperMode = false;
                AutoRestartMode = false;
                WindowWidth = 0;
                WindowHeight = 0;

                Queries = new Dictionary<string, LPackClientQuery>();                
                //ResourcesGridBox=new Dictionary<string,Dictionary<string, GridBox>>();
                Parameters = new Parameters();
                SystemInfo = new Dictionary<string, string>();

                AppSettings =new Settings();
                AppSettings.Restore();

                SetMode(false);
#if DEBUG
                SetMode(true);
#endif

                SessionValues = new Dictionary<string, Dictionary<string, string>>();

                ServerLocks = new Dictionary<string, string>();
                ServerParams = new Dictionary<string, string>();
                ServerInfo = new Dictionary<string, string>();

                //имя файла (используется системой обновления)
                SelfExe = "l-pack_erp.exe";
                BackupExe = "_l-pack_erp.exe";

                //имя программы
                var info = new AssemblyInfo();
                ProgramTitle = info.Title;
                WindowSize = "";

                Msg = new Msg();
                MessageBus = new MessageBus();
            }

            //загрузка конфигурации из файла
            {
                var pathInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
                string configFile = $"{pathInfo.Directory}\\application.config";

                try
                {
                    var configLoader = new Config<LPackConfig>(configFile);
                    Config = configLoader.Load();
                    ConfigContent = System.IO.File.ReadAllText(configFile);
                }
                catch (Exception e)
                {
                    string message = "";
                    message += $"Не удалось загрузить файл конфигурации.";
                    message += $"\nПрограмма будет закрыта.";
                    message += $"\n";
                    message += $"\nПрограмма повреждена, попробуйте ее переустановить.";
                    var description = $"";
                    //description += $"\n{ReportTo}";
                    description += $"\n";
                    description += $"\n{e}";
                    var e3 = new DialogWindow(message, "", description);
                    e3.ShowDialog();

                    Terminate();
                }
            }

            //обработка специальных переменных конфигурации
            if(Config!=null)
            {
                if(Config.KeyboardInputBufferClearTimeout > 0)
                {
                    Parameters.KeyboardInputBufferClearTimeout=Config.KeyboardInputBufferClearTimeout;
                }

                if(Config.DebugMode)
                {
                    DebugMode = true;
                }

                if(Config.DeveloperMode)
                {
                    DeveloperMode = true;
                }

                if(Config.FactoryId==0)
                {
                    Config.FactoryId=1;
                }
            }

            if (LPackClient == null)
            {
                LPackClient = new LPackClient();
            }
            LPackClient.Init(Config);


            State = "INIT";
            //SplashUpdate(1, "Программа запускается.");

            if (!reloadConfigOnly)
            {
                ConnectionCheck();
                //SplashUpdate(0, "");
            }
        }

        /// <summary>
        /// флажок отладочной сборки, поднимается, если клиент собран с таргетом DEBUG 
        /// </summary>
        public static bool DebugMode { get; set; }
        public static bool EmbdedMode { get; set; }
        public static bool DeveloperMode { get; set; }
        public static bool AutoRestartMode { get; set; }
        public static int WindowWidth { get; set; }
        public static int WindowHeight { get; set; }
        //общесистемные константы
        public static readonly string UpdatesRepo = "/repo/l-pack_erp/vers.xml";
        public static readonly string ReportTo = "Вы можете связаться с разработчиками для получения помощи \nпо телефону: 1708";
        public static readonly string DocServerUrl = "http://192.168.3.237";
        public static readonly string ProgramName = "L-Pack ERP";
        public static string ProgramVersion { get; set; }
        /// <summary>
        /// менеджер табов, централизует табы в едином реестре
        /// </summary>
        public static WindowManager WM { get; set; }
        public static Msg Msg { get; set; }
        public static MessageBus MessageBus { get; set; }

        public static string SelfExe { get; set; }
        public static string BackupExe { get; set; }
        public static string ProgramTitle { get; set; }
        public static string WindowSize { get; set; }     
        public static Stat Stat { get; set; }

        /// <summary>
        /// клиентская библиотека для отправки запросов на сервер
        /// </summary>
        public static LPackClient LPackClient { get; private set; }
        public static Dictionary<string, LPackClientQuery> Queries { get; set; }
        /// <summary>
        /// реестр ресурсов
        /// tabName->gridName->GridBox
        /// </summary>
        //public static Dictionary<string,Dictionary<string, GridBox>> ResourcesGridBox { get; set; }
        /// <summary>
        /// объект конфигурации
        /// </summary>
        public static LPackConfig Config { get; private set; }
        /// <summary>
        /// пользователь
        /// </summary>
        public static User User { get; set; }
        /// <summary>
        /// словарь переменных сервера (свойства сервера)
        /// </summary>
        public static Dictionary<string, string> ServerInfo { get; set; }
        public static Dictionary<string, string> ServerLocks { get; set; }
        public static Dictionary<string, string> ServerParams { get; set; }
        /// <summary>
        /// объект системы обновления
        /// </summary>
        public static Updater2 Updater;      
        public static Parameters Parameters { get; set; }
        public static Dictionary<string, Dictionary<string, string>> SessionValues { get; set; }
        /// <summary>
        /// реестр локальных параметров
        /// (сохраняется в application.settings)
        /// </summary>
        public static Settings AppSettings { get; set; }
        /// <summary>
        /// выполняется восстановление соединения
        /// </summary>
        [Obsolete]
        public static bool ConnectingInProgress { get; set; }
        public static bool ConnectingHopping { get; set; }
        /// <summary>
        /// абстрактный флаг состояния (INIT, UPDATE, LOGIN, WORK, OFFLINE)
        /// </summary>
        public static string State { get; set; }
        public static Navigator Navigator { get; set; }
      
        /// <summary>
        /// IP адрес текущего сервера, например: 192.168.3.204
        /// </summary>
        public static string ServerIP { get; set; }
        public static void SetServerIP(string ip)
        {
            ServerIP = ip;
        }
        public static DialogWindow Dialog { get; set; }
        public static Logger Logger { get; set; }
        public static string Version {get;set;}
        public static int Uptime {get;set;}
        public static DateTime StartDate {get;set;}
        public static string ConfigContent  {get;set;}

        public static SplashWindow SplashInterface { get; set; }
        public static LoginWindow LoginInterface { get; set; }

        /// <summary>
        /// установка режима: отладочный/ рабочий
        /// </summary>
        /// <param name="mode"></param>
        public static void SetMode(bool mode)
        {
            DebugMode=mode;

            if(Parameters!=null)
            {
                if(DebugMode){
                    Parameters.GlobalDebugOutput = true;
                    Parameters.GlobalLogging=true;
                    //Parameters.UseRequestLog=true;
                }
                else
                {
                    Parameters.GlobalDebugOutput = false;
                    Parameters.GlobalLogging=false;
                    //Parameters.UseRequestLog=false;
                }
            }
        }

        public static void InitContinue()
        {
            //контроллер табов
            WM = new WindowManager();

            // автообновление
            State = "UPDATE";
            SplashUpdate(1, "Программа обновляется.");

            CheckUpdates();

            SplashUpdate(0, "");

            Application.Current.Dispatcher.Invoke(() =>
            {
                InitLogin();
            });
        }

        public static void SplashUpdate(int mode = 1, string text = "")
        {
            //if(!DebugMode)
            {
                if(SplashInterface == null)
                {
                    SplashInterface = new SplashWindow();
                }

                var action = "";
                switch(mode)
                {
                    case 1:
                    action = "ShowWait";
                    break;

                    case 9:
                    action = "Show";
                    break;

                    case 0:
                    action = "Hide";
                    break;
                }

                if(!action.IsNullOrEmpty())
                {
                    Central.MessageBus.SendMessage(new MessageItem()
                    {
                        ReceiverGroup = "",
                        ReceiverName = "SplashWindow",
                        SenderName = "Central",
                        Action = action,
                        Message = text
                    });
                }
            }
        }

        public static void InitLogin()
        {
            State = "LOGIN";

            if(LoginInterface == null)
            {
                LoginInterface = new LoginWindow();
            }


            LoginInterface.Login = Config.Login;
            LoginInterface.Password = Config.Password;
            LoginInterface.AutoLogin = Config.AutoLogin;
            LoginInterface.Init();
        }

        public static async void ConnectionCheck()
        {
            bool result = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Session");
            q.Request.SetParam("Object", "Info");
            q.Request.SetParam("Action", "GetVersion");

            q.Request.Timeout = Central.Parameters.RequestTimeoutSystem;
            q.Request.Attempts = 3;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                result = true;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                InitContinue();
            });
        }



        public static void CheckUpdates()
        {
            //Updater = new Updater2($"http://{ServerIP}");

            var updateServerUrl="http://192.168.3.204";
            var connection=Central.LPackClient.CurrentConnection;
            var u=connection.Host;
            if(!u.IsNullOrEmpty())
            {
                updateServerUrl=$"http://{u}";
            }

            Updater = new Updater2(updateServerUrl);

            //в отладочном режиме обновления не проверяются
            if (!DebugMode)
            {
                var makeUpdate = true;
                {
                    var lockFileName = "no_update";
                    if(System.IO.File.Exists(lockFileName))
                    {
                        makeUpdate=false;
                    }
                }

                if(makeUpdate)
                {
                    Updater.CheckUpdate(true);
                }                
            }
        }


        public static Window MainWindow;

        public async static void StartUp()
        {
            State = "WORK";

            /*
                (1) LoadUserInfo();
                        + используется
                    Session>Auth>GetUserInfo
                    ->Central.User
                        структура данных пользователя
                
                (2) LoadUserParameters();
                        + использутся повсеместно
                    Service>LiteBase>List
                    ->Central.User.UserParameterList
                        словарь кастомных параметров
                        пользовательские настройки для lpack_erp
                        Например сейчас там хранится информация 
                        — по выбору принтера на главном компьютере стекера
                        — по выбранному подписанту для печати документов
                        — по пользовательскому расположению колонок в гриде
                        извлекается из таблицы
                        <lite_base>(shared)/user_parameters
                
                (3) LoadServerInfo();
                        + используется для отображения состояния сервера:
                          версия, тип базы данных, флаг отладочного режима и т.д.
                    Session>Info>GetServerInfo
                    ->Central.ServerInfo
                        словарь кастомных параметров
                        Dictionary<string,string>
                        извлекается из внутренних регистров сервера

                (4) LoadServerParams();      
                        - не используется                    
                    Session>Info>GetServerParams
                        Server.Lock
                    ->Central.ServerLocks
                        все настроечные файлы сервера
                        Dictionary<string,string>
                            section=>content
             */

            LoadUserInfo();
            LoadUserParameters();
            LoadServerInfo();
            LoadServerParams();            

            Navigator = new Navigator();
            Navigator.Init();

            var mainWindow = new MainWindow();
            mainWindow.Show();
            MainWindow = mainWindow;

            {
                Central.MessageBus.SendMessage(new MessageItem()
                {
                    ReceiverGroup = "",
                    ReceiverName = "LoginWindow",
                    SenderName = "Central",
                    Action = "Hide",
                    Message = ""
                });
            }

            {
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "All",
                    ReceiverName = "",
                    SenderName = "Central",
                    Action = "StartUp",
                    Message = ""
                });
            }

            ProcessCmdArgs();
        }


        /// <summary>
        /// звгрузка данных пользователя
        /// </summary>
        public static void LoadUserInfo()
        {
            User = _LPackClientDataProvider.DoQueryDeserialize<User>("Session", "Auth", "GetUserInfo", "", null, Central.Parameters.RequestTimeoutSystem);
        }

        [Obsolete]
        public async static void LoadServerInfo()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Session");
            q.Request.SetParam("Object", "Info");
            q.Request.SetParam("Action", "GetServerInfo");

            q.Request.Timeout = Central.Parameters.RequestTimeoutSystem;
            q.Request.Attempts = 3;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                ServerInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(q.Answer.Data);
            }
        }

        [Obsolete]
        public static async void LoadServerParams()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Session");
            q.Request.SetParam("Object", "Info");
            q.Request.SetParam("Action", "GetServerParams");

            q.Request.Timeout = Central.Parameters.RequestTimeoutSystem;
            q.Request.Attempts = 3;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var locksDS = ListDataSet.Create(result, "LOCKS");
                    if (locksDS.Items.Count > 0)
                    {
                        var first = locksDS.Items.First();
                        if (first != null)
                        {
                            ServerLocks = first;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Получение пользовательских настроек
        /// </summary>
        public static async void LoadUserParameters()
        {
            User.UserParameterList = new List<UserParameter>();

            var p = new Dictionary<string, string>();
            p.Add("TABLE_NAME", "user_parameters");
            p.Add("TABLE_DIRECTORY", $"{Central.GetSystemInfo().CheckGet("HOST_USER_ID").ToLower()}/{User.Login.ToLower()}");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Service");
            q.Request.SetParam("Object", "LiteBase");
            q.Request.SetParam("Action", "List");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestTimeoutSystem;
            q.Request.Attempts = 3;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var userParametersDS = ListDataSet.Create(result, "user_parameters");
                    if (userParametersDS != null && userParametersDS.Items != null && userParametersDS.Items.Count > 0)
                    {
                        foreach (var item in userParametersDS.Items)
                        {
                            UserParameter userParameter = new UserParameter(
                                item.CheckGet("NAME"),
                                item.CheckGet("VALUE"),
                                item.CheckGet("DESCRIPTION"),
                                item.CheckGet("INTERFACE"),
                                item.CheckGet("LOGIN"),
                                item.CheckGet("HOST_USER_ID"),
                                item.CheckGet("PRIMARY_KEY"));

                            User.UserParameterList.Add(userParameter);
                        }
                        var r0 = User.UserParameterList;
                    }
                }
            }
        }

        public static void SaveUserParameters()
        {
            List<Dictionary<string, string>> itemList = new List<Dictionary<string, string>>();
            if (User != null && User.UserParameterList != null)
            {
                foreach (UserParameter userParameter in User.UserParameterList)
                {
                    Dictionary<string, string> parameters = new Dictionary<string, string>();
                    parameters.Add("NAME", userParameter.Name);
                    parameters.Add("VALUE", userParameter.Value);
                    parameters.Add("DESCRIPTION", userParameter.Description);
                    parameters.Add("INTERFACE", userParameter.Interface);
                    parameters.Add("LOGIN", userParameter.Login);
                    parameters.Add("HOST_USER_ID", userParameter.HostUserId);
                    parameters.Add("PRIMARY_KEY", userParameter.PrimaryKey);
                    itemList.Add(parameters);
                }

                var p = new Dictionary<string, string>();
                var jsonString = JsonConvert.SerializeObject(itemList);
                p.Add("ITEMS", jsonString);
                p.Add("TABLE_NAME", "user_parameters");
                p.Add("TABLE_DIRECTORY", $"{Central.GetSystemInfo().CheckGet("HOST_USER_ID").ToLower()}/{User.Login.ToLower()}");
                p.Add("PRIMARY_KEY", "PRIMARY_KEY");

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Service");
                q.Request.SetParam("Object", "LiteBase");
                q.Request.SetParam("Action", "SaveList");
                q.Request.SetParams(p);

                q.Request.Timeout = Central.Parameters.RequestTimeoutSystem;
                q.Request.Attempts = 1;

                q.DoQuery();
            }
        }

        /// <summary>
        /// Разбор параметров командной строки
        /// </summary>
        public static void ProcessCmdArgs(Dictionary<string, string> arguments = null)
        {

            if (arguments == null)
            {
                //разбираем аргументы командной строки
                arguments = new Dictionary<string, string>();
                string[] a = Environment.GetCommandLineArgs();
                for (int index = 1; index < a.Length - 1; index += 2)
                {
                    arguments.Add(a[index], a[index + 1]);
                }
            }

            //если есть параметр url, осуществляем переход 
            if (arguments.ContainsKey("-url"))
            {
                var url = arguments["-url"];
                url = url.Trim();

                Dbg($"URL: [{url}]");
                //DialogWindow.ShowDialog($"URL: [{url}]");

                if (!string.IsNullOrEmpty(url))
                {
                    Navigator.ProcessURL(url);
                }
            }
        }


        /// <summary>
        /// процедура логина, использует Connection.Login, а также делает дополнительные запросы
        /// возвращает 0 если логин прошел успешно
        /// </summary>
        /// <param name="login"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static int Login(string login, string password)
        {
            int result = 1;
            return result;
        }



        /// <summary>
        /// Проверяет, не выполняется ли сейчас код в дизайнере.
        /// Если выполняется, возвращает true
        /// (когда код разметки XAML загружается в дизайнер, вызываются его конструкторы,
        /// таким образом мы предотвращаем выполнение программного кода в процессе работы с дизайнером)
        /// </summary>
        /// <returns></returns>
        public static bool InDesignMode()
        {
            return !(Application.Current is App);
        }

        public static void Terminate()
        {
            LPackClient.Destroy();
            Application.Current.Shutdown();
        }

        public static void Restart()
        {
            LPackClient.Destroy();
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        public static string GetTempFilePathWithExtension(string extension)
        {
            var path = Path.GetTempPath();
            var fileName = Guid.NewGuid().ToString() + "." + extension;
            return Path.Combine(path, fileName);
        }

        public static string GetTempFilePathWithExtension(string extension, string name)
        {
            var path = Path.GetTempPath();
            var fileName = name + "." + extension;
            return Path.Combine(path, fileName);
        }

        public static void DoService()
        {
            try
            {
                if(Central.Parameters.UseRequestLog)
                {
                    if(Queries != null)
                    {
                        if(Queries.Count > 0)
                        {
                            lock(Queries)
                            {
                                var list = new Dictionary<string, LPackClientQuery>(Queries);
                                foreach(var item in list)
                                {
                                    var id = item.Key;
                                    var finish = item.Value.Finish.ToString();
                                    var dt = Tools.TimeOffset(finish);
                                    if(dt > 300000)
                                    {
                                        if(Queries.ContainsKey(id))
                                        {
                                            Queries.Remove(id);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }                
            }
            catch(Exception e)
            {

            }
        }

        /// <summary>
        /// Открытие файла в ассоциированной программе.        
        /// </summary>
        /// <param name="path">путь к файлу в  файловой системе клиента.</param>
        public static void OpenFile(string path = "")
        {
            if (path != "")
            {
                try
                {
                    Dbg($"OpenFile path=[{path}]");
                    System.Diagnostics.Process.Start(path);
                }
                catch (Exception ex)
                {
                    var fileInfo = new FileInfo(path);

                    string message = "";
                    message += $"Не удалось открыть файл.";

                    string description = $"";
                    description += $"\n";
                    description += $"\nДополнительная информация.";
                    description += $"\nФайл: {path}";
                    description += $"\nДата: {DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}";
                    description += $"\nОшибка: {ex.Message}";

                    DialogWindow.ShowDialog(message, "Открытие файла", description);
                }
            }
            else
            {
                string message = "";
                message += $"Не удалось открыть файл.";
                message += $"Путь к файлу пуст.";
                string description = $"";
                DialogWindow.ShowDialog(message, "Открытие файла", description);
            }
        }

        /// <summary>
        /// Сохранение файла.
        /// Пользователю будет задан вопрос, где сохранить файл. Далее файл будет сохранен
        /// по указанному пользователем пути.
        /// </summary>
        /// <param name="filePath">путь ко временному файлу в  файловой системе клиента.</param>
        public static string SaveFile(string filePath = "", bool addExtension = false, string customFileName = "", string initialDirectory="")
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                string name = fileInfo.Name;
                string ext = fileInfo.Extension;

                var fd = new SaveFileDialog();
                fd.FileName = $"{name}";

                if (!string.IsNullOrEmpty(customFileName))
                {
                    fd.FileName = customFileName;
                }
                
                if (addExtension)
                {
                    fd.DefaultExt = ext;
                    fd.AddExtension = true;
                }

                if (!initialDirectory.IsNullOrEmpty()) {
                    fd.InitialDirectory = initialDirectory;
                }

                var fdResult = fd.ShowDialog();
                if (fdResult == true)
                {
                    if (!string.IsNullOrEmpty(fd.FileName))
                    {
                        var filePath2 = fd.FileName;
                        //Central.Dbg($"DownloadSave path=[{filePath}] path2=[{filePath2}]");

                        try
                        {
                            //если мы сюда попали, пользователь уже подтвердил перезапись
                            if (System.IO.File.Exists(filePath2) && filePath!= filePath2)
                            {
                                System.IO.File.Delete(filePath2);
                            }
                            File.Copy(filePath, filePath2);
                            return filePath2;
                        }
                        catch (Exception e)
                        {
                            string message = "";
                            message += $"Не удалось сохранить файл.";
                            message += $"\nПопробуйте выбрать другое место для сохранения файла.";
                            string description = $"";
                            description += $"\n";
                            //description += $"\n{Central.ReportTo}";
                            //description += $"\n";
                            description += $"\nДополнительная информация.";
                            description += $"\nФайл: {filePath2}";
                            description += $"\nДата: {DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}";
                            description += $"\n{e.ToString()}";

                            DialogWindow.ShowDialog(message, "Сохранение файла", description);
                            return null;
                        }
                    }

                }
            }
            return null;
        }

        /// <summary>
        /// Сохранение файла.
        /// Пользователю будет задан вопрос, где сохранить файл. Далее файл будет сохранен
        /// по указанному пользователем пути.
        /// </summary>
        /// <param name="filePath">путь ко временному файлу в  файловой системе клиента.</param>
        public static void SaveFile(int windowLeft, int windowTop, string filePath = "", bool addExtension = false)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                Window tempWindow = new Window();
                tempWindow.Width = 0;
                tempWindow.Height = 0;
                tempWindow.Left = windowLeft - 450; //MainWindow.Width / 2 - 450;
                tempWindow.Top = windowTop - 250; //MainWindow.Height / 2 - 250;
                tempWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                tempWindow.WindowStyle = WindowStyle.None;
                tempWindow.ResizeMode = ResizeMode.NoResize;
                tempWindow.Show();

                var fileInfo = new FileInfo(filePath);
                string name = fileInfo.Name;
                string ext = fileInfo.Extension;

                var fd = new SaveFileDialog();
                fd.FileName = $"{name}";

                if (addExtension)
                {
                    fd.DefaultExt = ext;
                    fd.AddExtension = true;
                }

                var fdResult = fd.ShowDialog();
                if (fdResult == true)
                {
                    if (!string.IsNullOrEmpty(fd.FileName))
                    {
                        var filePath2 = fd.FileName;
                        //Central.Dbg($"DownloadSave path=[{filePath}] path2=[{filePath2}]");

                        try
                        {
                            //если мы сюда попали, пользователь уже подтвердил перезапись
                            if (System.IO.File.Exists(filePath2))
                            {
                                System.IO.File.Delete(filePath2);
                            }
                            File.Copy(filePath, filePath2);

                        }
                        catch (Exception e)
                        {
                            string message = "";
                            message += $"Не удалось сохранить файл.";
                            message += $"\nПопробуйте выбрать другое место для сохранения файла.";
                            string description = $"";
                            description += $"\n";
                            //description += $"\n{Central.ReportTo}";
                            //description += $"\n";
                            description += $"\nДополнительная информация.";
                            description += $"\nФайл: {filePath2}";
                            description += $"\nДата: {DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}";
                            description += $"\n{e.ToString()}";

                            DialogWindow.ShowDialog(message, "Сохранение файла", description);
                        }

                    }

                }

                tempWindow.Close();
                tempWindow = null;
            }
        }

        /// <summary>
        /// Открытие пути к папке в Explorer
        /// </summary>
        /// <param name="folderPath"></param>
        public static void OpenFolder(string folderPath="")
        {
            if (!string.IsNullOrEmpty(folderPath))
            {
                if (Directory.Exists(folderPath))
                {
                    try
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            Arguments = folderPath,
                            FileName = "explorer.exe"
                        };

                        Process.Start(startInfo);
                    }
                    catch(Exception ex)
                    {
                        DialogWindow.ShowDialog($"Нет доступа к папке [{folderPath}]", "Открытие папки", 
                            $"{Environment.NewLine}Дополнительная информация." +
                            $"{Environment.NewLine}Ошибка: {ex.Message}");
                    }
                }
                else
                {
                    DialogWindow.ShowDialog($"Папка [{folderPath}] не найдена", "Открытие папки");
                }
            }
        }

        public static void ShowHelp(string url, bool embded = false, bool urlAbsolute=false)
        {
            bool resume = true;

            /*
                страницы документации плохо отображаются в IE
                сначала попробуем открыть страницу документации в Chrome
                если не получится, то пробуем стандартный браузер
            */


            if (resume)
            {
                if (Central.EmbdedMode || embded)
                {
                    var m = "mode=embded";
                    url = $"{Central.DocServerUrl}{url}?{m}";
                    var h = new DocTouch();
                    h.Url = url;
                    h.Init();
                    resume = false;
                }
            }


            if (resume)
            {
                try
                {
                    var u = $"{Central.DocServerUrl}{url}";
                    if(urlAbsolute)
                    {
                        u = $"{url}";
                    }
                    Process.Start("chrome", u);
                    resume = false;
                }
                catch (Exception)
                {
                    // ignored
                }
            }


            if (resume)
            {
                try
                {
                    Process.Start($"{Central.DocServerUrl}{url}");
                }
                catch (Exception)
                {
                    var message = "";
                    message += $"Не удалось открыть страницу документации.";

                    var description = $"";
                    description += $"\nПроверьте веб-браузер в системе.";
                    description += $"\nНеобходимо чтобы веб-браузер был установлен.";

                    var e3 = new DialogWindow(message, "Просмотр документации", description);
                    e3.ShowDialog();
                }
            }
        }

        /// <summary>
        /// Функция отработки ошибок логики приложения.
        /// коллектор для централизации отработки ошибок
        /// http://192.168.3.237/developer/l-pack-erp/client/errors
        /// </summary>
        public static void ProcError(Error error, string customMessage = "", bool sendReport = false, LPackClientQuery q = null)
        {
            bool showDialog = true;
            bool sendReport2 = sendReport;


            var reportId = Cryptor.MakeRandom();
            if (
                error.Code == 31           
            )
            {
                return;
            }

            var route=$"{q?.Request.Params.CheckGet("Module")}>{q?.Request.Params.CheckGet("Object")}>{q?.Request.Params.CheckGet("Action")}";

            //message
            string title = ProgramName;

            string message = "";

            if (!string.IsNullOrEmpty(customMessage))
            {
                message += $"{customMessage}";

            }
            else if (!string.IsNullOrEmpty(error.Message))
            {
                message += $"{error.Message}";

                {
                    error.Description += $"\n{route}";
                }

                if (!string.IsNullOrEmpty(error.Description))
                {
                    message += $"\n{error.Description}";
                }

            }
            else
            {
                message += $"В программе произошла ошибка.";
            }

            if (error.Code == 102)
            {
                message = "";
                message += $"\nПри выполнении операции произошла ошибка.";
            }

            if (error.Code == 6 || error.Code == 7 || error.Code == 9)
            {
                message = "";
                message += $"\nПри выполнении операции произошла ошибка.";
                message += $"\nПовторите предыдущую операцию.";

                showDialog = false;
                sendReport2 = false;
            }

            string description = $"";
            description += $"\n";
            description += $"\nКод ошибки: {error.Code}";
            description += $"\nДата: {DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")} Репорт: {reportId}";
            description += $"\nВерсия: {ProgramVersion} Сервер: {ServerIP}";




            if(sendReport2)
            {
                if(User != null)
                {
                    description += $"\nЛогин: {User.Login}";

                    var today = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    var login = User.Login;
                    var version = Assembly.GetExecutingAssembly().GetName().Version;


                    var s = "";
                    s = $"{s}\n отчет об ошибке клиентской программы";
                    s = $"{s}\n reportId=[{reportId}]";
                    s = $"{s}\n today=[{today}]";
                    s = $"{s}\n login=[{User.Login}]";
                    s = $"{s}\n userId=[{User.Id}]";
                    s = $"{s}\n version=[{version}]";
                    s = $"{s}\n machine=[{Environment.MachineName}]";
                    s = $"{s}\n errorCode=[{error.Code}]";
                    s = $"{s}\n errorMessage=[{error.Message}]";
                    s = $"{s}\n errorDescription=";
                    s = $"{s}\n ------------------------------------------------------------";
                    s = $"{s}\n {error.Description}";
                    s = $"{s}\n ------------------------------------------------------------";

                    if(q != null)
                    {
                        if(q.SilentErrorProcess)
                        {
                            showDialog = false;
                        }

                        s = $"{s}\n ";

                        s = $"{s}\n ServerIp=[{q.ServerIp}]";
                        s = $"{s}\n ServerUrl=[{q.Url}]";
                        s = $"{s}\n ClientId=[{LPackClient.ClientId}]";
                        s = $"{s}\n RequestId=[{q.RequestId}]";

                        s = $"{s}\n RequestTimeout=[{q.Request.Timeout}]";
                        s = $"{s}\n RequestAttempts=[{q.Request.Attempts}]";
                        s = $"{s}\n AnswerStatus=[{q.Answer.Status}]";
                        s = $"{s}\n AnswerTime=[{q.Answer.Time}]";

                        {
                            s = $"{s}\n ";
                            s = $"{s}\n параметры запроса";
                            foreach(KeyValuePair<string, string> item in q.Request.Params)
                            {
                                s = $"{s}\n     [{item.Key}]=[{item.Value}]";
                            }
                        }

                        {
                            s = $"{s}\n ";
                            s = $"{s}\n ответ";
                            s = $"{s}\n Data=";
                            s = $"{s}\n ------------------------------------------------------------";
                            s = $"{s}\n {q.Answer.Data.ToString()}";
                            s = $"{s}\n ------------------------------------------------------------";
                            s = $"{s}\n DataRaw=";
                            s = $"{s}\n ------------------------------------------------------------";
                            s = $"{s}\n {q.Answer.DataRaw.ToString()}";
                            s = $"{s}\n ------------------------------------------------------------";
                        }
                    }

                    s = $"{s}\n ";
                    var fileName = $"{today}_login-{login}_report-{reportId}_code-{error.Code}.txt";
                    var f = $"{System.IO.Path.GetTempPath()}{fileName}";
                    System.IO.File.WriteAllText(f, s);
                    UploadReport(f, "client_error");
                }
            }
            

            if (showDialog)
            {
                if (Central.Config.SingleInterfaceMode)
                {
                    var m = "";
                    m = $"{m}{message}";
                    m = $"{m}\n{description}";

                    var errorTouch = new ErrorTouch();
                    errorTouch.Show(title, m);
                }
                else
                {
                    DialogWindow.ShowDialog(message, title, description);
                }
            }


        }


        /// <summary>
        /// Отправка отчета о возникновении критической ошибки.
        /// Эта функция должна вызываться при отработке неконтролируемых исключений.
        /// Контролируемые ошибки (ошибки логики приложения) следует отрабатывать в функции ProcError
        /// http://192.168.3.237/developer/l-pack-erp/client/errors
        /// </summary>
        /// <param name="customMessage"></param>
        /// <param name="serializeObject"></param>
        /// <param name="isFatal"></param>
        /// <param name="controlledException">false - не показывать пользователю диалоговое окно</param>
        public static void SendReport(string customMessage = "", object serializeObject = null, bool isFatal = false, bool controlledException = false)
        {
            Mouse.OverrideCursor = null;

            var curVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var version = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion", "ProductName", null);
            var w = SystemParameters.WorkArea.Width;
            var h = SystemParameters.WorkArea.Height;


            var rand = new Random();
            var reportId = rand.Next(111111, 999999);
            var reportFile = $"{DateTime.Now:yyyy.MM.dd HH-mm-ss}_ver_{curVersion}_pc_{Environment.MachineName}_id_{reportId}";

            //generate report
            string report = "";
            report += $"\r\n";
            report += $"\r\nREPORT_ID:{reportId}";
            report += $"\r\nL-PACK ERP. Отчет о программной ошибке";
            report += $"\r\n--------------------------------------";
            report += $"\r\nОбщая информация:";
            report += $"\r\n    Сегодня: {DateTime.Now:dd.MM.yyyy HH:mm:ss}";
            report += $"\r\n    Версия программы: {curVersion}";
            report += $"\r\n    Имя компьютера: {Environment.MachineName}";
            report += $"\r\n    Операционная система: {version}";
            report += $"\r\n    Разрешение экрана: {w}x{h}";

            if (User != null)
            {
                report += $"\r\n    Логин: {User.Login} ID:{User.Id}";
                if (User.Roles != null)
                {
                    if (User.Roles.Count > 0)
                    {
                        report += $"\r\n    Роли:";
                        foreach (KeyValuePair<string, Role> role in User.Roles)
                        {
                            report += $"\r\n        {role.Value.Code.ToString()}";
                        }
                    }
                }
            }


            report += $"\r\n--------------------------------------";
            report += $"\r\nИнформация о сервере:";

            if (ServerInfo != null)
            {
                foreach (var i in ServerInfo)
                {
                    report += $"\r\n    {i.Key}={i.Value}";
                }
            }


            var dumpedObject = DumpObject(serializeObject);
            var x = JsonHelper.FormatJson(dumpedObject);
            report += $"\r\n--------------------------------------";
            report += $"\r\nОбъект, связанный с ошибкой:";
            report += $"\r\n{x}";


            var stackTrace = new StackTrace();
            report += $"\r\n--------------------------------------";
            report += $"\r\nТрейс:";
            report += $"\r\n{stackTrace}";

            var reportSent = "";
            string filePath = "";

            //report file
            {
                var location = new FileInfo(Assembly.GetExecutingAssembly().Location);
                var reportDirPath = $"{location.Directory}\\exceptions";
                Directory.CreateDirectory(reportDirPath);
                filePath = $"{reportDirPath}\\{reportFile}.txt";
                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, report);
                }

                UploadReport(filePath, "client_exception");

                File.Delete(filePath);
            }


            if (dumpedObject.IndexOf("System.ArgumentNullException") > -1)
            {
                controlledException = true;
            }

            // показываем окно пользователю
            if (!controlledException)
            {
                //message
                string title = "Ошибка";

                string message = "";

                message += $"В программе произошла ошибка.";
                if (!string.IsNullOrEmpty(customMessage))
                {
                    message += $"\r\n{customMessage}";
                }

                if (isFatal)
                {
                    message += $"\r\nПрограмма будет закрыта, пожалуйста, запустите программу снова.";
                }
                else
                {
                    message += $"\r\nВы можете отменить операцию и продолжить работу с программой.";
                }


                string description = $"";
                //description += $"\r\n{ReportTo}";
                description += $"\r\n";
                //description += reportSent;
                description += $"\r\nДополнительная информация.";
                description += $"\r\nДата: {DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}";
                description += $"\r\nОтчет: {filePath}";

                DialogWindow.ShowDialog(message, title, description);
            }
        }

        public static bool UploadReportResult { get; set; }
        public async static void UploadReport(string filePath, string storage = "")
        {
            UploadReportResult = false;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "FileStorage");
            q.Request.SetParam("Object", "File");
            q.Request.SetParam("Action", "Upload");

            q.Request.SetParam("Storage", storage);
            q.Request.SetParam("Class", "month");
            q.Request.Type = RequestTypeRef.MultipartForm;

            q.Request.UploadFilePath = filePath;
            q.Request.Attempts = 1;
            q.Request.Timeout = 3000;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                UploadReportResult = true;
            }

            return;
        }

        public static void TestServerError()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProjectManager");
            q.Request.SetParam("Object", "Test");
            q.Request.SetParam("Action", "TestError");

            q.Answer.Type = LPackClientAnswer.AnswerTypeRef.File;

            q.DoQuery();

            if (q.Answer.Status != 0)
            {
                q.ProcessError();
            }
        }

        public static string DumpObject(object obj)
        {
            // некоторые объекты нельзя сериализовать в json

            string str;
            try
            {
                str = JsonConvert.SerializeObject(obj);
            }
            catch (Exception)
            {
                str = "ошибка в процессе сериализации объекта";
                if (obj != null)
                {
                    str += " " + obj;
                    str += " тип объекта " + obj.GetType();
                }
            }

            return str;
        }

        public static void DestroyObject(object obj)
        {
            var type = obj.GetType();
            if (type.GetMethod("Destroy") != null)
            {
                type.InvokeMember("Destroy", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, obj, null);
            }
        }

        /// <summary> 
        /// отладочная функция, выводит сообщение в консоль VisualStudio (Ctrl+Alt+O)
        /// </summary>
        /// <param name="message"></param>
        public static void Dbg(string message)
        {
            if (Parameters.GlobalDebugOutput)
            {
                Debug.WriteLine($"{message}");
            }
        }

        /// <summary>
        /// Сбор диагоностической информации
        /// </summary>
        public static void Diag()
        {
            Process currentProc = Process.GetCurrentProcess();
            long memoryUsed = currentProc.PrivateMemorySize64;
            Central.Dbg($"COMMAND2: Mem=[{memoryUsed}]");
        }

        public static void DoEvents()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
        }

        public static void Exit()
        {

        }

        /// <summary>
        /// отладочная информация, текущее состояние программы
        /// </summary>
        /// <returns></returns>
        public static string MakeInfoString()
        {
            string result = "";

            result = $"{result} Today=[{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}]";

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            result = $"{result}\n Version=[{currentVersion}]";

            var osVersion = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion", "ProductName", null);
            result = $"{result}\n OS=[{osVersion}]";

            result = $"{result}\n EmbdedMode=[{Central.EmbdedMode}] (true=single mode)";
            result = $"{result}\n DebugMode=[{Central.DebugMode}]";
            result = $"{result}\n AutoRestartMode=[{Central.AutoRestartMode}]";
            result = $"{result}\n WindowSize=[{Central.WindowWidth}x{Central.WindowHeight}]";
            result = $"{result}\n DataBase=[{Central.Parameters.BaseLabel}]";

            return result;
        }

        public static int GetUsedMemory()
        {
            Process currentProc = Process.GetCurrentProcess();
            long memoryUsed = currentProc.PrivateMemorySize64;
            int memoryUsedMb = (int)memoryUsed/1000000;            
            return memoryUsedMb;
        }

        private static PerformanceCounter cpuCounter { get; set; }
        private static PerformanceCounter ramCounter { get; set; }
        public static Dictionary<string, string> GetResourcesUsed()
        {
            var p = new Dictionary<string, string>();

            p.CheckAdd("USED_CPU","0");
            p.CheckAdd("USED_RAM", "0");

            try
            {
                if(cpuCounter == null)
                {
                    cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                }

                if(ramCounter == null)
                {
                    ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                }

                if(cpuCounter != null)
                {
                    p.CheckAdd("USED_CPU", cpuCounter.NextValue().ToString());
                }

                if(ramCounter != null)
                {
                    p.CheckAdd("USED_RAM", ramCounter.NextValue().ToString());
                }
            }
            catch(Exception e)
            {

            }

            
            return p;
        }

        public static Dictionary<string, string> SystemInfo {  get; set; }
        public static Dictionary<string,string> GetSystemInfo()
        {

            /*
                [SAVE_CLIENT_DATA]=[1]
                [SYSTEM_OS_NAME]=[Windows 10 Enterprise]
                [SYSTEM_OS_RELEASE]=[2009]
                [SYSTEM_OS_VERSION]=[6.2.9200.0]
                [SYSTEM_OS_TYPE]=[64-bit]
                [SYSTEM_OS_BUILD]=[19044]
                [SYSTEM_OS_UBR]=[1766]
                [SYSTEM_SCREEN_RESOLUTION]=[1920x1080]
                [SYSTEM_USER_NAME]=[L-PACK\balchugov_dv]
                [SYSTEM_USER_NAME2]=[balchugov_dv]
                [SYSTEM_NETWORK_IP]=[192.168.21.70]
                [SYSTEM_NETWORK_IP_ALL]=[192.168.56.1,192.168.21.70]
                [SYSTEM_HOSTNAME]=[BALCHUGOV-DV]
             */

            var result=new Dictionary<string,string>();

            string HKLMWinNTCurrent = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion";
            string osName = Registry.GetValue(HKLMWinNTCurrent, "productName", "").ToString();
            string osRelease = Registry.GetValue(HKLMWinNTCurrent, "ReleaseId", "").ToString();
            string osVersion = Environment.OSVersion.Version.ToString();
            string osType = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
            string osBuild = Registry.GetValue(HKLMWinNTCurrent, "CurrentBuildNumber", "").ToString();
            string osUBR = Registry.GetValue(HKLMWinNTCurrent, "UBR", "").ToString();

            result.CheckAdd("SYSTEM_OS_NAME",       osName);
            result.CheckAdd("SYSTEM_OS_RELEASE",    osRelease);
            result.CheckAdd("SYSTEM_OS_VERSION",    osVersion);
            result.CheckAdd("SYSTEM_OS_TYPE",       osType);
            result.CheckAdd("SYSTEM_OS_BUILD",      osBuild);
            result.CheckAdd("SYSTEM_OS_UBR",        osUBR);

            string screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth.ToString();
            string screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight.ToString();
            var screenSize=$"{screenWidth}x{screenHeight}";
            result.CheckAdd("SYSTEM_SCREEN_RESOLUTION", screenSize);
            result.CheckAdd("SYSTEM_SCREEN_WIDTH", screenWidth);
            result.CheckAdd("SYSTEM_SCREEN_HEIGHT", screenHeight);

            string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            result.CheckAdd("SYSTEM_USER_NAME", userName);

            string userName2 = Environment.UserName;
            result.CheckAdd("SYSTEM_USER_NAME2", userName2);

            result.CheckAdd("SYSTEM_NETWORK_IP", GetLocalIPAddress());
            result.CheckAdd("SYSTEM_NETWORK_IP_ALL", GetLocalIPAddressAll());
            
            var hostname=System.Environment.MachineName;
            result.CheckAdd("SYSTEM_HOSTNAME", hostname);

            //result.CheckAdd("TODAY", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
            //result.CheckAdd("UPTIME", GetUptime().ToString());
            //result.CheckAdd("VERSION", Version.ToString());

            result.CheckAdd("SYSTEM_HOSTNAME0",   result.CheckGet("SYSTEM_HOSTNAME").ToUpper() );

            {
                var host=result.CheckGet("SYSTEM_HOSTNAME");
                if(!host.IsNullOrEmpty())
                {
                    host=host.ToUpper();
                    if(host.IndexOf("SESSIONHOST") > -1)
                    {
                        host="TERMINAL-SERVER";
                    }
                    result.CheckAdd("SYSTEM_HOSTNAME",host);
                }                    
            }
            
            var key=$"{result.CheckGet("SYSTEM_HOSTNAME")}~{result.CheckGet("SYSTEM_USER_NAME2")}";
            result.CheckAdd("HOST_USER_ID", key.ToString());

            result.CheckAdd("SYSTEM_HOSTNAME",   result.CheckGet("SYSTEM_HOSTNAME").ToUpper() );
            result.CheckAdd("SYSTEM_HOSTNAME0",   result.CheckGet("SYSTEM_HOSTNAME0").ToUpper() );
            result.CheckAdd("SYSTEM_USER_NAME",  result.CheckGet("SYSTEM_USER_NAME").ToUpper() );
            result.CheckAdd("SYSTEM_USER_NAME2", result.CheckGet("SYSTEM_USER_NAME2").ToUpper() );
            result.CheckAdd("HOST_USER_ID",      result.CheckGet("HOST_USER_ID").ToUpper() );

            if(SystemInfo.Count==0)
            {
                SystemInfo = result;
            }

            return result;
        }

        public static string GetLocalIPAddress()
        {
            var result="";
            var host = Dns.GetHostEntry(Dns.GetHostName());
            try
            {
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        result= ip.ToString();
                    }
                }
            }
            catch(Exception e)
            {
            }
            return result;
        }

        public static string GetLocalIPAddressAll()
        {
            var result="";
            var host = Dns.GetHostEntry(Dns.GetHostName());
            try
            {
                 foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        result=result.AddComma();
                        result=result.Append(ip.ToString());
                    }
                }
            }
            catch(Exception e)
            {
            }
            return result;
        }

        public static int GetUptime()
        {
            var result=0;
            TouchUptime();
            result=Uptime;
            return result;
        }

        public static void TouchUptime()
        {
            var today=DateTime.Now;
            var timeDiff=today-StartDate;
            Uptime=timeDiff.TotalSeconds.ToInt();
        }

        public static string GetUptimeString()
        {
            var result="";
            TouchUptime();
            TimeSpan t = TimeSpan.FromSeconds( Uptime );
            result = string.Format(
                "{0:D2}d{1:D2}h{2:D2}m{3:D2}s", 
                t.Days,
                t.Hours, 
                t.Minutes, 
                t.Seconds                
            );
            return result;
        }

        /// <summary>
        /// смена списка серверов на новый список
        /// </summary>
        /// <param name="v"></param>
        public static void ConfigUpdate(string serverList)
        {
            List<string> urlList = new List<string>();
            string[] servers = serverList.Split(',');

            foreach(string server in servers)
            {
                string url = server;
                url=url.TrimEnd().TrimStart();
                url = url.ToLower();
                if(url != string.Empty)
                {
                    urlList.Add(url);
                }
            }

            // если есть сервера для смены
            if(urlList.Count > 0)
            {
                if(Central.ConfigUpdateServerList(urlList))
                {
                    Central.Init(true);
                    Central.LPackClient.ChangeServer();
                }
            }
        }

        internal static bool ConfigUpdateServerList(List<string> serverList)
        {
            bool result = false;

            var pathInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
            string configFile = $"{pathInfo.Directory}\\application.config";


            {
                if (File.Exists(configFile))
                {
                    XmlDocument ConfigData = new XmlDocument();
                    ConfigData.Load(configFile);


                    string bakFile = configFile + ".bak";

                    int bakCount = 0;
                    int maxBack = 3;

                    while (File.Exists(bakFile))
                    {
                        if (++bakCount > maxBack) break;

                        bakFile += ".bak";
                    }

                    XmlNodeList servers = ConfigData.SelectNodes("descendant::ServerAddresses/string");

                    if (servers.Count > 0)
                    {
                        XmlNode Server = servers[0].ParentNode;

                        // Server.RemoveAll();
                        var node = Server.SelectSingleNode("string");
                        while (node != null)
                        {
                            Server.RemoveChild(node);
                            node = Server.SelectSingleNode("string");
                            result = true;
                        }

                        if (result)
                        {
                            result = false;
                            foreach (string url in serverList)
                            {
                                Server.AppendChild(ConfigData.CreateElement("string")).InnerText = url;
                                result = true;
                            }

                            // если подмена произошла, обновим конфиг файл, старый переименуем в бак файл
                            if (result)
                            {
                                result = false;

                                try
                                {
                                    File.Delete(bakFile);
                                    File.Move(configFile, bakFile);
                                    ConfigData.Save(configFile);

                                    result = true;
                                }
                                catch (IOException ee)
                                {

                                }
                            }
                        }

                    }
                }
            }

            return result;
        }

        public static string GetServerLabel()
        {
            var result = "";
            if(User != null)
            {
                var login = User.Login.Replace("_", "__");
                result = $"{login}@{ServerIP}";
            }
            else
            {
                result = $"{ServerIP}";
            }
            return result;
        }

        public static Dictionary<string, string> GetStorageDataByCode(string storageCode)
        {
            Dictionary<string, string> storageData = new Dictionary<string, string>();

            var p = new Dictionary<string, string>();
            p.Add("CODE", storageCode);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "FileStorage");
            q.Request.SetParam("Object", "File");
            q.Request.SetParam("Action", "GetPathByCode");
            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var dataSet = ListDataSet.Create(result, "STORAGE");
                    if (dataSet != null && dataSet.Items != null && dataSet.Items.Count > 0)
                    {
                        storageData = dataSet.Items[0];
                    }
                }
            }

            return storageData;
        }

        public static string GetStorageNetworkPathByCode(string storageCode)
        {
            string storageNetworkPath = "";

            Dictionary<string, string> storageData = Central.GetStorageDataByCode(storageCode);
            if (storageData != null && storageData.Count > 0)
            {
                storageNetworkPath = storageData.CheckGet("NETWORK_PATH");
            }

            return storageNetworkPath;
        }

        /// <summary>
        /// Загрузка файла с сервера при вызове экшена.
        /// data должен содержат ключи Module, Object, Action
        /// Резудьтатом должен быть файл
        /// </summary>
        /// <param name="data"></param>
        public static void GetFileFromAction(Dictionary<string, string> data)
        {
            var q = new LPackClientQuery();
            q.Request.SetParams(data);

            q.DoQuery();
            if (q.Answer.Status == 0)
            {
                Central.OpenFile(q.Answer.DownloadFilePath);
            }
        }
    }
}
