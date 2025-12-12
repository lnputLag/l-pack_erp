using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh
{
    internal class CorrugatorErrors
    {
        /// <summary>
        /// Запись информации об ошибке в файл (log file)
        /// </summary>
        /// <param name="ex"></param>
        public static void LogError(Exception ex)
        {
            try
            {
                var location = new FileInfo(Assembly.GetExecutingAssembly().Location);
                var reportDirPath = $"{location.Directory}\\exceptionsKsh";
                Directory.CreateDirectory(reportDirPath);
                var filePath = $"{reportDirPath}\\corrugator.txt";

                var time = DateTime.Now.ToString("D");
                
                File.AppendAllText(filePath,  time + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine);
            }
            catch (Exception)
            {
            }
        }
    }
}
