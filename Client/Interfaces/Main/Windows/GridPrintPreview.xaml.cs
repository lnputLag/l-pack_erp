using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Utilities;
using SharpVectors.Dom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using System.Xml;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// Форма редактирования времени работы склада
    /// </summary>
    /// <author>motenko_ek</author>
    public partial class GridPrintPreview : Window
    {
        public GridPrintPreview(string header, List<DataGridHelperColumn> columns, List<Dictionary<string, string>> items)
        {
            InitializeComponent();

            Header = header;
            Columns = columns;
            Items = items;
            Size = new Size(Dlg.PrintableAreaWidth, Dlg.PrintableAreaHeight);

            Render();

            PrintButton.Click += (object sender, RoutedEventArgs e) => { Print(); };
            SettingsButton.Click += (object sender, RoutedEventArgs e) => { Settings(); };

            //DocumentationUrl = "/doc/l-pack-erp/print_preview";
            Title = header;
            //FrameMode = 2;
            //OnKeyPressed = (KeyEventArgs e) =>
            //{
            //    if (!e.Handled)
            //    {
            //        Commander.ProcessKeyboard(e);
            //    }
            //};

            //Commander.Add(new CommandItem()
            //{
            //    Name = "close",
            //    Enabled = true,
            //    Title = "Закрыть",
            //    Description = "",
            //    ButtonUse = true,
            //    ButtonName = "CloseButton",
            //    HotKey = "Escape",
            //    Action = () =>
            //    {
            //        Close();
            //    },
            //});
            //Commander.Init(this);

            //Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            //windowParametrs.Add("center_screen", "1");
            //Central.WM.FrameMode = FrameMode;
            //Central.WM.Show(GetFrameName(), FrameTitle, true, "main", this, "", windowParametrs);
            Show();
        }

        private void Print()
        {
            var paginator = Dlg.PageRangeSelection == PageRangeSelection.UserPages ?
                new DocPaginator(
                                     DocumentViewer.Document.DocumentPaginator,
                                     Dlg.PageRange)
                : DocumentViewer.Document.DocumentPaginator;
            Dlg.PrintDocument(paginator, Header);
        }
        private void Settings()
        {
            var res = Dlg.ShowDialog();
            if (Size.Width != Dlg.PrintableAreaWidth || Size.Height != Dlg.PrintableAreaHeight)
            {
                Size = new Size(Dlg.PrintableAreaWidth, Dlg.PrintableAreaHeight);
                Render();
            }
            if (res == true)
            {
                Print();
            }
        }

        private string Header;
        private List<DataGridHelperColumn> Columns;
        private List<Dictionary<string, string>> Items;
        private PrintDialog Dlg = new PrintDialog() { UserPageRangeEnabled = true};
        private Size Size;
        void Render()
        {
            var margin = 20d;

            var i = 0;
            var page = 1;
            var fd = new FixedDocument();

            var list = new List<UIElement>();
            var headers = new List<TextBlock>();
            var m = Double.MinValue;
            while (i < Items.Count)
            {
                var pc = new PageContent();
                fd.Pages.Add(pc);

                var fp = new FixedPage();
                pc.Child = fp;
                fp.Width = Size.Width;
                fp.Height = Size.Height;

                var sp = new StackPanel();
                fp.Children.Add(sp);
                sp.Margin = new Thickness(margin);
                sp.Orientation = System.Windows.Controls.Orientation.Vertical;

                var csz = new Size(Size.Width - 2 * margin, 0);
                var tb = new TextBlock();
                sp.Children.Add(tb);
                headers.Add(tb);
                tb.Text = $"{Header} Страница {page} из ";
                tb.Margin = new Thickness(2);
                tb.TextWrapping = TextWrapping.Wrap;
                tb.Width = csz.Width;
                tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                tb.Measure(Size);
                tb.Arrange(new Rect(tb.DesiredSize));
                csz.Height = Size.Height - 2 * margin - tb.ActualHeight;

                var br = new Border();
                sp.Children.Add(br);
                br.BorderThickness = new Thickness(2);
                br.BorderBrush = System.Windows.Media.Brushes.Black;

                var gr = new Grid();
                br.Child = gr;
                gr.Width = csz.Width;
                gr.RowDefinitions.Add(new RowDefinition());
                foreach (var column in Columns)
                {
                    var cd = new ColumnDefinition();
                    gr.ColumnDefinitions.Add(cd);
                    cd.Name = column.Path;
                    cd.Width = new GridLength((double)column.Width2, GridUnitType.Star);

                    tb = new TextBlock();
                    tb.Text = column.Header;
                    tb.Margin = new Thickness(2);
                    tb.TextWrapping = TextWrapping.Wrap;

                    br = new Border();
                    br.BorderThickness = new Thickness(1);
                    br.BorderBrush = System.Windows.Media.Brushes.Black;
                    br.Child = tb;

                    Grid.SetRow(br, 0);
                    Grid.SetColumn(br, gr.ColumnDefinitions.Count - 1);
                    gr.Children.Add(br);
                }
                gr.Measure(csz);
                gr.Arrange(new Rect(gr.DesiredSize));
                var contentHeight = gr.ActualHeight;
                while (i < Items.Count)
                {
                    //Расчет высоты строки
                    var j = 0;
                    if(list.Count == 0)
                    {
                        foreach (var cd in gr.ColumnDefinitions)
                        {
                            br = new Border();
                            list.Add(br);
                            br.BorderThickness = new Thickness(1);
                            br.BorderBrush = System.Windows.Media.Brushes.Black;

                            tb = new TextBlock();
                            br.Child = tb;
                            tb.Text = Items[i][cd.Name];
                            tb.Margin = new Thickness(2);
                            tb.TextWrapping = TextWrapping.Wrap;



                            br.Measure(new Size(cd.ActualWidth, Size.Height));
                            br.Arrange(new Rect(br.DesiredSize));
                            m = Math.Max(m, br.ActualHeight);
                        }
                        i++;

                        //Недопустимый контент. Не влазиет на одну страницу
                        if (m > csz.Height)
                            return;
                    }

                    if (contentHeight + m > csz.Height)
                    {
                        page++;
                        break;
                    }

                    contentHeight += m;
                    m = Double.MinValue;
                    //Добавление строки
                    var rd = new RowDefinition();
                    gr.RowDefinitions.Add(rd);
                    rd.Height = GridLength.Auto;
                    j = 0;
                    foreach (var el in list)
                    {
                        Grid.SetRow(el, gr.RowDefinitions.Count - 1);
                        Grid.SetColumn(el, j++);
                        gr.Children.Add(el);
                    }
                    list.Clear();
                }
            }

            headers.ForEach(h => { h.Text += headers.Count; });

            DocumentViewer.Document = fd.DocumentPaginator.Source;
        }
    }
    public class DocPaginator : DocumentPaginator
    {
        private readonly DocumentPaginator _documentPaginator;
        private readonly int _startPageIndex;
        private readonly int _endPageIndex;
        private readonly int _pageCount;

        public DocPaginator(DocumentPaginator documentPaginator, PageRange pageRange)
        {
            // Set document paginator.
            _documentPaginator = documentPaginator;

            // Set page indices.
            _startPageIndex = pageRange.PageFrom - 1;
            _endPageIndex = pageRange.PageTo - 1;

            // Validate and set page count.
            if (_startPageIndex >= 0 &&
                _endPageIndex >= 0 &&
                _startPageIndex <= _documentPaginator.PageCount - 1 &&
                _endPageIndex <= _documentPaginator.PageCount - 1 &&
                _startPageIndex <= _endPageIndex)
                _pageCount = _endPageIndex - _startPageIndex + 1;
        }

        public override bool IsPageCountValid => true;

        public override int PageCount => _pageCount;

        public override IDocumentPaginatorSource Source => _documentPaginator.Source;

        public override Size PageSize { get => _documentPaginator.PageSize; set => _documentPaginator.PageSize = value; }

        public override DocumentPage GetPage(int pageNumber)
        {
            DocumentPage documentPage = _documentPaginator.GetPage(_startPageIndex + pageNumber);

            // Workaround for "FixedPageInPage" exception.
            if (documentPage.Visual is FixedPage fixedPage)
            {
                var containerVisual = new ContainerVisual();
                foreach (object child in fixedPage.Children)
                {
                    var childClone = (UIElement)child.GetType().GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(child, null);

                    FieldInfo parentField = childClone.GetType().GetField("_parent", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (parentField != null)
                    {
                        parentField.SetValue(childClone, null);
                        containerVisual.Children.Add(childClone);
                    }
                }

                return new DocumentPage(containerVisual, documentPage.Size, documentPage.BleedBox, documentPage.ContentBox);
            }

            return documentPage;
        }
    }
}
