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
    /// Форма редактирования менеджера
    /// Страница веб-техкарт.
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class WebTechnologicalMapChangeManager : ControlBase
    {
        public WebTechnologicalMapChangeManager()
        {

            InitializeComponent();
            FrameMode = 2;
            FrameName = "WebTechnologicalMapChangeManager";
            OnGetFrameTitle = () =>
            {
                return "Менеджер";
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
                    Title = "Выбрать",
                    Description = "Выбрать",
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
                LoadEmploes();
            };
        }

        public int ManagerId { get; set; }
        public int TkId { get; set; }
        public string ReciverName { get; set; }

        /// <summary>
        /// Заполнение selektbox менеджерами
        /// </summary>
        public async void LoadEmploes()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "ListEmployesForChangeNameUser");

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
                    var employes = new Dictionary<string, string>();
                    employes.Add("WEB", "Web-аккаунт");
                    foreach (var item in ds.Items)
                    {
                        employes.CheckAdd(item.CheckGet("LOGIN"), item.CheckGet("FIO"));
                    }
                    EmployesSelectBox.Items = employes;
                    EmployesSelectBox.SetSelectedItemByKey("WEB");
                }
            }
        }

        public async void Save()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "SetNameUser");

            q.Request.SetParam("ID_TK", TkId.ToString());
            q.Request.SetParam("NAME_USER", EmployesSelectBox.SelectedItem.Key.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "WebTechnologicalMap",
                    ReceiverName = ReciverName,
                    SenderName = ControlName,
                    Message = "",
                    Action = "Refresh",
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
