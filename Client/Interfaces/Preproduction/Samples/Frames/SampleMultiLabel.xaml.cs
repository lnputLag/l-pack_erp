using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
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

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Печатная форма сводного ярлыка для нескольких образцов
    /// </summary>
    public partial class SampleMultiLabel : UserControl
    {
        public SampleMultiLabel()
        {
            InitializeComponent();

            SampleItems = new List<Dictionary<string, string>>();
        }

        /// <summary>
        /// Индекс страницы, отправляемой на печать
        /// </summary>
        private int CurrentPageIndex { get; set; }
        /// <summary>
        /// Индекс последней строки данных, напечатанной на листе
        /// </summary>
        private int LastPrintedIndex { get; set; }
        /// <summary>
        /// Таймер печати
        /// </summary>
        private Timeout PrintPageTimer { get; set; }
        /// <summary>
        /// Данные по образцам для печати
        /// </summary>
        public List<Dictionary<string, string>> SampleItems { get; set; }

        private PrintDialog PrintDialog { get; set; }


        public async void GetData(List<int> smplIdList)
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Samples");
            q.Request.SetParam("Action", "GetTaskReport");
            var l = string.Join(",", smplIdList);
            q.Request.SetParam("ID_LIST", l);

            await Task.Run(() =>
            {
                q.DoQuery();
            });
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    var ds = ListDataSet.Create(result, "SampleReport");
                    SampleItems = ds.Items;

                    Make();
                }
            }
        }


        private void InitPrinter()
        {
            PrintDialog = new PrintDialog();
        }


        private void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            switch (command)
            {
                case "print":
                    {
                        PrintStart();
                    }
                    break;

                case "setup":
                    {
                        InitPrinter();
                        if ((bool)PrintDialog.ShowDialog())
                        {
                            //PrintDialog.PrintVisual(LabelContainer, "");
                            PrintPages2();
                            Close();
                        }
                    }
                    break;

                case "close":
                    {
                        Close();
                    }
                    break;
            }
        }

        private void GridToolbarButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                var c = (System.Windows.Controls.Button)sender;
                var tag = c.Tag.ToString();
                ProcessCommand(tag);
            }
        }

        private void PrintStart()
        {
            InitPrinter();
            //PrintDialog.PrintVisual(LabelContainer, "");
            PrintPages2();
        }

        public void PrintLabel(List<int> smplIdList, string note="")
        {
            if (smplIdList.Count > 0)
            {
                Note.Text = note;
                GetData(smplIdList);
            }
        }

        public void Make()
        {
            if (SampleItems.Count > 0)
            {
                int i = 0;
                SampleGridContainer.Children.Clear();
                SampleGridContainer.RowDefinitions.Clear();

                CustomerName.Text = SampleItems[0].CheckGet("CUSTOMER_NAME");

                foreach (var item in SampleItems)
                {
                    SampleGridContainer.RowDefinitions.Add(new RowDefinition());
                    var smpl = new SampleLabel();
                    string sampleSize = item.CheckGet("SAMPLE_SIZE");
                    string sampleClass = item.CheckGet("SAMPLE_CLASS");
                    string cardboard = "";
                    if (item.CheckGet("HIDE_MARK").ToBool())
                    {
                        cardboard = item.CheckGet("PROFILE_NAME");
                    }
                    else
                    {
                        cardboard = item.CheckGet("CARDBOARD_NAME");
                    }

                    // Проверяем настройку ярлыка
                    string labelCustomizing = item.CheckGet("LABEL_TEXT");
                    string labelNote = "";
                    if (!labelCustomizing.IsNullOrEmpty())
                    {
                        var v = labelCustomizing.Split(';');
                        if (v[2].ToInt() == 1)
                        {
                            sampleSize = v[3];
                        }

                        if (v[4].ToInt() == 1)
                        {
                            sampleClass = v[5];
                        }

                        if (v[6].ToInt() == 1)
                        {
                            cardboard = v[7];
                        }

                        if (v.Length > 8)
                        {
                            labelNote = v[8];
                        }
                    }

                    smpl.SampleCompletedDt.Text = $"{item.CheckGet("DT_COMPLITED")} / {item.CheckGet("ID")}";
                    smpl.SampleSize.Text = sampleSize;
                    smpl.SampleType.Text = sampleClass;
                    smpl.SampleQty.Text = $"{item.CheckGet("QTY").ToInt()} шт";
                    string cb = $"{cardboard} [{item.CheckGet("CARDBOARD_NUM").ToInt()}]";
                    string sampleNum = item.CheckGet("SAMPLE_NUM");
                    if (!string.IsNullOrEmpty(sampleNum))
                    {
                        cb = $"{cb} ({sampleNum})";
                    };
                    smpl.SampleRaw.Text = cb;
                    smpl.SampleNote.Text = labelNote;

                    SampleGridContainer.Children.Add(smpl);
                    Grid.SetRow(smpl, i);
                    Grid.SetColumn(smpl, 0);
                    i++;
                }

                Central.WM.FrameMode = 2;
                Central.WM.Show("MultiLabel", "Ярлыки на образцы", true, "add", this, "");
            }
        }

        private void PrintOnePage()
        {
            var timeout=new Timeout(
                1,
                () =>
                {
                    PrintDialog.PrintVisual(LabelContainer, "");
                }
            );
            timeout.SetIntervalMs(2000);
            timeout.Run();
        }

        private void PrintPages()
        {
            CurrentPageIndex = 1;
            LastPrintedIndex = 0;
            bool printNext = true;

            if (SampleItems.Count > 0)
            {
                CustomerName.Text = SampleItems[0].CheckGet("CUSTOMER_NAME");
                NoteBlock.Visibility = Visibility.Collapsed;
                int i = 0;

                while (printNext)
                {
                    if (i == 0)
                    {
                        SampleGridContainer.Children.Clear();
                        SampleGridContainer.RowDefinitions.Clear();
                        if (CurrentPageIndex != 1)
                        {
                            HeaderBlock.Visibility = Visibility.Collapsed;
                            CustomerBlock.Visibility = Visibility.Collapsed;
                        }
                    }
                    else
                    {
                        if (CurrentPageIndex == 1 && i == 5)
                        {
                            //PrintDialog.PrintVisual(LabelContainer, "");
                            PrintOnePage();
                            i = 0;
                            CurrentPageIndex++;
                            continue;
                        }
                        else if (CurrentPageIndex != 1 && i == 6)
                        {
                            //PrintDialog.PrintVisual(LabelContainer, "");
                            PrintOnePage();
                            i = 0;
                            CurrentPageIndex++;
                            continue;
                        }
                    }

                    if (SampleItems.Count > LastPrintedIndex)
                    {
                        SampleGridContainer.RowDefinitions.Add(new RowDefinition());
                        var item = SampleItems[LastPrintedIndex];
                        var smpl = new SampleLabel();

                        smpl.SampleCompletedDt.Text = $"{item.CheckGet("DT_COMPLITED")} / {item.CheckGet("ID")}";
                        smpl.SampleSize.Text = item.CheckGet("SAMPLE_SIZE");
                        smpl.SampleType.Text = item.CheckGet("SAMPLE_CLASS");
                        smpl.SampleQty.Text = $"{item.CheckGet("QTY").ToInt()} шт";
                        string cb = $"{item.CheckGet("PROFILE_NAME")} [{item.CheckGet("CARDBOARD_NUM").ToInt()}]";
                        string sampleNum = item.CheckGet("SAMPLE_NUM");
                        if (!string.IsNullOrEmpty(sampleNum))
                        {
                            cb = $"{cb} ({sampleNum})";
                        };
                        smpl.SampleRaw.Text = cb;

                        SampleGridContainer.Children.Add(smpl);
                        Grid.SetRow(smpl, i);
                        Grid.SetColumn(smpl, 0);
                        i++;
                        LastPrintedIndex++;
                    }
                    else
                    {
                        NoteBlock.Visibility = Visibility.Visible;
                        //PrintDialog.PrintVisual(LabelContainer, "");
                        PrintOnePage();
                        printNext = false;
                    }
                }

            }
        }

        public void Close()
        {
            Central.WM.Close("MultiLabel");
        }

        private void PrintPages2()
        {
            var label = new SampleMultiLabelPrint();
            label.Note = Note.Text;
            label.SampleItems = SampleItems;
            var xps = label.Make();

            PrintDialog.PrintTicket.PageOrientation = System.Printing.PageOrientation.Portrait;
            PrintDialog.PrintDocument(xps.GetFixedDocumentSequence().DocumentPaginator, "");
        }
    }
}
