using NPOI.SS.Formula.Functions;
using Client.Common;
using Client.Interfaces.Main;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Client.Interfaces.Main.DataGridHelperColumn;
using Client.Assets.HighLighters;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Interaction logic for AddColorComponent.xaml
    /// Диалог для добавления компонентов краски
    /// <autor>eletskikh_ya</autor>
    /// </summary>
    public partial class AddColorComponent : UserControl
    {
        /// <summary>
        /// конструктор формы, в качестве параметра принимает данные с уже добавленными компонентами
        /// </summary>
        /// <param name="existsComponents"></param>
        public AddColorComponent(List<Dictionary<string,string>> existsComponents = null)
        {
            InitializeComponent();

            ExistsComponents = existsComponents;

            InitForm();
            InitGrid();
            SetDefaults();
        }


        /// <summary>
        /// список компонент краски
        /// </summary>
        List<Dictionary<string, string>> ExistsComponents;

        public string ReceiverName;

        public string FrameName;

        /// <summary>
        /// форма
        /// </summary>
        public FormHelper Form { get; set; }

        public Dictionary<string, string> SelectedItem;

        // Id цвета
        public int Id { get; set; }

        public void InitForm()
        {
            Form = new FormHelper();

            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="RATIO",
                    FieldType=FormHelperField.FieldTypeRef.Double,
                    Control=TextQty,
                    ControlType = "TextBox",
                    Format = "0.00000",

                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                        { FormHelperField.FieldFilterRef.DigitCommaOnly, null },
                        { FormHelperField.FieldFilterRef.MaxLen, 8 },
                        { FormHelperField.FieldFilterRef.MinValue, 0.00001 },
                        { FormHelperField.FieldFilterRef.MaxValue, 1.0 },
                    },
                },
            };

            Form.SetFields(fields);

            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;
        }

        /// <summary>
        /// Инициалитзация грида для отображения возможных компонентов
        /// </summary>
        private void InitGrid()
        {
            var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="ИД",
                        Path="PACO_ID",
                        Doc="ИД",
                        ColumnType=ColumnTypeRef.Integer,
                        MinWidth=50,
                        MaxWidth=50,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Наименование",
                        Path="NAME",
                        Doc="Наименование",
                        ColumnType=ColumnTypeRef.String,
                        MinWidth=160,
                        MaxWidth=250,
                    },
                };

            if (ExistsComponents != null)
            {
                // цветовая маркировка строк
                PaintComponent.RowStylers = new Dictionary<DataGridHelperColumn.StylerTypeRef, DataGridHelperColumn.StylerDelegate>()
                {
                    // Цвета фона строк
                    {
                        DataGridHelperColumn.StylerTypeRef.ForegroundColor,
                        row =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            int IdColor = row.CheckGet("PACO_ID").ToInt();
                            bool currentStatus = false;

                            foreach (var items in ExistsComponents)
                            {
                                if (items.CheckGet("PACO_ID").ToInt() == IdColor)
                                {
                                    currentStatus = true;
                                    break;
                                }
                            }

                            if (currentStatus == true)
                            {
                                color = HColor.Olive;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };
            }
            

            PaintComponent.SetColumns(columns);

            PaintComponent.SetSorting("NAME", ListSortDirection.Ascending);
            PaintComponent.AutoUpdateInterval = 0;
            PaintComponent.Init();
            PaintComponent.OnLoadItems = PaintComponentGridLoad;

            PaintComponent.Run();

            //при выборе строки в гриде, обновляются актуальные действия для записи
            PaintComponent.OnSelectItem = selectedItem =>
            {
                GridCompositionUpdateActions(selectedItem);
            };
        }

        private void GridCompositionUpdateActions(Dictionary<string, string> selectedItem)
        {
            SelectedItem = selectedItem;
        }

        /// <summary>
        /// Загрузка компонентов краски
        /// </summary>
        public async void PaintComponentGridLoad()
        {
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Preproduction");
                q.Request.SetParam("Object", "Color");
                q.Request.SetParam("Action", "ListComponents");

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
                        {
                            var ds = ListDataSet.Create(result, "ITEMS");
                            PaintComponent.UpdateItems(ds);
                        }

                    }
                }
            }
        }

        /// <summary>
        /// Функция редактирования
        /// </summary>
        /// <param name="id">Идентификатор цвет, если 0 то это создание нового цвета</param>
        public void Edit(int id = 0)
        {
            Id = id;
            FrameName = $"AddColorComponent_{Id}";
            Show();
        }

        /// <summary>
        /// Показ формы
        /// </summary>
        public void Show()
        {
            // режим отображения новых фреймов
            //     0=по умолчанию
            //     1=новая вкладка
            //     2=новое окно
            Central.WM.FrameMode = 1;

            Central.WM.Show(FrameName, "Новый компонент", true, "add", this);
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Destroy()
        {
            //отключаем обработчик сообщений
            Messenger.Default.Unregister<ItemMessage>(this);
        }

        public void SetDefaults()
        {
            Form.SetDefaults();
        }

        /// <summary>
        /// сокрытие фрейма
        /// </summary>
        public void Close()
        {
            Central.WM.Close(FrameName);
            Destroy();

            // вернуться на нужную вкладку
            Central.WM.SetActive(ReceiverName);

        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }

        /// <summary>
        /// сохранение\обновление данных
        /// </summary>
        public void Save()
        {
            bool resume = SelectedItem != null;
            string error = "";

            //стандартная валидация данных средствами формы
            if (resume)
            {
                var validationResult = Form.Validate();
                if (!validationResult)
                {
                    resume = false;
                }
            }

            if (resume)
            {
                if (SelectedItem != null)
                {
                    if (ExistsComponents != null)
                    {
                        //RATIO,
                        // Используем тип decimal чтобы исключить ошибку округления
                        decimal ratio = 0;

                        foreach (var items in ExistsComponents)
                        {
                            ratio += (decimal)items.CheckGet("RATIO").ToDouble();
                        }

                        if (ratio + (decimal)TextQty.Text.ToDouble() > 1M)
                        {
                            resume = false;
                            error = $"Общее количество компонентов больше 1.0 вы можете добавить не более {1M-ratio}";

                            TextQty.Text = (1M - ratio).ToString().Replace('.',',');
                        }

                        if (resume)
                        {
                            int componentId = SelectedItem["PACO_ID"].ToInt();
                            foreach (var items in ExistsComponents)
                            {
                                if (items.CheckGet("PACO_ID").ToInt() == componentId)
                                {
                                    resume = false;
                                    error = $"Такая компонента {items.CheckGet("NAME")} уже есть в краске";
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            //отправка данных
            if (resume)
            {

                if (Id != 0)
                {
                    var v = Form.GetValues();

                    v.Add("ID", Id.ToString());
                    v.Add("COMPONENT_ID", SelectedItem["PACO_ID"]);
                    SaveData(v);
                }
                else
                {
                    SelectedItem.Add("RATIO", TextQty.Text);
                    SelectedItem.Add("Id", "0");

                    //отправляем сообщение гриду о необходимости обновить данные
                    Messenger.Default.Send(new ItemMessage()
                        {
                        ReceiverGroup = "colorMain",
                        ReceiverName = ReceiverName,
                        SenderName = FrameName,
                        Action = "AddComponent",
                        ContextObject = SelectedItem,
                    }
                    );

                    Close();
                }
            }
            else
            {
                Form.SetStatus(error, 1);
            }
        }

        /// <summary>
        /// отпаравка данных на сервер
        /// </summary>
        public async void SaveData(Dictionary<string, string> p)
        {
            DisableControls();

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "Color");
            q.Request.SetParam("Action", "AddPaintComponent");

            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                {
                    //отправляем сообщение гриду о необходимости обновить данные
                    Messenger.Default.Send(new ItemMessage()
                    {
                        ReceiverGroup = "colorMain",
                        ReceiverName = ReceiverName,
                        SenderName = FrameName,
                        Action = "Refresh",
                    });

                    Close();
                }
            }
            else
            {
                q.ProcessError();
            }

            EnableControls();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
