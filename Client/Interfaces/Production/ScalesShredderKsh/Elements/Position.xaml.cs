using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Preproduction;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Production.ScalesShredderKsh
{
    /// <summary>
    /// Interaction logic for Position.xaml
    /// </summary>
    public partial class Position : UserControl
    {
        public event Action SavePositions;
        public Position()
        {
            InitializeComponent();
        }

        public async void Init(Action savePositions, string place = "")
        {
            SavePositions += savePositions;
            if (place.IsNullOrEmpty())
            {
                LoadSklads();
            }
            else
            {
                string[] skladPlace = place.Split(',');
                if (skladPlace.Length == 2)
                {
                    await LoadSklads();
                    if (Sklad.Items.Contains(skladPlace[0]))
                    {
                        Sklad.SelectedItem = skladPlace[0];

                        if (Place.Items.Contains(skladPlace[1]))
                        {
                            Place.SelectedItem = skladPlace[1];
                            Place.IsDropDownOpen = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// получение списка рядов
        /// </summary>
        private async Task LoadSklads()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "ScalesShredderKsh");
            q.Request.SetParam("Action", "ListSkladsKsh");

            // склад Кашира
            q.Request.SetParam("WMWA_ID", "8");
            // зона
            q.Request.SetParam("WMZO_ID", "12");

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
                    var sklads = new List<string>();
                    var places = new List<string>();

                    var ds = ListDataSet.Create(result, "SKLADS");
                    var items = ds?.Items;
                    foreach (var item in items)
                    {
                        var sklad = item?.CheckGet("SKLAD");
                        sklads.Add(sklad);

                        var place = item?.CheckGet("NUM");
                        places.Add(place);
                    }
                    Sklad.ItemsSource = sklads;
                    Place.ItemsSource = places;
                }
            }
        }

        private void Sklad_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Sklad?.SelectedItem != null)
            {
                SavePositions();
                Place.IsDropDownOpen = true;
            }
        }

        private void Place_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Place?.SelectedItem != null)
            {
                SavePositions();
            }
        }
    }
}
