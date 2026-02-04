using Client.Common;
using Client.Interfaces.Main;
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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Остаток по сырьевым композициям на складе
    /// в карточном виде
    /// </summary>
    /// <author>kurasov_dp</author>
    public partial class RawCompositionMaterialMonitorCardsTab : ControlBase
    {
        public RawCompositionMaterialMonitorCardsTab()
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
                RefreshCompositionData();
                SetDefaults();
            };

            OnUnload = () =>
            {

            };

            OnFocusGot = () =>
            {
                
            };

            OnFocusLost = () =>
            {
                
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

        private void RefreshCompositionData()
        {
            // Очищаем панель
            ClearCards();

            var compositions = LoadCompositionsData();

            foreach (var composition in compositions)
            {
                AddMaterialCard(composition);
            }
        }



        /// <summary>
        /// Загрузка данных (из БД)
        /// В модель карточек
        /// </summary>
        private List<MaterialDataComposition> LoadCompositionsData()
        {
            var compositions = new List<MaterialDataComposition>();

            var p = new Dictionary<string, string>();

            // Выбор из выпадающего списка
            var selectedPlatform = PlatformSelectBox.SelectedItem;
            if (!selectedPlatform.Equals(default(KeyValuePair<string, string>)))
            {
                p.Add("FACTORY_ID", selectedPlatform.Key);
            }
            else
            {
                p.Add("FACTORY_ID", "1"); // Значение по умолчанию
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Stock");
            q.Request.SetParam("Object", "RawMaterialResidueMonitor");
            q.Request.SetParam("Action", "RawCompositionList");
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
                        int idc = item.CheckGet("IDC").ToInt();

                        // Поиск существующей композиции по IDC
                        if (compositions.Count(x => x.Idc == idc) > 0)
                        {
                            var comp = compositions.FirstOrDefault(x => x.Idc == idc);

                            string layerNumber = item.CheckGet("LAYER_NUMBER").ToString();
                            string rawGroup = item.CheckGet("RAW_GROUP").ToString();

                            // Поиск существующего слоя в композиции
                            if (comp.Layers.Count(x => x.LayerNumber == layerNumber && x.RawGroup == rawGroup) > 0)
                            {
                                var layer = comp.Layers.FirstOrDefault(x => x.LayerNumber == layerNumber && x.RawGroup == rawGroup);
                                layer.Widths.Add(new MaterialWidthData()
                                {
                                    Width = item.CheckGet("WIDTH").ToInt(),
                                    StockKg = item.CheckGet("STOCK_KG").ToInt()
                                });
                            }
                            else
                            {
                                // Создание нового слоя
                                var newLayer = new MaterialLayerData();
                                newLayer.LayerNumber = layerNumber;
                                newLayer.RawGroup = rawGroup;
                                newLayer.Widths.Add(new MaterialWidthData()
                                {
                                    Width = item.CheckGet("WIDTH").ToInt(),
                                    StockKg = item.CheckGet("STOCK_KG").ToInt()
                                });
                                comp.Layers.Add(newLayer);
                            }
                        }
                        else
                        {
                            // Создание новой композиции
                            var newComp = new MaterialDataComposition();
                            newComp.Idc = idc;
                            newComp.CartonName = item.CheckGet("CARTON_NAME").ToString();

                            // Создание первого слоя
                            var newLayer = new MaterialLayerData();
                            newLayer.LayerNumber = item.CheckGet("LAYER_NUMBER").ToString();
                            newLayer.RawGroup = item.CheckGet("RAW_GROUP").ToString();
                            newLayer.Widths.Add(new MaterialWidthData()
                            {
                                Width = item.CheckGet("WIDTH").ToInt(),
                                StockKg = item.CheckGet("STOCK_KG").ToInt()
                            });

                            newComp.Layers.Add(newLayer);
                            compositions.Add(newComp);
                        }
                    }

                    // Сортировка по ширине внутри каждого слоя
                    foreach (var comp in compositions)
                    {
                        foreach (var layer in comp.Layers)
                        {
                            layer.Widths = layer.Widths.OrderBy(w => w.Width).ToList();
                        }
                    }
                }
            }
            else
            {
                q.ProcessError();
            }

            return compositions;
        }

        /// <summary>
        /// Добавление карточки материала
        /// </summary>
        private void AddMaterialCard(MaterialDataComposition composition)
        {
            var materialCompositionElement = new MaterialCompositionElement();
            materialCompositionElement.SetValue(composition);
            CardName.Children.Add(materialCompositionElement);
        }

        /// <summary>
        /// Очистка всех карточек
        /// </summary>
        private void ClearCards()
        {
            CardName.Children.Clear();
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
            RefreshCompositionData();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshCompositionData();
        }
    }
}
