using Client.Assets.HighLighters;
using Client.Common;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Interfaces.Shipments
{
    /// <summary>
    /// Управление отгрузками, вкладка "Монитор"
    /// Вспомогательный класс для отображения блока отгрузки
    /// </summary>
    /// <author>balchugov_dv</author>    
    public partial class ShipmentsMonitorItem : UserControl
    {
        public ShipmentsMonitorItem(ShipmentMonitorGridCell cell = null)
        {
            InitializeComponent();

            if (cell != null)
            {
                InitByCell(cell);
            }
        }



        public void InitByCell(ShipmentMonitorGridCell cell)
        {

            Width = cell.Len * 3;
            Id.Text = cell.Id;
            BayerName.Text = cell.BayerName;
            DriverName.Text = cell.DriverName.SurnameInitials();
            ForkliftDriverName.Text = cell.ForkliftDriverName.SurnameInitials();
            Progress.Text = $"{cell.Loaded}/{cell.ForLoading}";


            //сегмент

            if (cell.Len < 30)
            {
                BayerName.Visibility = Visibility.Collapsed;
                DriverName.Visibility = Visibility.Collapsed;
                ForkliftDriverName.Visibility = Visibility.Collapsed;
                LDriverName.Visibility = Visibility.Collapsed;
                LForkliftDriverName.Visibility = Visibility.Collapsed;
                Progress.Visibility = Visibility.Collapsed;
            }

            //тултип
            TId.Text = cell.Id;
            TShipmentStart.Text = cell.StartTime;
            TShipmentFinish.Text = cell.FinishTime;
            TBayerName.Text = cell.BayerName;

            TProductionType.Text = "";
            switch (int.Parse(cell.ProductionType))
            {
                default:
                    TProductionType.Text = "гофра";
                    break;

                case 2:
                    TProductionType.Text = "бумага";
                    break;

            }

            TShipmentType.Text = "";
            switch (int.Parse(cell.SelfShipment))
            {
                default:
                    TShipmentType.Text = "доставка";
                    break;

                case 1:
                    TShipmentType.Text = "самовывоз";
                    break;

            }

            TPackaging.Text = "";
            switch (int.Parse(cell.PackagingType))
            {
                case 1:
                    TPackaging.Text = "Паллеты";
                    Packaging.Text = "ПАЛ";
                    break;

                case 2:
                    TPackaging.Text = "Россыпью";
                    Packaging.Text = "РОС";
                    break;

                case 3:
                    TPackaging.Text = "Рулоны";
                    Packaging.Text = "РУЛ";
                    break;

            }

            TDriver.Text = $"{cell.DriverName}";
            TTerminal.Text = $"{cell.TerminalTitle}";
            TForkliftDriver.Text = cell.ForkliftDriverName.SurnameInitials();
            TProgress.Text = $"{cell.Loaded}/{cell.ForLoading}";



            //отладочная информация
            if (Central.DebugMode)
            {
                //element.TDebug.Text=$"{cell.StartTime}-{cell.FinishTime} ({cell.Len})  t:{cell.TerminalNumber} \n {cell.BayerName} \n x:{time}";
                TDebug.Text = "";
                TDebug.Visibility = Visibility.Visible;
            }
            else
            {
                TDebug.Visibility = Visibility.Collapsed;
            }


            TShipmentFinish.Visibility = Visibility.Collapsed;
            LShipmentFinish.Visibility = Visibility.Collapsed;

            string bgColor;
            switch (cell.Status)
            {
                default:
                    bgColor = HColor.White;
                    TStatus.Text = "внешние операции";
                    break;

                case 2:
                    bgColor = HColor.Blue;
                    TStatus.Text = "отгрузка";
                    break;

                case 3:
                    bgColor = HColor.Green;
                    TStatus.Text = "отгружено";
                    TShipmentFinish.Visibility = Visibility.Visible;
                    LShipmentFinish.Visibility = Visibility.Visible;
                    break;
            }
            var bc = new BrushConverter();
            var brush = (Brush)bc.ConvertFrom(bgColor);
            MonBlock.Background = brush;
        }

        private void ContextMenu_View_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var item = (MenuItem)sender;
            if (item.CommandParameter != null)
            {
                if (!string.IsNullOrEmpty(item.CommandParameter.ToString()))
                {
                }
            }
        }
    }
}
