using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Production.Corrugator;
using DevExpress.XtraPrinting.Native;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Client.Common.LPackClientRequest;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// создание/редактирование записи в журнале оператора БДМ
    /// </summary>
    /// <author>greshnyh_ni</author>
    public partial class LogbookRecordPaperMachine : ControlBase
    {

        private FormHelper Form { get; set; }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }
        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;

        public int IdLogbook { get; set; }
        public int IdUnit { get; set; }
        public string NameUnit { get; set; }
        public string Problem { get; set; }
        public string Decision { get; set; }
        public int StldId { get; set; }
        public string StldName { get; set; }

        private List<(int, string)> UnitIdName { get; set; }

        /// <summary>
        /// Список id и имён всех служб 
        /// </summary>
        private List<(int, string)> DepartmentIdName { get; set; }

        public delegate void OnCloseDelegate();
        public OnCloseDelegate OnClose;

        public int MachineId { get; private set; }
        public Dictionary<string, string> Values { get; set; }

        /// <summary>
        /// Данные для списка приложенных файлов
        /// </summary>
        ListDataSet FilesDS { get; set; }

        /// <summary>
        /// Список ID удалённых файлов
        /// </summary>
        private Dictionary<int, string> DeletedFileIds;

        public Dictionary<string, string> SelectedFileItem { get; set; }

        public LogbookRecordPaperMachine(int currentMachineId, Dictionary<string, string> record = null)
        {
            InitializeComponent();

            MachineId = currentMachineId;

            IdLogbook = 0;
            DeletedFileIds = new Dictionary<int, string>();
            FilesDS = new ListDataSet();
            FilesDS.Init();

            if (record != null)
            {
                IdLogbook = record.CheckGet("ID_LOGBOOK").ToInt();
                IdUnit = record.CheckGet("ID_UNIT").ToInt();
                NameUnit = record.CheckGet("NAME_UNIT");
                Problem = record.CheckGet("PROBLEM");
                Decision = record.CheckGet("DECISION");
                StldId = record.CheckGet("STLD_ID").ToInt();
                StldName = record.CheckGet("DEP_NAME");

                ProblemTxtBx.Text = Problem;
                DecisionTxtBx.Text = Decision;
            }

            ControlSection = "paper_machine_control";
            //  RoleName = "[erp]developer";
            DocumentationUrl = "/doc/l-pack-erp/production/molded_container/machine_control";

            FrameName = ControlName;

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {

            };

            OnLoad = () =>
            {
            };

            OnUnload = () =>
            {
                FileGrid.Destruct();
            };

            OnFocusGot = () =>
            {
            };

            OnFocusLost = () =>
            {
            };

            OnNavigate = () =>
            {
            };


            Init();
        }

        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            double nScale = 1.5;
            GridParent.LayoutTransform = new ScaleTransform(nScale, nScale);
            SetDefaults();
        }

        public void SetDefaults()
        {
            OpenFileButton.IsEnabled = false;
            DeleteFileButton.IsEnabled = false;

            LoadUnits();
            LoadDepartments();
            InitFileGrid();
        }

        public async void LoadUnits()
        {
            DisableControls();

            UnitIdName = new List<(int, string)>();
            var units = new List<string>();

            var ds = await OperatorsLogPaperMachine.GetUnits();
            var items = ds?.Items;
            foreach (var item in items)
            {
                var idUnit = item?.CheckGet("ID_UNIT").ToInt();
                var nameUnit = item?.CheckGet("NAME_UNIT");
                UnitIdName.Add((idUnit ?? 0, nameUnit));
                units.Add(nameUnit);
            }

            Unit.ItemsSource = units;
            if (NameUnit != null)
            {
                Unit.SelectedItem = NameUnit;
            }

            EnableControls();
        }

        public async void LoadDepartments()
        {
            DisableControls();

            DepartmentIdName = new List<(int, string)>();
            var departments = new List<string>();

            var ds = await OperatorsLogPaperMachine.GetDepartments();
            var items = ds?.Items;
            foreach (var item in items)
            {
                var stldId = item?.CheckGet("STLD_ID").ToInt();
                var department = item?.CheckGet("NAME");
                if (department != "Все")
                {
                    DepartmentIdName.Add((stldId ?? 0, department));
                    departments.Add(department);
                }
            }

            Department.ItemsSource = departments;
            if (StldName != null)
            {
                Department.SelectedItem = StldName;
            }

            EnableControls();
        }

        /// <summary>
        /// Создание новой записи для журнала оператора БДМ
        /// </summary>
        public async void InsertRecord()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.CheckAdd("ID_ST", MachineId.ToString());
            p.CheckAdd("ID_UNIT", IdUnit.ToString());
            p.CheckAdd("PROBLEM", Problem.ToString());
            p.CheckAdd("DECISION", Decision.ToString());
            p.CheckAdd("STLD_ID", StldId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "Monitoring");
            q.Request.SetParam("Action", "LogCreate");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                // получаем id_logbook новой записи
                //q.Answer.Data = "{"ID_LOGBOOK":"4603754"}"}"    
                var res = JsonConvert.DeserializeObject<Dictionary<string, string>>(q.Answer.Data);
                {
                    string IdLogbookStr = IdLogbook.ToString();
                    if (IdLogbook == 0)
                    {
                        IdLogbookStr = res.CheckGet("ID_LOGBOOK").ToInt().ToString();
                    }

                    // отправляем на сервер новые файлы
                    foreach (var item in FilesDS.Items)
                    {
                        if (item["STLF_ID"].ToInt() == 0)
                        {
                            if (File.Exists(item["FILE_NAME"]))
                            {
                                SaveReceiptFile(IdLogbookStr, item);
                            }
                        }
                    }
                }

                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "Production",
                    ReceiverName = ReceiverName,
                    SenderName = ControlName,
                    Action = "RefreshLogBook",
                });

                FileGrid.Destruct();
                Close();
            }

            EnableControls();
        }

        /// <summary>
        /// Изменение записи в журнала оператора БДМ
        /// </summary>
        public async void UpdateRecord()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.CheckAdd("ID_LOGBOOK", IdLogbook.ToString());
            p.CheckAdd("ID_UNIT", IdUnit.ToString());
            p.CheckAdd("PROBLEM", Problem.ToString());
            if (!Decision.IsNullOrEmpty())
                p.CheckAdd("DECISION", Decision.ToString());
            else
                p.CheckAdd("DECISION", " ");

            p.CheckAdd("STLD_ID", StldId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorLog");
            q.Request.SetParam("Action", "Save");

            q.Request.SetParams(p);

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                string IdLogbookStr = IdLogbook.ToString();

                // отправляем на сервер новые файлы
                foreach (var item in FilesDS.Items)
                {
                    if (item["STLF_ID"].ToInt() == 0)
                    {
                        if (File.Exists(item["FILE_NAME"]))
                        {
                            SaveReceiptFile(IdLogbookStr, item);
                        }
                    }
                }

                // удаляем отмеченные для удаления
                if (DeletedFileIds.Count > 0)
                {
                    var p2 = new Dictionary<string, string>();
                    p2.CheckAdd("ID", IdLogbook.ToString());
                    p2.Add("DeletedFiles", JsonConvert.SerializeObject(DeletedFileIds));

                    var q2 = new LPackClientQuery();
                    q2.Request.SetParam("Module", "ProductionPm");
                    q2.Request.SetParam("Object", "Monitoring");
                    q2.Request.SetParam("Action", "DeletedFiles");

                    q2.Request.SetParams(p2);

                    q2.Request.Timeout = Central.Parameters.RequestGridTimeout;
                    q2.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                    await Task.Run(() =>
                    {
                        q2.DoQuery();
                    });
                }

                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "Production",
                    ReceiverName = ReceiverName,
                    SenderName = ControlName,
                    Action = "RefreshLogBook",
                });
                FileGrid.Destruct();

                Close();
            }

            EnableControls();
        }

        /// <summary>
        /// обработчик клавиатуры
        /// </summary>
        public void ProcessKeyboard2()
        {
            var e = Central.WM.KeyboardEventsArgs;
            switch (e.Key)
            {
                case Key.Escape:
                    //   Close();
                    e.Handled = true;
                    break;
                case Key.Enter:
                    Save();
                    e.Handled = true;
                    break;
            }
        }

        public void Edit()
        {
            if (IdLogbook == 0)
            {
                FrameTitle = $"Добавление записи в журнал БДМ";
            }
            else
            {
                FrameTitle = $"Изменение записи №{IdLogbook.ToInt().ToString()} в журнале БДМ";
            }

            Show();
        }

        /// <summary>
        /// подготовка данных
        /// </summary>
        public void Save()
        {
            Problem = ProblemTxtBx.Text;
            Decision = DecisionTxtBx.Text;

            var indexIdUnit = UnitIdName.FindIndex(unitIdName => unitIdName.Item2 == (Unit.SelectedItem as string));
            if (indexIdUnit > -1)
            {
                IdUnit = UnitIdName[indexIdUnit].Item1;
            }

            var indexIdDepartment = DepartmentIdName.FindIndex(departmentIdName => departmentIdName.Item2 == (Department.SelectedItem as string));
            if (indexIdDepartment > -1)
            {
                StldId = DepartmentIdName[indexIdDepartment].Item1;
            }

            // новая запись
            if (IdLogbook == 0)
            {
                InsertRecord();
            }
            // изменение существующей записи
            else
            {
                UpdateRecord();
            }
        }


        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }


        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Save();
        }

        /// <summary>
        ///  настройка грида
        /// </summary>
        private void InitFileGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Название",
                    Path="ORIGINAL_FILE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Width = 550,
                },
                new DataGridHelperColumn
                {
                    Header="ID файла",
                    Path="STLF_ID",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.Integer,
                    Hidden=true,
                },
                new DataGridHelperColumn
                {
                    Header="Имя файла",
                    Path="FILE_NAME",
                    ColumnType=DataGridHelperColumn.ColumnTypeRef.String,
                    Hidden=true,
                },
            };
            FileGrid.SetColumns(columns);
            FileGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;

            //при выборе строки в гриде, обновляются актуальные действия для записи
            FileGrid.OnSelectItem = selectedItem =>
            {
                if (selectedItem.Count > 0)
                {
                    SelectedFileItem = selectedItem;
                }
            };
            //данные грида
            FileGrid.OnLoadItems = FileGridLoadItems;
            FileGrid.Init();
            FileGrid.Run();
        }


        /// <summary>
        /// Загрузка списка файлов для выбранной записи
        /// </summary>
        public async void FileGridLoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "Monitoring");
            q.Request.SetParam("Action", "GetLogbookFiles");
            q.Request.SetParam("ID", IdLogbook.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {

                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    FilesDS = ListDataSet.Create(result, "ITEMS");

                    OpenFileButton.IsEnabled = (FilesDS.Items.Count != 0);
                    DeleteFileButton.IsEnabled = (FilesDS.Items.Count != 0);

                    FileGrid.UpdateItems(FilesDS);
                }
            }
        }

        /// <summary>
        ///  добавляем файл
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddFileButton_Click(object sender, RoutedEventArgs e)
        {
            AddFile();
        }

        /// <summary>
        ///  Удаляем файл
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteFileButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteFile();
        }

        /// <summary>
        /// Просмотр файла
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenReceiptFile();
        }

        private void AddFile()
        {
            var fd = new OpenFileDialog();
            var fdResult = (bool)fd.ShowDialog();
            if (fdResult)
            {
                var fileName = Path.GetFileName(fd.FileName);
                bool resume = true;

                foreach (var fn in FilesDS.Items)
                {
                    if (fileName == fn["ORIGINAL_FILE_NAME"])
                    {
                        resume = false;
                        var dw = new DialogWindow("Такой файл уже есть в списке", "Добавление файла");
                        dw.ShowDialog();
                    }
                }

                if (resume)
                {
                    var filesDStmp = new ListDataSet();
                    filesDStmp.Items.AddRange(FilesDS.Items);
                    int newRowNum = 1;
                    if (FilesDS.Items.Count > 0)
                    {
                        newRowNum = FilesDS.Items.Last()["_ROWNUMBER"].ToInt() + 1;
                    }
                    filesDStmp.Items.Add(new Dictionary<string, string>()
                    {
                        { "_ROWNUMBER", newRowNum.ToString() },
                        { "ORIGINAL_FILE_NAME", fileName },
                        { "STLF_ID", "0" },
                        { "FILE_NAME", fd.FileName },
                    });
                    FileGrid.UpdateItems(filesDStmp);
                    FileGrid.SelectRowByKey(newRowNum.ToString());
                    FilesDS.Items = filesDStmp.Items;

                    OpenFileButton.IsEnabled = (FilesDS.Items.Count != 0);
                    DeleteFileButton.IsEnabled = (FilesDS.Items.Count != 0);
                }
            }
        }

        private void DeleteFile()
        {
            if (FileGrid.SelectedItem != null)
            {
                var dw = new DialogWindow($"Удалить файл \"{FileGrid.SelectedItem["ORIGINAL_FILE_NAME"]}\" из списка?", "Удаление файла", "", DialogWindowButtons.NoYes);
                if (dw.ShowDialog() == true)
                {
                    var filesDStmp = new ListDataSet();
                    filesDStmp.Items.AddRange(FilesDS.Items);
                    int selectedPlrf = FileGrid.SelectedItem["STLF_ID"].ToInt();
                    if (selectedPlrf > 0)
                    {
                        DeletedFileIds.Add(selectedPlrf, FileGrid.SelectedItem["FILE_NAME"]);
                    }
                    filesDStmp.Items.Remove(FileGrid.SelectedItem);

                    FileGrid.UpdateItems(filesDStmp);
                    FilesDS.Items = filesDStmp.Items;

                    OpenFileButton.IsEnabled = (FilesDS.Items.Count != 0);
                    DeleteFileButton.IsEnabled = (FilesDS.Items.Count != 0);
                }
            }
            else
            {
                var dw = new DialogWindow("Не выбран файл", "Удаление файла");
                dw.ShowDialog();
            }
        }

        private async void OpenReceiptFile()
        {
            if (FileGrid.SelectedItem != null)
            {
                if (FileGrid.SelectedItem.CheckGet("STLF_ID").ToInt() > 0)
                {
                    // загрузка сохранённого файла
                    var fileName = FileGrid.SelectedItem["FILE_NAME"];
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        var q = new LPackClientQuery();
                        q.Request.SetParam("Module", "ProductionPm");
                        q.Request.SetParam("Object", "Monitoring");
                        q.Request.SetParam("Action", "OpenLogBookFile");
                        q.Request.SetParam("ID", IdLogbook.ToString());
                        q.Request.SetParam("FILE_NAME", fileName);

                        q.Request.Timeout = Central.Parameters.RequestGridTimeout;
                        q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

                        await Task.Run(() =>
                           {
                               q.DoQuery();
                           });

                        if (q.Answer.Status == 0)
                        {
                            Central.OpenFile(q.Answer.DownloadFilePath);
                        }
                        else
                        {
                            q.ProcessError();
                        }

                    }
                }
                else
                {
                    // загрузка несохранённого файла
                    Central.OpenFile(FileGrid.SelectedItem["FILE_NAME"]);
                }
            }
            else
            {
                var dw = new DialogWindow("Не выбран файл", "Открытие файла");
                dw.ShowDialog();
            }
        }

        private async void SaveReceiptFile(string Id, Dictionary<string, string> item)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "ProductionPm");
            q.Request.SetParam("Object", "Monitoring");
            q.Request.SetParam("Action", "SaveOperatorReceiptFile");
            q.Request.SetParam("ID", Id);
            q.Request.Type = RequestTypeRef.MultipartForm;
            q.Request.UploadFilePath = item["FILE_NAME"];

            await Task.Run(() =>
            {
                q.DoQuery();
            });
        }

        /////
    }
}
