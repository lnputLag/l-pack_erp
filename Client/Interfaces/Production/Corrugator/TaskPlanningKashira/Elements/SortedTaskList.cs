using Client.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Client.Interfaces.Production.Corrugator.TaskPlanningKashira
{
    /// <summary>
    /// Сортировка задач
    /// <author>eletskikh_ya</author>
    /// </summary>
    internal class SortedTaskList
    {
        private static bool IsEqualLayer(string layer1, string layer2)
        {
            if(layer1==layer2) return true;
            if(string.IsNullOrEmpty(layer1) && string.IsNullOrEmpty(layer2)) return true;

            return false;
        }


        /// <summary>
        /// поиск подходящей позиции для first
        /// </summary>
        /// <param name="first"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        private static int GetIndexBestTaskFor(Dictionary<string, string> first, List<Dictionary<string, string>> list)
        {
            int result = -1;

            int format = first.CheckGet(TaskPlaningDataSet.Dictionary.Format).ToInt();
            string profileName = first.CheckGet(TaskPlaningDataSet.Dictionary.ProfilName);

            int i, n = list.Count;

            var Layer1 = first.CheckGet(TaskPlaningDataSet.Dictionary.Layer1);
            var Layer2 = first.CheckGet(TaskPlaningDataSet.Dictionary.Layer2);
            var Layer3 = first.CheckGet(TaskPlaningDataSet.Dictionary.Layer3);
            var Layer4 = first.CheckGet(TaskPlaningDataSet.Dictionary.Layer4);
            var Layer5 = first.CheckGet(TaskPlaningDataSet.Dictionary.Layer5);

            for (i = 0; i < n; i++)
            {
                var tmp = list[i];

                if (profileName == tmp.CheckGet(TaskPlaningDataSet.Dictionary.ProfilName) && format == tmp.CheckGet(TaskPlaningDataSet.Dictionary.Format).ToInt())
                {
                    // в первую очередь мы ищем совпадение по всем слоям
                    if (
                        IsEqualLayer(tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer1), Layer1) &&
                        IsEqualLayer(tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer2), Layer2) &&
                        IsEqualLayer(tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer3), Layer3) &&
                        IsEqualLayer(tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer4), Layer4) &&
                        IsEqualLayer(tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer5), Layer5))
                    {
                        result = i;
                        break;
                    }
                }
            }


            if (result == -1)
            {
                // что мы сюда попали означает что полных совпадений уже нет

                int bestIndex = -1;
                int bestCount = -1;

                for (i = 0; i < n; i++)
                {
                    var tmp = list[i];

                    if (profileName == tmp.CheckGet(TaskPlaningDataSet.Dictionary.ProfilName) && format == tmp.CheckGet(TaskPlaningDataSet.Dictionary.Format).ToInt())
                    {
                        {
                            
                            int layerEqualCount =
                                 (IsEqualLayer(Layer1, tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer1)) ? 1 : 0) +
                                 (IsEqualLayer(Layer2, tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer2)) ? 1 : 0) +
                                 (IsEqualLayer(Layer3, tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer3)) ? 1 : 0) +
                                 (IsEqualLayer(Layer4, tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer4)) ? 1 : 0) +
                                 (IsEqualLayer(Layer5, tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer5)) ? 1 : 0);

                            // несовпадает только один
                            if (layerEqualCount == 4)
                            {
                                int layerCount = list.Count(x =>
                                    IsEqualLayer(x.CheckGet(TaskPlaningDataSet.Dictionary.Layer1), tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer1)) &&
                                    IsEqualLayer(x.CheckGet(TaskPlaningDataSet.Dictionary.Layer2), tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer2)) &&
                                    IsEqualLayer(x.CheckGet(TaskPlaningDataSet.Dictionary.Layer3), tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer3)) &&
                                    IsEqualLayer(x.CheckGet(TaskPlaningDataSet.Dictionary.Layer4), tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer4)) &&
                                    IsEqualLayer(x.CheckGet(TaskPlaningDataSet.Dictionary.Layer5), tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer5))
                                );

                                if(layerCount>bestCount)
                                {
                                    bestCount = layerCount;
                                    bestIndex = i;
                                }
                            }
                        }
                    }
                    else
                    {
                        // если не подходит формат либо профиль тогда не имеет смысла искать дальше
                        break;
                    }
                }

                if(bestIndex>=0)
                {
                    result = bestIndex;
                }
            }

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            if (result == -1)
            {
                // что мы сюда попали означает что полных совпадений уже нет

                int bestIndex = -1;
                int bestCount = -1;

                for (i = 0; i < n; i++)
                {
                    var tmp = list[i];

                    if (profileName == tmp.CheckGet(TaskPlaningDataSet.Dictionary.ProfilName) && format == tmp.CheckGet(TaskPlaningDataSet.Dictionary.Format).ToInt())
                    {
                        {

                            int layerEqualCount =
                                 (IsEqualLayer(Layer1, tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer1)) ? 1 : 0) +
                                 (IsEqualLayer(Layer2, tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer2)) ? 1 : 0) +
                                 (IsEqualLayer(Layer3, tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer3)) ? 1 : 0) +
                                 (IsEqualLayer(Layer4, tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer4)) ? 1 : 0) +
                                 (IsEqualLayer(Layer5, tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer5)) ? 1 : 0);

                            // несовпадает только один
                            if (layerEqualCount == 3)
                            {
                                int layerCount = list.Count(x =>
                                    IsEqualLayer(x.CheckGet(TaskPlaningDataSet.Dictionary.Layer1), tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer1)) &&
                                    IsEqualLayer(x.CheckGet(TaskPlaningDataSet.Dictionary.Layer2), tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer2)) &&
                                    IsEqualLayer(x.CheckGet(TaskPlaningDataSet.Dictionary.Layer3), tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer3)) &&
                                    IsEqualLayer(x.CheckGet(TaskPlaningDataSet.Dictionary.Layer4), tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer4)) &&
                                    IsEqualLayer(x.CheckGet(TaskPlaningDataSet.Dictionary.Layer5), tmp.CheckGet(TaskPlaningDataSet.Dictionary.Layer5))
                                );

                                if (layerCount > bestCount)
                                {
                                    bestCount = layerCount;
                                    bestIndex = i;
                                }
                            }
                        }
                    }
                    else
                    {
                        // если не подходит формат либо профиль тогда не имеет смысла искать дальше
                        break;
                    }
                }

                if (bestIndex >= 0)
                {
                    result = bestIndex;
                }
            }

            return result;
        }

        public static List<Dictionary<string,string>> SortTasks(List<Dictionary<string,string>> list)
        {
            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();

            List<Dictionary<string, string>> sortedOrder = list.OrderBy(x => x.CheckGet(TaskPlaningDataSet.Dictionary.ProfilName))
                                                            .ThenByDescending(x => x.CheckGet(TaskPlaningDataSet.Dictionary.Format).ToInt())
                                                            .ThenBy(x => x.CheckGet(TaskPlaningDataSet.Dictionary.Layer1))
                                                            .ThenBy(x => x.CheckGet(TaskPlaningDataSet.Dictionary.Layer2))
                                                            .ThenBy(x => x.CheckGet(TaskPlaningDataSet.Dictionary.Layer3))
                                                            .ThenBy(x => x.CheckGet(TaskPlaningDataSet.Dictionary.Layer3))
                                                            .ThenBy(x => x.CheckGet(TaskPlaningDataSet.Dictionary.Layer3)).ToList();

            int startCount = sortedOrder.Count;


            while (sortedOrder.Count > 0)
            {
                var first = sortedOrder[0];
                sortedOrder.RemoveAt(0);

                result.Add(first);

                // ищем наиболее подходящие для first записи и ставим их в конец
                while (sortedOrder.Count > 0)
                {
                    var bestTaskIndex = GetIndexBestTaskFor(first, sortedOrder);
                    if (bestTaskIndex != -1)
                    {
                        first = sortedOrder[bestTaskIndex];
                        result.Add(first);
                        sortedOrder.RemoveAt(bestTaskIndex);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if(startCount!= result.Count)
            {
                Console.WriteLine("Error");
            }


            return result;
        }

    }
}
