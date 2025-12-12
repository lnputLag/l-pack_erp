
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Client.Common
{
    /// <summary>
    /// упрощенная версия библиотеки filemanager
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>2</version>
    /// <released>2022-08-22</released>
    /// <changed>2019-11-01</changed>
    public class FileManager
    {
        public static string GetTempFilePathWithExtension(string extension)
        {
            var path = Path.GetTempPath();
            var fileName = Guid.NewGuid().ToString() + "." + extension;
            return Path.Combine(path, fileName);
        }

        public static string FilenameAddPrefix(string filePath, string prefix)
        {
            string folder=Path.GetDirectoryName(filePath);
            string baseName = Path.GetFileNameWithoutExtension(filePath);
            string ext = Path.GetExtension(filePath);
            filePath=$"{folder}\\{prefix}_{baseName}{ext}";
            return filePath;
        }

        public static string FilenameAddSuffix(string filePath, string suffix)
        {
            string folder=Path.GetDirectoryName(filePath);
            string baseName = Path.GetFileNameWithoutExtension(filePath);
            string ext = Path.GetExtension(filePath);
            filePath=$"{folder}\\{baseName}_{suffix}{ext}";
            return filePath;
        }

    }
}
