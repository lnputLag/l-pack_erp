using Client.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Client.Interfaces.Main;

namespace Client.Interfaces.Production.Corrugator.TaskPlannings
{
    /// <summary>
    /// DataSet для работы с данными планирования гофроагрекатов
    /// <author>eletskikh_ya</author>
    /// </summary>
    public class TaskPlaningDataSet
    {
        public TaskPlaningDataSet()
        {
            StartCalculatedDateTime = DateTime.Now;
            EndCalculatedDateTime = StartCalculatedDateTime.AddDays(1);
        }

        static TaskPlaningDataSet()
        {
            LastDateTime = DateTime.Now.AddDays(2);
        }

        /// <summary>
        /// Начало расчетного периода, используется для получения простоев
        /// </summary>
        public DateTime StartCalculatedDateTime { get; set; }
        public DateTime EndCalculatedDateTime { get; set; }

        /// <summary>
        /// Для удобства составления плана надо добавить подсветку ячейки «начать до» на линии, если значение превышает заданное значение.
        /// Для ввода значения нужно дополнительно окошко (Settings).
        /// Например, я хочу, чтобы в плане были задания с «Начать до» в течение ближайших 2 суток;
        /// при введении значения «48» часов - более поздние по времени производства задания в плане подсвечиваются.
        /// </summary>
        public static DateTime LastDateTime { get; set; } = DateTime.MaxValue;

        public static string DateTimeFormat { get => "dd.MM HH:mm"; }

        /// <summary>
        /// ИД текущего станка
        /// 2 21 22
        /// либо 0, если задание еще не на га
        /// </summary>
        ///                                     БХС1
        public enum TypeStanok { Unknow = 0, Gofra5 = 2, Gofra3 = 21, Fosber = 22 };

        /// <summary>
        /// Наименование станков
        /// </summary>
        public static Dictionary<TypeStanok, string> MachineName = new Dictionary<TypeStanok, string>
        {
            { TypeStanok.Fosber, "Фосбер"},
            { TypeStanok.Gofra5, "БХС 2" },
            { TypeStanok.Gofra3, "БХС 1" },
            { TypeStanok.Unknow, "Не распределено" }
        };

        public static Dictionary<TypeStanok, int> MachineCode = new Dictionary<TypeStanok, int>
        {
            { TypeStanok.Fosber, 1 },
            { TypeStanok.Gofra5, 2 },
            { TypeStanok.Gofra3, 4 },
        };


        /// <summary>
        /// Справочник имен полей БД
        /// </summary>
        public class Dictionary
        {
            /// <summary>
            /// Id станка ID_ST
            /// </summary>
            public static String StanokId { get => "ID_ST"; }
            public static String RowNumber { get => "_ROWNUMBER"; }
            public static String NumberIdFree { get => "NUMBER_ID_FREE"; }
            
            /// <summary>
            /// Флаг качества (новое тогда = 1, либо 0)
            /// </summary>
            public static String CheckQid { get => "CHECK_QID"; }
            
            public static String NameCliche { get => "NKLISHE"; }

            // номер по порядку в очереди станка
            public static String NumberId { get => "NUMBER_ID"; }
            public static String Order { get => "PRODUCTION_TASK_NUMBER"; }

            /// <summary>
            /// ID_PZ
            /// </summary>
            public static String ProductionTaskId { get => "PRODUCTION_TASK_ID"; } 

            public static String CalculatedTime { get => "CALCULATED_START_PLANNED"; }
            /// <summary>
            /// Начать до, для простоев это основаня дата
            /// </summary>
            public static String StartBeforeTime { get => "START_BEFORE"; }


            public static String SelectedColumn { get => "_SELECTED"; }

            public static String OnMachine { get => "ON_MACHINE"; }

            public static String StartPlanedTime { get => "START_PLANNED"; }
            public static String Duration { get => "DURATION"; }

            public static String CalculatedDuration { get => "CALCULATEDDURATION"; }

            public static String LastDate { get => "LASTDATE"; }

            /// <summary>
            /// Id простоя
            /// </summary>
            public static String DropdownId { get => "DOPL_ID"; }

            public static String Format { get => "WIDTH"; }

            public static String Length { get => "LENGTH"; }
            public static String BlockLength { get => "BLOCK_LENGTH"; }

            public static String BlockTimeLength { get => "BLOCK_TIME_LENGTH"; }

            public static String Fanfold { get => "FANFOLD"; }
            public static String Glued { get => "GLUED"; }

            /// <summary>
            /// Имя профиля
            /// </summary>
            public static String ProfilName { get => "PROFIL_NAME"; }
            public static String Layer1 { get => "LAYER_1"; }
            public static String Layer2 { get => "LAYER_2"; }
            public static String Layer3 { get => "LAYER_3"; }
            public static String Layer4 { get => "LAYER_4"; }
            public static String Layer5 { get => "LAYER_5"; }

            public static String Reel { get => "REEL"; }

            public static String TransportId { get => "TRANSPORT_ID"; }

            /// <summary>
            /// Вал 1
            /// </summary>
            public static string MF1 { get => "MF1"; }
            /// <summary>
            /// Вал 2
            /// </summary>
            public static string MF2 { get => "MF2"; }

            // валы из bhs_queue
            public static string QMF1 { get => "QMF1"; }
            public static string QMF2 { get => "QMF2"; }

            public static string VAL { get => "VAL"; }
            public static string Machine { get => "MACHINE"; }

            public static string OtherMachine { get => "OTHERMACHINE"; }

            public static string NonclicheIs { get => "NONCLICHE_IS"; }
            public static string NonshtanzIs { get => "NONSHTANZ_IS"; }
            
            public static string CarArrivalFlag { get => "DRIVER_LOG_COUNT"; }
            
            public static string DriverAssigned { get => "DRIVER_UNKNOWN";}

            /// <summary>
            /// Качество
            /// </summary>
            public static string QID { get => "QID"; }

            /// <summary>
            /// Варианты производства задания
            /// 
            /// 1	Все ГА
            /// 2	Только BHS-1
            /// 3	Только BHS-2
            /// 4	Только Fosber
            /// 5	BHS-1 и BHS-2
            /// 6	BHS-1 и Fosber
            /// 7	BHS-2 и Fosber
            /// </summary>
            public static string PossibleMachine { get => "PRCS_ID"; }

            /// <summary>
            /// Артикул
            /// </summary>
            public static string Artikul { get => "ARTIKUL"; }

            public static string FLAG { get => "FLAG";  }

            public static string ID2Count { get => "ID2_COUNT"; }

            public static string Tandem { get => "TANDEM"; }
            public static string FlagPeristyle
            {
                get => "FLAG_PERISTYLE";
            }
            
            public static string FlagSample
            {
                get => "FLAG_SAMPLE";
            }

            public static string CountPassed { get => "COUNT_PASSED"; }
            /// <summary>
            /// Минимальная дата отгрузки
            /// </summary>
            public static string DateTs { get => "DATETS"; }

        }

        /// <summary>
        /// Проверка на одинаковое качество, принадлежность к одному блоку
        /// </summary>
        /// <param name="item"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsEqualQuality(Dictionary<string, string> item, Dictionary<string, string> other)
        {
            bool result = item.CheckGet(TaskPlaningDataSet.Dictionary.ProfilName) == other.CheckGet(TaskPlaningDataSet.Dictionary.ProfilName) &&
                   item.CheckGet(TaskPlaningDataSet.Dictionary.Format).ToInt() == other.CheckGet(TaskPlaningDataSet.Dictionary.Format).ToInt();

            if (result)
            {
                var layer1 = item.CheckGet(TaskPlaningDataSet.Dictionary.Layer1) == other.CheckGet(TaskPlaningDataSet.Dictionary.Layer1);

                if (layer1)
                {

                    var ilayer2 = item.CheckGet(TaskPlaningDataSet.Dictionary.Layer2);
                    var olayer2 = other.CheckGet(TaskPlaningDataSet.Dictionary.Layer2);

                    var ilayer3 = item.CheckGet(TaskPlaningDataSet.Dictionary.Layer3);
                    var olayer3 = other.CheckGet(TaskPlaningDataSet.Dictionary.Layer3);

                    var ilayer4 = item.CheckGet(TaskPlaningDataSet.Dictionary.Layer4);
                    var olayer4 = other.CheckGet(TaskPlaningDataSet.Dictionary.Layer4);

                    var ilayer5 = item.CheckGet(TaskPlaningDataSet.Dictionary.Layer5);
                    var olayer5 = other.CheckGet(TaskPlaningDataSet.Dictionary.Layer5);

                    var layer2 = ilayer2 == olayer2;
                    var layer3 = ilayer3 == olayer3;
                    var layer4 = ilayer4 == olayer4;
                    var layer5 = ilayer5 == olayer5;

                    result = layer2 && layer3 && layer4 && layer5;
                    
                    if (!result)
                    {
                        // проверим отдельно на разных валах
                        if (string.IsNullOrEmpty(ilayer2) && string.IsNullOrEmpty(ilayer3)
                            &&
                            string.IsNullOrEmpty(olayer4) && string.IsNullOrEmpty(olayer5)
                            )
                        {
                            result = ilayer4 == olayer2 && 
                                     ilayer5 == olayer3;
                        }
                        else if (string.IsNullOrEmpty(olayer2) && string.IsNullOrEmpty(olayer3)
                            &&
                            string.IsNullOrEmpty(ilayer4) && string.IsNullOrEmpty(ilayer5)
                            )
                        {
                            result = olayer4 == ilayer2 &&
                                     olayer5 == ilayer3;
                        }
                    }
                }
                else return false;
            }

            return result;
        }


        /// <summary>
        /// Проверка на одинаковое качество, принадлежность к одному блоку
        /// </summary>
        /// <param name="item"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsEqualQuality(DataRow item, DataRow other)
        {
            return item[TaskPlaningDataSet.Dictionary.ProfilName].ToString() == other[TaskPlaningDataSet.Dictionary.ProfilName].ToString() &&
                   item[TaskPlaningDataSet.Dictionary.Format].ToString().ToInt() == other[TaskPlaningDataSet.Dictionary.Format].ToString().ToInt() &&
                   item[TaskPlaningDataSet.Dictionary.Layer1].ToString() == other[TaskPlaningDataSet.Dictionary.Layer1].ToString() &&
                   item[TaskPlaningDataSet.Dictionary.Layer2].ToString() == other[TaskPlaningDataSet.Dictionary.Layer2].ToString() &&
                   item[TaskPlaningDataSet.Dictionary.Layer3].ToString() == other[TaskPlaningDataSet.Dictionary.Layer3].ToString() &&
                   item[TaskPlaningDataSet.Dictionary.Layer4].ToString() == other[TaskPlaningDataSet.Dictionary.Layer4].ToString() &&
                   item[TaskPlaningDataSet.Dictionary.Layer5].ToString() == other[TaskPlaningDataSet.Dictionary.Layer5].ToString();
        }


        public static string GetKeyRow(DataRow item)
        {
            //return item[TaskPlaningDataSet.Dictionary.ProfilName].ToString() +
            //       item[TaskPlaningDataSet.Dictionary.Format].ToString() +
            //       item[TaskPlaningDataSet.Dictionary.Layer1].ToString() +
            //       item[TaskPlaningDataSet.Dictionary.Layer2].ToString() +
            //       item[TaskPlaningDataSet.Dictionary.Layer3].ToString() +
            //       item[TaskPlaningDataSet.Dictionary.Layer4].ToString() +
            //       item[TaskPlaningDataSet.Dictionary.Layer5].ToString();

            return item[TaskPlaningDataSet.Dictionary.Format].ToString() + item[TaskPlaningDataSet.Dictionary.QID].ToString();
        }


        /// <summary>
        /// если в результате изменений данные требуют сохранения
        /// то уведомим об этом владельца датасета
        /// </summary>
        private bool _NeedSave = false;
        public bool NeedSave
        {
            get
            {
                return _NeedSave;
            }
            set
            {
                _NeedSave = value;

                OnSaveNeeded?.Invoke(_NeedSave);
            }
        }

        /// <summary>
        /// Событие передачи ошибки
        /// </summary>
        /// <param name="message"></param>
        public delegate void ErrorMessage(string message);
        public event ErrorMessage OnError;

        /// <summary>
        /// Событие необходимости сохранения, подсвечивает кнопку сохранить
        /// </summary>
        /// <param name="save"></param>
        public delegate void NeedSaveEventArg(bool save);
        public event NeedSaveEventArg OnSaveNeeded;

        /// <summary>
        /// событие требующее обновить грид отображающий TypeStanok
        /// </summary>
        /// <param name="type"></param>
        public delegate void UpdateGridAction(TypeStanok type);
        public event UpdateGridAction UpdateGrid;
        public event UpdateGridAction EndUpdate;


        public delegate void UpdateLine(TypeStanok typeStanok, string key);
        public event UpdateLine OnUpdateLine;

        /// <summary>
        /// информационные сообщения, отображаются в "красном квадратике" в панели приложения
        /// </summary>
        /// <param name="message"></param>
        public delegate void InformMessage(string message);
        public event InformMessage OnMessage;

        private int LoadErrors = 0;

        public class UserAction
        {
            public enum ActionType {
                
                MoveInsertDown, // Перенос позиций в конец грида
                MoveUp,         // перенос позиций на одну вверх
                MoveDown,       // перенос позиций на одну вниз
                Move,           // перенос позиций 
                Selected,       // отметить позиции как выбраные
                DeSelected,     // снять отметку выбрано
                ChangeLayers,   // сменить валы
                DeleteFromProd, // удалить задание со станка
                ChangeFormat,   // сменить формат
                DiffResources   // поиск разрыва сырья
            }
        }

        /// <summary>
        /// Добавление ошибок
        /// </summary>
        /// <param name="message"></param>
        /// <param name="item"></param>
        private void AddInformation(string message, Dictionary<string, string> item = null)
        {
            if (OnMessage != null)
            {
                if (item != null)
                {
                    message += " : Id " + item.CheckGet(Dictionary.ProductionTaskId).ToInt().ToString() + " : " + item.CheckGet(Dictionary.Order) + " позиция " + item.CheckGet(Dictionary.RowNumber) + " Id станка " + item.CheckGet(Dictionary.StanokId);
                }

                OnMessage(message);
            }
        }

        /// <summary>
        /// длина стека изменений
        /// </summary>
        private const int MAX_UNDO_SIZE = 30;

        /// <summary>
        /// Контейнер хранения состояний данных, позволяет "откатыать" изменения на MaxUndoArrayLength шагов
        /// </summary>
        private List<ConcurrentBag<Dictionary<string, string>>> Stack = new List<ConcurrentBag<Dictionary<string, string>>>();

        /// <summary>
        /// Позиции что присутствуют на станке но нет в плане (были добавленны оператором)
        /// </summary>
        private List<Dictionary<string, string>> NotExistsItem { get; set; }

        /// <summary>
        /// Механизм отмены изменений, сохраняет полностью данные
        /// </summary>
        private void Push()
        {
            if(Stack.Count > MAX_UNDO_SIZE)
            {
                Stack.RemoveAt(0);
            }

            ConcurrentBag<Dictionary<string,string>> newData = new ConcurrentBag<Dictionary<string, string>>();

            CurrentData.ForEach(currentData =>
            {
                newData.Add(currentData.ToDictionary(entry => entry.Key,
                                               entry => entry.Value));
            });

            //newData.ForEach(x => x[Dictionary.Selected] = "0.0");

            Stack.Add(newData);             
        }

        /// <summary>
        /// удаляет последнюю копию данных
        /// тем самым иы возвращаемся к предыдущему изменению
        /// </summary>
        private ConcurrentBag<Dictionary<string, string>> Pop()
        {
            ConcurrentBag<Dictionary<string, string>> res = null;

            if(Stack.Count > 1)
            {
                res = Stack[Stack.Count - 1];
                //res.ForEach(x => x[Dictionary.Selected] = "0.0");
                Stack.RemoveAt(Stack.Count-1);
            }
            
            return res;
        }

        /// <summary>
        /// Отмена последнего действия
        /// Осуществляется путем восстановления данных из стека
        /// </summary>
        /// <returns></returns>
        public bool Undo()
        {
            bool res = false;

            // откатим данные на предыдущий шаг
            // проведем синхронизацию
            if (Pop() !=null)
            {
                try
                {
                    //Synchronization(await LoadDataAsync(cancelTokenSource.Token, new ConcurrentBag<Dictionary<string, string>>(), false));
                    res = true;
                }
                catch(Exception)
                {

                }
            }

            if (res)
            {
                UpdateGrid?.Invoke(TypeStanok.Unknow);
                UpdateGrid?.Invoke(TypeStanok.Gofra3);
                UpdateGrid?.Invoke(TypeStanok.Gofra5);
                UpdateGrid?.Invoke(TypeStanok.Fosber);
            }

            return res;
        }

        private void Error(string message)
        {
            OnError?.Invoke(message);
        }

        /// <summary>
        /// Выполнение операций над данными
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="Items"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="AfterRowIndex"></param>
        /// <returns></returns>
        public async void MakeAction(UserAction.ActionType type, IEnumerable<Dictionary<string, string>> Items, int source = -1, int destination = -1, int AfterRowIndex = -1)
        {
            if(type!= UserAction.ActionType.Selected && type!= UserAction.ActionType.DeSelected)
            {
                Push();
            }

            {
                int changeCount = 0;
                int i;
                int n = Items == null ? 0 : Items.Count();

                switch (type)
                {
                    case UserAction.ActionType.ChangeFormat:
                        {
                            foreach (var item in Items)
                            {
                                int ProdId = item.CheckGet(Dictionary.ProductionTaskId).ToInt();
                                var curItem = CurrentData.Where(x => x.CheckGet(Dictionary.ProductionTaskId).ToInt() == ProdId).FirstOrDefault();

                                if (curItem != null)
                                {
                                    // необходимо изменить формат в базе, а также в гриде, он передается в переменной AfterRowIndex

                                    if (curItem[Dictionary.Format].ToInt() != AfterRowIndex)
                                    {
                                        var idTask = curItem[Dictionary.ProductionTaskId];

                                        var changeFormat = LPackClientQuery.DoQueryAsync("Production", "TaskPlanning", "UpdateTaskFormat", "ITEMS",
                                           new Dictionary<string, string>()
                                               {
                                                    { "I_ID_PZ", idTask.ToString() },
                                                    { "I_WEB_WIDTH", AfterRowIndex.ToString() },
                                               }
                                           );

                                        var res = await changeFormat;
                                        if (res.Answer.Status == 0)
                                        {
                                            if (res.Answer.QueryResult != null)
                                            {
                                                if (res.Answer.QueryResult.Items != null)
                                                {
                                                    if (res.Answer.QueryResult.Items.Count > 0)
                                                    {
                                                        //  Признак: 1 - если запись формата и обрези изменилась в t.PROIZ_ZAD; 0 - если нет
                                                        if (res.Answer.QueryResult.Items[0].CheckGet("ID").ToInt() != 0)
                                                        {
                                                            curItem[Dictionary.Format] = AfterRowIndex.ToString();
                                                            changeCount++;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case UserAction.ActionType.DeleteFromProd:
                        {
                            // ищем первую позицию после которой можно будет вставить удаленные позиции
                            // это не должен быть простОй 
                            var query = CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == destination
                                        && x.CheckGet(Dictionary.OnMachine).ToInt() != 0);

                            int pastAfterRowNumber = query.Any() ? query.Max(x=>x.CheckGet(Dictionary.RowNumber).ToInt()) : -1;

                            Task<LPackClientQuery> checkFlagDelete;

                            checkFlagDelete = LPackClientQuery.DoQueryAsync("Production", "TaskPlanning", "CheckFlagDelete", string.Empty,
                                new Dictionary<string, string>()
                                {
                                            { "ID_PZ", Items.First().CheckGet(Dictionary.ProductionTaskId) }
                                });

                            var response = await checkFlagDelete;

                            var Data = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Answer.Data);

                            var resume = Data.CheckGet("COUNT").ToInt() == 0 ? true : false;

                            if (resume)
                            {
                                foreach (var item in Items)
                                {
                                    // необходимо отправить операцию удаления 
                                    int ProdId = item.CheckGet(Dictionary.ProductionTaskId).ToInt();
                                    int stanok = item.CheckGet(Dictionary.StanokId).ToInt();
                                    var curItem = CurrentData.Where(x => x.CheckGet(Dictionary.ProductionTaskId).ToInt() == ProdId).FirstOrDefault();

                                    if (curItem != null)
                                    {
                                        //отправить команду на удаление в базу
                                        Task<LPackClientQuery> deleteTask;

                                        // Для Фосбера используем DeleteTaskAction
                                        if (stanok == (int)TypeStanok.Fosber)
                                        {
                                            deleteTask = LPackClientQuery.DoQueryAsync("Production", "TaskQueue", "DeleteTaskAction", string.Empty,
                                                new Dictionary<string, string>()
                                                {
                                                { "ID_ST", stanok.ToString() },
                                                { "ID_PZ", ProdId.ToString() },
                                                { "IS_COMPLETED", "0" }
                                                }
                                            );
                                        }
                                        else
                                        {
                                            deleteTask = LPackClientQuery.DoQueryAsync("Production", "TaskPlanning", "DeleteTask", string.Empty,
                                                new Dictionary<string, string>()
                                                {
                                                { "ID_ST", stanok.ToString() },
                                                { "ID_PZ", ProdId.ToString() },
                                                { "USER", Central.User.Login }
                                                }
                                            );
                                        }

                                        var q = await deleteTask;

                                        if (q.Answer != null)
                                        {
                                            if (q.Answer.Status == 0)
                                            {
                                                changeCount++;
                                                curItem[Dictionary.OnMachine] = "0.0";
                                                curItem[Dictionary.SelectedColumn] = "1.0";
                                            }
                                        }
                                    }
                                }

                                // далее необходимо переместить данные задания за пределы области на станке
                                if (pastAfterRowNumber >= 0)
                                {
                                    MakeAction(UserAction.ActionType.Move, Items, source, destination, pastAfterRowNumber);
                                }
                            } 
                            else
                            {
                                var dialog = new DialogWindow($"Невозможно удалить задание {Items.First().CheckGet(Dictionary.Order)}  - завезено сырье", "Удаление со станка");
                                dialog.ShowDialog();
                            }
                        }

                        break;
                    case UserAction.ActionType.ChangeLayers:
                        foreach (var item in Items)
                        {
                            int ProdId = item.CheckGet(Dictionary.ProductionTaskId).ToInt();
                            var curItem = CurrentData.Where(x => x.CheckGet(Dictionary.ProductionTaskId).ToInt() == ProdId).FirstOrDefault();

                            if (curItem != null)
                            {
                                string l2 = curItem[Dictionary.Layer2];
                                string l3 = curItem[Dictionary.Layer3];

                                string mf1 = curItem[Dictionary.MF1];
                                string mf2 = curItem[Dictionary.MF2];

                                curItem[Dictionary.MF1] = mf2;
                                curItem[Dictionary.MF2] = mf1;

                                curItem[Dictionary.Layer2] = curItem[Dictionary.Layer4];
                                curItem[Dictionary.Layer3] = curItem[Dictionary.Layer5];

                                curItem[Dictionary.Layer4] = l2;
                                curItem[Dictionary.Layer5] = l3;

                                changeCount++;
                            }
                        }
                        break;
                    case UserAction.ActionType.Selected:
                    case UserAction.ActionType.DeSelected:

                        if(type== UserAction.ActionType.Selected)
                        {
                            CurrentData.ForEach(x => x[Dictionary.SelectedColumn] = "0.0");
                        }

                        foreach (var item in Items)
                        {
                            int StanokId = item.CheckGet(Dictionary.StanokId).ToInt();
                            int CurRowNum = item.CheckGet(Dictionary.RowNumber).ToInt();

                            //int ProdId = item.CheckGet(Dictionary.ProductionTaskId).ToInt();
                            var curItem = CurrentData.Where(x => x.CheckGet(Dictionary.RowNumber).ToInt() == CurRowNum && x.CheckGet(Dictionary.StanokId).ToInt()==StanokId).FirstOrDefault();

                            if (curItem != null)
                            {
                                curItem[Dictionary.SelectedColumn] = type == UserAction.ActionType.Selected ? "1.0" : "0.0";
                            }
                        }

                        break;
                    case UserAction.ActionType.MoveUp:

                        {
                            foreach (var item in Items)
                            {
                                int StanokId = item.CheckGet(Dictionary.StanokId).ToInt();
                                int CurRowNum = item.CheckGet(Dictionary.RowNumber).ToInt();
                                //int ProdId = item.CheckGet(Dictionary.ProductionTaskId).ToInt();
                                int NumberId = item.CheckGet(Dictionary.NumberId).ToInt();

                                source = StanokId;

                                var curItem = CurrentData.Where(x => x.CheckGet(Dictionary.RowNumber).ToInt() == CurRowNum && x.CheckGet(Dictionary.StanokId).ToInt() == StanokId).FirstOrDefault();
                                var prevItem = CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == StanokId && x.CheckGet(Dictionary.RowNumber).ToInt() < CurRowNum)
                                                .OrderByDescending(x => x.CheckGet(Dictionary.RowNumber).ToInt()).FirstOrDefault();


                                if (prevItem != null && curItem != null)
                                {
                                    int OnMachine = prevItem.CheckGet(Dictionary.OnMachine).ToInt();
                                    int PrevRowNum = prevItem.CheckGet(Dictionary.RowNumber).ToInt();
                                    int PrevNumberId = prevItem.CheckGet(Dictionary.NumberId).ToInt();


                                    if (OnMachine != 0)
                                    {
                                        // перемешение не возможно, сверху находится задание которое уже выполняется
                                        break;
                                    }

                                    // пермещение осуществляется сменой значений полей сортировки задания и очреди
                                    curItem[Dictionary.RowNumber] = PrevRowNum.ToString();
                                    prevItem[Dictionary.RowNumber] = CurRowNum.ToString();

                                    curItem[Dictionary.NumberId] = PrevNumberId.ToString();
                                    prevItem[Dictionary.NumberId] = NumberId.ToString();
                                    changeCount++;

                                    //curItem[Dictionary.Selected] = "1.0";
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }

                        break;
                    case UserAction.ActionType.MoveDown:

                        foreach (var item in Items.Reverse())
                        {
                            int StanokId = item.CheckGet(Dictionary.StanokId).ToInt();
                            int CurRowNum = item.CheckGet(Dictionary.RowNumber).ToInt();
                            //int ProdId = item.CheckGet(Dictionary.ProductionTaskId).ToInt();
                            int NumberId = item.CheckGet(Dictionary.NumberId).ToInt();

                            source = StanokId;

                            var curItem = CurrentData.Where(x => x.CheckGet(Dictionary.RowNumber).ToInt() == CurRowNum && x.CheckGet(Dictionary.StanokId).ToInt() == StanokId).FirstOrDefault();
                            var prevItem = CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == StanokId && x.CheckGet(Dictionary.RowNumber).ToInt() > CurRowNum)
                                            .OrderBy(x => x.CheckGet(Dictionary.RowNumber).ToInt()).FirstOrDefault();


                            if (prevItem != null && curItem != null)
                            {
                                int OnMachine = prevItem.CheckGet(Dictionary.OnMachine).ToInt();
                                int PrevRowNum = prevItem.CheckGet(Dictionary.RowNumber).ToInt();
                                int PrevNumberId = prevItem.CheckGet(Dictionary.NumberId).ToInt();

                                if (OnMachine != 0)
                                {
                                    // перемешение не возможно, сверху находится задание которое уже выполняется
                                    break;
                                }

                                // пермещение осуществляется сменой значений полей сортировки задания и очреди
                                curItem[Dictionary.RowNumber] = PrevRowNum.ToString();
                                prevItem[Dictionary.RowNumber] = CurRowNum.ToString();

                                curItem[Dictionary.NumberId] = PrevNumberId.ToString();
                                prevItem[Dictionary.NumberId] = NumberId.ToString();
                                changeCount++;
                                //curItem[Dictionary.Selected] = "1.0";
                            }
                            else
                            {
                                break;
                            }
                        }

                        break;
                    case UserAction.ActionType.MoveInsertDown:

                        {
                            var list = CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == destination);
                            if (!list.Any()) // очередь пуста
                            {
                                int maxRow = 0;
                                int maxNumberId = 0;

                                foreach (var item in Items)
                                {
                                    maxRow++;
                                    maxNumberId++;

                                    int ProdId = item.CheckGet(Dictionary.ProductionTaskId).ToInt();
                                    var curItem = CurrentData.Where(x => x.CheckGet(Dictionary.ProductionTaskId).ToInt() == ProdId).FirstOrDefault();

                                    if (curItem != null)
                                    {
                                        curItem[Dictionary.RowNumber] = maxRow.ToString();
                                        curItem[Dictionary.NumberId] = maxNumberId.ToString();
                                        curItem[Dictionary.StanokId] = destination.ToString();
                                    }

                                    changeCount++;
                                }
                            }
                            else 
                            {
                                var maxRow = list.Max(x => x.CheckGet(Dictionary.RowNumber).ToInt());

                                var maxNumberId = CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == destination)
                                    .Max(x => x.CheckGet(Dictionary.NumberId).ToInt());

                                int startRowNum = -1;

                                foreach (var item in Items)
                                {
                                    maxRow++;
                                    maxNumberId++;

                                    int ProdId = item.CheckGet(Dictionary.ProductionTaskId).ToInt();
                                    var curItem = CurrentData.Where(x => x.CheckGet(Dictionary.ProductionTaskId).ToInt() == ProdId).FirstOrDefault();

                                    if (startRowNum == -1)
                                    {
                                        startRowNum = item.CheckGet(Dictionary.NumberId).ToInt();
                                    }

                                    if (curItem != null)
                                    {
                                        curItem[Dictionary.RowNumber] = maxRow.ToString();
                                        curItem[Dictionary.NumberId] = maxNumberId.ToString();
                                        curItem[Dictionary.StanokId] = destination.ToString();
                                    }

                                    changeCount++;
                                }

                                // необходимо вернуть правильные порядковые номера в очереди

                                if (source != 0)
                                {
                                    var items = CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == source && x.CheckGet(Dictionary.NumberId).ToInt() > startRowNum)
                                                .OrderBy(x => x.CheckGet(Dictionary.NumberId).ToInt()).ToList();

                                    foreach (var item in items)
                                    {
                                        item[Dictionary.NumberId] = startRowNum.ToString();
                                        startRowNum++;
                                    }

                                }
                                else
                                {

                                }
                            }
                        }

                        break;
                    case UserAction.ActionType.Move:
                        if(AfterRowIndex > 0)
                        {
                            // необходимо вставить позиции Items после позиции с номером AfterRowIndex
                            // для этого выбрать все у кого AfterRowIndex больше чем и проставить им RowIndex больше на Count улементов
                            bool notDownTime = true;
                            var count = Items.Count();
                            int RowIndex = AfterRowIndex;

                            int notDropDownCount = 0;

                            Items.ForEach(x => 
                            {
                                if (x.CheckGet(Dictionary.DropdownId).ToInt() == 0) notDropDownCount++;
                            });

                            if(notDropDownCount==0) notDownTime = false;

                            var curItem = CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == destination
                                        && x.CheckGet(Dictionary.RowNumber).ToInt() == AfterRowIndex).FirstOrDefault();

                            if (curItem != null)
                            {
                                if(curItem.CheckGet(Dictionary.OnMachine).ToInt()!=0)
                                {
                                    // необходимо проверить есть ли записи ниже данного задания, если нет тогда можно добавлять
                                    if (AfterRowIndex != CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == destination).Max(x => x.CheckGet(Dictionary.RowNumber).ToInt()))
                                    {
                                        // также возможен вариант когда ниже заказы свободные
                                        var nextitem = CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == destination
                                            && x.CheckGet(Dictionary.RowNumber).ToInt() > AfterRowIndex).FirstOrDefault();

                                        // он не должен быть null данный шаг мы проверили выше
                                        if (nextitem.CheckGet(Dictionary.OnMachine).ToInt() != 0 && notDownTime)
                                        {
                                            Error("Данное действие затрагивает заказы, которые уже запланированы на гофроагрегат ON_MACHINE=1");
                                            Pop();

                                            if (destination != -1) UpdateGrid?.Invoke((TypeStanok)destination);
                                            if (source != -1 && destination != source) UpdateGrid?.Invoke((TypeStanok)source);

                                            return;
                                        }
                                    }
                                }

                                int nextNumberId = curItem[Dictionary.NumberId].ToInt() + 1;
                                var RowNumbersSource = new List<int>();

                                Items.ForEach(item =>
                                {
                                    RowNumbersSource.Add(item.CheckGet(Dictionary.RowNumber).ToInt());
                                });

                                bool up = nextNumberId < RowNumbersSource[0];

                                if (source != destination)
                                {
                                    //if (curItem[Dictionary.OnMachine].ToInt() == 0)
                                    {
                                        //CurrentData.ForEach(x => x[Dictionary.Selected] = "0.0");

                                        CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == destination
                                                    && x.CheckGet(Dictionary.RowNumber).ToInt() > AfterRowIndex)
                                                        .OrderBy(x => x.CheckGet(Dictionary.RowNumber).ToInt())
                                                        .ForEach(item =>
                                                        {
                                                            int rowNum = item[Dictionary.RowNumber].ToInt();
                                                            item[Dictionary.RowNumber] = (rowNum + count).ToString();
                                                            changeCount++;
                                                        });

                                        // а для позиции что в списке сменить станок и выставить ++AfterRowIndex
                                        CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == source
                                                    && RowNumbersSource.Contains(x.CheckGet(Dictionary.RowNumber).ToInt()))
                                                        .OrderBy(x => x.CheckGet(Dictionary.RowNumber).ToInt())
                                                        .ForEach(item =>
                                                        {
                                                            AfterRowIndex++;
                                                            item[Dictionary.StanokId] = destination.ToString();
                                                            item[Dictionary.RowNumber] = AfterRowIndex.ToString();

                                                            item[Dictionary.SelectedColumn] = "1.0";

                                                            changeCount++;

                                                        });

                                        // восстановить нумерацию производственных заданий
                                        CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == destination
                                                    && x.CheckGet(Dictionary.RowNumber).ToInt() > RowIndex)
                                                        .OrderBy(x => x.CheckGet(Dictionary.RowNumber).ToInt())
                                                        .ForEach(item =>
                                                        {
                                                            item[Dictionary.NumberId] = (nextNumberId).ToString();
                                                            nextNumberId++;
                                                            changeCount++;
                                                        });
                                    }
                                    

                                    if(destination== (int)TypeStanok.Gofra3)
                                    {
                                        // необходимо проверить валы
                                        foreach (var item in Items)
                                        {
                                            int ProdId = item.CheckGet(Dictionary.ProductionTaskId).ToInt();
                                            var curItem1 = CurrentData.Where(x => x.CheckGet(Dictionary.ProductionTaskId).ToInt() == ProdId).FirstOrDefault();

                                            if (curItem != null)
                                            {
                                                string l2 = curItem1[Dictionary.Layer2];
                                                string l3 = curItem1[Dictionary.Layer3];

                                                if (!string.IsNullOrEmpty(l2) && !string.IsNullOrEmpty(l3))
                                                {
                                                    string mf1 = curItem1[Dictionary.MF1];
                                                    string mf2 = curItem1[Dictionary.MF2];

                                                    curItem1[Dictionary.MF1] = mf2;
                                                    curItem1[Dictionary.MF2] = mf1;

                                                    curItem1[Dictionary.Layer2] = curItem1[Dictionary.Layer4];
                                                    curItem1[Dictionary.Layer3] = curItem1[Dictionary.Layer5];

                                                    curItem1[Dictionary.Layer4] = l2;
                                                    curItem1[Dictionary.Layer5] = l3;

                                                    changeCount++;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    {
                                        int MinRowNumber = Items.Min(x => x.CheckGet(Dictionary.RowNumber).ToInt()) - 1;
                                        int MinNumberId = Items.Min(x => x.CheckGet(Dictionary.NumberId).ToInt()) - 1;

                                        // получим позиции которые перемещаем, потому как потом мы изменим их индексы и уже не найдем по ним
                                        var sourceItems = CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == source
                                                        && RowNumbersSource.Contains(x.CheckGet(Dictionary.RowNumber).ToInt()))
                                                            .OrderBy(x => x.CheckGet(Dictionary.RowNumber).ToInt()).ToList();


                                        // необходимо увеличить все позиции с той куда перемещаем на + count, 
                                        CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == destination
                                                        && x.CheckGet(Dictionary.RowNumber).ToInt() > AfterRowIndex)
                                                            .OrderBy(x => x.CheckGet(Dictionary.RowNumber).ToInt())
                                                            .ForEach(item =>
                                                            {
                                                                int rowNum = item[Dictionary.RowNumber].ToInt();
                                                                item[Dictionary.RowNumber] = (rowNum + count).ToString();
                                                                changeCount++;
                                                            });

                                        sourceItems.ForEach(item =>
                                        {
                                            AfterRowIndex++;
                                            item[Dictionary.RowNumber] = AfterRowIndex.ToString();
                                        });


                                        // необходимо восстановить RowNumbers 

                                        int IndexRecalculate = MinRowNumber;

                                        CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == destination
                                                        && x.CheckGet(Dictionary.RowNumber).ToInt() > MinRowNumber)
                                                            .OrderBy(x => x.CheckGet(Dictionary.RowNumber).ToInt())
                                                            .ForEach(item =>
                                                            {
                                                                IndexRecalculate++;
                                                                item[Dictionary.RowNumber] = IndexRecalculate.ToString();
                                                                changeCount++;
                                                            });

                                        // необходимо восстановить NumberId если это имеет смысл
                                        if (MinNumberId > 0)
                                        {
                                            IndexRecalculate = MinNumberId;
                                            CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == destination
                                                        && x.CheckGet(Dictionary.NumberId).ToInt() > MinNumberId)
                                                            .OrderBy(x => x.CheckGet(Dictionary.RowNumber).ToInt())
                                                            .ForEach(item =>
                                                            {
                                                                IndexRecalculate++;
                                                                if (item[Dictionary.NumberId].ToInt() > 0)
                                                                {
                                                                    item[Dictionary.NumberId] = IndexRecalculate.ToString();
                                                                    changeCount++;
                                                                }

                                                            });
                                        }
                                    }
                                }


                                
                            }
                        }
                        else
                        {
                            // поставить в самое начало
                            var count = Items.Count();

                            Items.ForEach(x =>
                            {
                                changeCount++;
                                var realItem = CurrentData.FirstOrDefault(y => y.CheckGet(Dictionary.StanokId).ToInt() == source && y.CheckGet(Dictionary.RowNumber).ToInt() == x.CheckGet(Dictionary.RowNumber).ToInt());
                                if (realItem != null)
                                {
                                    realItem[Dictionary.RowNumber] = (-count).ToString();
                                }
                                count--;
                            });

                            // необходимо восстановить RowNumbers и NumberId

                            int IndexRecalculate = 0;

                            CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == destination )
                                                .OrderBy(x => x.CheckGet(Dictionary.RowNumber).ToInt())
                                                .ForEach(item =>
                                                {
                                                    IndexRecalculate++;
                                                    item[Dictionary.RowNumber] = IndexRecalculate.ToString();
                                                    item[Dictionary.NumberId] = IndexRecalculate.ToString();
                                                    changeCount++;
                                                });
                        }

                        break;
                    case UserAction.ActionType.DiffResources:
                        // Поиск разрывов в сырье
                        {
                            var stanokId = source;

                            // получим все позиции с заданными параметрами
                            var items = CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == stanokId && x.CheckGet(Dictionary.DropdownId).ToInt() == 0)
                                .OrderBy(x => x.CheckGet(Dictionary.RowNumber).ToInt()).ToList();


                            // поделим список на блоки
                            int prevFormat = 0;

                            var groupedFormats = new List<List<Dictionary<string, string>>>();
                            var currentFormatList = new List<Dictionary<string, string>>();

                            foreach (var item in items)
                            {
                                int currentFormat = item.CheckGet(Dictionary.Format).ToInt();
                                if (currentFormat != prevFormat)
                                {
                                    prevFormat = currentFormat;
                                    if (currentFormatList.Count > 0)
                                    {
                                        groupedFormats.Add(currentFormatList);
                                    }

                                    currentFormatList = new List<Dictionary<string, string>>();
                                    currentFormatList.Add(item);
                                }
                                else
                                {
                                    currentFormatList.Add(item);
                                }
                            }

                            if (currentFormatList.Count > 0)
                            {
                                groupedFormats.Add(currentFormatList);
                            }

                            // пробежим по блокам и найдем все повторяющиеся качества
                            foreach (var gitems in groupedFormats)
                            {
                                FindSameQuality(gitems);
                            }

                        }
                        break;
                }


                var stanokUnknowId = (int)TypeStanok.Unknow;

                if (destination==stanokUnknowId)
                {
                    var unkowTask = CurrentData.Where(y => y.CheckGet(Dictionary.StanokId).ToInt() == stanokUnknowId);

                    unkowTask.ForEach(m =>
                    {
                        m[Dictionary.OtherMachine] = "0";

                        var item = CurrentData.FirstOrDefault(x => x.CheckGet(Dictionary.StanokId).ToInt() != stanokUnknowId && IsEqualQuality(x, m));
                        if (item != null)
                        {
                            m[Dictionary.OtherMachine] = item.CheckGet(Dictionary.StanokId);
                            item[Dictionary.OtherMachine] = "-1";
                        }
                    }
                    );
                }


                if (source != -1)
                {
                    UpdateGrid?.Invoke((TypeStanok)source);
                }

                // source!=destination если уже не обновляли source
                if (destination != -1 && source!=destination)
                {
                    UpdateGrid?.Invoke((TypeStanok)destination);
                }

                if(changeCount>0)
                {
                    // если назначение и источник является грид со станком ID == 0 это изменение не должно сохраняться
                    if ((source == destination && source == 0) || (source == 0 && destination==-1))
                    {

                    }
                    else
                    {
                        NeedSave = true;
                    }
                }

            }

            //return source != -1 || destination != -1;
        }

        private void FindSameQuality(IList<Dictionary<string,string>> items)
        {
            items.ForEach(x =>
            {
                x[Dictionary.FLAG] = "0";
            }
                            );

            int prevFormat = -1;

            // массив профиль формат, слой, качество
            Dictionary<string, Dictionary<int, Dictionary<int, List<string>>>> PDoubleBlock = new Dictionary<string, Dictionary<int, Dictionary<int, List<string>>>>();
            Dictionary<string, Dictionary<int, Dictionary<int, List<string>>>> PRepeatableQuality = new Dictionary<string, Dictionary<int, Dictionary<int, List<string>>>>();


            // вычислим все повторяющееся сырье в рамках оджного формата
            for (int ilayer = 1; ilayer < 6; ilayer++)
            {
                string prevLayer = string.Empty;

                items.ForEach(x =>
                {
                    var profil = x.CheckGet(Dictionary.ProfilName);
                    var format = x.CheckGet(Dictionary.Format).ToInt();
                    var layer = x.CheckGet($"LAYER_{ilayer}");

                    if (prevLayer != layer)
                    {
                        // качество сменилось, необходимо проверить было ли оно ранее?
                        prevLayer = layer;

                        if (!PDoubleBlock.ContainsKey(profil))
                        {
                            PDoubleBlock.Add(profil, new Dictionary<int, Dictionary<int, List<string>>>());
                        }

                        var DoubleBlock = PDoubleBlock[profil];

                        if (!DoubleBlock.ContainsKey(format))
                        {
                            DoubleBlock.Add(format, new Dictionary<int, List<string>>() { { ilayer, new List<string>() { layer } } });
                        }
                        else
                        {
                            if (!DoubleBlock[format].ContainsKey(ilayer))
                            {
                                DoubleBlock[format].Add(ilayer, new List<string>() { layer });
                            }
                            else
                            {
                                if (DoubleBlock[format][ilayer].Contains(layer))
                                {
                                    // такое качество уже встречалось!

                                    if (!PRepeatableQuality.ContainsKey(profil))
                                    {
                                        PRepeatableQuality.Add(profil, new Dictionary<int, Dictionary<int, List<string>>>());
                                    }

                                    var RepeatableQuality = PRepeatableQuality[profil];

                                    if (!RepeatableQuality.ContainsKey(format)) RepeatableQuality.Add(format, new Dictionary<int, List<string>>());
                                    if (!RepeatableQuality[format].ContainsKey(ilayer)) RepeatableQuality[format].Add(ilayer, new List<string>());

                                    if (!RepeatableQuality[format][ilayer].Contains(layer))
                                    {
                                        RepeatableQuality[format][ilayer].Add(layer);
                                    }
                                }
                                else
                                {
                                    DoubleBlock[format][ilayer].Add(layer);
                                }
                            }
                        }
                    }
                }
                );
            }

            // пробежим по всем повторяющемуся сырью и проставим признак повторения

            PRepeatableQuality.ForEach(profile =>
            {
                profile.Value.ForEach(format =>
                {
                    format.Value.ForEach(ilayer =>
                    {
                        ilayer.Value.ForEach(layer =>
                        {
                            var p = profile.Key;
                            var f = format.Key;
                            var i = ilayer.Key;
                            var l = layer;

                            string layerName = $"LAYER_{i}";

                            items.Where(x =>
                                    x.CheckGet(Dictionary.ProfilName) == p &&
                                    x.CheckGet(Dictionary.Format).ToInt() == f &&
                                    x.CheckGet(layerName) == layer)
                                .ForEach(item =>
                                {
                                    int flag = item.CheckGet(Dictionary.FLAG).ToInt();
                                    int pow = (int)Math.Pow(2, i - 1);

                                    if ((flag & pow) == 0)
                                    {
                                        flag += pow;
                                        item[Dictionary.FLAG] = flag.ToString();
                                    }
                                });
                        }
                        );
                    }
                    );
                }
                );
            });
        }

        /// <summary>
        /// Получение блока заказов с заданным станком и таким же качеством как item
        /// </summary>
        /// <param name="stanokId"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        internal List<Dictionary<string, string>> Select(int stanokId, Dictionary<string, string> item)
        {
            List<Dictionary<string, string>> result = CurrentData.Where(x =>
                x.CheckGet(Dictionary.StanokId).ToInt() == stanokId && IsEqualQuality(x, item)

            ).ToList();

            return result;
        }

        private ConcurrentBag<Dictionary<string, string>> CurrentData
        {
            get
            {
                if (Stack.Count > 0)
                {
                    return Stack.Last();
                }
                
                return null;
            }

            set
            {
                Stack.Add(value);

                while(Stack.Count > MAX_UNDO_SIZE)
                {
                    Stack.RemoveAt(0);
                }
            }
        }

        public async Task<List<Dictionary<string, string>>> GetAllOrders()
        {

            var list1 = await GetDataAsync(TypeStanok.Fosber);
            var list0 = await GetDataAsync(TypeStanok.Unknow);
            var list2 = await GetDataAsync(TypeStanok.Gofra5);
            var list3 = await GetDataAsync(TypeStanok.Gofra3);

            List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();

            result.AddRange(list0.Items);
            result.AddRange(list1.Items);
            result.AddRange(list2.Items);
            result.AddRange(list3.Items);

            return result;
        }


        private CancellationTokenSource cancelTokenSource;

        /// <summary>
        /// Загрузка данных
        /// Данные загружаются один раз, а потом лишь обновляются
        /// </summary>
        /// <param name="forced">принудительная загрузка</param>
        /// <returns></returns>
        public async Task<ConcurrentBag<Dictionary<string, string>>> Load(bool forced)
        {
            if (cancelTokenSource != null)
            {
                // если загрузка инициализированна в то время как она еще идет,
                // то отправим в выполняемую операцию отмену
                cancelTokenSource.Cancel();
                cancelTokenSource = null;
            }

            cancelTokenSource = new CancellationTokenSource();

            // если данных еще нет, то загрузим их
            if (CurrentData == null || forced)
            {
                try
                {
                    CurrentData = new ConcurrentBag<Dictionary<string, string>>();
                    _ = await LoadDataAsync(cancelTokenSource.Token, CurrentData, true);
                }
                catch(Exception ex)
                {

                }
            }
            else
            {
                Synchronization(await LoadDataAsync(cancelTokenSource.Token, new ConcurrentBag<Dictionary<string, string>>(), false));
            }

            
            // обновление всех гридов
            EndUpdate?.Invoke(TypeStanok.Unknow);

            return CurrentData;
        }

        public async Task<ConcurrentBag<Dictionary<string, string>>> Synchronization()
        {
            Synchronization(await LoadDataAsync(cancelTokenSource.Token, new ConcurrentBag<Dictionary<string, string>>(), false));

            EndUpdate?.Invoke(TypeStanok.Unknow);

            return CurrentData;
        }

        /// <summary>
        /// Синхронизация текущих данных с новыми данными (полученными из БД)
        /// добавляет новые задания если они пояаились
        /// кдаляет выполненные
        /// </summary>
        /// <param name="newData"></param>
        private void Synchronization(ConcurrentBag<Dictionary<string, string>> newData)
        {
            bool needGridsUpdate = true;

            List<string> listForDeleting = new List<string>();
            List<string> listForAdd = new List<string>();

            ///
            // проверить какие задания уже выполнены
            foreach (var item in CurrentData)
            {
                bool exists = false;
                foreach(var item2 in newData)
                {
                    if (item[Dictionary.ProductionTaskId] == item2[Dictionary.ProductionTaskId])
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    listForDeleting.Add(item[Dictionary.ProductionTaskId]);
                }
            }

            ///
            // проверить какие задания добавились
            foreach (var item in newData)
            {
                bool exists = false;
                foreach (var item2 in CurrentData)
                {
                    if (item[Dictionary.ProductionTaskId] == item2[Dictionary.ProductionTaskId])
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    listForAdd.Add(item[Dictionary.ProductionTaskId]);
                }
            }

            // высьавить on_machine
            newData
                .ForEach(x =>
                {
                    var item = CurrentData.Where(xi => xi.CheckGet(Dictionary.ProductionTaskId) == x.CheckGet(Dictionary.ProductionTaskId)).FirstOrDefault();

                    string changeIdDevice = "";

                    if(item != null)
                    {
                        if (item[Dictionary.ProductionTaskId].ToInt() != 0)
                        {
                            if (item[Dictionary.OnMachine].ToInt() != x[Dictionary.OnMachine].ToInt())
                            {
                                item[Dictionary.OnMachine] = x[Dictionary.OnMachine];
                            }

                            if (item[Dictionary.StanokId].ToInt() != x[Dictionary.StanokId].ToInt())
                            {
                                //item[Dictionary.StanokId] = x[Dictionary.StanokId];
                                //AddInformation("Задание изменило Id станка " + x[Dictionary.StanokId].ToInt() + ", данное изменение не синхронизированно", item);

                                changeIdDevice += item[Dictionary.ProductionTaskId] + ", " + x[Dictionary.StanokId].ToInt() + "=>" +  item[Dictionary.StanokId] + Environment.NewLine;
                            }


                            if (item[Dictionary.Duration].ToDouble() != x[Dictionary.Duration].ToDouble())
                            {
                                if (item[Dictionary.StanokId].ToInt() != 0)
                                {
                                    AddInformation("Время выполнения изменилось с " + item[Dictionary.Duration] + " на " + x[Dictionary.Duration], item);
                                }

                                item[Dictionary.Duration] = x[Dictionary.Duration];
                            }

                            if (item[Dictionary.StartPlanedTime] != x[Dictionary.StartPlanedTime])
                            {
                                //if (item[Dictionary.StanokId].ToInt() != 0)
                                //{
                                //    AddInformation("Время начала работы изменилось ", item);
                                //}

                                item[Dictionary.StartPlanedTime] = x[Dictionary.StartPlanedTime];
                            }

                        }
                    }

                    if(changeIdDevice!=string.Empty)
                    {
                        //AddInformation("Задание изменило Id станка " + changeIdDevice);
                    }

                });

            // если есть данные для изменения сделаем бэкап
            if(listForDeleting.Count > 0 || listForAdd.Count > 0)
            {
                Push();
                AddInformation("Данные синхронизируются, удаленных " + listForDeleting.Count + " новых " + listForAdd.Count);
            }
            else
            {
                AddInformation("Данные синхронизированны, никаких изменений нет " + DateTime.Now);
                needGridsUpdate = false;
            }

            // FIXME: currentData.Count/5 бывает что один из гридов не пргрузился, и поэтому новые данные требует зачистить полностью грид
            if (listForDeleting.Count > 0 && listForDeleting.Count < (CurrentData.Count/5))
            {
                var items = CurrentData.Where(x => !listForDeleting.Contains(x[Dictionary.ProductionTaskId]));
                var deleted = CurrentData.Where(x => listForDeleting.Contains(x[Dictionary.ProductionTaskId]));

                CurrentData = new ConcurrentBag<Dictionary<string, string>>();
                foreach (var item in items)
                {
                    CurrentData.Add(item);
                    // AddInformation("Удален", item);
                }

                foreach (var item in deleted)
                {
                    AddInformation("Удален" , item);
                }
            }

            if (listForAdd.Count > 0)
            {
                var items = newData.Where(x => listForAdd.Contains(x[Dictionary.ProductionTaskId]));

                foreach(var item in items)
                {
                    if (item[Dictionary.StanokId].ToInt() ==0)
                    {
                        CurrentData.Add(item);

                        AddInformation("Добавлен", item);


                        // после добавления необходимо восстановить нумерацию заданий
                        int maxRowNum = item[Dictionary.RowNumber].ToInt();

                        var itemsForRestoreRowNumber = CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == 0 && x.CheckGet(Dictionary.RowNumber).ToInt()>= maxRowNum)
                                                        .OrderBy(x=>x.CheckGet(Dictionary.RowNumber).ToInt());

                        foreach(var restoreItem in itemsForRestoreRowNumber)
                        {
                            restoreItem[Dictionary.RowNumber] = maxRowNum.ToString();
                            maxRowNum++;
                        }


                    }
                    else
                    {
                        Console.WriteLine(item[Dictionary.StanokId]);
                    }

                }
            }


            // FIXME нужно записать в каких именно станках произошли изменения и обновить тольлько их
            if (needGridsUpdate)
            {
                UpdateGrid?.Invoke(TypeStanok.Unknow);
                UpdateGrid?.Invoke(TypeStanok.Gofra3);
                UpdateGrid?.Invoke(TypeStanok.Gofra5);
                UpdateGrid?.Invoke(TypeStanok.Fosber);
            }
        }

        private List<Task<LPackClientQuery>> tasks = new List<Task<LPackClientQuery>>();

        public bool IsLoading
        {
            get
            {
                return tasks.Any();
            }
        }

        private void TaskNotExistsInBhsQueue(List<Dictionary<string,string>> notExistitems)
        {
            NotExistsItem = notExistitems.ToList();
        }

        /// <summary>
        /// Загрузим все задания по гофроагрегатам
        /// Во внутренний dataset
        /// </summary>
        /// <param name="token">токен для отмены загрузки, отмена нужна для того что бы остановить загрузку и начать ее заново</param>
        /// <param name="dataContainer">в данный контейнер будут загружаться данные</param>
        /// <param name="callEvents"></param>
        /// <returns></returns>
        private async Task<ConcurrentBag<Dictionary<string, string>>> LoadDataAsync(CancellationToken token, ConcurrentBag<Dictionary<string, string>> dataContainer, bool callEvents = true )
        {
            // обнулим ошибки
            LoadErrors = 0;

            Console.WriteLine("LoadDataAsync----");

            // получим список удаленных заданий
            var qd = await LPackClientQuery.DoQueryAsync("Production", "TaskPlanning", "ListDeletedTask", "ITEMS");
            var ProizZadDeleteTask = new List<int>();

            //получение данных на удалание
            if (qd.Answer.Status == 0)
            {
                if (qd.Answer.QueryResult != null)
                {
                    foreach (var item in qd.Answer.QueryResult.Items)
                    {
                        ProizZadDeleteTask.Add(item.CheckGet("ID_PZ").ToInt());
                    }
                }
            }

            tasks.Add(LPackClientQuery.DoQueryAsync("Production", "TaskPlanning", "List", "ITEMS", new Dictionary<string, string>() { { Dictionary.StanokId, "-1" }, { "START_DATE", StartCalculatedDateTime.ToString("dd.MM.yyyy HH:mm:ss") }, { "END_DATE", EndCalculatedDateTime.ToString("dd.MM.yyyy HH:mm:ss") } }, Central.Parameters.RequestGridTimeout * 100));
            
            {
                // Создаем асинхронную задачу для загрузки всех данных одним запросом
                var allMachineTask = Task.Run(() =>
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Production");
                    q.Request.SetParam("Object", "TaskPlanning");
                    q.Request.SetParam("Action", "ListAllMachine");
                    q.Request.SetParam("START_DATE", StartCalculatedDateTime.ToString("dd.MM.yyyy HH:mm:ss"));
                    q.Request.SetParam("END_DATE", EndCalculatedDateTime.ToString("dd.MM.yyyy HH:mm:ss"));

                    q.DoQuery();
                    return q;
                });

                tasks.Add(allMachineTask);
            }


            // возможно понадобится запрос для получения простоев
            while (tasks.Any())
            {
                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested(); // генерируем исключение

                Task<LPackClientQuery> finishedTask = await Task.WhenAny(tasks);
                tasks.Remove(finishedTask);

                var q = await finishedTask;

                string stanokId = string.Empty;

                if (q.Answer.Status==0)
                {
                    if (token.IsCancellationRequested)
                        token.ThrowIfCancellationRequested(); // генерируем исключение

                    // Проверяем ListAllMachine
                    if (q.Request.Params.ContainsKey("Action") && q.Request.Params["Action"] == "ListAllMachine")
                    {
                        // Обрабатываем ответ от ListAllMachine
                        try
                        {
                            var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                            if (result != null)
                            {
                                AddInformation($"ListAllMachine вернул данные для {result.Count} станков");

                                foreach (var item in result)
                                {
                                    string currentStanokId = item.Key.Replace("ITEMS_", "");

                                    var queryResult = item.Value;
                                    if (queryResult != null)
                                    {
                                        queryResult.Init();

                                        // Убеждаемся, что Items инициализирован
                                        if (queryResult.Items == null)
                                        {
                                            queryResult.Items = new List<Dictionary<string, string>>();
                                        }

                                        AddInformation($"Обрабатываем данные для станка {currentStanokId}, элементов: {queryResult.Items.Count}");

                                        // Создаем фиктивную задачу для обработки данных этого станка
                                        var fakeQuery = new LPackClientQuery();
                                        fakeQuery.Answer.Status = 0;
                                        fakeQuery.Answer.QueryResult = queryResult;
                                        fakeQuery.Request.SetParam(Dictionary.StanokId, currentStanokId);

                                        tasks.Add(Task.FromResult(fakeQuery));
                                    }
                                    else
                                    {
                                        AddInformation($"QueryResult для станка {currentStanokId} равен null");
                                    }
                                }
                            }
                            else
                            {
                                AddInformation("Результат десериализации ListAllMachine равен null");
                            }
                        }
                        catch (Exception ex)
                        {
                            AddInformation($"Ошибка при обработке ListAllMachine: {ex.Message}");
                        }
                    }
                    else if (q.Request.Params.ContainsKey(Dictionary.StanokId))
                    {
                        stanokId = q.Request.Params.CheckGet(Dictionary.StanokId);

                        if (stanokId == "-1")
                        {
                            if (q.Answer.QueryResult != null && q.Answer.QueryResult.Items != null && q.Answer.QueryResult.Items.Count > 0)
                            {
                                TaskNotExistsInBhsQueue(q.Answer.QueryResult.Items);
                            }
                        }
                        else
                        {
                            // Проверяем, что QueryResult и Items не null
                            if (q.Answer.QueryResult != null && q.Answer.QueryResult.Items != null)
                            {
                                // изменим сортировку
                                if (((TypeStanok)stanokId.ToInt()) == TypeStanok.Unknow)
                                {
                                    var list = SortedTaskList.SortTasks(q.Answer.QueryResult.Items);

                                    int rowNum = 0;

                                    list.ForEach(x =>
                                    {
                                        rowNum++;
                                        x[Dictionary.RowNumber] = rowNum.ToString();
                                        x[Dictionary.NumberId] = rowNum.ToString();
                                    });

                                    q.Answer.QueryResult.Items = list;
                                }

                                // подготовка и помещение данных в контейнер
                                foreach (var item in q.Answer.QueryResult.Items)
                                {
                                    if (item[TaskPlaningDataSet.Dictionary.Duration] == null)
                                    {
                                        item[TaskPlaningDataSet.Dictionary.Duration] = "0.0";
                                    }

                                    if (item[TaskPlaningDataSet.Dictionary.ProductionTaskId] == null)
                                    {
                                        // это простой
                                        item[TaskPlaningDataSet.Dictionary.ProductionTaskId] = "0";
                                    }

                                    // если данное задание установленно на "станок" проверим нужно ли его удалить
                                    if (item.CheckGet(TaskPlaningDataSet.Dictionary.OnMachine).ToInt() != 0)
                                    {
                                        // если в массиве удаленных присутствует задание на удаление
                                        var itemDel = ProizZadDeleteTask.FirstOrDefault(x => x == item.CheckGet(TaskPlaningDataSet.Dictionary.ProductionTaskId).ToInt());
                                        if (itemDel > 0)
                                        {
                                            item[TaskPlaningDataSet.Dictionary.OnMachine] = "0.0";
                                        }
                                    }

                                    item[Dictionary.StanokId] = stanokId;
                                    item[Dictionary.FLAG] = "0";

                                    dataContainer.Add(item);
                                }

                                PostProcessData((TypeStanok)stanokId.ToInt(), callEvents);
                            }
                        }
                    }
                }
                else
                {
                    // возникла проблема, по какой то причине данные по одному из станков не пришли
                    Interlocked.Increment(ref LoadErrors);
                    AddInformation($"Данные для станка {stanokId} не загруженны. Код ошибки {q.Answer.Status}");
                    
                    // Мы можем попытаться загрузить данные еще раз 
                    if (stanokId != string.Empty && LoadErrors < 4)
                    {
                        tasks.Add(LPackClientQuery.DoQueryAsync("Production", "TaskPlanning", "List", "ITEMS", new Dictionary<string, string>() { { Dictionary.StanokId, stanokId } }, Central.Parameters.RequestGridTimeout));
                    }
                }
            }

            PostAllProcessData();

            NeedSave = false;

            return dataContainer;
        }

        private void PostAllProcessData()
        {
            // проверим есть ли заказы на гофрах, которые не присутствуют в плане
            if(NotExistsItem!=null)
            {
                if(NotExistsItem.Count>0)
                {
                    foreach(var item in NotExistsItem.OrderBy(x=>x.CheckGet(Dictionary.NumberId).ToInt()).ToList())
                    {
                        var stanok = item.CheckGet(Dictionary.StanokId);
                        int numberId = item.CheckGet(Dictionary.NumberId).ToInt();

                        item[Dictionary.RowNumber] = numberId.ToString();

                        // сместим заказы вниз для вставки заказа в нужное место
                        CurrentData.Where(x => x.CheckGet(Dictionary.RowNumber).ToInt() >= numberId)
                            .ForEach(x =>
                            {
                                x[Dictionary.RowNumber] = (x[Dictionary.RowNumber].ToInt() + 1).ToString();
                            });

                        // добавим позицию
                        CurrentData.Add(item);
                    }

                    // очистим данную очередь
                    NotExistsItem = new List<Dictionary<string, string>>();
                }
            }



            //проверить есть ли данный формат на гофроагрегатах
            //IsEqualQuality
            int stanokId = (int)TypeStanok.Unknow;
            bool resume = false;

            var unkowTask = CurrentData.Where(y => y.CheckGet(Dictionary.StanokId).ToInt() == stanokId);

            unkowTask.ForEach(m =>
            {
                m[Dictionary.OtherMachine] = "0";

                var item = CurrentData.FirstOrDefault(x => x.CheckGet(Dictionary.StanokId).ToInt() != stanokId && IsEqualQuality(x, m));
                if (item != null)
                {
                    m[Dictionary.OtherMachine] = item.CheckGet(Dictionary.StanokId);
                    item[Dictionary.OtherMachine] = "-1";
                    resume = true;
                }
            }
            );

            if (resume)
            {
                UpdateGrid?.Invoke(TypeStanok.Unknow);
            }

            // необходимо получить планируемые простои
        }

        /// <summary>
        /// Обработка данных после загркзки
        /// </summary>
        /// <param name="typeStanok"></param>
        /// <param name="callEvents"></param>
        private async void PostProcessData(TypeStanok typeStanok, bool callEvents)
        {
            if (CurrentData != null)
            {
                int stanokId = (int)typeStanok;

                // у простоев нет нумерации, необходимо ее восстановить
                if (typeStanok != TypeStanok.Unknow)
                {
                    var listOfDownTime = CurrentData.Where(x => x.CheckGet(Dictionary.DropdownId).ToInt() > 0 && x.CheckGet(Dictionary.StanokId).ToInt() == stanokId).OrderBy(y => y.CheckGet(Dictionary.StartBeforeTime).ToDateTime());
                    if (listOfDownTime.Any())
                    {
                        int NumMax = CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == stanokId).Max(y => y.CheckGet(Dictionary.NumberId).ToInt()) + 1;

                        foreach (var downTime in listOfDownTime)
                        {
                            if(downTime.CheckGet(Dictionary.NumberId).ToInt()==0)
                            {
                                NumMax++;
                                downTime[Dictionary.NumberId] = NumMax.ToString();
                            }
                        }
                    }
                }


                if (typeStanok != TypeStanok.Unknow)
                {
                    // проверка на правильность показа слоев (смена валов)

                    CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == stanokId).ForEach(x =>
                    {
                        var qmf1 = x.CheckGet(Dictionary.QMF1).ToInt();
                        var qmf2 = x.CheckGet(Dictionary.QMF2).ToInt();

                        if(qmf1 != qmf2)
                        {
                            bool change_mf = false;
                            int id_pz = x.CheckGet(Dictionary.ProductionTaskId).ToInt();
                            
                            var mf1 = x.CheckGet(Dictionary.MF1);
                            var mf2 = x.CheckGet(Dictionary.MF2);

                            if(mf2!=string.Empty && qmf1==1)
                            {
                                change_mf = true;
                            }
                            
                            if(mf1 != string.Empty && qmf2 == 1)
                            {
                                change_mf = true;
                            }

                            if(change_mf)
                            {
                                string l2 = x[Dictionary.Layer2];
                                string l3 = x[Dictionary.Layer3];

                                x[Dictionary.MF1] = mf2;
                                x[Dictionary.MF2] = mf1;

                                x[Dictionary.Layer2] = x[Dictionary.Layer4];
                                x[Dictionary.Layer3] = x[Dictionary.Layer5];

                                x[Dictionary.Layer4] = l2;
                                x[Dictionary.Layer5] = l3;
                            }
                        }
                    });
                }


                // Создадим задания на загрузку данных
                List<Task<LPackClientQuery>> listOfTask = new List<Task<LPackClientQuery>>();

                CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == stanokId && x.CheckGet(Dictionary.OnMachine).ToInt() != 0)
                .ForEach(x =>
                {
                    listOfTask.Add(LPackClientQuery.DoQueryAsync("Production", "TaskPlanning", "ListRawMaterial", "ITEMS", new Dictionary<string, string>() { { "ID_PZ", x.CheckGet(Dictionary.ProductionTaskId).ToString() } }));
                });

                

                // Пока есть задания - обрабатываем
                while (listOfTask.Any())
                {
                    // получаем первое обработанное
                    var task = await Task.WhenAny(listOfTask);
                    listOfTask.Remove(task);

                    var q = await task;

                    if (q.Answer.Status == 0)
                    {
                        string result = string.Empty;

                        // Добавляем проверку на null
                        if (q.Answer.QueryResult != null && q.Answer.QueryResult.Items != null)
                        {
                            foreach (var item in q.Answer.QueryResult.Items)
                            {
                                int length = item.CheckGet("LENGTH_RAW").ToInt();
                                if (length > 0)
                                {
                                    string idReel = item.CheckGet("ID_REEL");

                                    result += idReel + ",";
                                }
                            }
                        }

                        int productionId = q.Request.Params["ID_PZ"].ToInt();

                        var itemProd = CurrentData.FirstOrDefault(x => x.CheckGet(Dictionary.ProductionTaskId).ToInt() == productionId);
                        if(itemProd != null)
                        {
                            itemProd[Dictionary.Reel] = result;
                        }

                    }
                    else
                    {
                        Console.WriteLine();
                    }
                }

                // по заврешению работы, при необходимости, отправить запрос на обновление данных
                if (callEvents)
                {
                    UpdateGrid?.Invoke(typeStanok);
                }
            }
        }


        /// <summary>
        /// Получение данных для станка с ID
        /// </summary>
        /// <param name="stanok"></param>
        /// <returns></returns>
        public Task<ListDataSet> GetDataAsync(TypeStanok stanok)
        {
            return Task.Run(() =>
                {
                    if(CurrentData != null && CurrentData.Count>0)
                    {
                        if (stanok == TypeStanok.Unknow)
                        {
                            foreach (var item in CurrentData)
                            {
                                if (item.CheckGet(Dictionary.StanokId).ToInt() == (int)TypeStanok.Unknow)
                                {
                                    item.CheckAdd(Dictionary.RowNumber, item.CheckGet(Dictionary.NumberIdFree));
                                }
                            }
                        }

                        var data = CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == (int)stanok).OrderBy(x => x.CheckGet(Dictionary.RowNumber).ToInt()).ToList();

                        var ds = new ListDataSet();

                        var cols = new List<string>();
                        var rows = new List<List<string>>();


                        data.ForEach(x =>
                        {
                            x.Keys.ForEach(y =>
                            {
                                if(!cols.Contains(y))
                                {
                                    cols.Add(y);
                                }
                            });
                        });


                        foreach (Dictionary<string, string> row in data)
                        {
                            var oneRow = new List<string>();

                            foreach(string col in cols)
                            {
                                if(row.ContainsKey(col))
                                {
                                    oneRow.Add(row[col]);
                                }
                                else
                                {
                                    oneRow.Add("");
                                }

                            }

                            rows.Add(oneRow);
                        }

                        ds.Cols = cols;
                        ds.Rows = rows;
                        ds.Init();

                        return ds;
                    }

                    return null;
                }
            );

        }


        /// <summary>
        /// Расчет дат производства
        /// </summary>
        /// <param name="Items"></param>
        public static double CalculateStartDates(List<Dictionary<string, string>> Items, double kpd, int CurrentProdTask, double CurrentProgress, int stanokId)
        {
            // сначала необходимо взять все простои, и посчитать все даты без них,
            // затем начать вставлять простои в подходящие метста, каждый раз пересчитывая

            //T item = base[oldIndex];
            //base.RemoveItem(oldIndex);
            //base.InsertItem(newIndex, item);

            double Hours = 0.0;
            double kpdRefined = kpd / 100.0;

            if (kpdRefined > 0.1 && kpdRefined < 2)
            {
                if (Items.Any())
                {
                    // начнем отсчет от первого планового времени
                    DateTime startDateTime = Items[0].CheckGet(Dictionary.StartPlanedTime).ToDateTime();

                    if (Items[0].CheckGet(Dictionary.DropdownId).ToInt() == 0)
                    {
                        startDateTime = DateTime.Now;
                    }

                    DateTime calculatedDateTime = new DateTime(startDateTime.Ticks);

                    int ProductionTaskId = CurrentProdTask;
                    double totalMinutes = 0;

                    foreach (var task in Items)
                    {
                        task[TaskPlaningDataSet.Dictionary.CalculatedTime] = calculatedDateTime.ToString(TaskPlaningDataSet.DateTimeFormat);

                        double duration = task[TaskPlaningDataSet.Dictionary.Duration].ToDouble();
                        double durationWithKpd = duration * kpdRefined;

                        totalMinutes += durationWithKpd;

                        if (task.CheckGet(TaskPlaningDataSet.Dictionary.ProductionTaskId).ToInt() == ProductionTaskId)
                        {
                            if (CurrentProgress > 0 && CurrentProgress <= 1.0)
                            {
                                durationWithKpd = durationWithKpd - durationWithKpd * CurrentProgress;
                            }
                        }

                        // если это простой, то время простоя не нужно домножать на коэфициент
                        if (task.CheckGet(TaskPlaningDataSet.Dictionary.DropdownId).ToInt() > 0)
                        {
                            durationWithKpd = duration;
                            //task[TaskPlaningDataSet.Dictionary.CalculatedTime] = 

                            calculatedDateTime = task[TaskPlaningDataSet.Dictionary.StartPlanedTime].ToDateTime();
                            task[TaskPlaningDataSet.Dictionary.CalculatedTime] = calculatedDateTime.ToString(TaskPlaningDataSet.DateTimeFormat);
                        }
                        else
                        {
                            // Заодно посчитаю поле Вал
                            string mf1 = task.CheckGet(TaskPlaningDataSet.Dictionary.MF1).TrimEnd();
                            string mf2 = task.CheckGet(TaskPlaningDataSet.Dictionary.MF2).TrimEnd();

                            if (mf1 != string.Empty || mf2 != string.Empty)
                            {
                                string mf =
                                    (mf1 == string.Empty ? "*" : mf1) +
                                    (mf2 == string.Empty ? "*" : mf2);

                                task[TaskPlaningDataSet.Dictionary.VAL] = mf;
                            }
                            else
                            {
                                task[TaskPlaningDataSet.Dictionary.VAL] = string.Empty;
                            }
                        }

                        // расчет и показ расмчетного времени
                        // если задание выполнено и время уже без этого учета то увелиывать время не будем
                        // в этом случае необходима синхронизация что бы убрать выполненное задание

                        task[TaskPlaningDataSet.Dictionary.CalculatedDuration] = durationWithKpd.ToString("0.00");
                        calculatedDateTime = calculatedDateTime.AddMinutes(durationWithKpd);
                    }

                    Hours = totalMinutes / 60;
                }
            }

            return Hours;
        }

        /// <summary>
        /// Установка даты и времени загрузки гофроагрегата
        /// </summary>
        /// <param name="currentMachineId"></param>
        /// <param name="lastDateTime"></param>
        internal void SetLastDataTime(TypeStanok currentMachineId, DateTime lastDateTime, double lastDurationMinutes)
        {
            int stabokId = (int)currentMachineId;

            if(currentMachineId!= TypeStanok.Unknow)
            {
                var freeOrders = CurrentData.Where(x => x.CheckGet(Dictionary.StanokId).ToInt() == 0);
                if(freeOrders.Any())
                {
                    freeOrders.ForEach(x =>
                    {
                        /// 1	Все ГА
                        /// 2	Только BHS-1
                        /// 3	Только BHS-2
                        /// 4	Только Fosber
                        /// 5	BHS-1 и BHS-2
                        /// 6	BHS-1 и Fosber
                        /// 7	BHS-2 и Fosber
                        int otherMachine = x.CheckGet(Dictionary.PossibleMachine).ToInt();
                        if (currentMachineId == TypeStanok.Gofra5) // БХС 1
                        {
                            if (otherMachine == 2 || otherMachine == 5 || otherMachine == 6)
                            {
                                x.CheckAdd(Dictionary.LastDate, lastDateTime.ToString());
                            }
                        }
                        else if (currentMachineId== TypeStanok.Gofra3)
                        {
                            if(otherMachine==3|| otherMachine==5 || otherMachine==7)
                            {
                                x.CheckAdd(Dictionary.LastDate, lastDateTime.ToString());
                            }
                        }
                        else if(currentMachineId== TypeStanok.Fosber)
                        {
                            if (otherMachine == 4 || otherMachine == 6 || otherMachine == 7)
                            {
                                x.CheckAdd(Dictionary.LastDate, lastDateTime.ToString());
                            }
                        }
                    });
                }
            }
        }
    }
}
