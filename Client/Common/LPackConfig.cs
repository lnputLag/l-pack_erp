using System.Collections.Generic;
using System.Xml.Serialization;

namespace Client.Common
{
    /// <summary>
    /// Структура данных конфигурации
    /// </summary>
    [XmlRoot("Config")]
    public class LPackConfig
    {
        /// <summary>
        /// Строка соединения с сервером
        /// </summary>
        public string ConnectionString;

        /// <summary>
        /// Список адресов серверов
        /// </summary>
        public List<string> ServerAddresses = new List<string>();

        /// <summary>
        /// логин пользователя
        /// </summary>
        public string Login;

        /// <summary>
        /// пароль пользователя
        /// </summary>
        public string Password;

        /// <summary>
        /// автоматический логин с указанными в конфиг-файле пераметрами
        /// </summary>
        public bool AutoLogin;

        /// <summary>
        /// отключить развертывание программы на полный экран при старте
        /// (для разработчиков)
        /// </summary>
        public bool NoFullScreen;

        public int WindowWidth;
        public int WindowHeight;
        public bool SingleInterfaceMode;
        public bool FullScreenMode;
        public bool DeveloperMode;
        public bool DebugMode;

        /// <summary>
        /// Список адресов интерфейсов для автозагрузки
        /// </summary>
        public List<string> AutoloadInterfaces = new List<string>();

        /// <summary>
        /// Папка для загрузки файлов по умолчанию
        /// </summary>
        public string DownloadFolder;

        /// <summary>
        /// Идентификатор станка (stanok.id_st)
        /// Используется для определения станка, на котором запущена программа
        /// </summary>
        public int CurrentMachineId;

        /// <summary>
        /// Идентификатор стекера (ppz.cutoff_allocation):
        /// 1 -- Нижний стекер;
        /// 2 -- Верхний стекер;
        /// 3 -- Фенфолд.
        /// Используется для определения стекера, на котором запущена программа.
        /// </summary>
        public int CurentCutoffAllocation;

        /// <summary>
        /// Временный параметр для тестирования работы стекера только на выбранной машине
        /// </summary>
        public int TempStackerFlag;

        /// <summary>
        /// Флаг использования нового механизма печати
        /// </summary>
        public int UseNewPrintingFlag;

        /// <summary>
        /// Флаг использования поподдонной печати ярлыков на стекере ГА по данным о съёме
        /// </summary>
        public int PrintPalletByStackerDataFlag;

        /// <summary>
        /// Флаг того, что, при использовании поподдонной печати ярлыков, для поддонов с листовой продукцией ярлыки нужно всё равно печатать по старому механизму 
        /// </summary>
        public int PrintGoodsPalletByOldAlgoritm;

        /// <summary>
        /// Флаг того, что при создании поддонов на стекере номера поддонов пытаемся получить через диапазон свободных номеров.
        /// Если > 0 -- Рассчитываем диапазон свободных номеров поддонов для раждого стекера, создаём поддоны с заранее определённым номером поддона;
        /// Если == 0 -- Номер поддона до создания записи по поддону не известен, получаем следующий номер поддона при создании поддона.
        /// </summary>
        public int PrintPalletNumberByRange;

        /// <summary>
        /// Количество гридов, расположенных в один ряд по ширине для интерфейса Монитор матера
        /// </summary>
        public int PilloryGridCountByWidth;

        /// <summary>
        /// Количество гридов, расположенных в один ряд по высоте для интерфейса Монитор матера
        /// </summary>
        public int PilloryGridCountByHeight;

        /// <summary>
        /// Флаг того, что контролл обработки данных со сканера должен ставить автоматически фокус в поле ввода данных штрихкода.
        /// Используется для экстренных случаев.
        /// Если > 0 -- true
        /// Если == 0 -- false
        /// </summary>
        public int ScannerInputAutoFocus;


        public int KeyboardInputBufferClearTimeout;

        /// <summary>
        /// ИД площадки: 1=Липецк,2=Кашира
        /// </summary>
        public int FactoryId;

        /// <summary>
        /// Список имён портов
        /// </summary>
        public List<Port> Ports = new List<Port>();
        public class Port
        {
            public string PortName { get; set; }
            public string BaudRate { get; set; }
            public string Parity { get; set; }
            public string DataBits { get; set; }
            public string StopBits { get; set; }
            public string ReadTimeout { get; set; }
        }
    }

}

