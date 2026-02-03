using Client.Interfaces.Stock.RawMaterialMonitor;
using DevExpress.XtraRichEdit.Fields;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Client.Interfaces.Stock
{
    /// <summary>
    /// Карточка материала для отображения в группе
    /// </summary>
    public partial class MaterialGroupElement : UserControl
    {
        public MaterialGroupElement()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Получение данных из таба и заполнение карточки
        /// Программно создали 
        /// </summary>

        public void SetValue(MaterialData materialData)
        {
            MaterialNameText.Text = materialData.Name;
            foreach (var a in materialData.MaterialDataFormats)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(60, GridUnitType.Pixel) });
                TextBlock formatNameTextBlock = new TextBlock();
                formatNameTextBlock.Text = a.Name;
                TextBlock formatQuantityTextBlock = new TextBlock();
                formatQuantityTextBlock.Text = a.QUTY.ToString();
                grid.Children.Add(formatNameTextBlock);
                grid.Children.Add(formatQuantityTextBlock);
                Grid.SetColumn(formatNameTextBlock, 0);
                Grid.SetColumn(formatQuantityTextBlock, 1);
                FormatContainer.Children.Add(grid);
            }    
        }
    }
}