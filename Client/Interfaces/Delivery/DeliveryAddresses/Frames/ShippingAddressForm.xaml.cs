using Client.Common;
using Client.Interfaces.Main;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.XtraReports.Native;
using DevExpress.XtraReports.UI;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Client.Interfaces.Main.DataGridHelperColumn;
using static Client.Interfaces.Main.SelectBox;

namespace Client.Interfaces.DeliveryAddresses
{
    /// <summary>
    /// форма редактирования адреса доставки
    /// </summary>
    /// <author>motenko_ek</author>
    /// <version>1</version>
    /// <released>2025-04-24</released>
    public partial class ShippingAddressForm : ControlBase
    {
        public ShippingAddressForm(ItemMessage message, int id, bool isPost, int idAdres = 0, int frameMode = 0)
        {
            InitializeComponent();

            Message = message;
            IdAdres = idAdres;
            Id = id;
            IsPost = isPost;
            FrameMode = frameMode;

            if (IsPost)
            {
                IdSTmText.Visibility = Visibility.Collapsed;
                IdSTmControl.Visibility = Visibility.Collapsed;
                KashiraIdSTmText.Visibility = Visibility.Collapsed;
                KashiraIdSTmControl.Visibility = Visibility.Collapsed;
                ReturnUpdFlagControl.Visibility = Visibility.Collapsed;
                ReturnTnFlagControl.Visibility = Visibility.Collapsed;
                IdClientText.Visibility = Visibility.Collapsed;
                IdClientControl.Visibility = Visibility.Collapsed;
            }
            AddressTextBox.IsReadOnly = true;
            FileAddButton.Click += FileAdd;
            FileDeleteButton.Click += FileDelete;
            FileShowButton.Click += FileShow;
            AddressEditButton.Click += AddressEdit;

            RoleName = "[erp]delivery_addresses";
            DocumentationUrl = "/doc/l-pack-erp/delivery/delivery_addresses/delivery_to_customer";
            FrameMode = 0;
            FrameTitle = idAdres > 0 ? $"Адрес доставки #{idAdres}" : "Новый адрес доставки";
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
                var columns = new List<DataGridHelperColumn>
                {
                    new DataGridHelperColumn
                    {
                        Header="Рабочий день",
                        Path="WORK_FLAG",
                        ColumnType=ColumnTypeRef.Boolean,
                        Width2=11,
                    },
                    new DataGridHelperColumn
                    {
                        Header="День недели",
                        Path="DAWE_ID",
                        ColumnType=ColumnTypeRef.Integer,
                        //Width2=15,
                        Visible=false,
                    },
                    new DataGridHelperColumn
                    {
                        Header="День недели",
                        Path="DAWE_NAME",
                        ColumnType=ColumnTypeRef.String,
                        Width2=10,
                    },
                    new DataGridHelperColumn
                    {
                        Header="Начало работы",
                        Path="BEGIN_TM",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,

                    },
                    new DataGridHelperColumn
                    {
                        Header="Окончание работы",
                        Path="END_TM",
                        ColumnType=ColumnTypeRef.String,
                        Width2=15,
                    },
                };
                ShipAdresScheduleGrid.SetColumns(columns);
                ShipAdresScheduleGrid.SetPrimaryKey("DAWE_ID");
                ShipAdresScheduleGrid.Toolbar = ShipAdresScheduleGridToolbar;
                ShipAdresScheduleGrid.Commands = Commander;
                ShipAdresScheduleGrid.AutoUpdateInterval = 0;
                ShipAdresScheduleGrid.ItemsAutoUpdate = false;
                ShipAdresScheduleGrid.ColumnWidthMode = GridBox.ColumnWidthModeRef.Full;
                ShipAdresScheduleGrid.Init();

                FillData();
            };
            OnUnload = () =>
            {
                ShipAdresScheduleGrid.Destruct();
            };

            Form = new FormHelper();
            var fields = new List<FormHelperField>()
            {
                new FormHelperField()
                {
                    Path="ADDRESS",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=AddressTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path="ID_DIR",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=IdDirSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ID_DIR_TENDER",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=IdDirTenderSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="NOTE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NoteTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CONTACT_NAME",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ContactNameTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CONTACT_PHONE",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=ContactPhoneTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="EUROTRUCK_BAN_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=EurotruckBanFlagCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="UNLOADING_TYPE",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    Control=UnloadingTypeSelectBox,
                    ControlType="SelectBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    OnCreate = (FormHelperField field) =>
                    {
                        var c=(SelectBox)field.Control;

                        c.SetItems(new Dictionary<string, string>{
                            {"1", "Задняя"},
                            {"2", "Боковая"},
                            {"3", "Верхняя"},
                        });
                    }
                },
                new FormHelperField()
                {
                    Path="NOTE_LOADER",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Control=NoteLoaderTextBox,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ARCHIVE_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    Control=ArchiveFlagCheckBox,
                    ControlType="CheckBox",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="CHECKED",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="ADDR_ID",
                    FieldType=FormHelperField.FieldTypeRef.Integer,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
                new FormHelperField()
                {
                    Path="VERIFIED_FLAG",
                    FieldType=FormHelperField.FieldTypeRef.Boolean,
                    ControlType="void",
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };
            if (!IsPost)
            {
                fields.Add(new FormHelperField()
                {
                    Path = "ID_S_TM",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = IdSTmSelectBox,
                    ControlType = "SelectBox",
                });
                fields.Add(new FormHelperField()
                {
                    Path = "KASHIRA_ID_S_TM",
                    FieldType = FormHelperField.FieldTypeRef.String,
                    Control = KashiraIdSTmSelectBox,
                    ControlType = "SelectBox",
                });
                fields.Add(new FormHelperField()
                {
                    Path = "RETURN_UPD_FLAG",
                    FieldType = FormHelperField.FieldTypeRef.Boolean,
                    Control = ReturnUpdFlagCheckBox,
                    ControlType = "CheckBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                    },
                });
                fields.Add(new FormHelperField()
                {
                    Path = "RETURN_TN_FLAG",
                    FieldType = FormHelperField.FieldTypeRef.Boolean,
                    Control = ReturnTnFlagCheckBox,
                    ControlType = "CheckBox",
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>
                    {
                    },
                });
                fields.Add(new FormHelperField()
                {
                    Path = "ID_CLIENT",
                    AutoloadItems = false,
                    Description = "Клиент",
                    FieldType = FormHelperField.FieldTypeRef.Integer,
                    Control = IdClientSelectBox,
                    ControlType = "SelectBox",
                    Width = 450,
                    Filters = new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                    OnCreate = (FormHelperField field) =>
                    {
                        var c = (SelectBox)field.Control;

                        var columns = new List<DataGridHelperColumn>()
                        {
                            new DataGridHelperColumn
                            {
                                Header="ИД клиента",
                                Path="ID_CLIENT",
                                ColumnType=ColumnTypeRef.Integer,
                                Width2=6,
                            },
                            new DataGridHelperColumn
                            {
                                Header="Клиент",
                                Path="NAME_CLIENT",
                                ColumnType=ColumnTypeRef.String,
                                Width2=30,
                            },
                            new DataGridHelperColumn
                            {
                                Header="Адрес",
                                Path="ADDRESS_DOC",
                                ColumnType=ColumnTypeRef.String,
                                Width2=60,
                            },
                            new DataGridHelperColumn
                            {
                                Header="Покупатель",
                                Path="NAME_POKUPATEL",
                                ColumnType=ColumnTypeRef.String,
                                Width2=30,
                            },
                        };
                        c.GridColumns = columns;
                        c.SelectedItemValue = "NAME_CLIENT";

                        c.ListBoxMinWidth = 600;
                        c.ListBoxMinHeight = 200;
                        //c.Style = FindResource("CustomFormField");
                        c.DataType = SelectBox.DataTypeRef.Grid;
                        c.GridPrimaryKey = field.Path;
                    },
                    QueryLoadItems = new RequestData()
                    {
                        Module = "Delivery",
                        Object = "ResellerClient",
                        Action = "List",
                        AnswerSectionKey = "ITEMS",
                        OnComplete = (FormHelperField field, ListDataSet ds) =>
                        {
                            var c = (SelectBox)field.Control;
                            c.GridDataSet = ds;
                        },
                    },
                });
            }
            Form.SetFields(fields);
            Form.ToolbarControl = FormToolbar;
            Form.StatusControl = StatusBar;
            Form.SetDefaults();

            Commander.Add(new CommandItem()
            {
                Name = "save",
                Enabled = true,
                Title = "Сохранить",
                Description = "",
                ButtonUse = true,
                ButtonName = "SaveButton",
                HotKey = "Ctrl+Return",
                Action = () =>
                {
                    Save();
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "cancel",
                Enabled = true,
                Title = "Отмена",
                Description = "",
                ButtonUse = true,
                ButtonName = "CancelButton",
                HotKey = "Escape",
                Action = () =>
                {
                    Close();
                },
            });
            Commander.SetCurrentGridName("ShipAdresScheduleGrid");
            Commander.Add(new CommandItem()
            {
                Name = "shipping_address_schedule_refresh",
                Group = "grid_base",
                Enabled = true,
                Title = "Обновить",
                Description = "Обновить",
                ButtonUse = false,
                MenuUse = false,
                Action = () =>
                {
                    ShipAdresScheduleGrid.UpdateItems();
                },
            });
            Commander.SetCurrentGroup("item");
            Commander.Add(new CommandItem()
            {
                Name = "shipping_address_schedule_edit",
                Title = "Изменить",
                MenuUse = true,
                HotKey = "Return|DoubleCLick",
                ButtonUse = true,
                ButtonName = "ShipAdresScheduleEditButton",
                //AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    new ShippingAddressScheduleTmForm(ShipAdresScheduleGrid.SelectedItem);
                    //i.Show();
                    //i.Focus();
                },
                CheckEnabled = () =>
                {
                    var result = false;
                    var k = ShipAdresScheduleGrid.GetPrimaryKey();
                    var row = ShipAdresScheduleGrid.SelectedItem;
                    if (row.CheckGet(k).ToInt() != 0)
                    {
                        result = true;
                    }
                    return result;
                },
            });
            Commander.Add(new CommandItem()
            {
                Name = "shipping_address_schedule_copy",
                Title = "Установить для остальных",
                MenuUse = true,
                ButtonUse = true,
                ButtonName = "ShipAdresScheduleCopyButton",
                AccessLevel = Common.Role.AccessMode.FullAccess,
                Action = () =>
                {
                    var k = ShipAdresScheduleGrid.GetPrimaryKey();
                    var row = ShipAdresScheduleGrid.SelectedItem;
                    if (row.CheckGet(k).ToInt() != 0)
                    {
                        foreach (var tm in ShipAdresScheduleGrid.Items)
                        {
                            tm["WORK_FLAG"] = row["WORK_FLAG"];
                            tm["BEGIN_TM"] = row["BEGIN_TM"];
                            tm["END_TM"] = row["END_TM"];
                        }
                        ShipAdresScheduleGrid.UpdateItems();
                    }
                },
                CheckEnabled = () =>
                {
                    var result = false;
                    var k = ShipAdresScheduleGrid.GetPrimaryKey();
                    var row = ShipAdresScheduleGrid.SelectedItem;
                    if (row.CheckGet(k).ToInt() != 0)
                    {
                        result = true;
                    }
                    return result;
                },
            });
            Commander.Init(this);

            if (FrameMode == 2)
            {
                Central.WM.FrameMode = FrameMode;
                Central.WM.Show(GetFrameName(), FrameTitle, true, "ShipAdresForm", this, "", new Dictionary<string, string>()
                    {
                        {"no_resize", "1" },
                        {"center_screen", "1" },
                    }
                );
            }
            else
                base.Show();
        }

        private ItemMessage Message;
        private int IdAdres = 0;
        private int Id = 0;
        private bool IsPost = false;
        private bool IsFile = false;
        //private bool VerifiedFlag = false; // Признак наличия правильного расположения файлов проезда
        private string FileName = String.Empty;
        private DBAddress DBAddress = new DBAddress();

        /// <summary>
        /// процессор форм
        /// </summary>
        private FormHelper Form { get; set; }

        /// <summary>
        /// получение данных с сервера
        /// </summary>
        private async void FillData()
        {
            Form.SetBusy(true);

            var p = new Dictionary<string, string>();
            {
                p.CheckAdd("ID_ADRES", IdAdres.ToString());
            }

            var q = new LPackClientQuery();
            q.Request.SetParam("Module", "Delivery");
            q.Request.SetParam("Object", "ShippingAddress");
            q.Request.SetParam("Action", "Get");
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
                        var ds = ListDataSet.Create(result, "DIRECTION");
                        IdDirSelectBox.SetItems(ds, "ID_DIR", "CITY");
                    }

                    {
                        var ds = ListDataSet.Create(result, "DIRECTION_TENDER");
                        IdDirTenderSelectBox.SetItems(ds, "ID_DIR", "CITY");
                    }

                    {
                        var ds = ListDataSet.Create(result, "SHIPMENT_TIME");
                        IdSTmSelectBox.SetItems(ds, "ID_S_TM", "TM");
                        KashiraIdSTmSelectBox.SetItems(ds, "ID_S_TM", "TM");
                    }

                    {
                        var ds = ListDataSet.Create(result, "ADRESS_CHEDULE");
                        ShipAdresScheduleGrid.UpdateItems(ds);
                    }

                    {
                        var ds = ListDataSet.Create(result, "RESELLER_CLIENT");
                        IdClientSelectBox.SetItems(ds, "ID_CLIENT", "NAME");
                    }

                    {
                        var ds = ListDataSet.Create(result, "ITEMS");
                        Form.SetValues(ds);

                        FileName = ds.GetFirstItemValueByKey("FILE_NAME");
                        if (FileName.IsNullOrEmpty())
                        {
                            FileName = ds.GetFirstItemValueByKey("DRIVEWAY");
                        }
                        else
                        {
                            IsFile = true;
                            FileName = "\\\\file-server-4\\external_services$\\DeliveryAddress\\" + FileName;
                        }
                        if (FileName.IsNullOrEmpty())
                        {
                            FileDeleteButton.IsEnabled = false;
                            FileShowButton.IsEnabled = false;
                        }
                        else
                        {
                            //FileNameCheckBox.IsChecked = true;
                            FileNameTextBox.Text = FileName;
                            FileAddButton.IsEnabled = false;
                        }

                        if (Form.GetFieldByPath("VERIFIED_FLAG").ActualValue.ToBool())
                        {
                            DBAddress.AddrId = Form.GetFieldByPath("ADDR_ID").ActualValue.ToInt();
                        }
                        else
                        {
                            DBAddress.FullAddress = ds.GetFirstItemValueByKey("ADDRESS");
                            if (IdAdres != 0) DialogWindow.ShowDialog("Необходимо привести адрес в правильный вид", "Адрес доставки", "");
                        }
                    }

                    if (result.CheckGet("ADDRESS")!=null)
                    {
                        var ds = ListDataSet.Create(result, "ADDRESS");

                        //DBAddress.AddrId = ds.GetFirstItemValueByKey("ADDR_ID").ToInt();
                        DBAddress.ZipNum = ds.GetFirstItemValueByKey("ZIP_NUM");
                        DBAddress.Region = ds.GetFirstItemValueByKey("REGION");
                        DBAddress.District = ds.GetFirstItemValueByKey("DISTRICT");
                        DBAddress.City = ds.GetFirstItemValueByKey("CITY");
                        DBAddress.Street = ds.GetFirstItemValueByKey("STREET");
                        DBAddress.Building = ds.GetFirstItemValueByKey("BUILDING");
                        if(!ds.GetFirstItemValueByKey("FULL_ADDRESS").IsNullOrEmpty())
                            DBAddress.FullAddress = ds.GetFirstItemValueByKey("FULL_ADDRESS");
                        DBAddress.Longtitude = ds.GetFirstItemValueByKey("LONGTITUDE");
                        DBAddress.Latitude = ds.GetFirstItemValueByKey("LATITUDE");
                        DBAddress.Code = ds.GetFirstItemValueByKey("CODE");
                        DBAddress.Okato = ds.GetFirstItemValueByKey("OKATO");
                        DBAddress.Country = ds.GetFirstItemValueByKey("COUNTRY");
                        DBAddress.Room = ds.GetFirstItemValueByKey("ROOM");
                        DBAddress.Distance = ds.GetFirstItemValueByKey("DISTANCE");

                        if (!DBAddress.FullAddress.IsNullOrEmpty()
                            && !DBAddress.Latitude.IsNullOrEmpty()
                            && !DBAddress.Longtitude.IsNullOrEmpty())
                            AddressTextBox.Text = DBAddress.FullAddress;
                    }

                    //base.Show();
                }
                else
                {
                    DialogWindow.ShowDialog("Неверный ответ", "Получение данных с сервера", "");
                }
            }
            else
            {
                q.ProcessError();
            }

            Form.SetBusy(false);
        }

        private void FileAdd(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = "\\\\l-pack\\net\\Отделы\\01 - ОБЩАЯ\\Транспортный отдел\\Схемы проезда";
            dlg.CheckFileExists = true;
            dlg.CheckPathExists = true;
            if (dlg.ShowDialog() == true)
            {
                FileName = dlg.FileName;
                //FileNameCheckBox.IsChecked = true;
                FileNameTextBox.Text = FileName;
                FileAddButton.IsEnabled = false;
                FileDeleteButton.IsEnabled = true;
                FileShowButton.IsEnabled = true;
            }
        }

        private void FileDelete(object sender, RoutedEventArgs e)
        {
            if (IsFile && DialogWindow.ShowDialog(FileName, "Подтверждение удаления", "", DialogWindowButtons.NoYes) == true)
            {
                try { System.IO.File.Delete(FileName); }
                catch { DialogWindow.ShowDialog("Невозможно удалить файл", "Сохранение адреса доставки", "");}
            }

            FileName = "";
            //FileNameCheckBox.IsChecked = false;
            FileNameTextBox.Text = "";
            FileAddButton.IsEnabled = true;
            FileDeleteButton.IsEnabled = false;
            FileShowButton.IsEnabled = false;
            IsFile = false;
        }

        private void FileShow(object sender, RoutedEventArgs e)
        {
            try { System.Diagnostics.Process.Start(FileName); }
            catch (Exception ex) { DialogWindow.ShowDialog(ex.Message, ex.Source, ""); }
        }

        private void AddressEdit(object sender, RoutedEventArgs e)
        {
            new AddressForm(DBAddress);
            if (!DBAddress.FullAddress.IsNullOrEmpty()
                && !DBAddress.Latitude.IsNullOrEmpty()
                && !DBAddress.Longtitude.IsNullOrEmpty())
                AddressTextBox.Text = DBAddress.FullAddress;
                //VerifiedFlag = true;
        }

        /// <summary>
        /// подготовка данных
        /// </summary>
        private async void Save()
        {
            if (!Form.Validate())
            {
                Form.SetStatus("Не все обязательные поля заполнены верно", 1);
                return;
            }

            var p = Form.GetValues();

            p["ID_ADRES"] = IdAdres.ToString();
            if(IsPost) p["ID_POST"] = Id.ToString();
            else p["ID_POK"] = Id.ToString();

            if (ShipAdresScheduleGrid != null && ShipAdresScheduleGrid.Items != null && ShipAdresScheduleGrid.Items.Count > 0)
            {
                string value = "";
                foreach (var item in ShipAdresScheduleGrid.Items)
                {
                    var workFlag = item.CheckGet("WORK_FLAG");
                    var daweId = item.CheckGet("DAWE_ID");
                    var beginTm = item.CheckGet("BEGIN_TM");
                    var endTm = item.CheckGet("END_TM");

                    var s = $"{daweId}:{workFlag}:{beginTm}:{endTm};";
                    value += s;
                }

                p.Add("ADRES_SCHEDULE", value);
            }

            p["CHECKED"] = "1";
            p["VERIFIED_FLAG"] = "1";

            p.Add("ADDRESS_ADDR_ID", DBAddress.AddrId.ToString());
            p.Add("ADDRESS_ZIP_NUM", DBAddress.ZipNum);
            p.Add("ADDRESS_REGION", DBAddress.Region);
            p.Add("ADDRESS_DISTRICT", DBAddress.District);
            p.Add("ADDRESS_CITY", DBAddress.City);
            p.Add("ADDRESS_STREET", DBAddress.Street);
            p.Add("ADDRESS_BUILDING", DBAddress.Building);
            p.Add("ADDRESS_FULL_ADDRESS", DBAddress.FullAddress);
            p.Add("ADDRESS_LONGTITUDE", DBAddress.Longtitude);
            p.Add("ADDRESS_LATITUDE", DBAddress.Latitude);
            p.Add("ADDRESS_CODE", DBAddress.Code);
            p.Add("ADDRESS_OKATO", DBAddress.Okato);
            p.Add("ADDRESS_COUNTRY", DBAddress.Country);
            p.Add("ADDRESS_ROOM", DBAddress.Room);
            p.Add("ADDRESS_DISTANCE", DBAddress.Distance);

            Form.SetBusy(true);

            try
            {
                int id = 0;
                {
                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Delivery");
                    q.Request.SetParam("Object", "ShippingAddress");
                    q.Request.SetParam("Action", "Save");

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
                            var ds = ListDataSet.Create(result, "ITEMS");
                            id = ds.GetFirstItemValueByKey("ID_ADRES").ToInt();
                        }
                        else
                        {
                            throw new Exception("Неверный ответ сервера");
                        }
                    }
                    else
                    {
                        q.ProcessError();
                        throw new Exception("");
                    }
                }

                if (!IsFile)
                {
                    var newFileName = "";
                    if (FileName != "")
                    {
                        newFileName = id.ToString() + System.IO.Path.GetExtension(FileName);
                        try { System.IO.File.Copy(FileName, "\\\\file-server-4\\external_services$\\DeliveryAddress\\" + newFileName); }
                        catch
                        {
                            throw new Exception("Невозможно скопировать файл");
                        }
                    }

                    var q = new LPackClientQuery();
                    q.Request.SetParam("Module", "Delivery");
                    q.Request.SetParam("Object", "ShippingAddress");
                    q.Request.SetParam("Action", "UpdateFileName");

                    q.Request.SetParams(new Dictionary<string, string>
                        {
                            { "ID_ADRES", id.ToString()}
                            ,{"FILE_NAME", newFileName}
                        });

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
                            id = ds.GetFirstItemValueByKey("ID_ADRES").ToInt();
                        }
                        else
                        {
                            throw new Exception("Неверный ответ сервера");
                        }
                    }
                    else
                    {
                        q.ProcessError();
                        throw new Exception("");
                    }
                }

                if (id != 0)
                {
                    if (Message == null)
                        Message = new ItemMessage()
                        {
                            ReceiverName = IsPost ? "DeliveryFromSupplierTab" : "DeliveryToCustomerTab",
                            Action = "shipping_address_refresh",
                        };
                    Message.SenderName = "ShipAdresForm";
                    Message.Message = $"{id}";
                    Central.Msg.SendMessage(Message);
                }

                Close();
            }
            catch (Exception e)
            {
                if (e.Message.Length > 0)
                    DialogWindow.ShowDialog(e.Message, "Сохранение адреса доставки", "");
            }

            Form.SetBusy(false);
        }
    }
}
