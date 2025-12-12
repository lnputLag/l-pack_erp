using Client.Common;
using Client.Interfaces.Main;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Client.Interfaces.Service.Storages
{
    class RedisUploadFileForm : FormDialog
    {
        public RedisUploadFileForm()
        {
            Mode = "create";
            DocumentationUrl = "/doc/l-pack-erp-new/";
            RoleName = "[erp]server";
            FrameName = "RedisUploadFileForm";

            OnKeyPressed = (KeyEventArgs e) =>
            {
                if (!e.Handled)
                {
                    Commander.ProcessKeyboard(e);
                }
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
        }

        public string StoragePath { get; set; }

        public void Init()
        {
            Fields = new List<FormHelperField>() 
            {
                new FormHelperField()
                {
                    Path= "PATH",
                    Description = "PATH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path= "KEY",
                    Description = "KEY",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                        { FormHelperField.FieldFilterRef.Required, null },
                    },
                },
                new FormHelperField()
                {
                    Path= "FILE_PATH",
                    Description = "FILE_PATH",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                    Fillers = new List<FormHelperFiller>()
                    {
                        new FormHelperFiller()
                        {
                            Name = "select_file",
                            Caption = "Выбрать файл",
                            Style= "ButtonCompact",
                            Description = "Содержимое файла в формате КЛЮЧ=ЗНАЧЕНИЕ",
                            Action = (FormHelper form) =>
                            {
                                return SelectFile();
                            }
                        }
                    },
                },
                new FormHelperField()
                {
                    Path= "FILE_CONTENT",
                    Description = "FILE_CONTENT",
                    FieldType=FormHelperField.FieldTypeRef.String,
                    Filters=new Dictionary<FormHelperField.FieldFilterRef, object>{
                    },
                },
            };

            OnGet += (FormDialog fd) =>
            {
                if (!string.IsNullOrEmpty(StoragePath))
                {
                    var p = new Dictionary<string, string>();
                    p.CheckAdd("PATH", StoragePath);
                    fd.SetValues(p);
                }

                return true;
            };
            AfterGet += (FormDialog fd) =>
            {
                fd.SaveButton.Content = "Загрузить файл";
                fd.Open();
            };
            OnSave += (FormDialog fd) =>
            {
                var result = false;
                if (fd.Validate())
                {
                    var p = fd.GetValues();
                    if (UploadFile(p))
                    {
                        result = true;

                        DialogWindow.ShowDialog($"Успешная загрузка файла в Redis", this.ControlTitle, "", DialogWindowButtons.OK);
                        Close();
                    }
                }
                return result;
            };

            Commander.Init(this);
            Run(Mode);
        }

        private string SelectFile()
        {
            string filePath = "";

            var fd = new OpenFileDialog();
            var fdResult = (bool)fd.ShowDialog();
            if (fdResult)
            {
                string fileName = Path.GetFileName(fd.FileName);
                filePath = fd.FileName;

                string fileContent = "";
                using (var stream = new StreamReader(filePath))
                {
                    fileContent = stream.ReadToEnd();
                }

                var p = new Dictionary<string, string>();
                p.Add("KEY", fileName);
                p.Add("FILE_CONTENT", fileContent);
                this.SetValues(p);
            }

            return filePath;
        }

        private bool UploadFile(Dictionary<string, string> item)
        {
            bool uploadResult = false;
            bool resume = false;

            var fileContentDictionary = new Dictionary<string, string>();
            string fileContent = item.CheckGet("FILE_CONTENT");
            if (!string.IsNullOrEmpty(fileContent))
            {
                try
                {
                    fileContentDictionary = DictionaryExtension.CreateFromTextConfig(fileContent);
                    if (fileContentDictionary != null && fileContentDictionary.Count > 0)
                    {
                        item.CheckAdd("DATA", JsonConvert.SerializeObject(fileContentDictionary));
                        resume = true;
                    }
                }
                catch (Exception ex)
                {
                    DialogWindow.ShowDialog($"Ошибка преобразования контента файла. {ex.Message}", this.ControlTitle, "", DialogWindowButtons.OK);
                }
            }

            if (resume)
            {
                var q = new LPackClientQuery();
                q.Request.SetParam("Module", "Service/Storage");
                q.Request.SetParam("Object", "Redis");
                q.Request.SetParam("Action", "Save");
                q.Request.SetParams(item);

                q.DoQuery();

                if (q.Answer.Status == 0)
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, ListDataSet>>(q.Answer.Data);
                    if (result != null)
                    {
                        uploadResult = true;
                    }
                }
                else
                {
                    q.ProcessError();
                }
            }

            return uploadResult;
        }
    }
}
