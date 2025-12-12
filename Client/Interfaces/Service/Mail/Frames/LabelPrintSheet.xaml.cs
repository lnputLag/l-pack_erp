using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Service.Mail
{
    /// <summary>
    /// форма печати ярлыков
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    /// <released>2023-06-22</released>
    /// <changed>2023-06-22</changed>
    public partial class LabelPrintSheet : UserControl
    {
        public LabelPrintSheet()
        {
            InitializeComponent();
            
            RecPerPage=21;

            Id = 0;
            FrameName = "LabelPrintSheet";
            Items=new List<Dictionary<string, string>>();
            PageDS=new ListDataSet();

            PrintPageTimer=new Timeout(
                1,
                () =>
                {
                    PrintOnePage();
                },
                true
            );
            PrintPageTimer.SetIntervalMs(1000);

            InitGrid();
        }

        public List<Dictionary<string, string>> Items { get; set; }
        private PrintDialog PrintDialog { get; set; }
        private ListDataSet PageDS { get; set; }
        private int RecPerPage {get;set;}

        public void Init()
        {
            int pageCont=0;           
            int itemCount=Items.Count;

            if( itemCount > 0 )
            {
                double c=(double)itemCount/(double)RecPerPage;
                pageCont=(int)(Math.Ceiling(c));
            }

            {
                var list=new List<Dictionary<string,string>>();
                for(int j = 1; j <= pageCont; j++)
                {
                    var row=new Dictionary<string,string>();
                    row.CheckAdd("ID",j.ToString());
                    list.Add(row);
                }
                PageDS=ListDataSet.Create(list);
                Grid.LoadItems();
            }

            Show();
        }

        /// <summary>
        /// инициализация блока
        /// </summary>
        public void RenderItems(List<Dictionary<string,string>> items)
        {
            int colMax=3;

            LabelContainer.Children.Clear();
            LabelContainer.RowDefinitions.Clear();

            colMax=LabelContainer.ColumnDefinitions.Count;

            if(items.Count > 0)
            {
                int colIndex=-1;
                int rowIndex=-1;
                int j=0;
                foreach(Dictionary<string, string> row in items)
                {
                    j++;

                    var b = new LabelBlock();

                    {
                        colIndex=colIndex+1;
                        if(colIndex >= (colMax) || j == 1)
                        {
                            colIndex=0;

                            rowIndex=rowIndex+1;
                            var rd = new RowDefinition
                            {
                                Height=GridLength.Auto,
                            };
                            LabelContainer.RowDefinitions.Add(rd);
                        }
                    }

                    {
                        b.RecipientName.Text=row.CheckGet("RECIPIENT");
                        b.RecipientIndex.Text=row.CheckGet("ZIP_CODE");
                        b.RecipientAddress.Text=row.CheckGet("ADDRESS");
                    }

                    LabelContainer.Children.Add(b);
                    System.Windows.Controls.Grid.SetRow(b,rowIndex);
                    System.Windows.Controls.Grid.SetColumn(b,colIndex);
                }

                //Show();
            }
        }

        private void RenderItemsPage(int id)
        {
            /*
                1 2 3 4 5 6 7 8 9
                -------              1  0  0  4
                        -------      2  1  4  8
                                -    3  2  8  12

             */

            //var id=Grid.GetSelectedRowId();    
            if ( id > 0 )
            {
                var list=new List<Dictionary<string,string>>();

                int j=0;
                int start=RecPerPage*(id-1);
                int finish=RecPerPage*(id);
                foreach (Dictionary<string,string> row in Items)
                {
                    if(j >= start && j<finish)
                    {
                        list.Add(row);
                    }
                    j++;
                }

                if(list.Count > 0)
                {
                    RenderItems(list);
                }
            }
        }


        public void InitGrid()
        {
             //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Страница",
                        Path="ID",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=70,
                        MaxWidth=200,
                    },
                };
                Grid.SetColumns(columns);
            };

            Grid.SetSorting("ID", ListSortDirection.Ascending);
            Grid.PrimaryKey = "ID";
            Grid.AutoUpdateInterval=0;
            Grid.SelectItemMode=2;

            Grid.DisableControls=()=>
            {
                GridToolbar.IsEnabled = false;
                Grid.ShowSplash();
            };
                
            Grid.EnableControls=()=>
            {
                GridToolbar.IsEnabled = true;
                Grid.HideSplash();
            };

            Grid.OnLoadItems = async ()=>
            {
                Grid.DisableControls();

                Grid.UpdateItems(PageDS);

                Grid.EnableControls();
            };

            Grid.OnSelectItem = (row) =>
            {
                var id=Grid.GetSelectedRowId();   
                RenderItemsPage(id);
            };
            
            Grid.OnDblClick= (row) =>
            {
                var id=Grid.GetSelectedRowId();   
                RenderItemsPage(id);
            };
            
            Grid.Init();
        }

        private int CurrentPageIndex {get;set;}
        private int MaxPageIndex {get;set;}
        private Timeout PrintPageTimer { get; set; }

        private void PrintStart()
        {
            CurrentPageIndex=1;
            MaxPageIndex=Grid.Items.Count;
            RenderItemsPage(CurrentPageIndex);
            PrintPageTimer.Run();
        }

        private void PrintOnePage()
        {
            //print
            InitPrinter();
            PrintDialog.PrintVisual(PrintCanvas,"");

            CurrentPageIndex=CurrentPageIndex+1;
            if(CurrentPageIndex <= MaxPageIndex)
            {
                RenderItemsPage(CurrentPageIndex);
            }
            else
            {
                PrintPageTimer.Finish();
                Close();
            }
        }

        private void InitPrinter()
        {
            PrintDialog=new PrintDialog();
        }

        private void ProcessCommand(string command)
        {
            command = command.ClearCommand();
            switch (command)
            {
                case "print":
                {
                    //InitPrinter();
                    PrintStart();
                }
                    break;

                 case "setup":
                {
                    InitPrinter();
                    PrintDialog.ShowDialog();                    
                }
                    break;

                case "close":
                {
                    Close();
                }
                    break;
            }
        }


        /// <summary>
        /// имя фрейма,
        /// техническое имя для идентификации таба, может совпадать с именем класса
        /// </summary>
        public string FrameName { get; set; }

        /// <summary>
        /// идентификатор записи, с которой работает форма
        /// (primary key записи таблицы)
        /// </summary>
        public int Id { get; set; }

        
        /// <summary>
        /// отображение фрейма
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 2;

            var frameName = GetFrameName();
            var frameTitle = "";
            if (Id == 0)
            {
                frameTitle = "Печать наклеек";
            }
            else
            {
                frameTitle = "Печать наклеек #{Id}";
            }

            Central.WM.Show(frameName, frameTitle, true, "add", this, "");
            //Central.WM.Show(frameName, frameTitle, true, "add", this, "");
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            var frameName = GetFrameName();
            Central.WM.Close(frameName);
        }

        /// <summary>
        /// формирует уникальный идентификатор фрейма
        /// </summary>
        /// <returns></returns>
        public string GetFrameName()
        {
            string result = "";
            result = $"{FrameName}_{Id}";
            return result;
        }


        private void GridToolbarButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (sender!=null)
            {
                var c = (System.Windows.Controls.Button) sender;
                var tag = c.Tag.ToString();
                ProcessCommand(tag);    
            }
        }

        private void PagesToolbarButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (sender!=null)
            {
                var c = (System.Windows.Controls.Button) sender;
                var tag = c.Tag.ToString();
                ProcessCommand(tag);    
            }
        }


        
    }
}
