using System;
using System.IO;
using System.Xml.Serialization;

namespace Client.Common
{
    /// <summary>
    /// Класс(хелпер) для работы с параметрами конфигурации
    ///
    /// Нужно передать структуру, в которую будут считаны параметры
    /// из файла конфигурации
    /// 
    /// пример использования: 
    /// var configLoader = new Config<LPackConfig>();
    /// LPackConfig Config = configLoader.Load();
    /// 
    /// </summary>
    /// <typeparam name="T">Класс описывающий настройки программы</typeparam>
    [Serializable]
    public class Config<T>
    {
        /// <summary>
        /// файл из которого берем настройки программы
        /// </summary>
        [NonSerialized] protected readonly string _fileName;

        public Config() : this("application.config")
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName">имя файла настроек</param>
        public Config(string fileName)
        {
            _fileName = fileName;
        }


        /// <summary>
        /// Сохраняем настройки в файл 
        /// </summary>
        public virtual void Save(T source)
        {
            var formatter = new XmlSerializer(typeof(T));

            using var fs = new FileStream(_fileName, FileMode.OpenOrCreate);
            formatter.Serialize(fs, source);
        }

        /// <summary>
        /// Загружаем настройки из файла и возвращаем прочитанный объект(прочитанные настройки)
        /// </summary>
        public virtual T Load()
        {
            var formatter = new XmlSerializer(typeof(T));

            using var fs = new FileStream(_fileName, FileMode.OpenOrCreate);
            return (T)formatter.Deserialize(fs);
        }
    }
}
