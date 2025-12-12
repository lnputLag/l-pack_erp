using Client.Assets.HighLighters;
using Client.Common;
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

namespace Client.Interfaces.DeliveryAddresses
{
    /// <summary>
    /// Форма редактирования времени работы склада
    /// </summary>
    /// <author>motenko_ek</author>
    public partial class ShippingAddressScheduleTmForm : ControlBase
    {
        public ShippingAddressScheduleTmForm(Dictionary<string, string> tm)
        {
            InitializeComponent();

            Tm = tm;

            WorkFlag.Checked += (object sender, RoutedEventArgs e) =>
            {
                BeginTm.Text = "00:00";
                EndTm.Text = "23:59";
            };
            WorkFlag.Unchecked += (object sender, RoutedEventArgs e) =>
            {
                BeginTm.Text = "";
                EndTm.Text = "";
            };
            WorkFlag.IsChecked = Tm.CheckGet("WORK_FLAG").ToInt() > 0;
            BeginTm.Text = Tm.CheckGet("BEGIN_TM");
            EndTm.Text = Tm.CheckGet("END_TM");

            DocumentationUrl = "/doc/l-pack-erp/delivery/delivery_addresses/delivery_to_customer";
            FrameTitle = $"{Tm.CheckGet("DAWE_NAME")}.";
            FrameMode = 2;
            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };


            Commander.Add(new CommandItem()
            {
                Name = "save",
                Enabled = true,
                Title = "Сохранить",
                Description = "",
                ButtonUse = true,
                ButtonName = "SaveButton",
                HotKey = "Ctrl+Return",
                Action = () =>
                {
                    Save();
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "cancel",
                Enabled = true,
                Title = "Отмена",
                Description = "",
                ButtonUse = true,
                ButtonName = "CancelButton",
                HotKey = "Escape",
                Action = () =>
                {
                    Close();
                },
            });
            Commander.Init(this);

            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            Central.WM.FrameMode = FrameMode;
            Central.WM.Show(GetFrameName(), FrameTitle, true, "ShipAdresForm", this, "", windowParametrs);
        }

        private Dictionary<string, string> Tm;

        public void Save()
        {
            Tm["WORK_FLAG"] = WorkFlag.IsChecked.ToInt().ToString();
            Tm["BEGIN_TM"] = BeginTm.EditValue != null ? BeginTm.Text : "";
            Tm["END_TM"] = EndTm.EditValue != null ? EndTm.Text : "";
            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverName = "ShippingAddressForm",
                SenderName = ControlName,
                Action = "shipping_address_schedule_refresh",
            });
            Close();
        }
    }
}
