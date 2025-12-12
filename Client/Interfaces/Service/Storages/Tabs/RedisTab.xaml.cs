using Client.Assets.HighLighters;
using Client.Common;
using Client.Interfaces.Main;
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
using static Client.Interfaces.Main.DataGridHelperColumn;

namespace Client.Interfaces.Service.Storages
{
    /// <summary>
    /// Файловое хранилище Redis
    /// </summary>
    /// <autor>sviridov_ae</autor>
    public partial class RedisTab : ControlBase
    {
        public RedisTab()
        {
            InitializeComponent();

            ControlSection = "storages_redis";
            RoleName = "[erp]server";
            ControlTitle = "Redis";
            DocumentationUrl = "/doc/l-pack-erp-new/administration/";

            OnMessage = (ItemMessage m) =>
            {
                if (m.ReceiverName == ControlName)
                {
                    Commander.ProcessCommand(m.Action, m);
                }
            };

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
            };

            OnLoad = () =>
            {
                SetDefaults();
                StorageGridInit();
            };

            OnUnload = () =>
            {
            };

            OnFocusGot = () =>
            {
                StorageGrid.Run();
            };

            OnFocusLost = () =>
            {
            };

            OnNavigate = () =>
            {
            };

            Commander.SetCurrentGroup("main");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "help",
                    Enabled = true,
                    Title = "Справка",
                    Description = "Показать справочную информацию",
                    ButtonUse = true,
                    ButtonName = "HelpButton",
                    HotKey = "F1",
                    Action = () =>
                    {
                        Central.ShowHelp(DocumentationUrl);
                    },
                });
            }

            Commander.SetCurrentGridName("StorageGrid");
            {
                Commander.Add(new CommandItem()
                {
                    Name = "upload_file",
                    Enabled = true,
                    Title = "Загрузить файл",
                    Description = "Загрузить файл в файловое хранилище",
                    ButtonUse = true,
                    ButtonName = "UploadFileButton",
                    AccessLevel = Role.AccessMode.FullAccess,
                    Action = () =>
                    {
                        UploadFile();
                    },
                });
            }

            Commander.Init(this);
        }

        private ListDataSet StorageGridDataSet { get; set; }

        private void SetDefaults()
        {
            StorageGridDataSet = new ListDataSet();
        }

        private void StorageGridInit()
        {
            //инициализация грида
            {
                //колонки грида
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="#",
                        Path="_ROWNUMBER",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                    },
                    new DataGridHelperColumn
                    {
                        Header="##",
                        Path="TREE_ITEM",
                        ColumnType=ColumnTypeRef.String,
                        Width2=8,
                    },
                    new DataGridHelperColumn
                    {
                        Header="NAME",
                        Path="NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=17,
                    },
                    new DataGridHelperColumn
                    {
                        Header="DESCRIPTION",
                        Path="DESCRIPTION",
                        ColumnType=ColumnTypeRef.String,
                        Width2=67,
                    },
                    new DataGridHelperColumn
                    {
                        Header="PATH",
                        Path="PATH",
                        ColumnType=ColumnTypeRef.String,
                        Width2=32,
                    },
                    new DataGridHelperColumn
                    {
                        Header="TTL",
                        Path="TTL",
                        ColumnType=ColumnTypeRef.Double,
                        Width2=8,
                        Format="N0",
                    },
                    new DataGridHelperColumn
                    {
                        Header="TYPE_NAME",
                        Path="TYPE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="KEY_TPL",
                        Path="KEY_TPL",
                        ColumnType=ColumnTypeRef.String,
                        Width2=7,
                    },
                    new DataGridHelperColumn
                    {
                        Header="TYPE",
                        Path="TYPE",
                        ColumnType=ColumnTypeRef.Integer,
                        Width2=4,
                    },
                };
                StorageGrid.SetColumns(columns);
                StorageGrid.OnLoadItems = StorageGridLoadItems;
                StorageGrid.SetPrimaryKey("_ROWNUMBER");
                StorageGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                StorageGrid.AutoUpdateInterval = 0;
                StorageGrid.ItemsAutoUpdate = false;
                StorageGrid.RowStylers = new Dictionary<StylerTypeRef, StylerDelegate>()
                {
                    // определение цветов фона строк
                    {
                        StylerTypeRef.BackgroundColor,
                        (Dictionary<string, string> row) =>
                        {
                            var result=DependencyProperty.UnsetValue;
                            var color = "";

                            switch (row.CheckGet("TYPE").ToInt())
                            {
                                case 1:
                                    color = HColor.Blue;
                                    break;

                                case 2:
                                    color = HColor.White;
                                    break;

                                case 3:
                                    color = HColor.Yellow;
                                    break;
                            }

                            if (!string.IsNullOrEmpty(color))
                            {
                                result=color.ToBrush();
                            }

                            return result;
                        }
                    },
                };
                StorageGrid.Commands = Commander;
                StorageGrid.UseProgressSplashAuto = false;
                StorageGrid.Init();
            }
        }

        private async void StorageGridLoadItems()
        {
            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Service/Storage");
            q.Request.SetParam("Object", "Redis");
            q.Request.SetParam("Action", "List");
            q.Request.Timeout = Central.Parameters.RequestGridTimeout;
            q.Request.Attempts = Central.Parameters.RequestAttemptsDefault;

            await Task.Run(() =>
            {
                q.DoQuery();
            });

            StorageGridDataSet = new ListDataSet();
            if (q.Answer.Status == 0)
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                if (result != null)
                {
                    StorageGridDataSet = ListDataSet.Create(result, "ITEMS");
                }
            }
            else
            {
                q.ProcessError();
            }
            StorageGrid.UpdateItems(StorageGridDataSet);
        }

        private void UploadFile()
        {
            var i = new RedisUploadFileForm();

            if (!string.IsNullOrEmpty(StorageGrid.SelectedItem.CheckGet("PATH")))
            {
                i.StoragePath = StorageGrid.SelectedItem.CheckGet("PATH");
            }

            i.Init();
        }
    }
}
