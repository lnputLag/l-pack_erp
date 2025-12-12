using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Xpf.Core.Native;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма редактирования площадей запечатки областей контейнера литой тары
    /// </summary>
    /// <author>ryasnoy_pv</author>
    public partial class MoldedContainerPrintingArea : ControlBase
    {
        /// <summary>
        /// Форма редактирования площадей запечатки областей контейнера литой тары
        /// </summary>
        public MoldedContainerPrintingArea()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;
        /// <summary>
        /// Форма редактирования техкарты
        /// </summary>
        FormHelper Form { get; set; }
        /// <summary>
        /// Идентификатор техкарты, для которой заполняются площади запечатки
        /// </summary>
        public int TechCardId;
        /// <summary>
        /// Данные для площадей запечатки
        /// </summary>
        public ListDataSet PrintingColorsDS {get;set;}

        /// <summary>
        /// Обработка и выполнение команд
        /// </summary>
        /// <param name="command"></param>
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

        private async void GetData()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "ListPrintColors");
            q.Request.SetParam("ID", TechCardId.ToString());


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
                    PrintingColorsDS = ListDataSet.Create(result, "PRINT");
                    if (PrintingColorsDS.Items != null)
                    {
                        if (PrintingColorsDS.Items.Count > 0)
                        {
                            ShowForm();
                        }
                    }
                }
            }
        }

        private void ShowForm()
        {
            int i = 0;
            Status.Text = "";

            BodyGrid.Children.Clear();
            BodyGrid.RowDefinitions.Clear();

            foreach (var item in PrintingColorsDS.Items)
            {
                var row = new RowDefinition();
                row.Height = new GridLength(0, GridUnitType.Auto);
                BodyGrid.RowDefinitions.Add(row);

                var borderLabel = new Border();
                //borderLabel.Style = (Style)borderLabel.TryFindResource("FormLabelContainer");
                borderLabel.Style = FindResource("FormLabelContainer");
                var label = new Label();
                //label.Style = (Style)label.TryFindResource("FormLabel");
                label.Style = FindResource("FormLabel");
                label.Content = $"{item.CheckGet("COLOR_NAME")} ({item.CheckGet("PRINTING_SPOT")})";
                borderLabel.Child = label;

                BodyGrid.Children.Add(borderLabel);
                System.Windows.Controls.Grid.SetRow(borderLabel, i);
                System.Windows.Controls.Grid.SetColumn(borderLabel, 0);

                var borderKey = new Border();
                //borderKey.Style = (Style)borderKey.TryFindResource("FormFieldContainer");
                borderKey.Style = FindResource("FormFieldContainer");
                var keyField = new System.Windows.Controls.TextBox();
                //keyField.Style = (Style)keyField.TryFindResource("FormField");
                keyField.Style = FindResource("FormField");
                keyField.Visibility = Visibility.Collapsed;
                keyField.Name = $"KeyField{i}";
                keyField.Text = item.CheckGet("ID");
                borderKey.Child = keyField;

                BodyGrid.Children.Add(borderKey);
                System.Windows.Controls.Grid.SetRow(borderKey, i);
                System.Windows.Controls.Grid.SetColumn(borderKey, 1);

                var borderArea = new Border();
                //borderArea.Style = (Style)borderArea.TryFindResource("FormFieldContainer");
                borderArea.Style = FindResource("FormFieldContainer");
                var areaField = new System.Windows.Controls.TextBox();
                //areaField.Style = (Style)areaField.TryFindResource("FormField");
                areaField.Style = FindResource("FormField");
                areaField.Name = $"AreaField{i}";

                string v = item.CheckGet("PRINTING_AREA");
                if (!v.IsNullOrEmpty())
                {
                    v= v.ToInt().ToString();
                }
                areaField.Text = v;
                borderArea.Child = areaField;

                BodyGrid.Children.Add(borderArea);
                System.Windows.Controls.Grid.SetRow(borderArea, i);
                System.Windows.Controls.Grid.SetColumn(borderArea, 2);

                i++;
            }

            ControlName = $"PrintingArea_{TechCardId}";
            ControlTitle = $"Запечатка {TechCardId}";
            Central.WM.AddTab(ControlName, ControlTitle, true, "add", this);
        }

        private Style FindResource(string name)
        {
            return (Style)SaveButton.FindResource(name);
        }

        /// <summary>
        /// Запуск редактирования площадей запечатки цветов
        /// </summary>
        /// <param name="id"></param>
        public void Edit(int id)
        {
            if (id > 0)
            {
                TechCardId = id;
                GetData();
            }
        }

        public void Save()
        {
            var fieldValuesDictionary = new Dictionary<string, string>();
            
            foreach (var elem in BodyGrid.Children)
            {
                if (elem is Border bd)
                {
                    if (bd.Child is System.Windows.Controls.TextBox t)
                    {
                        fieldValuesDictionary.Add(t.Name, t.Text);
                    }
                }
            }

            if (fieldValuesDictionary.Count > 0)
            {
                var areaDictionary = new Dictionary<string, string>();
                for (int i = 0; i < PrintingColorsDS.Items.Count; i++)
                {
                    string k = fieldValuesDictionary.CheckGet($"KeyField{i}");
                    string v = fieldValuesDictionary.CheckGet($"AreaField{i}");

                    if (!k.IsNullOrEmpty())
                    {
                        areaDictionary.Add(k, v);
                    }
                }

                if (areaDictionary.Count > 0)
                {
                    SaveData(areaDictionary);
                }
                else
                {
                    Status.Text = "Ошибка определения значений";
                }
            }
            else
            {
                Status.Text = "Ошибка определения полей";
            }
        }

        private async void SaveData(Dictionary<string, string> p)
        {
            var convertedAreaDictionary = JsonConvert.SerializeObject(p);

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "SavePrintingArea");
            q.Request.SetParam("PRINTING_AREA", convertedAreaDictionary);
            q.Request.SetParam("ID", TechCardId.ToString());

            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {

                Close();
            }
        }

        public void Close()
        {
            Central.WM.RemoveTab(ControlName);
        }

        private void ButtonOnClick(object sender, RoutedEventArgs e)
        {
            var b = (System.Windows.Controls.Button)sender;
            if (b != null)
            {
                var t = b.Tag.ToString();
                ProcessCommand(t);
            }
        }
    }
}
