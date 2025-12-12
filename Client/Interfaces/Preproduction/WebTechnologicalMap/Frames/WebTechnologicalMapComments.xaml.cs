using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Accounts;
using Client.Interfaces.Main;
using DevExpress.DirectX.StandardInterop.Direct2D;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using SharpVectors.Dom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма отображения комментариев по техкарте
    /// Страница веб-техкарты.
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class WebTechnologicalMapComments : ControlBase
    {
        public WebTechnologicalMapComments()
        {

            InitializeComponent();
            FrameMode = 2;
            FrameName = "WebTechnologicalMapComments";
            OnGetFrameTitle = () =>
            {
                return "Примечания";
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
                LoadComments();
            };
        }

        public int TkId { get; set; }
        // Примечания для какой группы пользователей будут отображаться
        // 1 - Инженеры, 2 - Дизайнеры, 3 - Конструкторы, 4 - Менеджеры
        public int ObjectType { get; set; }
        public string ReceiverName { get; set; }

        /// <summary>
        /// Заполнение selektbox причинами отправления на доработку
        /// </summary>
        public async void LoadComments()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "GetComments");

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
                    if (ds.Items.Count > 0)
                    {
                        var first = ds.Items.First();
                        ClientNote.Text = first.CheckGet("NOTE");
                        ClientComment.Text = first.CheckGet("COMMENTS");
                        switch (ObjectType)
                        {
                            case 2:
                                OppNote.Text = first.CheckGet("DRAWER_NOTE");
                                SaveButton.Visibility = Visibility.Collapsed;
                                break;
                            case 1:
                                OppNote.Text = first.CheckGet("DESIGNER_NOTE");
                                SaveButton.Visibility = Visibility.Collapsed;
                                break;

                        }
                    }
                }
            }
        }

        public async void Save()
        {
            Close();
        }
        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
        }

    }
}
