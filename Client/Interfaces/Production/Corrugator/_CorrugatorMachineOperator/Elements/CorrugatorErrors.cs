using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperator
{
    internal class CorrugatorErrors
    {
        /// <summary>
        /// Запись информации об ошибке в файл
        /// </summary>
        /// <param name="ex"></param>
        public static void LogError(Exception ex)
        {
            try
            {
                var location = new FileInfo(Assembly.GetExecutingAssembly().Location);
                var reportDirPath = $"{location.Directory}\\exceptions";
                Directory.CreateDirectory(reportDirPath);
                var filePath = $"{reportDirPath}\\corrugator.txt";

                File.AppendAllText(filePath, ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine);
            }
            catch (Exception)
            {
            }
        }
    }
}
