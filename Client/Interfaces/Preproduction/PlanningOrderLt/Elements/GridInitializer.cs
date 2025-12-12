using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction.PlanningOrderLt.Elements
{
    public static class GridInitializer
    {
        public static void IniGrid(GridBox4 grid, GridBox4.OnLoadItemsDelegate loadItems, StackPanel toolbar, CommandController command, bool highlightScheme10 = false)
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                  Header  = "#",
                  Path = "NUM",
                  ColumnType = ColumnTypeRef.Integer,
                  Width2 = 5,
                },
                new DataGridHelperColumn
                {
                    Header = "Время начала",
                    Path = "START_DTTM",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 15,
                    Visible = false,
                },
                new DataGridHelperColumn()
                {
                    Header = "Время окончания",
                    Path = "FINISH_DTTM",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 15,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";
                                
                                if (row.CheckGet("FINISH_DTTM").ToDateTime() > row.CheckGet("SHIP_DTTM").ToDateTime())
                                {
                                    color = HColor.Red;
                                }
                                
                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Время отгрузки",
                    Path = "SHIP_DTTM",
                    ColumnType = ColumnTypeRef.DateTime,
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header = "Артикул",
                    Path = "GOODS_CODE",
                    Description = "",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "Наименование",
                    Path = "GOODS_NAME",
                    Description = "",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 56,
                },
                new DataGridHelperColumn
                {
                    Header  = "Статус",
                    Path = "TASK_STATUS_TITLE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                },
                new DataGridHelperColumn
                {
                    Header = "Номер",
                    Path = "TASK_NUMBER",
                    Description = "",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 9,
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во в заявке, шт",
                    Path = "TASK_QUANTITY",
                    Description = "",
                    TotalsType = TotalsTypeRef.Summ,
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 12,
                },
                new DataGridHelperColumn
                {
                    Header = "Произведено, шт",
                    Path = "QTY_P",
                    Description = "",
                    TotalsType = TotalsTypeRef.Summ,
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 8,
                },
                new DataGridHelperColumn
                {
                    Header = "Заявка",
                    Path = "ORDER_TITLE",
                    Description = "",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 37,
                },
                new DataGridHelperColumn
                {
                    Header = "Время на производство, мин",
                    Path = "PRODUCTION_TIME",
                    Description = "",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 12,
                },
                new DataGridHelperColumn
                {
                    Header = "Время на перестройку, мин",
                    Path = "CHANGE_TIME",
                    Description = "",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 12,
                },
                new DataGridHelperColumn
                {
                    Header = "Примечание",
                    Path = "NOTE",
                    Description = "",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 15,
                },
                new DataGridHelperColumn
                {
                    Header = "TECN_ID",
                    Path = "TECN_ID",
                    Description = "",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false,
                },
                new DataGridHelperColumn
                {
                    Header = "Передний борт 1",
                    Path = "FRONT_BOARD_1",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";
                                
                                if (row.CheckGet("COUNT_INK1").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_1").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("FRONT_BOARD_1")))
                                {
                                    color = HColor.Red;
                                }
                                
                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Передний борт 2",
                    Path = "FRONT_BOARD_2",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";
                                
                                if (row.CheckGet("COUNT_INK2").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_2").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("FRONT_BOARD_2")))
                                {
                                    color = HColor.Red;
                                }
                                
                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Передний борт 3",
                    Path = "FRONT_BOARD_3",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";
                                
                                if (row.CheckGet("COUNT_INK3").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_3").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("FRONT_BOARD_3")))
                                {
                                    color = HColor.Red;
                                }
                                
                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Передний борт 4",
                    Path = "FRONT_BOARD_4",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";
                                
                                if (row.CheckGet("COUNT_INK4").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_4").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("FRONT_BOARD_4")))
                                {
                                    color = HColor.Red;
                                }
                                
                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Крышка 1",
                    Path = "CAP_1",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";
                                
                                if (row.CheckGet("COUNT_INK5").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_5").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("CAP_1")))
                                {
                                    color = HColor.Red;
                                }
                                
                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Крышка 2",
                    Path = "CAP_2",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";
                                
                                if (row.CheckGet("COUNT_INK6").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_6").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("CAP_2")))
                                {
                                    color = HColor.Red;
                                }
                                
                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Крышка 3",
                    Path = "CAP_3",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";
                                
                                if (row.CheckGet("COUNT_INK7").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_7").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("CAP_3")))
                                {
                                    color = HColor.Red;
                                }
                                
                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Крышка 4",
                    Path = "CAP_4",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";
                                
                                if (row.CheckGet("COUNT_INK8").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_8").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("CAP_4")))
                                {
                                    color = HColor.Red;
                                }
                                
                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Задний борт 1",
                    Path = "TAILGATE_1",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";
                                
                                if (row.CheckGet("COUNT_INK9").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_9").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("TAILGATE_1")))
                                {
                                    color = HColor.Red;
                                }
                                
                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Задний борт 2",
                    Path = "TAILGATE_2",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";
                                
                                if (row.CheckGet("COUNT_INK10").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_10").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("TAILGATE_2")))
                                {
                                    color = HColor.Red;
                                }
                                
                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Задний борт 3",
                    Path = "TAILGATE_3",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";
                                
                                if (row.CheckGet("COUNT_INK11").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_11").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("TAILGATE_3")))
                                {
                                    color = HColor.Red;
                                }
                                
                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Задний борт 4",
                    Path = "TAILGATE_4",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";
                                
                                if (row.CheckGet("COUNT_INK12").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_12").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("TAILGATE_4")))
                                {
                                    color = HColor.Red;
                                }
                                
                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Внутри 1",
                    Path = "INSIDE_1",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";
                                
                                if (row.CheckGet("COUNT_INK13").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_13").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("INSIDE_1")))
                                {
                                    color = HColor.Red;
                                }
                                
                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Внутри 2",
                    Path = "INSIDE_2",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";
                                
                                if (row.CheckGet("COUNT_INK14").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_14").ToInt() == 0  && !string.IsNullOrWhiteSpace(row.CheckGet("INSIDE_2")))
                                {
                                    color = HColor.Red;
                                }
                                
                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "TASK_ID",
                    Description = "Идентификатор ПЗЛТ",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                },
                new DataGridHelperColumn
                {
                    Header = "ID_SCHEME",
                    Path = "ID_SCHEME",
                    Description = "Идентификатор схемы",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false,
                    Exportable = false,
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 1",
                    Path = "COUNT_INK1",
                    Description = "Количество чернил 1",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 1",
                    Path = "ORDER_FLAG_1",
                    Description = "Флаг заказа 1",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 2",
                    Path = "COUNT_INK2",
                    Description = "Количество чернил 2",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 2",
                    Path = "ORDER_FLAG_2",
                    Description = "Флаг заказа 2",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 3",
                    Path = "COUNT_INK3",
                    Description = "Количество чернил 3",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 3",
                    Path = "ORDER_FLAG_3",
                    Description = "Флаг заказа 3",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 4",
                    Path = "COUNT_INK4",
                    Description = "Количество чернил 4",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 4",
                    Path = "ORDER_FLAG_4",
                    Description = "Флаг заказа 4",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 5",
                    Path = "COUNT_INK5",
                    Description = "Количество чернил 5",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 5",
                    Path = "ORDER_FLAG_5",
                    Description = "Флаг заказа 5",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 6",
                    Path = "COUNT_INK6",
                    Description = "Количество чернил 6",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 6",
                    Path = "ORDER_FLAG_6",
                    Description = "Флаг заказа 6",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 7",
                    Path = "COUNT_INK7",
                    Description = "Количество чернил 7",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 7",
                    Path = "ORDER_FLAG_7",
                    Description = "Флаг заказа 7",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 8",
                    Path = "COUNT_INK8",
                    Description = "Количество чернил 8",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 8",
                    Path = "ORDER_FLAG_8",
                    Description = "Флаг заказа 8",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 9",
                    Path = "COUNT_INK9",
                    Description = "Количество чернил 9",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 9",
                    Path = "ORDER_FLAG_9",
                    Description = "Флаг заказа 9",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 10",
                    Path = "COUNT_INK10",
                    Description = "Количество чернил 10",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 10",
                    Path = "ORDER_FLAG_10",
                    Description = "Флаг заказа 10",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 11",
                    Path = "COUNT_INK11",
                    Description = "Количество чернил 11",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 11",
                    Path = "ORDER_FLAG_11",
                    Description = "Флаг заказа 11",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 12",
                    Path = "COUNT_INK12",
                    Description = "Количество чернил 12",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 12",
                    Path = "ORDER_FLAG_12",
                    Description = "Флаг заказа 12",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 13",
                    Path = "COUNT_INK13",
                    Description = "Количество чернил 13",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 13",
                    Path = "ORDER_FLAG_13",
                    Description = "Флаг заказа 13",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 14",
                    Path = "COUNT_INK14",
                    Description = "Количество чернил 14",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 14",
                    Path = "ORDER_FLAG_14",
                    Description = "Флаг заказа 14",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "PRTS_ID",
                    Path = "PRTS_ID",
                    Description = "ИД статус заявки",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "ID_ST",
                    Path = "ID_ST",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "DOPL_ID",
                    Path = "DOPL_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "PRCP_ID",
                    Path = "PRCP_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Exportable = false,
                    Visible = false
                }
            };
            grid.SetColumns(columns);
            grid.SetPrimaryKey("PRCP_ID");
            grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            grid.OnLoadItems = loadItems;
            grid.Toolbar = toolbar;
            grid.Commands = command;
            grid.AutoUpdateInterval = 0;
            grid.EnableSortingGrid = false;

            grid.OnSelectItem = item =>
            {
                command.ProcessSelectItem(item);
            };
            
            if (highlightScheme10)
            {
                grid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>
                {
                    {
                        StylerTypeRef.BackgroundColor,
                        row =>
                        {
                            var result = DependencyProperty.UnsetValue;
                            var color = "";

                            if (row.CheckGet("ID_SCHEME").ToInt() == 10)
                            {
                                color = HColor.Yellow;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result = color.ToBrush();
                            }

                            return result;
                        }
                    },
                    {
                        StylerTypeRef.FontWeight,
                        row =>
                        {
                            var fontWeight = new FontWeight();
                            fontWeight = FontWeights.Normal;

                            if (row.CheckGet("DOPL_ID").ToInt() > 0)
                            {
                                fontWeight = FontWeights.Bold;
                            }

                            return fontWeight;
                        }
                    }
                };
            }
            
            grid.Init();
        }

        public static void InitGridTaskList(GridBox4 grid, GridBox4.OnLoadItemsDelegate loadItems, StackPanel toolbar, CommandController command)
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header = "Наименование",
                    Path = "GOODS_NAME",
                    Description = "",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 44,
                },
                new DataGridHelperColumn
                {
                  Header  = "Статус",
                  Path = "TASK_STATUS_TITLE",
                  ColumnType = ColumnTypeRef.String,
                  Width2 = 7,
                },
                new DataGridHelperColumn
                {
                    Header = "Номер",
                    Path = "TASK_NUMBER",
                    Description = "",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 9,
                },
                new DataGridHelperColumn
                {
                    Header = "Количество, шт",
                    Path = "TASK_QUANTITY",
                    Description = "",
                    TotalsType = TotalsTypeRef.Summ,
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 8,
                },
                new DataGridHelperColumn
                {
                    Header = "Произведено, шт",
                    Path = "BALANCE_QUANTITY_PRODUCED",
                    Description = "",
                    TotalsType = TotalsTypeRef.Summ,
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 8,
                },
                new DataGridHelperColumn
                {
                    Header = "Станок",
                    Path = "MACHINE_NAME",
                    Description = "",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 12,
                },
                new DataGridHelperColumn
                {
                    Header = "Заявка",
                    Path = "ORDER_TITLE",
                    Description = "",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 33,
                },
                new DataGridHelperColumn
                {
                    Header = "Рабочий центр",
                    Path = "WORK_CENTER_NAME",
                    Description = "",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 17,
                },
                new DataGridHelperColumn
                {
                    Header = "Схема производства",
                    Path = "PRODUCTION_SCHEME_NAME",
                    Description = "",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "Примечание",
                    Path = "ORDER_NOTE_GENERAL",
                    Description = "примечание ОПП и складу",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "Передний борт 1",
                    Path = "FRONT_BOARD_1",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("COUNT_INK1").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_1").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("FRONT_BOARD_1")))
                                {
                                    color = HColor.Red;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Передний борт 2",
                    Path = "FRONT_BOARD_2",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("COUNT_INK2").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_2").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("FRONT_BOARD_2")))
                                {
                                    color = HColor.Red;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Передний борт 3",
                    Path = "FRONT_BOARD_3",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("COUNT_INK3").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_3").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("FRONT_BOARD_3")))
                                {
                                    color = HColor.Red;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Передний борт 4",
                    Path = "FRONT_BOARD_4",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("COUNT_INK4").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_4").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("FRONT_BOARD_4")))
                                {
                                    color = HColor.Red;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Крышка 1",
                    Path = "CAP_1",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("COUNT_INK5").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_5").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("CAP_1")))
                                {
                                    color = HColor.Red;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Крышка 2",
                    Path = "CAP_2",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("COUNT_INK6").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_6").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("CAP_2")))
                                {
                                    color = HColor.Red;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Крышка 3",
                    Path = "CAP_3",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("COUNT_INK7").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_7").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("CAP_3")))
                                {
                                    color = HColor.Red;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Крышка 4",
                    Path = "CAP_4",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("COUNT_INK8").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_8").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("CAP_4")))
                                {
                                    color = HColor.Red;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Задний борт 1",
                    Path = "TAILGATE_1",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("COUNT_INK9").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_9").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("TAILGATE_1")))
                                {
                                    color = HColor.Red;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Задний борт 2",
                    Path = "TAILGATE_2",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("COUNT_INK10").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_10").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("TAILGATE_2")))
                                {
                                    color = HColor.Red;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Задний борт 3",
                    Path = "TAILGATE_3",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("COUNT_INK11").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_11").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("TAILGATE_3")))
                                {
                                    color = HColor.Red;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Задний борт 4",
                    Path = "TAILGATE_4",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("COUNT_INK12").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_12").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("TAILGATE_4")))
                                {
                                    color = HColor.Red;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Внутри 1",
                    Path = "INSIDE_1",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("COUNT_INK13").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_13").ToInt() == 0 && !string.IsNullOrWhiteSpace(row.CheckGet("INSIDE_1")))
                                {
                                    color = HColor.Red;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "Внутри 2",
                    Path = "INSIDE_2",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 7,
                    Stylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                    {
                        {
                            StylerTypeRef.BackgroundColor, row =>
                            {
                                var result = DependencyProperty.UnsetValue;
                                var color = "";

                                if (row.CheckGet("COUNT_INK14").ToInt() == 0 &&
                                    row.CheckGet("FLAG_ORDER_14").ToInt() == 0  && !string.IsNullOrWhiteSpace(row.CheckGet("INSIDE_2")))
                                {
                                    color = HColor.Red;
                                }

                                if (!string.IsNullOrEmpty(color))
                                {
                                    result = color.ToBrush();
                                }

                                return result;
                            }
                        }
                    }
                },
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "TASK_ID",
                    Description = "Идентификатор ПЗЛТ",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 1",
                    Path = "COUNT_INK1",
                    Description = "Количество чернил 1",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 1",
                    Path = "ORDER_FLAG_1",
                    Description = "Флаг заказа 1",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 2",
                    Path = "COUNT_INK2",
                    Description = "Количество чернил 2",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 2",
                    Path = "ORDER_FLAG_2",
                    Description = "Флаг заказа 2",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 3",
                    Path = "COUNT_INK3",
                    Description = "Количество чернил 3",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 3",
                    Path = "ORDER_FLAG_3",
                    Description = "Флаг заказа 3",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 4",
                    Path = "COUNT_INK4",
                    Description = "Количество чернил 4",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 4",
                    Path = "ORDER_FLAG_4",
                    Description = "Флаг заказа 4",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 5",
                    Path = "COUNT_INK5",
                    Description = "Количество чернил 5",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 5",
                    Path = "ORDER_FLAG_5",
                    Description = "Флаг заказа 5",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 6",
                    Path = "COUNT_INK6",
                    Description = "Количество чернил 6",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 6",
                    Path = "ORDER_FLAG_6",
                    Description = "Флаг заказа 6",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 7",
                    Path = "COUNT_INK7",
                    Description = "Количество чернил 7",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 7",
                    Path = "ORDER_FLAG_7",
                    Description = "Флаг заказа 7",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 8",
                    Path = "COUNT_INK8",
                    Description = "Количество чернил 8",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 8",
                    Path = "ORDER_FLAG_8",
                    Description = "Флаг заказа 8",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 9",
                    Path = "COUNT_INK9",
                    Description = "Количество чернил 9",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 9",
                    Path = "ORDER_FLAG_9",
                    Description = "Флаг заказа 9",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 10",
                    Path = "COUNT_INK10",
                    Description = "Количество чернил 10",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 10",
                    Path = "ORDER_FLAG_10",
                    Description = "Флаг заказа 10",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 11",
                    Path = "COUNT_INK11",
                    Description = "Количество чернил 11",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 11",
                    Path = "ORDER_FLAG_11",
                    Description = "Флаг заказа 11",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 12",
                    Path = "COUNT_INK12",
                    Description = "Количество чернил 12",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 12",
                    Path = "ORDER_FLAG_12",
                    Description = "Флаг заказа 12",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 13",
                    Path = "COUNT_INK13",
                    Description = "Количество чернил 13",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 13",
                    Path = "ORDER_FLAG_13",
                    Description = "Флаг заказа 13",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Кол-во чернил 14",
                    Path = "COUNT_INK14",
                    Description = "Количество чернил 14",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "Флаг заказа 14",
                    Path = "ORDER_FLAG_14",
                    Description = "Флаг заказа 14",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "ID_SCHEME",
                    Path = "ID_SCHEME",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "AVAILABLE_311",
                    Path = "AVAILABLE_311",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "AVAILABLE_312",
                    Path = "AVAILABLE_312",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "AVAILABLE_321",
                    Path = "AVAILABLE_321",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
                new DataGridHelperColumn
                {
                    Header = "AVAILABLE_322",
                    Path = "AVAILABLE_322",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 7,
                    Visible = false
                },
            };
            grid.SetColumns(columns);
            grid.SetPrimaryKey("TASK_ID");
            grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            grid.Toolbar = toolbar;
            grid.AutoUpdateInterval = 0;
            grid.OnLoadItems = loadItems;
            grid.Commands = command;
            grid.Init();
        }
    }
}