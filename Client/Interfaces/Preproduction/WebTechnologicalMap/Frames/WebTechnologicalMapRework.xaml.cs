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
    /// Форма выбора причины отправки техкарты на доработку
    /// Страница веб техкарт.
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class WebTechnologicalMapRework : ControlBase
    {
        public WebTechnologicalMapRework()
        {

            InitializeComponent();
            FrameMode = 2;
            FrameName = "WebTechnologicalMapRework";
            OnGetFrameTitle = () =>
            {
                return "Причина доработки";
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
                LoadReasons();
            };
        }

        public int ReasonId { get; set; }
        public int TkId { get; set; }
        public string ResiverName { get; set; }

        /// <summary>
        /// Заполнение selektbox причинами отправления на доработку
        /// </summary>
        public async void LoadReasons()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "ListReworks");

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
                    var reason = new Dictionary<string, string>();
                    var re = new Dictionary<string, string>(){};
                    foreach (var item in ds.Items)
                    {
                        reason.CheckAdd(item["ID"].ToInt().ToString(), item.CheckGet("NAME"));
                    }
                    ReasonRework.Items = reason;
                    ReasonRework.SetSelectedItemByKey("4");
                }
            }
        }

        public async void Save()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "AddRework");

            q.Request.SetParam("ID_TK", TkId.ToString());
            q.Request.SetParam("REASON", ReasonRework.SelectedItem.Key.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "WebTechnologicalMap",
                    ReceiverName = ResiverName,
                    SenderName = ControlName,
                    Message = ReasonRework.SelectedItem.Key.ToString(),
                    Action = "Rework",
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
