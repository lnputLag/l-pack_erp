using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh.Elements
{
    /// <summary>
    /// Окно для добавление/удалений заданий со станка (в ручную)
    /// </summary>
    public partial class ActionMovingTask : ControlBase
    {
        public ActionMovingTask()
        {
            idProductionTask = 0;
            keyOperation = 0;

            InitializeComponent();

            FrameMode = 2;

            NoteBox.PreviewTextInput += NoteBox_PreviewTextInput;

            OnGetFrameTitle = () =>
            {
                string title = string.Empty;

                if (keyOperation == 1)
                {
                    title = $"Добавление задания {idProductionTask} на станок";
                }
                else if (keyOperation == 0)
                {
                    title = $"Удаление задания {idProductionTask} со станка";
                }

                return title;
            };

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "add_task",
                        Enabled = true,
                        Title = "Добавить",
                        ButtonUse = true,
                        ButtonName = "AddTask",
                        Action = MovingTask
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "delete_task",
                        Enabled = true,
                        Title = "Удалить",
                        ButtonUse = true,
                        ButtonName = "DeleteTask",
                        Action = MovingTask
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel_operation",
                        Enabled = true,
                        Title = "Отмена",
                        ButtonUse = true,
                        ButtonName = "Cancel",
                        Action = Close
                    });
                }
                Commander.Init(this);
            }
        }
        private int idProductionTask { get; set; }
        private int keyOperation { get; set; }

        /// <summary>
        /// Фильтр ввода - разрешает только цифры
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NoteBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        
        public async void Open(int id, int key, string numTask)
        {
            idProductionTask = id; 
            keyOperation = key;
            Width = 300;
            Height = 150;
            
            if (keyOperation == 1)
            {
                DescriptionAction.Text = $"Добавление задания {numTask} на станок";
                DeleteTask.Visibility = Visibility.Collapsed;
                await GiveNumberAsync(idProductionTask);
            }
            else if (keyOperation == 0)
            {
                DescriptionAction.Text = $"Удаление задания {numTask} со станка";
                AddTask.Visibility = Visibility.Collapsed;
                await GiveNumberAsync(idProductionTask);
            }

            var windowParameters = new Dictionary<string, string>
            {
                { "no_resize", "1" },
                { "no_maximize", "1" },
                { "no_minimize", "1" },
                { "center_screen", "1" }
            };

            Central.WM.FrameMode = FrameMode;
            var frameName = GetFrameName();
            var frameTitle = FrameTitle;

            if (OnGetFrameTitle != null)
            {
                frameTitle = OnGetFrameTitle.Invoke();
                frameName = GetFrameName();
            }

            Central.WM.Show(frameName, frameTitle, true, "add", this, p: windowParameters);
        }

        /// <summary>
        /// Запрос для формирования задания на станок
        /// </summary>
        /// <param name="keyOperation"></param>
        private async void MovingTask()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorMachineOperatorKsh");
            q.Request.SetParam("Action", "MovingTask");
            q.Request.SetParam("ID_TASK", idProductionTask.ToString());
            q.Request.SetParam("KEY_OPERATION", keyOperation.ToString());
            q.Request.SetParam("NOTE", NoteBox.Text);

            await Task.Run(() => q.DoQuery());

            if (q.Answer.Status == 0)
            {
                Close();
            }
            else
            {
                q.ProcessError();
            }
        }

        private async Task GiveNumberAsync(int id)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "CorrugatorMachineOperatorKsh");
            q.Request.SetParam("Action", "GetNumberTask");
            q.Request.SetParam("ID_TASK", id.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);

                if (result != null) 
                {
                    var item = ListDataSet.Create(result, "ITEMS");
                    NoteBox.Text = item.Items[0].CheckGet("JS_NUM");
                }
            }
        }
    }
}
