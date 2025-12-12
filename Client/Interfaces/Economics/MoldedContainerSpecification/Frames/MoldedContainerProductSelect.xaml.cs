using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

namespace Client.Interfaces.Economics.MoldedContainer
{
    /// <summary>
    /// Фрейм выбора товара для позиции спецификации литой тары
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class MoldedContainerProductSelect : ControlBase
    {
        public MoldedContainerProductSelect()
        {
            InitializeComponent();
            InitGrid();

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "save",
                    Group = "main_form",
                    Enabled = true,
                    Title = "Выбрать",
                    Description = "Выбор изделия для позиции спецификации",
                    ButtonUse = true,
                    ButtonName = "SaveButton",
                    MenuUse = false,
                    HotKey = "Return|DoubleCLick",
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
        }
        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// ID спецификации, для которой выбирается изделие в редактируемую позицию
        /// </summary>
        public int SpecificationId;

        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=5,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="SKU_CODE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Наименование",
                    Path="PRODUCT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="ИД",
                    Path="PRODUCT_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=8,
                },
            };
            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("PRODUCT_ID");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Compact;
            Grid.SearchText = SearchText;
            Grid.Toolbar = Toolbar;
            Grid.Commands = Commander;

            Grid.OnLoadItems = LoadItems;
            Grid.AutoUpdateInterval = 0;
            Grid.OnSelectItem = (selectItem) =>
            {
                FormStatus.Text = "";
            };
            Grid.Init();
        }

        /// <summary>
        /// Загрузка содержимого таблицы
        /// </summary>
        private async void LoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Economics");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "ListProductSelect");
            q.Request.SetParam("SPECIFICATION_ID", SpecificationId.ToString());

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
                    var ds = ListDataSet.Create(result, "PRODUCTS");
                    Grid.UpdateItems(ds);
                }
            }

        }

        /// <summary>
        /// Сохранение выбора изделия
        /// </summary>
        public void Save()
        {
            if (Grid.Items != null)
            {
                if (Grid.SelectedItem != null)
                {
                    Central.Msg.SendMessage(new ItemMessage()
                    {
                        ReceiverGroup = "Economics",
                        ReceiverName = ReceiverName,
                        SenderName = ControlName,
                        Action = "ProductSelect",
                        ContextObject = Grid.SelectedItem,
                    });
                    Close();
                }
                else
                {
                    FormStatus.Text = "Выберите изделие в таблице";
                }
            }
            else
            {
                FormStatus.Text = "Нет изделий для выбора";
            }
        }

        /// <summary>
        /// Отображение вкладки
        /// </summary>
        public void Show()
        {
            string title = "Выбор изделия";
            Central.WM.Show(ControlName, title, true, "add", this);
        }

        /// <summary>
        /// Закрытие вкладки
        /// </summary>
        public void Close()
        {
            Central.WM.Close(ControlName);
            if (!ReceiverName.IsNullOrEmpty())
            {
                Central.WM.SetActive(ReceiverName);
                ReceiverName = "";
            }
        }
    }
}
