using Client.Interfaces.Main;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Input;
using System.Xml;
using static Client.Common.Logger;

namespace Client.Common
{
    /// <summary>
    /// библиотека получения обновлений
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1.3</version>
    /// <released>2023-02-15</released>
    /// <changed>2019-11-19</changed>
    public class Updater2
    {
        public Updater2(string url)
        {
            Name="Updater";    
            RepoUrl = url;
            RepoInfoUrl = "/repo/l-pack_erp/vers.xml";            
            RepoRoot="";
            MessagesTitle = $"Обновление {RepoUrl}";
            Errors = "";
            Autocheck = false;
            Description = "";
            UpdateEnabled=true;
            FileExeSelf="l-pack_erp.exe";
            FileExeBackup="_l-pack_erp.exe";
            Log="";
            RemoveBackupFile();
        }

        public String Name { get; set; }
        public string RepoUrl { get; set; }
        public string RepoInfoUrl { get; set; }
        public XmlDocument Xml { get; set; }
        /// <summary>
        /// заголовок для диалоговых окон
        /// </summary>
        public string MessagesTitle { get; set; }

        public Version RepoVersion { get; set; }
        public int FilesToUpdate { get; set; }
        public string RepoRoot { get; set; }
        public Dictionary<string, string> Files { get; set; }
        public string Errors { get; set; }
        public bool Autocheck { get; set; }
        public string Description { get; set; }
        public bool UpdateEnabled { get; set; }
        public string FileExeSelf { get; set; }
        public string FileExeBackup { get; set; }
        public string Log{get;set;}

        

        /// <summary>
        /// выполняется проверка обновлений и обновление, если существует новая версия
        /// </summary>
        /// <param name="autocheck">режим автоматической проверки при старте, не выводятся сообщения типа [У вас последняя версия]</param>
        /// <param name="force">сделать обновление принудительно (даже если контроль версии не прошел)</param>
        public void CheckUpdate(bool autocheck = false, bool force = false)
        {
            LogMsg($"CheckUpdate");                

            Autocheck = autocheck;
            bool parsingRepoResult = ParseRepoInfo();

            if(UpdateEnabled)
            {
                if (parsingRepoResult)
                {

                    Version curVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                    Central.ProgramVersion=curVersion.ToString();

                    //if (curVersion.CompareTo(RepoVersion) < 0 || force)
                    if (curVersion.CompareTo(RepoVersion) < 0 )
                    {
                        //Central.SplashUpdate(1, $"Новая версия: {RepoVersion.ToString()}");

                        if (autocheck != true)
                        {
                            string message = "";
                            message += $"Доступна новая версия программы: {RepoVersion}.";
                            message += $"\nВы используете старую версию: {curVersion}.";
                            message += $"\n";
                            message += $"\nВыполнить обновление сейчас?";
                            message += $"\nНажмите Да для начала обновления, после обновления программа перезапустится.";
                            var e = new DialogWindow(message, MessagesTitle, "", DialogWindowButtons.YesNo);
                            var result = e.ShowDialog();
                            if (result == true)
                            {
                                MakeUpdate();
                            }
                        }
                        else
                        {
                            MakeUpdate();
                        }
                    }
                    else
                    {
                        if (autocheck != true)
                        {
                            string message = "";
                            message += $"Вы используете самую новую версию: {RepoVersion}.";
                            var e = new DialogWindow(message, MessagesTitle, "", DialogWindowButtons.OK);
                            e.ShowDialog();
                        }
                    }

                }
                else
                {
                    if (autocheck != true)
                    {
                        string message = "";
                        message += $"Не удалось получить информацию об обновлениях.";
                        message += $"\nВозможно, сервер временно недоступен.";
                        message += $"\nПожалуйста, попробуйте выполнить обновление позже.";
                        string description = $"";
                        description += $"\n{Central.ReportTo}";
                        description += $"\n";
                        description += $"\nДополнительная информация.";
                        description += $"\nАдрес сервера: {RepoInfoUrl}";
                        var e3 = new DialogWindow(message, MessagesTitle, description);
                        e3.ShowDialog();
                    }
                }

            }

        }

        public void MakeUpdate()
        {
            LogMsg($"MakeUpdate");
            if (Xml != null)
            {
                var nodes = Xml.DocumentElement.SelectNodes("/item/files/*");

                int j = 0;
                int fileIndex = 0;
                foreach (XmlNode node in nodes)
                {
                    string k = node.Name.ToLower();
                    string v = node.InnerText.Trim();

                    LogMsg($"    ({j}) [{k}]=[{v}]");

                    switch (k)
                    {
                        case "update":
                            if (!string.IsNullOrEmpty(v))
                            {
                                fileIndex++;
                                var url = $"{v}";
                                var fileUpdateResult = UrlUpdateFile(url, fileIndex, FilesToUpdate);
                                if (fileUpdateResult)
                                {
                                    j++;
                                }
                            }
                            break;

                        case "updateconfigserveraddresses":

                            if (!string.IsNullOrEmpty(v))
                            {
                                var version = "";
                                var doCfgUpdate = false;
                                if (node.Attributes["maxVersion"] != null)
                                {
                                    version = node.Attributes["maxVersion"].Value;
                                }


                                if (!string.IsNullOrEmpty(version))
                                {
                                    Version maxVersion = new Version(version);
                                    Version curVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                                    if (curVersion.CompareTo(maxVersion) <= 0)
                                    {
                                        doCfgUpdate = true;
                                    }
                                }

                                if (doCfgUpdate)
                                {
                                    //DialogWindow.Msg($"mv={version}");

                                    var url = $"{v}";
                                    var configUpdateResult = UrlUpdateConfig(url);
                                    if (configUpdateResult)
                                    {
                                        j++;
                                    }
                                }
                                else
                                {
                                    j++;
                                }

                            }
                            break;
                    }
                }

                LogMsg($"    FilesToUpdate=[{FilesToUpdate}] updated=[{j}]");

                if (j >= FilesToUpdate)
                {
                    LogMsg($"    update complete");
                    LogSave();
                    Restart();
                }
                else
                {
                    //if( Autocheck != true )
                    //{
                    string message = "";
                    message += $"Обновление прошло неуспешно.";
                    string description = $"";
                    description += $"\n{Central.ReportTo}";
                    description += $"\n";
                    description += $"\nДополнительная информация.";
                    description += $"\nАдрес сервера: {RepoInfoUrl}";
                    description += $"\nF:{FilesToUpdate} J:{j}";
                    description += $"\n{Errors}";

                    var e3 = new DialogWindow(message, MessagesTitle, description);
                    e3.ShowDialog();
                    //}
                }

            }
        }

        public bool ParseRepoInfo()
        {
            bool result = false;
            var rnd=Cryptor.MakeRandom();
            var url = $"{RepoUrl}{RepoInfoUrl}?id={rnd}";

            LogMsg($"ParseRepoInfo url=[{url}]");

            if (!string.IsNullOrEmpty(url))
            {
                var repoInfo = UrlGetContents(url);

                if (!string.IsNullOrEmpty(repoInfo))
                {
                    Xml = new XmlDocument();
                    Xml.LoadXml(repoInfo);

                    if (Xml != null)
                    {
                        /*
                            <?xml version="1.0" encoding="UTF-8"?>
                            <item>
                                <mechanics>4</mechanics>
                                <version>11.4.7.415</version>
                                <url>http://192.168.3.204/repo/l-pack_erp/updates/update.zip</url>
                                <mandatory>false</mandatory>
                                <updateFiles>51</updateFiles>
                                <description></description>
                                <files>
                                    <update>/repo/l-pack_erp/updates/files/l-pack_erp.exe</update>
                                </files>
                            </item>

                         */

                        var nodes = Xml.DocumentElement.SelectNodes("/item/*");

                        foreach (XmlNode Node in nodes)
                        {
                            string k = Node.Name.ToLower();
                            string v = Node.InnerText.Trim();

                            switch (k)
                            {
                                case "version":
                                    RepoVersion = new Version(v);
                                    break;

                                case "enabled":
                                    UpdateEnabled = v.ToString().ToBool();
                                    break;

                                case "updatefiles":
                                    FilesToUpdate = v.ToInt();
                                    break;

                                case "description":
                                    Description = v;
                                    break;

                                case "reporoot":
                                    RepoRoot = v;
                                    break;
                            }
                        }

                        if (!string.IsNullOrEmpty(RepoVersion.ToString()))
                        {
                            result = true;
                            //DialogWindow.Msg($"->{RepoVersion.ToString()}");
                        }
                    }

                }
            }

            LogMsg($"    RepoVersion=[{RepoVersion}]");
            LogMsg($"    UpdateEnabled=[{UpdateEnabled}]");
            LogMsg($"    FilesToUpdate=[{FilesToUpdate}]");
            LogMsg($"    Description=[{Description}]");
            LogMsg($"    RepoRoot=[{RepoRoot}]");

            return result;
        }

        public string UrlGetContents(string url)
        {
            string result = "";
            var webRequest = WebRequest.Create($"{url}");

            try
            {
                using (var response = webRequest.GetResponse())
                using (var content = response.GetResponseStream())
                using (var reader = new StreamReader(content))
                {
                    result = reader.ReadToEnd();
                }
            }
            catch (WebException e)
            {
                Errors += $"UrlGetContents: Exception: {e}";
            }

            return result;
        }

        /*
            обновление файлов происходит по следующему принципу:
                fileUrl -- загружемый файл (далее file)
                Если а локальной папке существует файл _file, он удаляется.
                Если в локальной папке существует файл file, он переименовывается: file -> _file
                Загружается файл с сервера.
                Если загружен успешно, _file удаляется.
                Если неуспешно, переименовывается назад: _file -> file
         */
        public bool UrlUpdateFile(string fileUrl, int currentFileNumber = 0, int totalFiles = 0)
        {
            // http://192.168.3.204    /repo/l-pack_erp_agent/updates/files/x86/test.txt
            var remoteFileUrl = $"{RepoUrl}{fileUrl}";

            var remoteInfo = new FileInfo(fileUrl);
            
            // test.txt
            //var file = remoteInfo.Name;
            var file="";

            if(!RepoRoot.IsNullOrEmpty())
            {
                // /repo/l-pack_erp_agent/updates/files/x86/test.txt
                // /repo/l-pack_erp_agent/updates/files/
                // x86/test.txt
                file=fileUrl;
                file=file.Replace(RepoRoot,"");
            }
            else
            {
                file = remoteInfo.Name;
            }

            var localInfo = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var localFilePath = $"{localInfo.Directory}\\{file}";
            //var localBackupPath = $"{localInfo.Directory}\\_{file}";
            var localBackupPath=FileManager.FilenameAddPrefix(localFilePath,"");

            localFilePath=localFilePath.Replace('/','\\');
            localBackupPath=localBackupPath.Replace('/','\\');

            LogMsg($"        file=[{file}]");
            LogMsg($"        remoteFileUrl=[{remoteFileUrl}]");
            LogMsg($"        localFilePath=[{localFilePath}]");
            LogMsg($"        localBackupPath=[{localBackupPath}]");

            bool resume = true;

            if (File.Exists(localBackupPath))
            {
                try
                {
                    LogMsg($"            delete backup");
                    File.Delete(localBackupPath);
                }
                catch (Exception e)
                {
                    resume = false;
                    LogMsg($"ERROR can not delete backup");
                    LogMsg($"{e.ToString()}");
                }
            }

            if (resume)
            {
                if (File.Exists($"{localFilePath}"))
                {
                    try
                    {
                        LogMsg($"            move [{localFilePath}] -> [{localBackupPath}]");
                        File.Move(localFilePath, localBackupPath);
                    }
                    catch (Exception e)
                    {
                        resume = false;
                        LogMsg($"ERROR can not move [{localFilePath}] -> [{localBackupPath}]");
                        LogMsg($"{e.ToString()}");
                    }
                }
            }

            if (resume)
            {
                 string folder=Path.GetDirectoryName(localFilePath);
                 LogMsg($"    check folder=[{folder}]");
                 if(!Directory.Exists(folder))
                 {
                    try
                    {
                        LogMsg($"            mkdir [{folder}]");
                        Directory.CreateDirectory(folder);
                    }
                    catch (Exception e)
                    {
                        resume = false;
                        LogMsg($"ERROR can not mkdir [{folder}]");
                        LogMsg($"{e.ToString()}");
                    }
                 }
            }

            if (resume)
            {
                var webClient = new WebClient();
                var downloadComplete = false;
                var downloadSuccess = false;
                Exception downloadException = null;

                webClient.DownloadProgressChanged += (sender, e) =>
                {
                    if (Central.SplashInterface != null)
                    {
                        var fileName = Path.GetFileName(file);
                        Central.SplashInterface.UpdateProgress(e.ProgressPercentage, fileName, currentFileNumber, totalFiles);
                    }
                };

                webClient.DownloadFileCompleted += (sender, e) =>
                {
                    downloadComplete = true;
                    if (e.Error != null)
                    {
                        downloadException = e.Error;
                        downloadSuccess = false;
                    }
                    else if (e.Cancelled)
                    {
                        downloadSuccess = false;
                    }
                    else
                    {
                        downloadSuccess = true;
                    }
                };

                try
                {
                    LogMsg($"            download [{remoteFileUrl}] -> [{localFilePath}]");
                    
                    if (Central.SplashInterface != null)
                    {
                        Central.SplashInterface.ShowProgress();
                    }

                    webClient.DownloadFileAsync(new Uri(remoteFileUrl), localFilePath);
                    
                    while (!downloadComplete)
                    {
                        Thread.Sleep(50);
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                            System.Windows.Threading.DispatcherPriority.Background,
                            new Action(delegate { })
                        );
                    }

                    if (!downloadSuccess)
                    {
                        resume = false;
                        LogMsg($"ERROR can not download [{remoteFileUrl}]");
                        if (downloadException != null)
                        {
                            LogMsg($"{downloadException.ToString()}");
                        }
                    }
                    
                    if (Central.SplashInterface != null)
                    {
                        Central.SplashInterface.HideProgress();
                    }
                }
                catch (Exception e)
                {
                    resume = false;
                    LogMsg($"ERROR can not download [{remoteFileUrl}]");
                    LogMsg($"{e.ToString()}");
                    
                    if (Central.SplashInterface != null)
                    {
                        Central.SplashInterface.HideProgress();
                    }
                }
                finally
                {
                    webClient.Dispose();
                }
            }

            if (!resume)
            {
                if (File.Exists($"{localBackupPath}"))
                {
                    try
                    {
                        LogMsg($"            move [{localBackupPath}] -> [{localFilePath}]");
                        File.Move(localBackupPath, localFilePath);
                    }
                    catch (Exception e)
                    {
                        LogMsg($"ERROR can not move [{localBackupPath}] -> [{localFilePath}]");
                        LogMsg($"{e.ToString()}");
                    }
                }
            }

            LogMsg($"        result=[{resume}]");

            return resume;
        }

        public bool UrlUpdateConfig(string fileUrl)
        {
            string fileContent = "";

            var remoteFileUrl = $"{RepoUrl}{fileUrl}";

            var localInfo = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var tempConfigPath = $"{localInfo.Directory}\\_temp.config";

            var nativeConfigPath = $"{localInfo.Directory}\\application.config";
            var bkpConfigPath = $"{localInfo.Directory}\\_application.config";

            bool resume = true;

            //remove bkp
            if (File.Exists(bkpConfigPath))
            {
                try
                {
                    File.Delete(bkpConfigPath);
                }
                catch (Exception e)
                {
                    resume = false;
                    Errors += $"\nCant remove [{bkpConfigPath}] (0)";
                    Errors += $"\n    {e}";
                }
            }

            //check contents
            if (resume)
            {
                var webRequest = WebRequest.Create(remoteFileUrl);

                try
                {
                    using (var response = webRequest.GetResponse())
                    using (var content = response.GetResponseStream())
                    using (var reader = new StreamReader(content))
                    {
                        fileContent = reader.ReadToEnd();
                    }
                }
                catch (WebException e)
                {
                    Errors += $"UrlGetContents: Exception: {e} (1)";
                    resume = false;
                }
            }

            if (string.IsNullOrEmpty(fileContent))
            {
                resume = false;
            }


            //backup native
            if (resume)
            {
                if (File.Exists($"{nativeConfigPath}"))
                {
                    try
                    {
                        File.Copy(nativeConfigPath, bkpConfigPath);
                    }
                    catch (Exception e)
                    {
                        resume = false;
                        Errors += $"\nCant move [{nativeConfigPath}] -> [{bkpConfigPath}] (2)";
                        Errors += $"\n    {e}";
                    }
                }
            }


            //download remote
            if (resume)
            {
                var webClient = new WebClient();
                try
                {
                    webClient.DownloadFile(remoteFileUrl, tempConfigPath);
                }
                catch (WebException e)
                {
                    resume = false;
                    Errors += $"\nCant download [{remoteFileUrl}] (3)";
                    Errors += $"\n    {e}";
                }
            }


            //read remote to struct
            LPackConfig tempConfig = null;
            if (resume)
            {
                try
                {
                    var tempConfigLoader = new Config<LPackConfig>(tempConfigPath);
                    tempConfig = tempConfigLoader.Load();
                }
                catch (Exception e)
                {
                    resume = false;
                    Errors += $"\nCant load temp config (4)";
                    Errors += $"\n    {e}";
                }
            }


            //read native to struct
            var nativeConfigLoader = new Config<LPackConfig>(nativeConfigPath);
            LPackConfig nativeConfig = null;
            if (resume)
            {
                try
                {
                    nativeConfig = nativeConfigLoader.Load();
                }
                catch (Exception e)
                {
                    resume = false;
                    Errors += $"\nCant load native config (5)";
                    Errors += $"\n    {e}";
                }
            }

            //remove native
            if (resume)
            {
                if (File.Exists(nativeConfigPath))
                {
                    try
                    {
                        //System.IO.File.Delete(nativeConfigPath);
                        FileStream fileStream = new FileStream(nativeConfigPath, FileMode.Truncate, FileAccess.Write);
                        fileStream.Close();
                    }
                    catch (Exception e)
                    {
                        resume = false;
                        Errors += $"\nCant remove native [{nativeConfigPath}] (6)";
                        Errors += $"\n    {e}";
                    }
                }
            }

            //patch and save patched
            if (resume)
            {
                nativeConfig.ServerAddresses.Clear();
                foreach (string s in tempConfig.ServerAddresses)
                {
                    nativeConfig.ServerAddresses.Add(s);
                }
                nativeConfigLoader.Save(nativeConfig);
            }

            //restore native
            if (!resume)
            {
                if (File.Exists($"{bkpConfigPath}"))
                {
                    try
                    {
                        System.IO.File.Move(bkpConfigPath, nativeConfigPath);
                    }
                    catch (Exception e)
                    {
                        Errors += $"\nCant move [{bkpConfigPath}] -> [{nativeConfigPath}] (7)";
                        Errors += $"\n    {e}";
                    }
                }
            }

            return resume;
        }


        public void RemoveBackupFile()
        {
            LogMsg($"RemoveBackupFile");
            var pathInfo = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var backupFile = Central.BackupExe;
            var f=$"{pathInfo.Directory}/{backupFile}";
            LogMsg($"    file=[{f}]");
            if (File.Exists(f))
            {
                LogMsg($"    exists, deleting");
                File.Delete(f);
            }
        }

        public void Restart()
        {
            if (!string.IsNullOrEmpty(Description))
            {
                string message = "";
                message += $"Обновление завершено успешно.";
                message += $"\n";
                message += $"\nПримечания к новой версии.";
                message += $"\n{Description}";

                string description = $"";
                description += $"\nПрограмма будет перезапущена.";
                var d = new DialogWindow(message, MessagesTitle, description);
                d.ShowDialog();
            }
            
            LogMsg($"Restart");

            var pathInfo = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var selfFile = FileExeSelf;
            var f=$"{pathInfo.Directory}/{selfFile}";
            LogMsg($"    file=[{f}]");

            Process process = new Process();            
            process.StartInfo.FileName = f;
            process.Start();

            Process.GetCurrentProcess().Kill();
        }

        public void LogMsg(string message)
        {
            Central.Logger.Message(message,LoggerMessageTypeRef.Debug);
            
            var today=DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            message=$"\n\r{today} {message}";       
            Log=Log.Append(message);

              Errors += $"\n    {message}";
        }
        
        public void LogSave()
        {
            if(!Log.IsNullOrEmpty())
            {
                try
                {
                    var fileName="update.log";
                    var fileName1="update_1.log";
                    var fileName2="update_2.log";

                    if (File.Exists(fileName2))
                    {
                        File.Delete(fileName2);
                    }

                    if (File.Exists(fileName1))
                    {
                        System.IO.File.Move(fileName1,fileName2);
                    }

                    if (File.Exists(fileName))
                    {
                        System.IO.File.Move(fileName,fileName1);
                    }

                    if (File.Exists(fileName))
                    {
                        File.Delete(fileName);
                    }

                    System.IO.File.WriteAllText(fileName, Log);
                }
                catch(Exception e)
                {
                }
            }
        }
    }
}
