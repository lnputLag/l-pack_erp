using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Client.Common
{
    /// <summary>
    /// реестр параметров
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public class Parameters
    {
        public Parameters()
        {
            ModulesGridAutoRefresh = true;
            ModulesGridShowRefresh = false;

            GlobalDebugOutput = false;
            GlobalLogging=false;
            UseRequestLog=false;

            GlobalDebugMemoryDiag = true;

            DateRegex = new Regex(@"^(?:(?:31(\.)(?:0?[13578]|1[02]))\1|(?:(?:29|30)(\.)(?:0?[1,3-9]|1[0-2])\2))(?:(?:1[6-9]|[2-9]\d)?\d{2})$|^(?:29(\.)0?2\3(?:(?:(?:1[6-9]|[2-9]\d)?(?:0[48]|[2468][048]|[13579][26])|(?:(?:16|[2468][048]|[3579][26])00))))$|^(?:0?[1-9]|1\d|2[0-8])(\.)(?:(?:0?[1-9])|(?:1[0-2]))\4(?:(?:1[6-9]|[2-9]\d)?\d{4})$");
            DateFormat="dd.MM.yyyy";

            SystemName = "L-PACK ERP";
            BaseLabel = "";

            StorageServerPath = new Dictionary<string, string>()
            {
                { "Order.Proxy",        "\\\\192.168.3.243\\external_services$\\Order\\Proxy\\" },
                { "DeliveryAddress",    "\\\\192.168.3.243\\external_services$\\DeliveryAddress\\" },
            };

            //различные внутренние события
            NotificationsWarningInterval = 60;
            KeyboardInputBufferClearTimeout = 100;

            {
                RequestTimeoutMin = 1000;
                RequestTimeoutDefault = 30000;
                RequestTimeoutMax = 60000;
                RequestTimeoutSystem = 5000;
                RequestAttemptsDefault = 1;

                HopMode = 0;
                HopControlIntervalSlow = 2000;
                HopControlIntervalFast = 1000;
                
                RequestAttemptsFixMode = 0;
                DoSystemRequestFaultLimit = 5;
                QueryRepeatLimitTime = 120000;
                QueryRepeatDelay = 1000;
                WaitRepeatLimitTime = 20000;
                WaitRepeatDelay = 300;

                _HopWaitTimeout = 300;
            }

            StatusBarUpdateInterval = 15;
            PollInterval = 10;
            RequestGridTimeout = 10000;
            ProgressGridDelay = 5000;
            HopControlInterval = 0;
            RequestGridAttempts = 1;
        }

        public int RequestGridAttempts { get; set; }

        /// <summary>
        /// автообновление гридов по таймеру (иногда для отладки нужно отключить все "пинги")
        /// </summary>
        public bool ModulesGridAutoRefresh { get; set; }

        /// <summary>
        /// показать кнопку обновить у гридов в отладочном режиме
        /// </summary>
        public bool ModulesGridShowRefresh { get; set; }

        /// <summary>
        /// вывод отладочных сообщений в консоль VisualStudio
        /// </summary>
        public bool GlobalDebugOutput { get; set; }

        /// <summary>
        /// вывод отладочных сообщений в журнал
        /// </summary>
        public bool GlobalLogging { get; set; }

        /// <summary>
        /// ведение журнала запросов
        /// </summary>
        public bool UseRequestLog { get; set; }

        /// <summary>
        /// обновление информации о сервере и использованной памяти
        /// </summary>
        public bool GlobalDebugMemoryDiag { get; set; }

        /// <summary>
        /// регулярное выражение для проверки правильности формата и данных даты: DD.MM.YYYY        
        /// </summary>
        public Regex DateRegex { get; set; }

        /// <summary>
        /// стандартный формат даты
        /// </summary>
        public string DateFormat { get; set; }

        public string SystemName { get; set; }

        public string BaseLabel { get; set; }

        public Dictionary<string, string> StorageServerPath { get; set; }


       
        /// <summary>
        /// интервал акцента внимания на окне уведомлений
        /// проверяются важные уведомления, если они есть, показывается окно уведомлений
        /// </summary>
        public int NotificationsWarningInterval { get; set; }

        /// <summary>
        /// таймаут очистки буфера паттернов ввода, миллисекунды
        /// </summary>
        public int KeyboardInputBufferClearTimeout { get; set; }

        /// <summary>
        /// задержка перед отображением автоматического прогресс-бара в гриде, мс (5000)
        /// </summary>
        public int ProgressGridDelay { get; set; }


        /// <summary>
        /// интервал обновления данных статус-бара, сек.
        /// обновляется сервером в PollUser
        /// </summary>
        public int StatusBarUpdateInterval { get; set; }

        /// <summary>
        /// интервал отправки телеметрии, сек.
        /// обновляется сервером в PollUser
        /// </summary>
        public int PollInterval { get; set; }

        /// <summary>
        /// таймаут стандартного запроса обновления грида, мс
        /// обновляется сервером в PollUser 
        /// </summary>
        public int RequestGridTimeout { get; set; }

        /// <summary>
        /// число попыток повторения стандартного запроса обновления грида, шт.
        /// обновляется сервером в PollUser
        /// </summary>
        public int RequestAttemptsDefault { get; set; }

        /// <summary>
        /// таймаут стандартного минимального запроса, мс.
        /// обновляется сервером в PollUser
        /// </summary>
        public int RequestTimeoutMin { get; set; }

        /// <summary>
        /// таймаут стандартного запроса, мс
        /// обновляется сервером в PollUser
        /// </summary>
        public int RequestTimeoutDefault { get; set; }

        /// <summary>
        /// таймаут системного запроса, мс
        /// обновляется сервером в PollUser
        /// </summary>
        public int RequestTimeoutSystem { get; set; }

        /// <summary>
        /// таймаут специального запроса: обновление грида автораскроя, мс
        /// обновляется сервером в PollUser
        /// </summary>
        public int RequestTimeoutMax { get; set; }

        /// <summary>
        /// обновляется сервером в PollUser
        /// 2=глобальный хоп с циклом ожидания
        /// </summary>
        public int HopMode { get; set; }

        /// <summary>
        /// обновляется сервером в PollUser
        /// </summary>
        public int HopControlIntervalSlow { get; set; }

        /// <summary>
        /// обновляется сервером в PollUser
        /// </summary>
        public int HopControlIntervalFast { get; set; }

        /// <summary>
        /// обновляется сервером в PollUser
        /// </summary>
        public int _HopWaitTimeout { get; set; }


        public int HopControlInterval { get; set; }

        public int RequestAttemptsFixMode { get; set; }
        public int DoSystemRequestFaultLimit { get; set; }
        public int QueryRepeatLimitTime { get; set; }
        public int QueryRepeatDelay { get; set; }
        public int WaitRepeatLimitTime { get; set; }
        public int WaitRepeatDelay { get; set; }
    }
}
