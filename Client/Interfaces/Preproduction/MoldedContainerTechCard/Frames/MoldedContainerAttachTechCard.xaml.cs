using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using DevExpress.DirectX.StandardInterop.Direct2D;
using DevExpress.DocumentServices.ServiceModel.DataContracts;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using SharpVectors.Dom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Форма выбора техкарты при копировании информации
    /// Страница техкарт литой тары.
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class MoldedContainerAttachTechCard : ControlBase
    {
        public MoldedContainerAttachTechCard()
        {

            InitializeComponent();

            FrameName = "moldedcontainerattachtechcard_";
            OnGetFrameTitle = () =>
            {
                return "Техкарта по заявке от клиента";
            };
            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }

            };
            OnUnload = () =>
            {
                AttachId = 0;

            };
            Commander.SetCurrentGridName("main");
            Commander.SetCurrentGroup("item");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "select",
                    Group = "main_form",
                    Enabled = false,
                    Title = "Выбрать",
                    Description = "Выбрать",
                    ButtonUse = true,
                    ButtonName = "SelectButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        Close();

                    },
                    CheckEnabled = () =>
                    {
                        var result = false;
                        var row = TechCardsGrid.SelectedItem;
                        if (row.CheckGet("TECN_ID").ToInt() > 0)
                        {
                            AttachId = row.CheckGet("TECN_ID").ToInt();
                            result = true;
                        }
                        return result;
                    }
                });
                Commander.Add(new CommandItem()
                {
                    Name = "close",
                    Group = "attach_form",
                    Enabled = true,
                    Title = "Отмена",
                    Description = "Закрыть форму без сохранения",
                    ButtonUse = true,
                    ButtonName = "CancelButton",
                    MenuUse = false,
                    Action = () =>
                    {
                        AttachId = 0;
                        Close();
                    },
                });
            }
            Commander.Init(this);
            OnLoad = () =>
            {

                FormInit();
                TechCardsGridInit();
                TechCardsGridSearch.Focus();
            };
            
        }

        /// <summary>
        /// ИД техкарты, данные которой будем копировать
        /// </summary>
        public int AttachId { get; set; }

        public DispatcherTimer TemplateTimeoutTimer;

        public FormHelper Form { get; set; }
        private void FormInit()
        {
            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="SEARCH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=TechCardsGridSearch,
                    ControlType="TextBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    }
                },
            };
            Form.SetFields(fields);
        }

        public void Show()
        {
            this.MinHeight = 150;
            this.MinWidth = 400;
            ControlTitle = "Техкарта ЛТ";
            Dictionary<string, string> windowParametrs = new Dictionary<string, string>();
            windowParametrs.Add("no_resize", "1");
            windowParametrs.Add("center_screen", "1");
            Central.WM.FrameMode = 2;
            Central.WM.Show(FrameName, ControlTitle, false, "add", this, "top", windowParametrs);
        }
        private void TechCardsGridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="#",
                    Path="TECN_ID",
                    Width2=6,
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=true,
                },
                new DataGridHelperColumn
                {
                    Header="Артикул",
                    Path="SKU",
                    ColumnType=ColumnTypeRef.String,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Покупатель",
                    Path="CUSTOMER_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=38,
                },
                new DataGridHelperColumn
                {
                    Header="Дата создания",
                    Path="CREATED_DTTM",
                    ColumnType=ColumnTypeRef.DateTime,
                    Width2=14,
                    Format="dd.MM.yyyy",
                },
                new DataGridHelperColumn
                {
                    Header="Тип продукции",
                    Path="PRODUCT_TYPE_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=12,
                },
                new DataGridHelperColumn
                {
                    Header="Поддон",
                    Path="PALLET_NAME",
                    Doc="Наименование товара",
                    ColumnType=ColumnTypeRef.String,
                    Width2=9,
                },
                new DataGridHelperColumn
                {
                    Header="Цвет",
                    Path="COLOR",
                    ColumnType=ColumnTypeRef.String,
                    Width2=7,
                },
                new DataGridHelperColumn
                {
                    Header="id_tk",
                    Path="STATUS",
                    Width2=6,
                    ColumnType=ColumnTypeRef.Integer,
                    Visible=false
                },
            };
            TechCardsGrid.SetColumns(columns);
            TechCardsGrid.SetPrimaryKey("TECN_ID");
            TechCardsGrid.SetSorting("CREATED_DTTM", ListSortDirection.Descending);
            TechCardsGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            TechCardsGrid.Toolbar = TechCardsToolbar;
            TechCardsGrid.ItemsAutoUpdate = false;
            TechCardsGrid.SearchText = TechCardsGridSearch;
            TechCardsGrid.Commands = Commander;
            TechCardsGrid.OnSelectItem = (selectItem) =>
            {
                AttachId = TechCardsGrid.SelectedItem.CheckGet("ID").ToInt();
            };
            TechCardsGrid.OnLoadItems = TechCardsLoadItems;
            TechCardsGrid.Init();
        }

        private async void TechCardsLoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "MoldedContainer");
            q.Request.SetParam("Action", "ListForAttachCopied");

            q.DoQuery();

            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        TechCardsGrid.UpdateItems(ds);
                    }
                }
            }
            else
            {
                q.ProcessError();
            }
            
        }
        public void Close()
        {
            Central.WM.Close(FrameName);
        }

    }
}
