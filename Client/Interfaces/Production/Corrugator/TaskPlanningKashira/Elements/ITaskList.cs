using Client.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client.Interfaces.Production.Corrugator.TaskPlanningKashira
{
    /// <summary>
    /// Интерфейс позволяет основному окну управлять USerControl с любым гридом 
    /// реализовав данный интерфейс можно поставить грид для управления гофроагрекатом в основное окно
    /// сделано для тестирования GridBox и GridBox4 
    /// <author>eletskikh_ya</author>
    /// <refactor>volkov_as</refactor>
    /// </summary>
    public interface ITaskList
    {
        /// <summary>
        /// ИД текущего станка
        /// 23
        /// либо 0, если задание еще не на га
        /// </summary>

        TaskPlaningDataSet.TypeStanok CurrentMachineId { get; set; }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadItems();
        /// <summary>
        /// Обновить задания
        /// </summary>
        public void UpdateTask();

        /// <summary>
        /// Получить выделенные задания
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Dictionary<string, string>> GetSelectedItems();

        /// <summary>
        /// Действия необходимые для освобожения ресурсов 
        /// </summary>
        public void Destruct();

        /// <summary>
        /// Сброс выделенных заданий
        /// </summary>
        public void ClearAllCheckBoxes();

        /// <summary>
        /// Движение выделеных заданий 
        /// </summary>
        /// <param name="direction">up, down</param>
        public void MoveTasks(string direction);

        /// <summary>
        /// Установка фокуса на последнем задании в гриде
        /// </summary>
        public void SetSelectToLastRow();

        /// <summary>
        /// Сохранение текущих заданий
        /// </summary>
        public Task<LPackClientQuery> SaveQueue();

        /// <summary>
        /// Вызов функции при установки фокуса на гриде
        /// </summary>
        /// <param name="func"></param>
        public void SetActivate(Action<TaskPlaningDataSet.TypeStanok, Dictionary<string, string>> func);

        public void SetActive(bool active);

        /// <summary>
        /// блокировка редактирования
        /// </summary>
        /// <param name="enable"></param>
        void EnableEdit(bool enable);
        void Delete();

        public double GetHours();
        
        /// <summary>
        /// Вызывается при обновлении данных
        /// </summary>
        /// <param name="line"></param>
        /// <param name="key"></param>
        void UpdateLineAsync(TaskPlaningDataSet.TypeStanok typeStanok, string key);
        void CurrentItemSelect();
        void UpdateDownTime(int dopl_id, DateTime start, DateTime end);

        void MoveTasksInToCurrentPossition(IEnumerable<Dictionary<string, string>> items, TaskPlaningDataSet.TypeStanok stanokId, TaskPlaningDataSet.TypeStanok currentMachineId);
    }
}
