using Client.Common;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Интерфейс для ручной печати ярлыков на выходе с гофроагрегата
    /// </summary>
    /// <author>sviridov_ae</author>
    public class StackerManuallyPrintKshInterface
    {
        public StackerManuallyPrintKshInterface()
        {
            Central.WM.AddTab("manually_print_ksh", "Ручная печать ярлыков КШ");

            ////new
            //var taskQueue2 = Central.WM.CheckAddTab<TaskQueue2>("manually_print_queue2", "Очередь заданий", false, "manually_print");
            //taskQueue2.ProcessNavigation();

            //old
            var taskQueue = Central.WM.CheckAddTab<TaskQueueKsh>("manually_print_queue_ksh", "Очередь ПЗ КШ", false, "manually_print_ksh");
            taskQueue.ProcessNavigation();

            var stacker = Central.WM.CheckAddTab<StackerKsh>("manually_print_stacker_ksh", "Стекер КШ", false, "manually_print_ksh");
            stacker.ProcessNavigation();

            //отладочный интерфейс
            

            //FIXME: balchugov_dv: ProcessNavigation если контрол получил фокус

            Central.WM.SetActive("manually_print_queue_ksh");
        }
    }
}
