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

namespace Client.Interfaces.Production.Corrugator.TaskPlanningKashira
{
    /// <summary>
    /// Interaction logic for IdleItem.xaml
    /// </summary>
    public partial class IdleItem : ControlBase
    {
        public IdleItem()
        {
            InitializeComponent();

            ControlName = "IdleItem";
            ControlTitle = "Работа с простоями";
            FrameName = ControlName;
            InitializeFrom();

            OnLoad = () =>
            {
                SetDefaults();

            };

            OnUnload = () =>
            {

            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverGroup == "PreproductionSample")
                {
                    if (msg.ReceiverName == ControlName)
                    {
                        //ReceivedData.Clear();
                        //if (msg.ContextObject != null)
                        //{
                        //    ReceivedData = (Dictionary<string, string>)msg.ContextObject;
                        //}
                        //ProcessCommand(msg.Action);
                    }
                }
            };

            OnFocusGot = () =>
            {

            };

            OnFocusLost = () =>
            {

            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    //switch (e.Key)
                    //{
                    //    case Key.F1:
                    //        ProcessCommand("help");
                    //        e.Handled = true;
                    //        break;
                    //    case Key.F5:
                    //        SampleGrid.LoadItems();
                    //        e.Handled = true;
                    //        break;

                    //    case Key.Home:
                    //        SampleGrid.SetSelectToFirstRow();
                    //        e.Handled = true;
                    //        break;

                    //    case Key.End:
                    //        SampleGrid.SetSelectToLastRow();
                    //        e.Handled = true;
                    //        break;
                    //}
                }


            };
        }
        /// <summary>
        /// процессор форм
        /// </summary>
        public FormHelper Form { get; set; }

        private DateTime StartDate { get; set; }
        private DateTime EndDate { get; set; }

        public int Id { get;set; }
        public int IdIdleDetails { get; set; }
        public int IdStanok {  get; set; }

        private void InitializeFrom()
        {
            Form = new FormHelper();
            //список колонок формы
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="IDIDLES",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=DropdownId,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="DTTM_START",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Control=DateStart,
                    ControlType="dateedit",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

                new FormHelperField()
                {
                    Path="DTTM_END",
                    FieldType=FormHelperField.FieldTypeRef.DateTime,
                    Control=DateEnd,
                    ControlType="dateedit",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },

            };

            DateEnd.EditValueChanged += DateEnd_EditValueChanged;
            DateStart.EditValueChanged += DateEnd_EditValueChanged;

            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = Status;

            Form.OnValidate = (bool valid, string message) =>
            {
                if (valid)
                {
                    Status.Text = "";
                }
                else
                {
                    Status.Text = "Не все поля заполнены верно";
                }
            };


        }

        private void DateEnd_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (DateStart.EditValue is DateTime dateStart)
                if (DateEnd.EditValue is DateTime dateEnd)
                {
                    DropdownDuration.Text = (dateEnd- dateStart).TotalMinutes.ToString();

                }
        }

        private void SetDefaults()
        {
        }

        public void Edit(Dictionary<string, string> item)
        {
            /*OrderText.Text = item.CheckGet(TaskPlaningDataSet.Dictionary.ProductionTaskId).ToInt().ToString();
            OrderName.Text = item.CheckGet("PRODUCTION_TASK_NUMBER").ToString();
            FormatText.Text = item.CheckGet(TaskPlaningDataSet.Dictionary.Format).ToInt().ToString();*/

            Id = item.CheckGet(TaskPlaningDataSet.Dictionary.DropdownId).ToInt();
            PrimaryKeyValue = Id.ToString();

            FrameTitle = "Простой " + PrimaryKey;

            GetData();


            Form.Validate();


            //ControlName = $"{ControlName}_{item.CheckGet(TaskPlaningDataSet.Dictionary.ProductionTaskId).ToInt().ToString()}";
            //Central.WM.Show(ControlName, $"Простой #{Id.ToString()}", true, "add", this);
        }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        public async void GetData()
        {
            DisableControls();

            bool resume = Id != 0;

            if (resume)
            {
                var p = new Dictionary<string, string>();
                {
                    p.CheckAdd("DOPL_ID", Id.ToString());
                }

                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Production");
                q.Request.SetParam("Object", "TaskPlanningKashira");
                q.Request.SetParam("Action", "DownTimeGet");
                q.Request.SetParams(p);

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
                            Form.SetValues(ds);

                            DropdownId.Text = Id.ToString();

                            if (ds.Items.Count > 0)
                            {
                                StartDate = ds.Items[0].CheckGet("DTTM_START").ToDateTime();
                                EndDate = ds.Items[0].CheckGet("DTTM_END").ToDateTime();
                                IdIdleDetails = ds.Items[0].CheckGet("ID_IDLE_DETAILS").ToInt();
                                IdStanok = ds.Items[0].CheckGet("ID_ST").ToInt();

                                //DateStart.EditValue = StartDate;
                                //DateEnd.EditValue = EndDate;
                            }


                        }

                        Show();
                    }
                }
                else
                {
                    q.ProcessError();
                }

            }
            else
            {
                Show();
            }

            EnableControls();
        }

        /// <summary>
        /// активация контролов
        /// </summary>
        public void EnableControls()
        {
            FormToolbar.IsEnabled = true;
        }

        /// <summary>
        /// блокировка контролов на время выполнения запроса
        /// </summary>
        public void DisableControls()
        {
            FormToolbar.IsEnabled = false;
        }

        private async Task<bool> SaveAsync(DateTime startDate, DateTime endDate)
        {
            bool result = false;
            
            // необходимо сохранить данные и оповестить форму о том что для данного простоя необходимо найти место

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("DOPL_ID", Id.ToString());
                p.CheckAdd("ID_ST", IdStanok.ToString());
                p.CheckAdd("ID_IDLE_DETAILS", IdIdleDetails.ToString());
                p.CheckAdd("DTTM_START", startDate.ToString("dd.MM.yyyy HH:mm:00")); // dd.mm.yyyy hh24:mi:ss
                p.CheckAdd("DTTM_END", endDate.ToString("dd.MM.yyyy HH:mm:00"));
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Production");
            q.Request.SetParam("Object", "TaskPlanningKashira");
            q.Request.SetParam("Action", "DownTimeSave");
            q.Request.SetParams(p);

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            if (q.Answer.Status == 0)
            {
                result = true;
            }
            

            return result;
        }


       

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            bool resume = true;
            // Необходимо проверить данные изменились?

            if (DateStart.EditValue is DateTime dateStart)
                if (DateEnd.EditValue is DateTime dateEnd)
                {
                    if (StartDate.ToString("dd.MM.yyyy HH:mm") != dateStart.ToString("dd.MM.yyyy HH:mm") ||
                        EndDate.ToString("dd.MM.yyyy HH:mm") != dateEnd.ToString("dd.MM.yyyy HH:mm"))
                    {
                        var d = new DialogWindow(
                            "Изменить время запланированного простоя?",
                            "Изменение времени запланированного простоя!",
                            "Для сохранения нажмите \"Да\".",
                            DialogWindowButtons.YesNo);

                        d.ShowDialog();

                        if (d.DialogResult == true)
                        {
                            if (await SaveAsync(dateStart, dateEnd))
                            {
                                // предупредить форму что данные простоя изменились

                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "TaskPlanning",
                                    ReceiverName = "IdleItem",
                                    SenderName = "IdleItem",
                                    Action = "Refresh",
                                    Message = $"{Id},{dateStart.ToString("dd.MM.yyyy HH:mm:00")},{dateEnd.ToString("dd.MM.yyyy HH:mm:00")}",
                                });
                            }
                            else
                            {
                                // произошла ошибка, нужно вывести сообщение
                                resume = false;
                            }
                        }
                    }
                }


            if (resume)
            {
                Close();
            }
            else
            {

            }

        }
    }
}
