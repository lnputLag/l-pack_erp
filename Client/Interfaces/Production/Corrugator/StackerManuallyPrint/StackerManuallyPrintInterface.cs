using Client.Common;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Интерфейс для ручной печати ярлыков на выходе с гофроагрегата
    /// </summary>
    /// <author>sviridov_ae</author>
    public class StackerManuallyPrintInterface
    {
        public StackerManuallyPrintInterface()
        {
            Central.WM.AddTab("manually_print", "Ручная печать ярлыков");

            ////new
            //var taskQueue2 = Central.WM.CheckAddTab<TaskQueue2>("manually_print_queue2", "Очередь заданий", false, "manually_print");
            //taskQueue2.ProcessNavigation();

            //old
            var taskQueue = Central.WM.CheckAddTab<TaskQueue>("manually_print_queue", "Очередь заданий", false, "manually_print");
            taskQueue.ProcessNavigation();

            var stacker = Central.WM.CheckAddTab<Stacker>("manually_print_stacker", "Стекер", false, "manually_print");
            stacker.ProcessNavigation();

            //отладочный интерфейс
            

            //FIXME: balchugov_dv: ProcessNavigation если контрол получил фокус

            Central.WM.SetActive("manually_print_queue");
        }
    }
}
