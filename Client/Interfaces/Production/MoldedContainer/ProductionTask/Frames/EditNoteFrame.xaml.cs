using System.Threading.Tasks;
using System.Windows.Input;
using Client.Common;
using Client.Interfaces.Main;

namespace Client.Interfaces.Production.MoldedContainer
{
    public partial class EditNoteFrame : ControlBase
    {
        /// <summary>
        /// Окно для редактирования примечания в планировании
        /// </summary>
        public EditNoteFrame()
        {
            _note = "";
            _idProduct = 0;
            
            InitializeComponent();

            FrameMode = 2;
            OnGetFrameTitle = () =>
            {
                var result = $"Редактирование примечания для заявки {_idProduct}";
                return result;
            };
            
            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "save",
                        Enabled = true,
                        Title = "Сохранить",
                        ButtonUse = true,
                        ButtonName = "SaveButton",
                        Action = SaveNote
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "cancel",
                        Enabled = true,
                        Title = "Отмена",
                        ButtonUse = true,
                        ButtonName = "CancelButton",
                        Action = Close
                    });
                }
                
                Commander.Init(this);
            }
        }
        
        /// <summary>
        /// Примечание
        /// </summary>
        private string _note { get; set; }
        /// <summary>
        /// id2 тк
        /// </summary>
        private int _idProduct { get; set; }

        public void Edit(string oldNote, int schemeId)
        {
            _note = oldNote;
            _idProduct = schemeId;
            Height = 100;
            Width = 350;
            
            TaskNote.Text = _note;
            TaskNote.Focus();
            TaskNote.SelectAll();
            
            Show();
        }

        private async void SaveNote()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "MoldedContainer");
            q.Request.SetParam("Object", "Order");
            q.Request.SetParam("Action", "SaveNote");
            q.Request.SetParam("ORDER_POSITION_ID", _idProduct.ToString());
            q.Request.SetParam("NOTE", TaskNote.Text);
            
            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverName = "ProductionTaskTab",
                    Action = "order_refresh",
                    Message = $"{_idProduct}"
                });

                Close();
            }
        }

        private void TaskNote_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SaveNote();
                e.Handled = true;
            }
        }
    }
}