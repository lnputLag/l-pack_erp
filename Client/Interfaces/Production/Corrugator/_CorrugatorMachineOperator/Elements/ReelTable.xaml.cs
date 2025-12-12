using Client.Assets.HighLighters;
using Client.Common;
using System;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Client.Common;

namespace Client.Interfaces.Production.Corrugator.CorrugatorMachineOperator
{
    /// <summary>
    /// таблица с рулонами
    /// </summary>
    /// <author>vlasov_ea</author>   
    public partial class ReelTable : UserControl
    {
        public ReelTable()
        {
            InitializeComponent();
        }

        /// <summary>
        /// id списка рулонов
        /// </summary>
        private int ReelId;

        /// <summary>
        /// инициализация блока
        /// </summary>
        public void Init(int reelId)
        {
            ReelId = reelId;
            RawGridInit();
        }

        /// <summary>
        /// инициализация грида
        /// </summary>
        public void RawGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                     new DataGridHelperColumn
                    {
                        Header=ReelId.ToString(),
                        Path="NAME",
                        Doc="Сырьё на раскате",
                        ColumnType=ColumnTypeRef.String,
                        Width=155,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Вес, кг",
                        Path="WEIGHT",
                        Doc="Вес",
                        ColumnType=ColumnTypeRef.Integer,
                        Width=45,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Номер рулона",
                        Path="NUM",
                        Doc="Номер рулона",
                        ColumnType=ColumnTypeRef.String,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Статус",
                        Path="NAME_STATUS",
                        Doc="Статус",
                        ColumnType=ColumnTypeRef.String,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header="описание",
                        Path="DESCRIPTION",
                        Doc="описание",
                        ColumnType=ColumnTypeRef.String,
                        Hidden = true,
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Дата производства (наша)",
                        Path = "CREATED_ROLL",
                        ColumnType = ColumnTypeRef.String,
                        Hidden = true
                    },
                    new DataGridHelperColumn
                    {
                        Header = "Дата производства (стороняя)",
                        Path = "PRODUCED_ROLL",
                        ColumnType = ColumnTypeRef.String,
                        Hidden = true
                    }
                };

                RawGrid.UseRowHeader = false;
                RawGrid.UseSorting = false;
                RawGrid.FontSize = 16;
                RawGrid.FontWeight = FontWeights.Bold;
                RawGrid.Grid.RowHeight = 25;
                RawGrid.SetColumns(columns);
                RawGrid.Init();

                RawGrid.OnLoadItems = CurremtMachineRawGridLoadItems;
                
                RawGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result = DependencyProperty.UnsetValue;
                            var color = "";

                            if (!string.IsNullOrEmpty(row.CheckGet("PRODUCED_ROLL")))
                            {
                                DateTime producedDate = DateTime.Parse(row.CheckGet("PRODUCED_ROLL"));
                                DateTime currentDate = DateTime.Now;

                                int daysDifference = (currentDate - producedDate).Days;

                                if (daysDifference <= 30)
                                {
                                    color = HColor.Green;
                                }
                                else if (daysDifference >= 90)
                                {
                                    color = HColor.Yellow;
                                }
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result = color.ToBrush();
                            }

                            return result;
                        }
                    }
                };

                RawGrid.OnSelectItem = selectedItem =>
                {
                    DescriptionLabel.Text = selectedItem.CheckGet("DESCRIPTION");
                    DescriptionLabel.ToolTip = DescriptionLabel.Text;
                };

                RawGrid.Run();
            }
        }

        public void CurremtMachineRawGridLoadItems()
        {
            RawGridLoadItems(CorrugatorMachineOperator.SelectedMachineId);
        }

        /// <summary>
        /// получение записей
        /// </summary>
        public async void RawGridLoadItems(int machineId)
        {
            try
            {
                //RawGrid.ShowSplash();

                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("ID_ST", machineId.ToString());
                    p.CheckAdd("ID_REEL", ReelId.ToString());
                }

                var q = await LPackClientQuery.DoQueryAsync("Production", "CorrugatorMachineOperator", "RawList", "ITEMS", p);

                if (q.Answer.Status == 0)
                {
                    if (q.Answer.QueryResult != null)
                    {
                        var ds = q.Answer.QueryResult;// ListDataSet.Create(result, "ITEMS");
                        var first = ds.Items.FirstOrDefault();

                        if (first != null)
                        {
                            var num = first.CheckGet("NUM").ToString();
                            RawGrid.FindColumnByName("NAME").Header = $"{ReelId}: {num}";

                            // заполнение заголовка создается в смене позиции
                        }

                        RawGrid.Init();
                        RawGrid.UpdateItems(ds);
                    }
                }
            }
            catch (Exception ex)
            {
                CorrugatorErrors.LogError(ex);
            }
            finally
            {
                //RawGrid.HideSplash();
            }
        }
        
        public void LoadItems()
        {
            RawGrid.LoadItems();
        }
    }
}
