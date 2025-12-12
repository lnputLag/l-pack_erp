using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Блок с данными по сырьевой группе, который динамически создаётся в ходе работы с интерфейсом
    /// </summary>
    /// <author>sviridov_ae</author>
    public partial class RawGroupBlock : UserControl
    {
        /// <summary>
        /// Конструктор для блока с данными по сырьевой группе
        /// </summary>
        /// <param name="id">Ид блока</param>
        /// <param name="dt">Дата для размещения блока в нужную колонку</param>
        /// <param name="widthPaper">Ширина бумаги</param>
        public RawGroupBlock(RawMaterialGroupList.RawMaterialGroup rawMaterialGroup, int id, string dt, int paperWidth = 0)
        {
            InitializeComponent();

            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            Id = id;
            Dt = dt;
            PaperWidth = paperWidth;
            Checked = false;

            RawMaterialGroup = rawMaterialGroup;
            Blocked = RawMaterialGroup.BlockedFlag;
            // Если при загрузке данных сырьевая группа уже заблокированна,
            // то отмечаем чекбокс, чтобы в дальнейшем была возможномсть снять чекбокс и сохранить данные, тем самым разблокировав группу в базе
            if (Blocked)
            {
                Checked = true;
                CheckBox.IsChecked = true;
            }

            //int val = 1234567890;
            //var v1 = val.ToString("#,###,###,###");
        }
 
        /// <summary>
        /// ИД сырьевой группы (id_raw_group)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Дата (для расположения блока в нужную колонку)
        /// </summary>
        public string Dt { get; set; }

        /// <summary>
        /// Ширина бумаги
        /// </summary>
        public int PaperWidth { get; set; }

        /// <summary>
        /// Статус нажатия чекбокса
        /// </summary>
        public bool Checked { get; set; }

        /// <summary>
        /// Условный фокус мышью
        /// </summary>
        public bool MouseFocus { get; set; }

        /// <summary>
        /// Флаг того, что эта сырьевая группа заблокированна
        /// </summary>
        public bool Blocked { get; set; }

        /// <summary>
        /// (Пока что не заполняется. Если будет необходимость, то поменять запрос на получение данных)Идентификатор из таблицы raw_group_roll_ref.ragr_id
        /// </summary>
        public int IdGroupRoll { get; set; }

        public RawMaterialGroupList.RawMaterialGroup RawMaterialGroup { get; set; }

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        /// <summary>
        /// Обновляем флаг блокировки. Устанавливаем его значение таким же как в связанном объекте RawMaterialGroup
        /// </summary>
        public void UpdateBlockedFlag()
        {
            Blocked = RawMaterialGroup.BlockedFlag;
        }
        
        /// <summary>
        /// Получить список заявок
        /// </summary>
        public void GetListOrder()
        {
            Dictionary<string, string> value = new Dictionary<string, string>();
            value.CheckAdd("DT", Dt);
            value.CheckAdd("ID", Id.ToString());
            value.CheckAdd("WIDTH", PaperWidth.ToString());

            Messenger.Default.Send(new ItemMessage()
            {
                ReceiverGroup = "Preproduction",
                ReceiverName = "RawMaterialGroupList",
                SenderName = "RawGroupBlock",
                Action = "GetListOrder",
                Message = "",
                ContextObject = value,
            }
            );
        }

        /// <summary>
        /// Устанавливает стиль грида
        /// </summary>
        public void SetGridStyle()
        {
            if (MouseFocus)
            {
                MainGrid.Style = (Style)MainGrid.TryFindResource("DISelectedBlock");
            }
            else if (Checked)
            {
                MainGrid.Style = (Style)MainGrid.TryFindResource("DIClickedBlock");
            }
            else
            {
                MainGrid.Style = (Style)MainGrid.TryFindResource("DIGrid");
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (!(bool)CheckBox.IsChecked && Blocked)
            {
                string msg = $"Данная сырьевая группа была заблокирована в базе. Вы уверены, что хотите разблокировать её?";
                var d = new DialogWindow($"{msg}", "Разблокирование сырьевой группы", "", DialogWindowButtons.NoYes);
                if (d.ShowDialog() == true)
                {
                    Checked = (bool)CheckBox.IsChecked;
                    SetGridStyle();
                }
                else
                {
                    CheckBox.IsChecked = !(bool)CheckBox.IsChecked;
                }
            }
            else
            {
                Checked = (bool)CheckBox.IsChecked;
                SetGridStyle();
            }

            //Dictionary<string, string> value = new Dictionary<string, string>();
            //value.CheckAdd("DT", Dt);
            //value.CheckAdd("ID", Id.ToString());
            //value.CheckAdd("WIDTH", PaperWidth.ToString());

            //Messenger.Default.Send(new ItemMessage()
            //{
            //    ReceiverGroup = "Preproduction",
            //    ReceiverName = "RawMaterialGroupList",
            //    SenderName = "RawGroupBlock",
            //    Action = "Check",
            //    Message = "",
            //    ContextObject = value,
            //}
            //);
        }

        private void TopTextBlock_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TopTextBlock.Text.ToInt() < 0)
            {
                // Красный
                var color = "#ff0000";
                var brush = color.ToBrush();
                TopTextBlock.Foreground = brush;
            }
            else if (TopTextBlock.Text.ToInt() > 0)
            {
                // Чёрный
                var color = "#000000";
                var brush = color.ToBrush();
                TopTextBlock.Foreground = brush;
            }
        }

        private void MainGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            MouseFocus = true;
            SetGridStyle();

            ContextMenu contextMenu = new ContextMenu();
            var menuItem = new MenuItem { Header = "Список заявок", IsEnabled = true };
            menuItem.Click += (object sender, RoutedEventArgs e) =>
            {
                GetListOrder();
            };
            contextMenu.Items.Add(menuItem);
            contextMenu.IsOpen = true;

            e.Handled = true;
        }

        private void MainGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            MouseFocus = false;
            SetGridStyle();
        }

        private void MainGrid_Loaded(object sender, RoutedEventArgs e)
        {
            SetGridStyle();
        }
    }
}
