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
    /// Форма выбора товара для создания или редактирования схемы производства
    /// Страница схем производств.
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class MoldedContainerRework : ControlBase
    {
        public MoldedContainerRework()
        {

            InitializeComponent();
            FrameMode = 2;
            FrameName = "MoldedContainerRework";
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
            var status = new Dictionary<string, string>()
            {
                { "4", "Изменение дизайна" },
                { "5", "Неполные / некорректные данные" },
                { "6", "Другое" }
            };
            ReasonRework.Items = status;
            ReasonRework.SetSelectedItemByKey("4");
        }

        public async void Save()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "AddRework");

            q.Request.SetParam("TK_ID", TkId.ToString());
            q.Request.SetParam("REASON", ReasonRework.SelectedItem.Key.ToString());

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var m = new Dictionary<string, string>()
                {
                    {"REASON", ReasonRework.SelectedItem.Key.ToString()},
                    {"ID", TkId.ToString()}
                };

                Central.Msg.SendMessage(new ItemMessage()
                {
                    ReceiverGroup = "MoldedContainerTechCard",
                    ReceiverName = ResiverName,
                    SenderName = ControlName,
                    Message = JsonConvert.SerializeObject(m),
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
