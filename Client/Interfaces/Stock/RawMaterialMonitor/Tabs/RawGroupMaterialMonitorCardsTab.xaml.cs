using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock;
using Client.Interfaces.Stock.RawMaterialMonitor;
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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static DevExpress.Data.Filtering.Helpers.SubExprHelper.ThreadHoppingFiltering;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Остаток по сырьевым группам на складе
    /// в карточном виде
    /// </summary>
    /// <author>kurasovdp</author>
    public partial class RawGroupMaterialMonitorCardsTab : ControlBase
    {
        public RawGroupMaterialMonitorCardsTab()
        {
            InitializeComponent();

            RoleName = "[erp]raw_material_monitor";
            ControlTitle = "Монитор остатков сырья";
            DocumentationUrl = "/doc/l-pack-erp";

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (System.Windows.Input.KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            OnLoad = () =>
            {
                //RawGroupTableGridInit();
                SetDefaults();
            };

            OnUnload = () =>
            {
                //RawGroupTableGrid.Destruct();
            };

            OnFocusGot = () =>
            {
                //RawGroupTableGrid.ItemsAutoUpdate = true;
                //RawGroupTableGrid.Run();
            };

            OnFocusLost = () =>
            {
                // RawGroupTableGrid.ItemsAutoUpdate = false;
            };

            ///<summary>
            /// Система команд (Commander)
            ///</summary>
            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "help",
                        Enabled = true,
                        Title = "Справка",
                        Description = "Показать справочную информацию",
                        MenuUse = true,
                        ButtonUse = true,
                        ButtonName = "HelpButton",
                        HotKey = "F1",
                        Action = () =>
                        {
                            Central.ShowHelp(DocumentationUrl);
                        },
                    });
                }
            }
            Commander.Init(this);
        }

        

        /// <summary>
        /// Основной метод загрузки данных
        /// </summary>
        private void RefreshData()
        {
            // Очищаем панель
            ClearCards();

            // Загружаем данные
            var materials = LoadMaterialsData();

            // Создаем карточки
            foreach (var material in materials)
            {
                AddMaterialCard(material);
            }
        }

        /// <summary>
        /// Загрузка данных материалов (из БД или API)
        /// В модель карточек
        /// </summary>
        private List<MaterialData> LoadMaterialsData()
        {
            var materials = new List<MaterialData>();

            var p = new Dictionary<string, string>();
            p.Add("FACTORY_ID", $"1");

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "RawMaterialResidueMonitor");
            q.Request.SetParam("Action", "RawGroupList");
            q.Request.SetParams(p);
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "ITEMS");
                    foreach (var item in ds.Items)
                    {
                        if (materials.Count(x => x.IdRawGroup == item.CheckGet("ID_RAW_GROUP").ToInt()) > 0) 
                        {
                            var m = materials.FirstOrDefault(x => x.IdRawGroup == item.CheckGet("ID_RAW_GROUP").ToInt());
                            m.MaterialDataFormats.Add(new MaterialDataFormat() { Name = item.CheckGet("FORMAT"), QUTY = item.CheckGet("QTY_STOCK_ONLY").ToInt()});
                        }
                        else
                        {
                            var m = new MaterialData();
                            m.IdRawGroup = item.CheckGet("ID_RAW_GROUP").ToInt();
                            m.Name = item.CheckGet("NAME");
                            m.MaterialDataFormats.Add(new MaterialDataFormat() { Name = item.CheckGet("FORMAT"), QUTY = item.CheckGet("QTY_STOCK_ONLY").ToInt() });
                            materials.Add(m);
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            return materials;

        }

        /// <summary>
        /// Добавление карточки материала (аналогично PanelScore.Children.Add)
        /// </summary>
        private void AddMaterialCard(MaterialData material)
        {
            var materialGroupElement = new MaterialGroupElement();
            materialGroupElement.SetValue(material);
            CardName.Children.Add(materialGroupElement);
        }

       

        /// <summary>
        /// Очистка всех карточек
        /// </summary>
        private void ClearCards()
        {
            CardName.Children.Clear();
        }

       

        // Обработчики событий
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        public void SetDefaults()
        {
            PlatformSelectBox.SetItems(new Dictionary<string, string>()
            {
                {"1",  "Липецк"},
                {"2",  "Кашира"},
            });
            PlatformSelectBox.SelectedItem = PlatformSelectBox.Items.First();
        }

        private void PlatformSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        private void FormatSelectBox_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }
    }
}
