using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
using Client.Interfaces.Sales;
using Client.Interfaces.Service.Printing;
using DevExpress.Utils.Design;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static DevExpress.XtraPrinting.Native.ExportOptionsPropertiesNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Client.Interfaces.Preproduction
{
    /// <summary>
    /// Список контактов клиента
    /// </summary>
    /// <author>lavrenteva_ma</author>
    public partial class WebTechnologicalMapPokupatelContactList : ControlBase
    {
        public WebTechnologicalMapPokupatelContactList()
        {
            InitializeComponent();
            OnLoad = () =>
            {
                GridInit();
            };

            OnUnload = () =>
            {
                Grid.Destruct();
            };

            OnMessage = (ItemMessage msg) =>
            {
                if (msg.ReceiverName == ControlName)
                {
                    switch (msg.Action)
                    {
                        case "Refresh":
                            Grid.LoadItems();
                            break;
                    }
                }
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

            {
                Commander.SetCurrentGroup("main");
                {
                    Commander.Add(new CommandItem()
                    {
                        Name = "Add",
                        Enabled = true,
                        Title = "Добавить",
                        Description = "Обновить историю",
                        ButtonUse = true,
                        ButtonName = "AddButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            var contacts = new WebTechnologicalMapAddPokupatelContact();
                            contacts.ReceiverName = this.ControlName;
                            contacts.IdPoco = 0;
                            contacts.IdPok = IdPok;
                            contacts.Show();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "Edit",
                        Enabled = true,
                        Title = "Изменить",
                        Description = "Обновить историю",
                        ButtonUse = true,
                        ButtonName = "EditButton",
                        MenuUse = false,
                        Action = () =>
                        {
                            var contacts = new WebTechnologicalMapAddPokupatelContact();
                            contacts.ReceiverName = this.ControlName;
                            contacts.IdPoco = Grid.SelectedItem.CheckGet("POKUPATEL_POCO_ID").ToInt();
                            contacts.IdPok = IdPok;
                            contacts.Show();
                        },
                    });
                    Commander.Add(new CommandItem()
                    {
                        Name = "Cancel",
                        Enabled = true,
                        Title = "Отмена",
                        ButtonUse = true,
                        ButtonName = "CancelButton",
                        MenuUse = true,
                        Action = () =>
                        {
                            Close();
                        },
                    });
                }
                Commander.Init(this);
            }
        }

        /// <summary>
        /// ИД покупателя
        /// </summary>
        public int IdPok;

        /// <summary>
        /// Имя вкладки, окуда вызвана форма редактирования
        /// </summary>
        public string ReceiverName;


        /// <summary>
        /// Инициализация таблицы
        /// </summary>
        private void GridInit()
        {
            var columns = new List<DataGridHelperColumn>
            {
                new DataGridHelperColumn
                {
                    Header="ИД контакта",
                    Path="POKUPATEL_POCO_ID",
                    ColumnType=ColumnTypeRef.Integer,
                    Width2=10,
                },
                new DataGridHelperColumn
                {
                    Header="Имя",
                    Path="POKUPATEL_CONTACT_NAME",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
                new DataGridHelperColumn
                {
                    Header="Телефон",
                    Path="POKUPATEL_PHONE_TB",
                    ColumnType=ColumnTypeRef.String,
                    Width2=24,
                },
                new DataGridHelperColumn
                {
                    Header="Почта",
                    Path="POKUPATEL_EMAIL",
                    ColumnType=ColumnTypeRef.String,
                    Width2=20,
                },
                new DataGridHelperColumn
                {
                    Header="Контакт для созвона",
                    Path="CALL_FLAG",
                    ColumnType=ColumnTypeRef.Boolean,
                    Width2=17,
                },
                new DataGridHelperColumn
                {
                    Header="Примечание",
                    Path="POKUPATEL_NOTE",
                    ColumnType=ColumnTypeRef.String,
                    Width2=30,
                },
            };
            Grid.SetColumns(columns);

            Grid.SetSorting("POKUPATEL_POCO_ID", ListSortDirection.Ascending);
            Grid.SetPrimaryKey("POKUPATEL_POCO_ID");
            Grid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
            Grid.Commands = Commander;

            Grid.OnLoadItems = LoadItems;

            Grid.Init();
        }

        /// <summary>
        /// Загрузка данных в таблицу
        /// </summary>
        public async void LoadItems()
        {

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Preproduction");
            q.Request.SetParam("Object", "WebTechnologicalMap");
            q.Request.SetParam("Action", "ListContacts");

            q.Request.SetParam("ID_POK", IdPok.ToString());

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
                }
            }
        }

        ///// <summary>
        ///// Заполнение контактов для связи
        ///// </summary>
        //public async void SetContactData()
        //{
        //    var p = new Dictionary<string, string>();
        //    {
        //        p.CheckAdd("ID_POK", PokupatelSelectBox.SelectedItem.Key.ToString());
        //    }
        //    var q = new LPackClientQuery();
        //    q.Request.SetParam("Module", "Preproduction");
        //    q.Request.SetParam("Object", "WebTechnologicalMap");
        //    q.Request.SetParam("Action", "LoadContactData");
        //    q.Request.SetParams(p);

        //    await Task.Run(() =>
        //    {
        //        q.DoQuery();
        //    });
        //    if (q.Answer.Status == 0)
        //    {
        //        var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
        //        if (result != null)
        //        {

        //            var ds = ListDataSet.Create(result, "ITEMS");
        //            PokupatelContactDS = ds;
        //            Form.SetValues(ds);

        //        }
        //    }
        //    else
        //    {
        //        q.ProcessError();
        //    }
        //}

        public void Show()
        {
            FrameName = $"{FrameName}_{IdPok}";

            Central.WM.FrameMode = 1;
            Central.WM.Show(FrameName, $"Контакты покупателя {IdPok}", true, "add", this);
        }

        public void Close()
        {
            Central.WM.SetActive(ReceiverName);
            Central.WM.Close(FrameName);
        }
    }
}
