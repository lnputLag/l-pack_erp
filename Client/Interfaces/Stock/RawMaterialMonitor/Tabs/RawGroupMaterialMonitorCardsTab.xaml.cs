using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Stock.Elements;
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
        /// Загрузка данных для дизайна (чтобы видеть в редакторе)
        /// </summary>
        private void LoadDesignData()
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                // Тестовые данные для дизайна
                var testFormats1 = new List<MaterialFormat>
                {
                    new MaterialFormat { Format = "1000x700", QtyStock = 150 },
                    new MaterialFormat { Format = "800x600", QtyStock = 75 },
                    new MaterialFormat { Format = "1200x800", QtyStock = 200 }
                };

                var testFormats2 = new List<MaterialFormat>
                {
                    new MaterialFormat { Format = "700x500", QtyStock = 50 },
                    new MaterialFormat { Format = "600x400", QtyStock = 30 }
                };

                DesignCard1.SetData("Картон белый", testFormats1, 425);
                DesignCard2.SetData("Пленка ПВХ", testFormats2, 80);
                DesignCard3.SetData("Фольга золотая", testFormats1, 425);
            }
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
        /// </summary>
        private List<MaterialData> LoadMaterialsData()
        {
            var materials = new List<MaterialData>();

            // Здесь будет логика загрузки данных
            // Например, из вашего Grid.LoadItems()

            return materials;
        }

        /// <summary>
        /// Добавление карточки материала (аналогично PanelScore.Children.Add)
        /// </summary>
        private void AddMaterialCard(MaterialData material)
        {
            var card = new MaterialCard();
            card.SetData(material.Name, material.Formats, material.TotalQuantity);

            // Подписываемся на событие клика (как в OperatorProgressItem)
            card.OnMouseDown += (sender, e) =>
            {
                MaterialCardClicked(sender as MaterialCard, material);
            };

            CardsPanel.Children.Add(card);
        }

        /// <summary>
        /// Обработчик клика по карточке
        /// </summary>
        private void MaterialCardClicked(MaterialCard card, MaterialData material)
        {
            // Логика при клике на карточку
            MessageBox.Show($"Выбрано: {material.Name}");
        }

        /// <summary>


        /// Очистка всех карточек
        /// </summary>
        private void ClearCards()
        {
            CardsPanel.Children.Clear();
        }

        /// <summary>
        /// Класс для хранения данных материала
        /// </summary>
        public class MaterialData
        {
            public string Name { get; set; }
            public List<MaterialFormat> Formats { get; set; }
            public int TotalQuantity { get; set; }
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
    }
}
