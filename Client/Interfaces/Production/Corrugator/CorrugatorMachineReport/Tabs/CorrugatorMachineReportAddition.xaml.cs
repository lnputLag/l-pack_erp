using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static iTextSharp.text.pdf.qrcode.Version;

namespace Client.Interfaces.Production
{
    /// <summary>
    /// Отчёт по браку (лишним метрам) на гофроагрегате
    /// </summary>
    /// <author>vlasov_ea</author> 
    public partial class CorrugatorMachineReportAddition : ControlBase
    {
        private bool RecBlock;
        private string From;
        private object To;
        public CorrugatorMachineReportAddition()
        {
            InitializeComponent();

            ControlTitle = "Брак ГА";
            DocumentationUrl = "/doc/l-pack-erp/";
            RoleName = "[erp]machine_report";

            LeftShiftButton.Click += LeftShiftButton_Click;
            RightShiftButton.Click += RightShiftButton_Click;
            TimeSpanSelectBox.SelectedItemChanged += (DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            {
                SetTimeSpan(TimeSpanSelectBox.SelectedItem.Key);
            };
            FromDate.TextChanged += (object sender, TextChangedEventArgs e) =>
            {
                if (!RecBlock)
                {
                    TimeSpanSelectBox.DropDownListBox.SelectedItem = null;
                    TimeSpanSelectBox.ValueTextBox.Text = "";
                }
                ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
            };
            ToDate.TextChanged += (object sender, TextChangedEventArgs e) =>
            {
                if (!RecBlock)
                {
                    TimeSpanSelectBox.DropDownListBox.SelectedItem = null;
                    TimeSpanSelectBox.ValueTextBox.Text = "";
                }
                ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
            };


            OnLoad = () =>
            {
                GridInit();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnFocusGot = () =>
            {
                Grid.ItemsAutoUpdate = true;
                Grid.Run();
            };

            OnFocusLost = () =>
            {
                Grid.ItemsAutoUpdate = false;
            };



            //регистрация обработчика сообщений
            Messenger.Default.Register<ItemMessage>(this, ProcessMessages);

            //FormInit();
            SetDefaults();

            Commander.SetCurrentGridName("Grid");
            Commander.Add(new CommandItem()
            {
                Name = "refresh",
                Group = "grid_base",
                Enabled = true,
                Title = "Обновить",
                Description = "Обновить данные",
                ButtonUse = true,
                ButtonName = "ShowButton",
                MenuUse = true,
                Action = () =>
                {
                    //ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
                    if (DateTime.Compare(FromDate.Text.ToDateTime(), ToDate.Text.ToDateTime()) > 0)
                    {
                        DialogWindow.ShowDialog($"Дата начала должна быть меньше даты окончания.", "Проверка данных", "", DialogWindowButtons.OK);
                        return;
                    }
                    Grid.LoadItems();
                },
            });

            Commander.SetCurrentGroup("item");
            Commander.Add(new CommandItem()
            {
                Name = "edit",
                Group = "grid_base",
                Title = "Изменить",
                Description = "Изменить",
                MenuUse = true,
                HotKey = "Return|DoubleCLick",
                ButtonUse = true,
                AccessLevel = Role.AccessMode.FullAccess,
                ButtonName = "EditButton",
                Action = () =>
                {
                    Edit(Grid.SelectedItem);
                },
                CheckEnabled = () =>
                {
                    return Grid.SelectedItem != null
                        && Grid.SelectedItem.Count > 0;
                },
            });

            Commander.Add(new CommandItem()
            {
                Name = "delete",
                Group = "grid_base",
                Enabled = true,
                Title = "Удалить",
                Description = "Удалить",
                ButtonUse = true,
                AccessLevel = Role.AccessMode.FullAccess,
                ButtonName = "DeleteButton",
                MenuUse = true,
                Action = () =>
                {
                    Delete(Grid.SelectedItem);
                },
                CheckEnabled = () =>
                {
                    return Grid.SelectedItem != null
                        && Grid.SelectedItem.Count > 0;
                },
            });

            Commander.Init(this);
        }

        /// <summary>
        /// процессор форм
        /// </summary>
        //public FormHelper Form { get; set; }

        public Dictionary<string, string> SelectedItem { get; set; }

        /// <summary>
        /// Инициализация грида
        /// </summary>
        public void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="_ROWNUMBER",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2 = 4,
                },
                new DataGridHelperColumn
                {
                    Header = "Время",
                    Path = "DTTM",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 16,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД",
                    Path = "PRCI_ID",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "ИД ПЗ",
                    Path = "ID_PZ",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header = "Номер ПЗ",
                    Path = "NUM",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 10,
                },
                new DataGridHelperColumn
                {
                    Header="ГА",
                    Path="GA",
                    ColumnType=ColumnTypeRef.String,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Плановая длина",
                    Path = "LENGTH_PZ",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Фактическая длина",
                    Path = "LENGTH_FACT",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Добавлено, м",
                    Path = "DEVIATION_LENGTH",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Добавлено, м2",
                    Path = "DEVIATION_SQUARE",
                    ColumnType = ColumnTypeRef.Integer,
                    Width2 = 6,
                },
                new DataGridHelperColumn
                {
                    Header = "Участок",
                    Path = "SOURCE",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "Причина",
                    Path = "REASON",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 20,
                },
                new DataGridHelperColumn
                {
                    Header = "Комментарий",
                    Path = "COMMENTS",
                    ColumnType = ColumnTypeRef.String,
                    Width2 = 30,
                },
            };

            Grid.SetColumns(columns);
            Grid.SetPrimaryKey("PRCI_ID");
            Grid.OnLoadItems = GridLoadItems;
            Grid.SearchText = SearchText;
            Grid.Toolbar = GridToolbar;
            Grid.Commands = Commander;
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.AutoUpdateInterval = 0;
            Grid.ItemsAutoUpdate = false;
            Grid.EnableFiltering = true;
            Grid.Init();
            //Grid.Run();
            //Grid.Focus();
        }

        /// <summary>
        /// Получение данных 
        /// </summary>
        public async void GridLoadItems()
        {
            DisableControls();

            var resume = true;

            //var f = Form.GetValueByPath("FROM_DATE").ToDateTime();
            //var t = Form.GetValueByPath("TO_DATE").ToDateTime();

            //if (DateTime.Compare(f, t) > 0)
            //{
            //    const string msg = "Дата начала должна быть меньше даты окончания.";
            //    var d = new DialogWindow($"{msg}", "Проверка данных");
            //    d.ShowDialog();
            //    resume = false;
            //}

            if (resume)
            {
                //var p = new Dictionary<string, string>
                //{
                //    ["FROM_DATE"] = Form.GetValueByPath("FROM_DATE"),
                //    ["TO_DATE"] = Form.GetValueByPath("TO_DATE"),
                //};

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "Addition");
                q.Request.SetParam("Action", "List");
                q.Request.SetParam("FROM_DATE", FromDate.Text);
                q.Request.SetParam("TO_DATE", ToDate.Text);

                //q.Request.SetParams(p);

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
                        var ds = ListDataSet.Create(result, "ITEMS");
                        Grid.UpdateItems(ds);
                        From = FromDate.Text;
                        To = ToDate.Text;
                        ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
                    }
                }
            }

            EnableControls();
        }

        public void Edit(Dictionary<string, string> row)
        {
            if (row == null) return;

            var form = new FormExtend()
            {
                FrameName = "AdditionEdit",
                ID = "PRCI_ID",
                Id = row.CheckGet("PRCI_ID").ToInt(),
                Title = $"Добавление метров {row.CheckGet("NUM")}",

                QueryGet = new FormExtend.RequestData()
                {
                    Module = "Production",
                    Object = "Addition",
                    Action = "Get"
                },

                QuerySave = new FormExtend.RequestData()
                {
                    Module = "Production",
                    Object = "Addition",
                    Action = "Save"
                },

                Fields = new List<FormHelperField>()
                    {
                        new FormHelperField()
                        {
                            Path="PCRR_ID",
                            FieldType=FormHelperField.FieldTypeRef.Integer,
                            Description = "Причина:",
                            ControlType = "SelectBox",
                            Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                                { FormHelperField.FieldFilterRef.Required, null },
                            },
                            Width = 400
                        },
                        new FormHelperField()
                        {
                            Path="COMMENTS",
                            FieldType=FormHelperField.FieldTypeRef.String,
                            Description = "Комментарии:",
                            ControlType="TextBox",
                            Width = 400,
                        },
                        //new FormHelperField()
                        //{
                        //    Path="QTY",
                        //    FieldType=FormHelperField.FieldTypeRef.Integer,
                        //    Description = "Количество:",
                        //    ControlType="TextBox",
                        //    Width = 400,
                        //},
                    }
            };

            form["PCRR_ID"].OnAfterCreate += (control) =>
            {
                var Reason = control as SelectBox;
                Reason.Autocomplete = true;

                FormHelper.ComboBoxInitHelper(control as SelectBox, "Production", "Rework", "ListReason", "ID", "REASON", null, true);
            };

            form.OnAfterSave += (id, result) =>
            {
                Grid.LoadItems();
            };

            form.Show();
        }

        public async void Delete(Dictionary<string, string> row)
        {
            if (DialogWindow.ShowDialog($"Вы действительно хотите удалить добавление метров {row.CheckGet("DTTM")}?", "Удаление", "", DialogWindowButtons.NoYes) != true) return;

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "Addition");
            q.Request.SetParam("Action", "Delete");
            q.Request.SetParam("PRCI_ID", row.CheckGet(Grid.GetPrimaryKey()));

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    Grid.SelectRowPrev();
                    Grid.LoadItems();
                }
            }
        }

        /// <summary>
        /// Деактивация контролов
        /// </summary>
        public void DisableControls()
        {
            Grid.ShowSplash();
            GridToolbar.IsEnabled = false;
        }

        /// <summary>
        /// Активация контролов
        /// </summary>
        public void EnableControls()
        {
            Grid.HideSplash();
            GridToolbar.IsEnabled = true;
        }

        /// <summary>
        /// Управление доступом
        /// </summary>
        /// <param name="roleCode"></param>
        private void ProcessPermissions(string roleCode = "")
        {

        }

        /// <summary>
        /// Установка значений по умолчанию
        /// </summary>
        public void SetDefaults()
        {
            //Form.SetValueByPath("FROM_DATE", DateTime.Now.AddDays(-1).ToString("dd.MM.yyyy"));
            //Form.SetValueByPath("TO_DATE", DateTime.Now.ToString("dd.MM.yyyy"));

            //ShowButton.Style = (Style)ShowButton.TryFindResource("Button");
            FromDate.EditValue = DateTime.Now;

            TimeSpanSelectBox.SetItems(new Dictionary<string, string>()
            {
                {"Смена",  "Смена"},
                {"День",  "День"},
                {"Неделя",  "Неделя"},
                {"Месяц",  "Месяц"},
            });
            TimeSpanSelectBox.SelectedItem = TimeSpanSelectBox.Items.First();
        }

        /// <summary>
        /// Инициализация формы
        /// </summary>
        //public void FormInit()
        //{
        //    Form = new FormHelper();

        //    //список колонок формы
        //    var fields = new List<FormHelperField>()
        //    {
        //        new FormHelperField()
        //        {
        //            Path = "FROM_DATE",
        //            FieldType = FormHelperField.FieldTypeRef.String,
        //            Control = FromDate,
        //            ControlType = "TextBox",
        //            Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
        //            },
        //        },
        //        new FormHelperField()
        //        {
        //            Path = "TO_DATE",
        //            FieldType = FormHelperField.FieldTypeRef.String,
        //            Control = ToDate,
        //            ControlType = "TextBox",
        //            Filters = new Dictionary<FormHelperField.FieldFilterRef, object> {
        //            },
        //        },
        //    };

        //    Form.SetFields(fields);
        //    Form.ToolbarControl = GridToolbar;
        //}

        /// <summary>
        /// обработчик сообщений
        /// </summary>
        /// <param name="m"></param>
        private void ProcessMessages(ItemMessage m)
        {

        }

        /// <summary>
        /// экспорт записей грида в Excel
        /// </summary>
        private async void ExportToExcel()
        {
            Grid.ItemsExportExcel();
        }


        /// <summary>
        /// Обработчик выбора станка
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void Types_SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }
        private void DateTextChanged(object sender, RoutedEventArgs e)
        {
            ShowButton.Style = (Style)ShowButton.TryFindResource("FButtonPrimary");
        }
        private void SetTimeSpan(string timeSpan)
        {
            var now = (DateTime)FromDate.EditValue;
            RecBlock = true;
            switch (timeSpan)
            {
                case "Смена":
                    if (now.Hour >= 20)
                    {
                        now = new DateTime(now.Year, now.Month, now.Day, 20, 0, 0);
                    }
                    else if (now.Hour >= 8)
                    {
                        now = new DateTime(now.Year, now.Month, now.Day, 8, 0, 0);
                    }
                    else
                    {
                        now = new DateTime(now.Year, now.Month, now.Day, 20, 0, 0);
                        now = now.AddDays(-1);
                    }
                    FromDate.EditValue = now;
                    ToDate.EditValue = now.AddHours(12);
                    break;
                case "День":
                    now = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                    FromDate.EditValue = now;
                    ToDate.EditValue = now.AddDays(1);
                    break;
                case "Неделя":
                    now = now.AddDays(-((int)now.DayOfWeek + 6) % 7);
                    FromDate.EditValue = now;
                    ToDate.EditValue = now.AddDays(7);
                    break;
                case "Месяц":
                    now = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
                    FromDate.EditValue = now;
                    ToDate.EditValue = now.AddMonths(1);
                    break;
            }
            RecBlock = false;
        }

        private void LeftShiftButton_Click(object sender, RoutedEventArgs e)
        {
            var from = (DateTime)FromDate.EditValue;
            RecBlock = true;
            if (TimeSpanSelectBox.SelectedItem.Key == "Месяц")
            {
                ToDate.EditValue = FromDate.EditValue;
                FromDate.EditValue = from.AddMonths(-1);
            }
            else
            {
                var ts = (DateTime)ToDate.EditValue - from;
                ToDate.EditValue = FromDate.EditValue;
                FromDate.EditValue = from - ts;
            }
            RecBlock = false;
        }

        private void RightShiftButton_Click(object sender, RoutedEventArgs e)
        {
            var to = (DateTime)ToDate.EditValue;
            RecBlock = true;
            if (TimeSpanSelectBox.SelectedItem.Key == "Месяц")
            {
                FromDate.EditValue = ToDate.EditValue;
                ToDate.EditValue = to.AddMonths(1);
            }
            else
            {
                var ts = to - (DateTime)FromDate.EditValue;
                FromDate.EditValue = ToDate.EditValue;
                ToDate.EditValue = to + ts;
            }
            RecBlock = false;
        }
    }
}
