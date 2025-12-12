using System.Collections.Generic;
using System.Linq;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh
{
    /// <summary>
    /// Класс отвечает за конфигурацию для гофроагрегатов
    /// Можно задавать как отдельный станок. Так и группы (для упровления ими)
    /// </summary>
    /// <author>volkov_as</author>
    public static class MachineGroups
    {
        /// <summary>
        /// defaultMachine - дефолт машина, allowedMachines - машина разрешенная для управления, untouchableTasks - количество заданий которые нельзя двигать
        /// </summary>
        private static readonly Dictionary<string, (int defaultMachine, int[] allowedMachines, int untouchableTasks)> Groups =
            new Dictionary<string, (int, int[], int)>
            {
                // Стандартные режимы для одиночных машин
                {"1", (23, new[] {23}, 0)},        // J.S Machine
            };
        
        /// <summary>
        /// Получает конфигурацию группы по её идентификатору
        /// </summary>
        public static (int defaultMachine, int[] allowedMachines, int untouchableTasks) GetGroupConfig(string groupId)
        {
            return Groups.TryGetValue(groupId, out var config) 
                ? config 
                : (23, new[] { 23 }, 0); // Дефолтная конфигурация
        }
        
        /// <summary>
        /// Проверяет, может ли машина управляться в данной группе
        /// </summary>
        public static bool IsMachineAllowedInGroup(string groupId, int machineId)
        {
            var (_, allowedMachines, _) = GetGroupConfig(groupId);
            return allowedMachines.Contains(machineId);
        }
        
        /// <summary>
        /// Проверяет, содержится ли указанная машина в списке разрешенных для группы
        /// </summary>
        /// <param name="machineId">ID машины для проверки</param>
        /// <returns>true если машина разрешена в какой-либо группе, иначе false</returns>
        public static bool IsMachineInAnyGroup(int machineId)
        {
            foreach (var group in Groups.Values)
            {
                if (group.allowedMachines.Contains(machineId))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Проверяет, содержится ли указанная машина в списке разрешенных для конкретной группы
        /// </summary>
        /// <param name="groupId">ID группы</param>
        /// <param name="machineId">ID машины для проверки</param>
        /// <returns>true если машина разрешена в указанной группе, иначе false</returns>
        public static bool ContainsMachine(string groupId, int machineId)
        {
            var (_, allowedMachines, _) = GetGroupConfig(groupId);
            return allowedMachines.Contains(machineId);
        }
        
    }
}