using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Логика взаимодействия для BuyerSelect.xaml
    /// </summary>
    public partial class BuyerSelect : ControlBase
    {
        public BuyerSelect()
        {
            InitializeComponent();
            GridInit();

            SearchString = "";
            SelectedBuyerId = 0;

            OnLoad = () =>
            {
                
            };

            OnUnload = () =>
            {
                Grid.Destruct();
            };
        }

        /// <summary>
        /// Строка для поиска покупателей или партнеров
        /// </summary>
        private string SearchString;
        /// <summary>
        /// Имя вкладки, откуда вызвана форма и куда передается ответ
        /// </summary>
        public string ReceiverName;

        public int SelectedBuyerId;

        public void ProcessCommand(string command, ItemMessage m = null)
        {
            command = command.ClearCommand();
            if (!command.IsNullOrEmpty())
            {
                switch (command)
                {
                    case "close":
                        Close();
                        break;
                    case "save":
                        Save();
                        break;
                }
            }
        }

        private void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="Ид",
                    Path="ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Название",
                    Path="BUYER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Партнер",
                    Path="PARTNER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
                new DataGridHelperColumn
                {
                    Header="Потребитель",
                    Path="CUSTOMER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=15,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("ID");
            Grid.SetSorting("ID", ListSortDirection.Descending);
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            Grid.AutoUpdateInterval = 0;

            Grid.OnLoadItems = GetData;
            Grid.Init();
        }

        /// <summary>
        /// Получение данных из БД
        /// </summary>
        private async void GetData()
        {
            if (!SearchString.IsNullOrEmpty())
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Products");
                q.Request.SetParam("Object", "Assortment");
                q.Request.SetParam("Action", "ListBuyer");
                q.Request.SetParam("SEARCH", SearchString);

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
                        var ds = ListDataSet.Create(result, "BUYERS");
                        Grid.UpdateItems(ds);
                    }
                }
            }
        }

        /// <summary>
        /// Открытие формы
        /// </summary>
        /// <param name="filterString"></param>
        public void ShowForm(string filterString)
        {
            if (!filterString.IsNullOrEmpty())
            {
                SearchString = filterString;
                Show();
            }
        }

        /// <summary>
        /// Показывает форму списка покупателей
        /// </summary>
        public void Show()
        {
            ControlName = $"AssortmentSelectBuyers";
            ControlTitle = $"Покупатели";

            Central.WM.Show(ControlName, ControlTitle, true, "add", this);
        }

        /// <summary>
        /// Закрывает форму выбора покупателя
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            Central.WM.SetActive(ReceiverName);
        }

        /// <summary>
        /// Передаем выбранного покупателя в ассортимент
        /// </summary>
        public void Save()
        {
            if (Grid.Items != null)
            {
                if (Grid.Items.Count > 0)
                {
                    if (Grid.SelectedItem != null)
                    {
                        SelectedBuyerId = Grid.SelectedItem.CheckGet("ID").ToInt();
                    }
                    else
                    {
                        SelectedBuyerId = Grid.Items[0].CheckGet("ID").ToInt();
                    }

                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Preproduction",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "Selected",
                        ContextObject = SelectedBuyerId.ToString()
                    }); ;
                    Close();
                }
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            if (b != null)
            {
                var t = b.Tag.ToString();
                ProcessCommand(t);
            }
        }
    }
}
