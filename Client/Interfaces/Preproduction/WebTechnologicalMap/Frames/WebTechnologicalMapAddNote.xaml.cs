using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Accounts;
using Client.Interfaces.Main;
using Client.Interfaces.Sales;
using DevExpress.DirectX.StandardInterop.Direct2D;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using SharpVectors.Dom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма добавления примечания для дизайнера или конструктора
    /// Страница веб-техкарты.
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class WebTechnologicalMapAddNote : ControlBase
    {
        public WebTechnologicalMapAddNote()
        {

            InitializeComponent();
            FrameMode = 2;
            FrameName = "WebTechnologicalMapAddNote";
            OnGetFrameTitle = () =>
            {
                return "Примечание";
            };
            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }

            };
            Commander.SetCurrentGroup("item");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Сохранить",
                    Description = "Сохранить",
                    ButtonUse = true,
                    ButtonName = "SaveButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Save();

                    },
                });
                Commander.Add(new CommandItem()
                {
                    Name = "close",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Отмена",
                    Description = "Закрыть форму без сохранения",
                    ButtonUse = true,
                    ButtonName = "CancelButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Close();
                    },
                });
            }
            Commander.Init(this);
            OnLoad = () =>
            {
                if (Type == 1)
                {
                    RecipientNote.Text = "Примечание для дизайнера : ";
                }
                else if (Type == 2)
                {
                    RecipientNote.Text = "Примечание для конструктора : ";
                }
                LoadNotes();
            };
        }

        public int Type { get; set; }
        public int TkId { get; set; }
        public string ReceiverName { get; set; }

        /// <summary>
        /// Заполнение selektbox примечаниями 
        /// </summary>
        public async void LoadNotes()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "ListNotes");

            q.Request.SetParam("ID_TK", TkId.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");

                    if (Type == 1)
                    {
                        CommentTextBox.Text = ds.Items[0].CheckGet("DESIGNER_NOTE");
                    }
                    else if (Type == 2)
                    {
                        CommentTextBox.Text = ds.Items[0].CheckGet("DRAWER_NOTE");
                    }
                }
            }
        }

        public async void Save()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "UpdateNotes");

            q.Request.SetParam("ID_TK", TkId.ToString());
            q.Request.SetParam("TYPE", Type.ToString());
            q.Request.SetParam("NOTE", CommentTextBox.Text.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "WebTechnologicalMap",
                    ReceiverName = ReceiverName,
                    SenderName = ControlName,
                    Message = Type.ToString(),
                    Action = "UpdateStatusDesignOrDrawing",
                });
            }
            Close();
        }
        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
        }

    }
}
