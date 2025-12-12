using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.DirectX.StandardInterop.Direct2D;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.Xpo.DB.Helpers;
using DevExpress.XtraPrinting.Native;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.Native;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using iTextSharp.text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using NPOI.Util;
using Org.BouncyCastle.Crypto;
using SharpVectors.Dom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static DevExpress.XtraPrinting.Native.ExportOptionsPropertiesNames;
using static NPOI.HSSF.Util.HSSFColor;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Security.Policy;
using System.IO;
using DevExpress.Xpf.Core.Native;
using System.Windows.Media.Effects;
using System.Windows.Media;
using DevExpress.Utils.Html;
using static iTextSharp.text.pdf.qrcode.Version;
using ICSharpCode.SharpZipLib.GZip;
using NPOI.SS.Util;

namespace Client.Interfaces.DeliveryAddresses
{
    public class DBAddress
    {
        public int AddrId;
        public string ZipNum;
        public string Region;
        public string District;
        public string City;
        public string Street;
        public string Building;
        public string FullAddress;
        public string Longtitude;
        public string Latitude;
        public string Code;
        public string Okato;
        public string Country;
        public string Room;
        public string Distance;
    }
    /// <summary>
    /// Форма редактирования адреса
    /// </summary>
    /// <author>motenko_ek</author>
    public partial class AddressForm : ControlBase
    {
        //Четыре способа ввсести информацию: полный адрес, по подсказкам(включая зип), координаты, карта.
        //Карта и координаты жестко связаны, изменение в одном изменяет другое т.к. состояние карты сохраняется только как координаты
        //Изменение расстояния ни на что не влияет. Пользователь может подкорректировать расстояние в ручную, составив свой маршрут.
        //Если кликаем мышкой на карте то прилетает коорд и дист. Если устанавливаем SetCenter то прилетает только дист.

        public AddressForm(DBAddress address)
        {
            InitializeComponent();

            DBAddress = address;

            Loaded += AddressFormLoaded;

            FullAddressTextBox.TextChanged += FullAddressTextBoxTextChanged;

            ZipNumTextBox.TextChanged += ZipNumTextBoxTextChanged;

            CountryComboBox.StaysOpenOnEdit = true;
            CountryComboBox.IsTextSearchEnabled = false;
            CountryComboBox.IsEditable = true;
            CountryComboBox.GotFocus += ComboBoxGotFocus;
            CountryComboBox.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(CountryComboBoxTextChanged));
            CountryComboBox.DisplayMemberPath = "FullName";

            RegionComboBox.StaysOpenOnEdit = true;
            RegionComboBox.IsTextSearchEnabled = false;
            RegionComboBox.IsEditable = true;
            RegionComboBox.GotFocus += ComboBoxGotFocus;
            RegionComboBox.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(RegionComboBoxTextChanged));
            RegionComboBox.DisplayMemberPath = "FullName";

            DistrictComboBox.StaysOpenOnEdit = true;
            DistrictComboBox.IsTextSearchEnabled = false;
            DistrictComboBox.IsEditable = true;
            DistrictComboBox.GotFocus += ComboBoxGotFocus;
            DistrictComboBox.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(DistrictComboBoxTextChanged));
            DistrictComboBox.DisplayMemberPath = "FullName";

            CityComboBox.StaysOpenOnEdit = true;
            CityComboBox.IsTextSearchEnabled = false;
            CityComboBox.IsEditable = true;
            CityComboBox.GotFocus += ComboBoxGotFocus;
            CityComboBox.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(CityComboBoxTextChanged));
            CityComboBox.DisplayMemberPath = "FullName";

            StreetComboBox.StaysOpenOnEdit = true;
            StreetComboBox.IsTextSearchEnabled = false;
            StreetComboBox.IsEditable = true;
            StreetComboBox.GotFocus += ComboBoxGotFocus;
            StreetComboBox.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(StreetComboBoxTextChanged));
            StreetComboBox.DisplayMemberPath = "FullName";

            BuildingComboBox.StaysOpenOnEdit = true;
            BuildingComboBox.IsTextSearchEnabled = false;
            BuildingComboBox.IsEditable = true;
            BuildingComboBox.GotFocus += ComboBoxGotFocus;
            BuildingComboBox.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(BuildingComboBoxTextChanged));
            BuildingComboBox.DisplayMemberPath = "FullName";

            RoomTextBox.TextChanged += RoomTextBoxTextChanged;

            LatitudeTextBox.TextChanged += CoordTextChanged;

            LongitudeTextBox.TextChanged += CoordTextChanged;

            WebView.WebMessageReceived += WebViewWebMessageReceived;
            WebView.NavigationStarting += WebViewNavigationStarting;

            DaDataClient = new HttpClient()
            {
                //BaseAddress = new Uri("http://suggestions.dadata.ru"),
                Timeout = new TimeSpan(0, 0, 2),
            };
            DaDataClient.DefaultRequestHeaders.Add("Authorization", "Token 5b6460149b99cff3d899e914a50374276d2c3266");
            DaDataClient.DefaultRequestHeaders.Add("X-Secret", "a4668714705c23b40c366cfc2a3b93c59ceb2425");

            //YandexClient = new HttpClient()
            //{
            //    BaseAddress = new Uri("https://geocode-maps.yandex.ru"),
            //    Timeout = new TimeSpan(0, 0, 5),
            //};

            RoleName = "[erp]delivery_addresses";
            DocumentationUrl = "/doc/l-pack-erp/delivery/delivery_addresses/delivery_to_customer";
            FrameMode = 2;
            FrameTitle = "Новый адрес";
            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="FULL_ADDRESS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=FullAddressTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="LATITUDE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=LatitudeTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="LONGTITUDE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=LongitudeTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
            };
            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = StatusBar;
            Form.SetDefaults();

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

        private struct Address
        {
            public string Id { get; set; }// Код ФИАС объекта
            public string Okato { get; set; }// Код ОКАТО (КЛАДР) объекта
            public string Zip { get; set; }// индекс объекта
            public string Name { get; set; }// Название объекта
            public string Type { get; set; }// Тип объекта полностью
            public string TypeShort { get; set; }// Тип объекта коротко
            public string ContentType { get; set; }// Тип возвращаемого объекта (region, district, city, street, building)
            public string FullName { get; set; }
            public string Lat { get; set; }
            public string Lon { get; set; }
            public string FullAddress { get; set; }
            public bool FlagCity { get; set; }
        }

        private DBAddress DBAddress;

        /// <summary>
        /// процессор форм
        /// </summary>
        private FormHelper Form { get; set; }

        private static HttpClient DaDataClient;
        //private static HttpClient YandexClient;
        //private static string YandexUrl = "1.x/?apikey=0ce7bc34-d4d3-448a-ba1e-8d673d87bb2e&geocode=";

        //Если пользователь воспользовался источником данных то больше его не обновляем до тех пор пока пользователь не удалит то что он ввел.
        private bool FullAddressUsed = false;
        private bool SuggestionsUsed = false;
        private bool CoordinatesUsed = false;

        private void AddressFormLoaded(object sender, RoutedEventArgs e)
        {
            //Инициализация данных
            RecBlock++;
            FullAddressTextBox.Text = DBAddress.FullAddress;
            CodeTextBox.Text = DBAddress.Code;
            OkatoTextBox.Text = DBAddress.Okato;
            ZipNumTextBox.Text = DBAddress.ZipNum;
            if (DBAddress.AddrId == 0 && DBAddress.Country == null) CountryComboBox.Text = "Россия";
            else CountryComboBox.Text = DBAddress.Country;
            RegionComboBox.Text = DBAddress.Region;
            DistrictComboBox.Text = DBAddress.District;
            CityComboBox.Text = DBAddress.City;
            StreetComboBox.Text = DBAddress.Street;
            BuildingComboBox.Text = DBAddress.Building;
            RoomTextBox.Text = DBAddress.Room;
            LongitudeTextBox.Text = DBAddress.Longtitude;
            LatitudeTextBox.Text = DBAddress.Latitude;
            DistanceTextBox.Text = DBAddress.Distance;
            RecBlock--;
        }

        private void WebViewNavigationStarting(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.Uri == "about:blank")
            {
                var uri = new Uri("pack://application:,,,/Assets/Documents/Delivery/DeliveryAddresses/MapsYandex.html");
                var stream = Application.GetResourceStream(uri).Stream;
                StreamReader reader = new StreamReader(stream);
                string text = reader.ReadToEnd();
                WebView.CoreWebView2.NavigateToString(text);
            }
        }







        private async Task<JArray?> GetSuggestions(string jasonString)
        {
            string jsonResponse;

            try
            {
                using var content = new StringContent(jasonString, Encoding.UTF8, "application/json");
                using var response = await DaDataClient.PostAsync("https://suggestions.dadata.ru/suggestions/api/4_1/rs/suggest/address", content);
                if (!response.IsSuccessStatusCode) return null;
                jsonResponse = await response.Content.ReadAsStringAsync();
            }
            catch { return null; }

            var suggestions = JsonConvert.DeserializeObject<JObject>(jsonResponse)["suggestions"];

            if(suggestions != null && suggestions is JArray) return (JArray)suggestions;

            return null;
        }
        private async Task<JArray?> GetAddressDaData(string latitude, string longitude)
        {
            string jsonResponse;

            try
            {
                var str = "{ \"lat\": " + latitude + ", \"lon\": " + longitude + ", \"count\": 1 }";
                using var content = new StringContent(str, Encoding.UTF8, "application/json");
                using var response = await DaDataClient.PostAsync("https://suggestions.dadata.ru/suggestions/api/4_1/rs/geolocate/address", content);
                if (!response.IsSuccessStatusCode) return null;
                jsonResponse = await response.Content.ReadAsStringAsync();
            }
            catch { return null; }

            var suggestions = JsonConvert.DeserializeObject<JObject>(jsonResponse)["suggestions"];

            if (suggestions != null && suggestions is JArray) return (JArray)suggestions;

            return null;
        }
        private async Task<JArray?> GetAddressDaData(string address)
        {
            string jsonResponse;

            try
            {
                var str = "[ \"" + address + "\"]";
                using var content = new StringContent(str, Encoding.UTF8, "application/json");
                using var response = await DaDataClient.PostAsync("https://cleaner.dadata.ru/api/v1/clean/address", content);
                if (!response.IsSuccessStatusCode) return null;
                jsonResponse = await response.Content.ReadAsStringAsync();
            }
            catch { return null; }

            var suggestions = JsonConvert.DeserializeObject<JArray>(jsonResponse);

            if (suggestions != null && suggestions is JArray) return (JArray)suggestions;

            return null;
        }
        //private async Task<string?> GetAddressYandex(string latitude, string longitude)
        //{
        //    string stringResponse;

        //    try
        //    {
        //        using var response = await YandexClient.GetAsync(YandexUrl + latitude + "," + longitude + "&results=1");
        //        if (!response.IsSuccessStatusCode) return null;
        //        stringResponse = await response.Content.ReadAsStringAsync();
        //    }
        //    catch { return null; }

        //    stringResponse.Remove(stringResponse.IndexOf("</text>"));
        //    stringResponse.Remove(0, stringResponse.IndexOf("<text>")+6);

        //    return stringResponse;
        //}
        //private async void ComboBoxLostFocus(object sender, RoutedEventArgs e)
        //{
        //    ComboBox cb = sender as ComboBox;

        //    if (cb.SelectedIndex == -1)
        //    {
        //        if(cb.Items.Count > 0) cb.SelectedIndex = 0;

        //        if (cb.SelectedItem != null)
        //        {
        //            Address address = (Address)cb.SelectedItem;

        //            var zoom = 16;
        //            if (address.ContentType == "country") zoom = 3;

        //            var lat = address.Lat;
        //            var lon = address.Lon;
        //            if (lat.IsNullOrEmpty() || lon.IsNullOrEmpty())
        //            {
        //                await WebView.ExecuteScriptAsync($"setCenterA('{address.FullName}', {zoom})");
        //            }
        //            else
        //            {
        //                LatitudeTextBox.Text = lat.ToString();
        //                LongitudeTextBox.Text = lon.ToString();
        //                await WebView.ExecuteScriptAsync($"setCenterC([{lat}, {lon}], {zoom});");
        //            }
        //        }
        //    }
        //}
        private void ComboBoxGotFocus(object sender, RoutedEventArgs e)
        {
            ((ComboBox)sender).IsDropDownOpen = true;
        }
        //private void ComboBoxPreviewKeyDown(object sender, KeyEventArgs e)
        //{
        //    var cb = sender as ComboBox;

        //    //Обрабатываем заранее переход по полям кнопкой таб, т.к. ComboBoxLostFocus
        //    //приходит только после того как система определит следующий контрол для фокуса
        //    //соответственно проскакивают контролы которые отключены на момент входа в ComboBoxLostFocus
        //    if (e.Key == Key.Tab)
        //    {
        //        ComboBoxLostFocus(sender, e);
        //        return;
        //    }
        //}
        //private void ComboBoxKeyDown(object sender, KeyEventArgs e)
        //{
        //    var cb = sender as ComboBox;

        //    if (cb.SelectedIndex == -1
        //        && cb.Items.Count > 0
        //        )
        //    {
        //        if (e.Key == Key.Down) cb.SelectedIndex = 0;
        //        else if (e.Key == Key.Up) cb.SelectedIndex = cb.Items.Count - 1;
        //    }
        //}
        //TextBox CountryTextBox;
        //TextBox RegionTextBox;
        //TextBox DistrictTextBox;
        //TextBox CityTextBox;
        //TextBox StreetTextBox;
        //TextBox BuildingTextBox;
        /// <summary>
        /// Блокировка рекурсивного вызова который происходит при сбросе выбора ComboBox (SelectedIndex = -1)
        /// </summary>
        private int RecBlock = 0;
        private bool CheckSelection(ComboBox cb, TextBox tb)
        {
            foreach (Address item in cb.Items)
            {
                if (item.FullName == cb.Text)
                {
                    //Тут не должно быть рекурсии так как текст не меняется.
                    cb.SelectedItem = item;

                    RecBlock++;
                    CodeTextBox.Text = item.Id.IsNullOrEmpty() ? String.Empty : item.Id;
                    RecBlock--;

                    RecBlock++;
                    OkatoTextBox.Text = item.Okato.IsNullOrEmpty() ? String.Empty : item.Okato;
                    RecBlock--;

                    RecBlock++;
                    ZipNumTextBox.Text = item.Zip.IsNullOrEmpty() ? String.Empty : item.Zip;
                    RecBlock--;

                    if (!CoordinatesUsed)
                    {
                        if (!item.Lat.IsNullOrEmpty() && !item.Lon.IsNullOrEmpty())
                        {
                            RecBlock++;
                            LatitudeTextBox.Text = item.Lat;
                            LongitudeTextBox.Text = item.Lon;
                            RecBlock--;

                            //Обновляем положение карты. В ответ прилетит дистанция в сообщении
                            WebView.ExecuteScriptAsync($"setCenterC([{item.Lat}, {item.Lon}], 16);");

                            CoordUpdated(item.Lat, item.Lon);
                        }
                        else//Нет координат в подсказках переходим по адресу
                        {
                            var zoom = item.ContentType == "country" ? 2 : 16;
                            WebView.ExecuteScriptAsync($"setCenterA('{item.FullAddress}', {zoom})");
                        }
                    }
                    return true;
                }
            }
            //string str = tb.Text;
            //RecBlock++;
            //cb.SelectedIndex = -1;
            //tb.Text = str;
            //RecBlock--;
            //tb.SelectionStart = str.Length;

            return false;
        }
        private void ClearItems(ComboBox cb, TextBox tb)
        {
            string str = tb.Text;
            RecBlock++;
            cb.Items.Clear();
            tb.Text = str;
            RecBlock--;
            tb.SelectionStart = str.Length;

            //if(cb == CountryComboBox) ClearItems()
        }
        private void CountryComboBoxItemsAdd(JToken data, string fullAddress = null)
        {
            CountryComboBox.Items.Add(new Address
            {
                //Id = data["country_iso_code"].ToString(),
                Okato = data["okato"].ToString(),
                Name = data["country"].ToString(),
                ContentType = "country",
                FullName = data["country"].ToString(),
                Lat = data["geo_lat"].ToString(),
                Lon = data["geo_lon"].ToString(),
                Zip = data["postal_code"].ToString(),
                FullAddress = fullAddress,
            });
        }
        private void RegionComboBoxItemsAdd(JToken data, string fullAddress = null)
        {
            RegionComboBox.Items.Add(new Address
            {
                Id = data["region_fias_id"].ToString(),
                Okato = data["okato"].ToString(),
                Name = data["region"].ToString(),
                Type = data["region_type_full"].ToString(),
                TypeShort = data["region_type"].ToString(),
                ContentType = "region",
                FullName = data["region_with_type"].ToString(),
                Lat = data["geo_lat"].ToString(),
                Lon = data["geo_lon"].ToString(),
                Zip = data["postal_code"].ToString(),
                FullAddress = fullAddress,
            });
        }
        private void DistrictComboBoxItemsAdd(JToken data, string fullAddress = null)
        {
            DistrictComboBox.Items.Add(new Address
            {
                Id = data["area_fias_id"].ToString(),
                Okato = data["okato"].ToString(),
                Name = data["area"].ToString(),
                Type = data["area_type_full"].ToString(),
                TypeShort = data["area_type"].ToString(),
                ContentType = "area",
                FullName = data["area_with_type"].ToString(),
                Lat = data["geo_lat"].ToString(),
                Lon = data["geo_lon"].ToString(),
                Zip = data["postal_code"].ToString(),
                FullAddress = fullAddress,
            });
        }
        private void CityComboBoxItemsAdd(JToken data, string fullAddress = null)
        {
            var city = data["city_with_type"].ToString();
            if (city == "") city = data["settlement_with_type"].ToString();

            if (CityComboBox.Items.OfType<Address>().Any(p => p.FullName == city)) return;

            if (data["city"].ToString() != "")
            {
                CityComboBox.Items.Add(new Address
                {
                    Id = data["city_fias_id"].ToString(),
                    Okato = data["okato"].ToString(),
                    Name = data["city"].ToString(),
                    Type = data["city_type_full"].ToString(),
                    TypeShort = data["city_type"].ToString(),
                    ContentType = "city",
                    FullName = data["city_with_type"].ToString(),
                    Lat = data["geo_lat"].ToString(),
                    Lon = data["geo_lon"].ToString(),
                    Zip = data["postal_code"].ToString(),
                    FullAddress = fullAddress,
                    FlagCity = true,
                });
            }
            else
            {
                CityComboBox.Items.Add(new Address
                {
                    Id = data["settlement_fias_id"].ToString(),
                    Okato = data["okato"].ToString(),
                    Name = data["settlement"].ToString(),
                    Type = data["settlement_type_full"].ToString(),
                    TypeShort = data["settlement_type"].ToString(),
                    ContentType = "city",
                    FullName = data["settlement_with_type"].ToString(),
                    Lat = data["geo_lat"].ToString(),
                    Lon = data["geo_lon"].ToString(),
                    Zip = data["postal_code"].ToString(),
                    FullAddress = fullAddress,
                    FlagCity = false,
                });
            }
        }
        private void StreetComboBoxItemsAdd(JToken data, string fullAddress = null)
        {
            if (StreetComboBox.Items.OfType<Address>().Any(p => p.FullName == data["street_with_type"].ToString())) return;

            StreetComboBox.Items.Add(new Address
            {
                Id = data["street_fias_id"].ToString(),
                Okato = data["okato"].ToString(),
                Name = data["street"].ToString(),
                Type = data["street_type_full"].ToString(),
                TypeShort = data["street_type"].ToString(),
                ContentType = "street",
                FullName = data["street_with_type"].ToString(),
                Lat = data["geo_lat"].ToString(),
                Lon = data["geo_lon"].ToString(),
                Zip = data["postal_code"].ToString(),
                FullAddress = fullAddress,
            });
        }
        private void BuildingComboBoxItemsAdd(JToken data, string fullAddress = null)
        {
            var building = data["house_type"].ToString() + " " + data["house"].ToString();

            if (BuildingComboBox.Items.OfType<Address>().Any(p => p.FullName == building)) return;

            BuildingComboBox.Items.Add(new Address
            {
                Id = data["house_fias_id"].ToString(),
                Okato = data["okato"].ToString(),
                Name = data["house"].ToString(),
                Type = data["house_type_full"].ToString(),
                TypeShort = data["house_type"].ToString(),
                ContentType = "building",
                FullName = building,
                Lat = data["geo_lat"].ToString(),
                Lon = data["geo_lon"].ToString(),
                Zip = data["postal_code"].ToString(),
                FullAddress = fullAddress,
            });
        }

        private async void FullAddressTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (RecBlock > 0) return;

            FullAddressUsed = true;

            if (SuggestionsUsed && CoordinatesUsed) return;

            var suggestions = await GetAddressDaData(FullAddressTextBox.Text);
            if (suggestions != null)
            {
                var data = suggestions.First();

                if (!SuggestionsUsed)
                {
                    //Обновляем поля
                    ClearRestAddress(null);
                    CountryComboBoxItemsAdd(data);
                    RegionComboBoxItemsAdd(data);
                    DistrictComboBoxItemsAdd(data);
                    CityComboBoxItemsAdd(data);
                    StreetComboBoxItemsAdd(data);
                    BuildingComboBoxItemsAdd(data);
                    RecBlock++;
                    CodeTextBox.Text = data["fias_id"].ToString();
                    OkatoTextBox.Text = data["okato"].ToString();
                    ZipNumTextBox.Text = data["postal_code"].ToString();
                    CountryComboBox.SelectedIndex = 0;
                    RegionComboBox.SelectedIndex = 0;
                    DistrictComboBox.SelectedIndex = 0;
                    CityComboBox.SelectedIndex = 0;
                    StreetComboBox.SelectedIndex = 0;
                    BuildingComboBox.SelectedIndex = 0;
                    RoomTextBox.Text = data["flat"].ToString();
                    if(RoomTextBox.Text.IsNullOrEmpty()) RoomTextBox.Text = data["room"].ToString();
                    RecBlock--;
                }

                if (!CoordinatesUsed)
                {
                    var lat = data["geo_lat"].ToString();
                    var lon = data["geo_lon"].ToString();
                    if (!lat.IsNullOrEmpty() && !lon.IsNullOrEmpty())
                    {
                        //Координаты
                        RecBlock++;
                        LatitudeTextBox.Text = lat;
                        LongitudeTextBox.Text = lon;
                        RecBlock--;

                        //Обновляем положение карты. В ответ прилетит дистанция в сообщении
                        WebView.ExecuteScriptAsync($"setCenterC([{lat}, {lon}], 16);");
                    }
                }
            }
            else if(!CoordinatesUsed)
            {
                //Обновляем положение карты. В ответ прилетит координаты и дистанция в сообщении
                WebView.ExecuteScriptAsync($"setCenterA('{FullAddressTextBox.Text}', 16)");
            }
        }

        private void ZipNumTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (RecBlock > 0) return;

            SuggestionsUsed = true;

            var tb = sender as TextBox;

            var newStr = String.Empty;
            var selectionStart = tb.SelectionStart;

            foreach (var ch in tb.Text)
            {
                if (ch >= '0' && ch <= '9')
                    newStr += ch;
                else selectionStart--;
            }
            RecBlock++;
            tb.Text = newStr;
            RecBlock--;
            tb.SelectionStart = selectionStart;

            if (!FullAddressUsed) UpdateFullAddressFromSuggestions();
        }
        private async void CountryComboBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (RecBlock > 0) return;

            SuggestionsUsed = true;

            var cb = sender as ComboBox;
            var tb = e.OriginalSource as TextBox;

            if (tb.IsFocused)
                cb.IsDropDownOpen = true;

            ClearRestAddress(cb);
            if (!FullAddressUsed) UpdateFullAddressFromSuggestions();
            if (CheckSelection(cb, tb)) return;
            ClearItems(cb, tb);

            string jsonString = "{ \"locations\": [{\"country_iso_code\": \"*\"}]," +
                "\"from_bound\": { \"value\": \"country\" }," +
                "\"to_bound\": { \"value\": \"country\" }," +
                "\"query\": \"" + cb.Text.Trim() + "\"," +
                "\"count\": 10" +
                "}";

            var suggestions = await GetSuggestions(jsonString);
            if (suggestions == null) return;

            foreach (var suggestion in suggestions)
            {
                var data = suggestion["data"];
                CountryComboBoxItemsAdd(data, suggestion["unrestricted_value"].ToString());
            }

            CheckSelection(cb, tb);
        }
        private async void RegionComboBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (RecBlock > 0) return;

            SuggestionsUsed = true;

            var cb = sender as ComboBox;
            var tb = e.OriginalSource as TextBox;

            if (tb.IsFocused)
                cb.IsDropDownOpen = true;

            ClearRestAddress(cb);
            if (!FullAddressUsed) UpdateFullAddressFromSuggestions();
            if (CheckSelection(cb, tb)) return;
            ClearItems(cb, tb);

            string jsonString = "{ \"locations\": [{\"country\": \"" + CountryComboBox.Text + "\"}]," +
                "\"from_bound\": { \"value\": \"region\" }," +
                "\"to_bound\": { \"value\": \"region\" }," +
                "\"query\": \"" + cb.Text.Trim() + "\"," +
                "\"count\": 10" +
                "}";

            var suggestions = await GetSuggestions(jsonString);
            if (suggestions == null) return;

            foreach (var suggestion in suggestions)
            {
                var data = suggestion["data"];
                RegionComboBoxItemsAdd(data, suggestion["unrestricted_value"].ToString());
            }

            CheckSelection(cb, tb);
        }
        private async void DistrictComboBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (RecBlock > 0) return;

            SuggestionsUsed = true;

            var cb = sender as ComboBox;
            var tb = e.OriginalSource as TextBox;

            if (tb.IsFocused)
                cb.IsDropDownOpen = true;

            ClearRestAddress(cb);
            if (!FullAddressUsed) UpdateFullAddressFromSuggestions();
            if (CheckSelection(cb, tb)) return;
            ClearItems(cb, tb);

            if (RegionComboBox.SelectedItem == null) return;

            string jsonString = "{ \"locations\": [{ \"region_fias_id\":\"" + ((Address)RegionComboBox.SelectedItem).Id + "\"}]," +
                "\"from_bound\": { \"value\": \"area\" }," +
                "\"to_bound\": { \"value\": \"area\" }," +
                "\"restrict_value\": true," +
                "\"query\": \"" + cb.Text.Trim() + "\"," +
                "\"count\": 10" +
                "}";

            var suggestions = await GetSuggestions(jsonString);
            if (suggestions == null) return;

            foreach (var suggestion in suggestions)
            {
                var data = suggestion["data"];
                DistrictComboBoxItemsAdd(data, suggestion["unrestricted_value"].ToString());
            }

            CheckSelection(cb, tb);
        }
        private async void CityComboBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (RecBlock > 0) return;

            SuggestionsUsed = true;

            var cb = sender as ComboBox;
            var tb = e.OriginalSource as TextBox;

            if (tb.IsFocused)
                cb.IsDropDownOpen = true;

            ClearRestAddress(cb);
            if (!FullAddressUsed) UpdateFullAddressFromSuggestions();
            if (CheckSelection(cb, tb)) return;
            ClearItems(cb, tb);

            string jsonString = String.Empty;

            if (DistrictComboBox.SelectedIndex != -1)
            {
                if (DistrictComboBox.SelectedItem == null) return;
                //Ограничение на поиск в выбранном районе
                jsonString = "{ \"locations\": [{ \"area_fias_id\":\"" + ((Address)DistrictComboBox.SelectedItem).Id + "\"}]," +
                    "\"from_bound\": { \"value\": \"city\" }," +
                    "\"to_bound\": { \"value\": \"settlement\" }," +
                    "\"restrict_value\": true," +
                    "\"query\": \"" + cb.Text.Trim() + "\"," +
                    "\"count\": 10" +
                    "}";
            }
            else
            {
                if (RegionComboBox.SelectedItem == null) return;
                //Ограничение на поиск в выбранной области
                jsonString = "{ \"locations\": [{\"country\": \"" + CountryComboBox.Text + "\"}" +
                    ", { \"region_fias_id\":\"" + ((Address)RegionComboBox.SelectedItem).Id + "\"}]," +
                    "\"from_bound\": { \"value\": \"city\" }," +
                    "\"to_bound\": { \"value\": \"settlement\" }," +
                    "\"restrict_value\": true," +
                    "\"query\": \"" + cb.Text.Trim() + "\"," +
                    "\"count\": 10" +
                    "}";
            }
                    

            var suggestions = await GetSuggestions(jsonString);
            if (suggestions == null) return;

            foreach (var suggestion in suggestions)
            {
                var data = suggestion["data"];
                CityComboBoxItemsAdd(data, suggestion["unrestricted_value"].ToString());
            }

            CheckSelection(cb, tb);
        }
        private async void StreetComboBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (RecBlock > 0) return;

            SuggestionsUsed = true;

            var cb = sender as ComboBox;
            var tb = e.OriginalSource as TextBox;

            if (tb.IsFocused)
                cb.IsDropDownOpen = true;

            ClearRestAddress(cb);
            if (!FullAddressUsed) UpdateFullAddressFromSuggestions();
            if (CheckSelection(cb, tb)) return;
            ClearItems(cb, tb);

            if (CityComboBox.SelectedItem == null) return;

            string jsonString = "";

            //if ((((Address)RegionComboBox.SelectedItem).Id == "c2deb16a-0330-4f05-821f-1d09c93331e6") // Санкт петербург
            //    || (((Address)RegionComboBox.SelectedItem).Id == "0c5b2444-70a0-4932-980c-b4dc0d3f02b5")) // Москва
            //    {
            //    jsonString = "{ \"locations\": [{\"country\": \"" + ((Address)CountryComboBox.SelectedValue).Name + "\"}" +
            //        ", { \"region_fias_id\":\"" + ((Address)RegionComboBox.SelectedItem).Id + "\"}]," +
            //        "\"from_bound\": { \"value\": \"street\" }," +
            //        "\"to_bound\": { \"value\": \"street\" }," +
            //        "\"restrict_value\": true," +
            //        "\"query\": \"" + cb.Text.Trim() + "\"," +
            //        "\"count\": 10" +
            //        "}";
            //}
            //else 
            if (((Address)CityComboBox.SelectedItem).FlagCity)
            {
                jsonString = "{ \"locations\": [{ \"city_fias_id\":\"" + ((Address)CityComboBox.SelectedItem).Id + "\"}]," +
                    "\"from_bound\": { \"value\": \"street\" }," +
                    "\"to_bound\": { \"value\": \"street\" }," +
                    "\"restrict_value\": true," +
                    "\"query\": \"" + cb.Text.Trim() + "\"," +
                    "\"count\": 10" +
                    "}";
            }
            else
            {
                jsonString = "{ \"locations\": [{ \"settlement_fias_id\":\"" + ((Address)CityComboBox.SelectedItem).Id + "\"}]," +
                    "\"from_bound\": { \"value\": \"street\" }," +
                    "\"to_bound\": { \"value\": \"street\" }," +
                    "\"restrict_value\": true," +
                    "\"query\": \"" +cb.Text.Trim() + "\"," +
                    "\"count\": 10" +
                    "}";
            }

            var suggestions = await GetSuggestions(jsonString);
            if (suggestions == null) return;

            foreach (var suggestion in suggestions)
            {
                var data = suggestion["data"];
                StreetComboBoxItemsAdd(data, suggestion["unrestricted_value"].ToString());
            }

            CheckSelection(cb, tb);
        }
        private async void BuildingComboBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (RecBlock > 0) return;

            SuggestionsUsed = true;

            var cb = sender as ComboBox;
            var tb = e.OriginalSource as TextBox;

            if (tb.IsFocused)
                cb.IsDropDownOpen = true;

            ClearRestAddress(cb);
            if (!FullAddressUsed) UpdateFullAddressFromSuggestions();
            if (CheckSelection(cb, tb)) return;
            ClearItems(cb, tb);

            string jsonString = "";

            //Обработать вариант без улици
            if (StreetComboBox.SelectedIndex == -1)
            {
                if (CityComboBox.SelectedItem == null) return;

                if (((Address)CityComboBox.SelectedItem).FlagCity)
                {
                    jsonString = "{ \"locations\": [{ \"city_fias_id\":\"" + ((Address)CityComboBox.SelectedItem).Id + "\"}]," +
                        "\"from_bound\": { \"value\": \"house\" }," +
                        "\"to_bound\": { \"value\": \"house\" }," +
                        "\"restrict_value\": true," +
                        "\"query\": \"" + cb.Text.Trim() + "\"," +
                        "\"count\": 10" +
                        "}";
                }
                else
                {
                    jsonString = "{ \"locations\": [{ \"settlement_fias_id\":\"" + ((Address)CityComboBox.SelectedItem).Id + "\"}]," +
                        "\"from_bound\": { \"value\": \"house\" }," +
                        "\"to_bound\": { \"value\": \"house\" }," +
                        "\"restrict_value\": true," +
                        "\"query\": \"" + cb.Text.Trim() + "\"," +
                        "\"count\": 10" +
                        "}";
                }
            }
            else
            {
                if (StreetComboBox.SelectedItem == null) return;

                jsonString = "{ \"locations\": [{ \"street_fias_id\":\"" + ((Address)StreetComboBox.SelectedItem).Id + "\"}]," +
                    "\"from_bound\": { \"value\": \"house\" }," +
                    "\"restrict_value\": true," +
                    "\"query\": \"" + cb.Text.Trim() + "\"," +
                    "\"count\": 10" +
                    "}";
            }

            var suggestions = await GetSuggestions(jsonString);
            if (suggestions == null) return;

            foreach (var suggestion in suggestions)
            {
                var data = suggestion["data"];
                BuildingComboBoxItemsAdd(data, suggestion["unrestricted_value"].ToString());
            }

            CheckSelection(cb, tb);
        }
        private void RoomTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (RecBlock > 0) return;

            SuggestionsUsed = true;

            if (!FullAddressUsed) UpdateFullAddressFromSuggestions();
        }
        private async void CoordTextChanged(object sender, TextChangedEventArgs e)
        {
            if (RecBlock > 0) return;

            var tb = sender as TextBox;

            var newStr = String.Empty;
            var selectionStart = tb.SelectionStart;
            var chCnt = 0;

            foreach (var ch in tb.Text)
            {
                if ((ch >= '0' && ch <= '9'))
                {
                    newStr += ch;
                }
                else if(ch == '.' && chCnt == 0)
                {
                    chCnt++;
                    newStr += ch;
                }
                else selectionStart--;
            }
            RecBlock++;
            tb.Text = newStr;
            RecBlock--;
            tb.SelectionStart = selectionStart;

            CoordinatesUsed = true;

            var lat = LatitudeTextBox.Text;
            var lon = LongitudeTextBox.Text;
            if (!lat.IsNullOrEmpty() && !lon.IsNullOrEmpty())
            {
                //Обновляем положение карты. В ответ прилетит дистанция в сообщении
                WebView.ExecuteScriptAsync($"setCenterC([{lat}, {lon}], 16);");

                CoordUpdated(lat, lon);
            }
        }
        private async void CoordUpdated(string lat, string lon)
        {
            if (FullAddressUsed && SuggestionsUsed) return;

            if (!SuggestionsUsed && !FullAddressUsed)
            {
                RecBlock++;
                FullAddressTextBox.Text = String.Empty;
                RecBlock--;
            }

            var suggestions = await GetAddressDaData(lat, lon);
            if (suggestions == null || suggestions.Count == 0) return;

            //Обновляем поля
            var data = suggestions.First()["data"];

            if (!SuggestionsUsed)
            {
                ClearRestAddress(null);
                CountryComboBoxItemsAdd(data);
                RegionComboBoxItemsAdd(data);
                DistrictComboBoxItemsAdd(data);
                CityComboBoxItemsAdd(data);
                StreetComboBoxItemsAdd(data);
                BuildingComboBoxItemsAdd(data);
                RecBlock++;
                CodeTextBox.Text = data["fias_id"].ToString();
                OkatoTextBox.Text = data["okato"].ToString();
                ZipNumTextBox.Text = data["postal_code"].ToString();
                CountryComboBox.SelectedIndex = 0;
                RegionComboBox.SelectedIndex = 0;
                DistrictComboBox.SelectedIndex = 0;
                CityComboBox.SelectedIndex = 0;
                StreetComboBox.SelectedIndex = 0;
                BuildingComboBox.SelectedIndex = 0;
                RoomTextBox.Text = data["flat"].ToString();
                if (RoomTextBox.Text.IsNullOrEmpty()) RoomTextBox.Text = data["room"].ToString();
                RecBlock--;

                if (!FullAddressUsed) UpdateFullAddressFromSuggestions();
            }
        }
        private void ClearRestAddress(ComboBox cb)
        {
            RecBlock++;
            RoomTextBox.Text = String.Empty;
            RecBlock--;

            if (cb == BuildingComboBox) return;
            RecBlock++;
            BuildingComboBox.Items.Clear();
            BuildingComboBox.Text = String.Empty;
            RecBlock--;

            if (cb == StreetComboBox) return;
            RecBlock++;
            StreetComboBox.Items.Clear();
            StreetComboBox.Text = String.Empty;
            RecBlock--;

            if (cb == CityComboBox) return;
            RecBlock++;
            CityComboBox.Items.Clear();
            CityComboBox.Text = String.Empty;
            RecBlock--;

            if (cb == DistrictComboBox) return;
            RecBlock++;
            DistrictComboBox.Items.Clear();
            DistrictComboBox.Text = String.Empty;
            RecBlock--;

            if (cb == RegionComboBox) return;
            RecBlock++;
            RegionComboBox.Items.Clear();
            RegionComboBox.Text = String.Empty;
            RecBlock--;

            if (cb == CountryComboBox) return;
            RecBlock++;
            CountryComboBox.Items.Clear();
            CountryComboBox.Text = String.Empty;
            RecBlock--;
        }

        //private async void CountryComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (CountryComboBox.SelectedIndex == 0)
        //    {
        //        //RegionComboBox.IsEnabled = true;
        //    }
        //    else
        //    {
        //        //RegionComboBox.IsEnabled = false;
        //        RecBlock++;
        //        //RegionComboBox.SelectedIndex = -1;
        //        RecBlock--;
        //    }
        //}
        //private void RegionComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (RegionComboBox.SelectedIndex != -1)
        //    {
        //        //DistrictComboBox.IsEnabled = true;
        //        //CityComboBox.IsEnabled = true;
        //    }
        //    else
        //    {
        //        //DistrictComboBox.IsEnabled = false;
        //        //CityComboBox.IsEnabled = false;
        //        RecBlock++;
        //        //DistrictComboBox.SelectedIndex = -1;
        //        //CityComboBox.SelectedIndex = -1;
        //        RecBlock--;
        //    }
        //}
        //private void CityComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (CityComboBox.SelectedIndex != -1)
        //    {
        //        //StreetComboBox.IsEnabled = true;
        //        //BuildingComboBox.IsEnabled = true;
        //    }
        //    else
        //    {
        //        //StreetComboBox.IsEnabled = false;
        //        //BuildingComboBox.IsEnabled = false;
        //        RecBlock++;
        //        //StreetComboBox.SelectedIndex = -1;
        //        //BuildingComboBox.SelectedIndex = -1;
        //        RecBlock--;
        //    }
        //}
        //private void BuildingComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
            
        //    if (BuildingComboBox.SelectedIndex != -1)
        //    {
        //        //RoomTextBox.IsEnabled = true;
        //    }
        //    else
        //    {
        //        //StreetComboBox.IsEnabled = false;
        //        //StreetComboBox.Text = "";
        //    }
        //}

        private async void WebViewWebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            var str = e.TryGetWebMessageAsString();

            switch(str.Substring(0, 5))
            {
                case "LtnC=":
                    CoordinatesUsed = true;
                    goto case "LtnA=";
                case "LtnA=":
                    var coord = str.Substring(5).Split(',');

                    RecBlock++;
                    LatitudeTextBox.Text = coord[0].ToDouble().ToString("N6").Replace(',','.');
                    LongitudeTextBox.Text = coord[1].ToDouble().ToString("N6").Replace(',', '.');
                    RecBlock--;

                    CoordUpdated(LatitudeTextBox.Text, LongitudeTextBox.Text);

                    break;
                case "Dist=":
                    DistanceTextBox.Text = str.Substring(5).ToDouble().ToString("N1");
                    break;
                case "Addr=":
                    if (!FullAddressUsed && FullAddressTextBox.Text == String.Empty)
                    {
                        RecBlock++;
                        FullAddressTextBox.Text = str.Substring(5);
                        RecBlock--;
                    }
                    break;
                case "Ready":
                    var lat = LatitudeTextBox.Text;
                    var lon = LongitudeTextBox.Text;
                    if (!lat.IsNullOrEmpty() && !lon.IsNullOrEmpty())
                    {
                        //Обновляем положение карты. В ответ прилетит дистанция в сообщении
                        WebView.ExecuteScriptAsync($"setCenterC([{lat}, {lon}], 16);");
                    }
                    else if(!FullAddressTextBox.Text.IsNullOrEmpty())
                    {
                        WebView.ExecuteScriptAsync($"setCenterI('{FullAddressTextBox.Text}', 16)");
                    }
                    break;
            }
        }

        private void UpdateFullAddressFromSuggestions()
        {
            string address = String.Empty;

            if (!ZipNumTextBox.Text.IsNullOrEmpty()) address += ZipNumTextBox.Text + ", ";
            if (!CountryComboBox.Text.IsNullOrEmpty()) address += CountryComboBox.Text;
            if (!RegionComboBox.Text.IsNullOrEmpty()) address += ", " + RegionComboBox.Text;
            if (!DistrictComboBox.Text.IsNullOrEmpty()) address += ", " + DistrictComboBox.Text;
            if (!CityComboBox.Text.IsNullOrEmpty()) address += ", " + CityComboBox.Text;
            if (!StreetComboBox.Text.IsNullOrEmpty()) address += ", " + StreetComboBox.Text;
            if (!BuildingComboBox.Text.IsNullOrEmpty()) address += ", " + BuildingComboBox.Text;
            if (!RoomTextBox.Text.IsNullOrEmpty()) address += ", " + RoomTextBox.Text;

            //if (BuildingComboBox.Text != string.Empty) return ((Address)BuildingComboBox.SelectedItem).FullName + ", " + RoomTextBox.Text;
            //if (StreetComboBox.SelectedIndex != -1) return ((Address)StreetComboBox.SelectedItem).FullName + ", " + RoomTextBox.Text;
            //if (CityComboBox.SelectedIndex != -1) return ((Address)CityComboBox.SelectedItem).FullName + ", " + RoomTextBox.Text;
            //if (DistrictComboBox.SelectedIndex != -1) return ((Address)DistrictComboBox.SelectedItem).FullName + ", " + RoomTextBox.Text;
            //if (RegionComboBox.SelectedIndex != -1) return ((Address)RegionComboBox.SelectedItem).FullName + ", " + RoomTextBox.Text;
            //if (CountryComboBox.SelectedIndex != -1) return ((Address)CountryComboBox.SelectedItem).FullName + ", " + RoomTextBox.Text;

            RecBlock++;
            FullAddressTextBox.Text = address;
            RecBlock--;
        }
        private void Save()
        {
            if (!Form.Validate())
            {
                Form.SetStatus("Не все обязательные поля заполнены верно", 1);
                return;
            }

            DBAddress.FullAddress = FullAddressTextBox.Text;
            DBAddress.Code = CodeTextBox.Text;
            DBAddress.Okato = OkatoTextBox.Text;
            DBAddress.ZipNum = ZipNumTextBox.Text;
            DBAddress.Country = CountryComboBox.Text;
            DBAddress.Region = RegionComboBox.Text;
            DBAddress.District = DistrictComboBox.Text;
            DBAddress.City = CityComboBox.Text;
            DBAddress.Street = StreetComboBox.Text;
            DBAddress.Building = BuildingComboBox.Text;
            DBAddress.Room = RoomTextBox.Text;
            DBAddress.Longtitude = LongitudeTextBox.Text;
            DBAddress.Latitude = LatitudeTextBox.Text;
            DBAddress.Distance = DistanceTextBox.Text;

            //Central.Msg.SendMessage(new ItemMessage()
            //{
            //    ReceiverName = "ShipAdresForm",
            //    SenderName = ControlName,
            //    Action = "address_refresh",
            //});
            Close();
        }
    }
}
