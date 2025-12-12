using Client.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh
{
    /// <summary>
    /// создание/редактирование записи в журнал оператора ГА
    /// </summary>
    /// <author>vlasov_ea</author>
    public partial class LogbookRecord : UserControl
    {
        public LogbookRecord(Dictionary<string, string> record = null)
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Init();

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
                
                // Проверяем наличие фотографий после загрузки данных записи
                CheckAndDisplayPhotos();
            }

            SetDefaults();
        }

        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

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


        /// <summary>
        /// инициализация компонентов
        /// </summary>
        public void Init()
        {
            IdLogbook = 0;
            FrameName = "LogbookRecord";
        }

        public void SetDefaults()
        {
            LoadUnits();
            LoadDepartments();
        }

        public async void LoadUnits()
        {
            DisableControls();

            UnitIdName = new List<(int, string)>();
            var units = new List<string>();

            var ds = await Logbook.GetUnits();
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

            var ds = await Logbook.GetDepartments();
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
        /// Создание новой записи для журнала оператора ГА
        /// </summary>
        public async void InsertRecord()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.CheckAdd("ID_ST", CorrugatorMachineOperator.SelectedMachineId.ToString());
            p.CheckAdd("ID_UNIT", IdUnit.ToString());
            p.CheckAdd("PROBLEM", Problem.ToString());
            p.CheckAdd("DECISION", Decision.ToString());
            p.CheckAdd("STLD_ID", StldId.ToString());

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorLog");
            q.Request.SetParam("Action", "Create");

            q.Request.SetParams(p);

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
                    if (result.ContainsKey("ITEMS"))
                    {
                        OnClose();
                        Close();
                    }
                }
            }

            EnableControls();
        }

        /// <summary>
        /// Изменение записи в журнала оператора ГА
        /// </summary>
        public async void UpdateRecord()
        {
            DisableControls();

            var p = new Dictionary<string, string>();
            p.CheckAdd("ID_LOGBOOK", IdLogbook.ToString());
            p.CheckAdd("ID_UNIT", IdUnit.ToString());
            p.CheckAdd("PROBLEM", Problem.ToString());
            p.CheckAdd("DECISION", Decision.ToString());
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
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    if (result.ContainsKey("ITEMS"))
                    {
                        OnClose();
                        Close();
                    }
                }
            }

            EnableControls();
        }

        public void Destroy()
        {
            //отправляем сообщение о закрытии окна
            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "tape_counter",
                ReceiverName = "",
                SenderName = "LogbookRecord",
                Action = "Closed",
            });

            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {
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
                    Close();
                    e.Handled = true;
                    break;
                case Key.Enter:
                    Save();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            Central.WM.FrameMode = 2;

            var frameName = GetFrameName();
            if (IdLogbook == 0)
            {
                Central.WM.Show(frameName, "Добавление записи в журнал ГА", true, "add", this);
            }
            else
            {
                Central.WM.Show(frameName, $"Изменение записи в журнале ГА", true, "change", this);
            }
        }


        /// <summary>
        /// закрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
        }


        /// <summary>
        /// формирует уникальный идентификатор фрейма
        /// </summary>
        /// <returns></returns>
        public string GetFrameName()
        {
            string result = "";
            result = $"{FrameName}_{IdLogbook}";
            return result;
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
        /// Проверяет наличие фотографий неисправностей и отображает соответствующие кнопки
        /// </summary>
        private void CheckAndDisplayPhotos()
        {
            if (IdLogbook <= 0) return;
            
            try
            {
                string basePath = @"\\l-pack\net\Отделы\Производство\Фото_неисправностей\";
                string pattern = $"{IdLogbook}_";
                
                PhotoButtonsPanel.Children.Clear();
                
                if (!System.IO.Directory.Exists(basePath))
                {
                    return;
                }
                
                var files = System.IO.Directory.GetFiles(basePath)
                    .Where(f => System.IO.Path.GetFileNameWithoutExtension(f).StartsWith(pattern) && 
                          (System.IO.Path.GetExtension(f).ToLower() == ".jpg" || 
                           System.IO.Path.GetExtension(f).ToLower() == ".jpeg" || 
                           System.IO.Path.GetExtension(f).ToLower() == ".png"))
                    .ToList();
                
                for (int i = 0; i < files.Count; i++)
                {
                    string filePath = files[i];
                    Button photoButton = new Button
                    {
                        Content = $"Фото {i + 1}",
                        Margin = new Thickness(5, 0, 0, 0),
                        Tag = filePath,
                        Style = Application.Current.Resources["Button"] as Style
                    };
                    photoButton.Click += PhotoButton_Click;
                    PhotoButtonsPanel.Children.Add(photoButton);
                }

                if (files.Count == 0)
                {
                    PhotoLabel.Visibility = Visibility.Collapsed;
                    Height = 400;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при поиске фотографий: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Обработчик нажатия на кнопку с фотографией
        /// </summary>
        private void PhotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filePath)
            {
                try
                {
                    System.Diagnostics.Process.Start(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
