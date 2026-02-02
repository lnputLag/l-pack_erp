using Client.Interfaces.Accounts;
using Client.Interfaces.Debug;
using Client.Interfaces.DeliveryAddresses;
using Client.Interfaces.Economics.MoldedContainer;
using Client.Interfaces.Main;
using Client.Interfaces.Messages;
using Client.Interfaces.Orders.MoldedContainer;
using Client.Interfaces.Preproduction;
using Client.Interfaces.Production;
using Client.Interfaces.Production.Corrugator;
using Client.Interfaces.Production.CreatingTasks;
using Client.Interfaces.Production.Monitor;
using Client.Interfaces.Production.ProcessingMachines;
using Client.Interfaces.Production.Testing;
using Client.Interfaces.Sales;
using Client.Interfaces.Shipments;
using Client.Interfaces.Stock;
using Client.Interfaces.Stock.ForkliftDrivers;
using Client.Interfaces.Test;
using Client.Interfaces.Production.EnergyResources;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json.Linq;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Client.Interfaces.Production.IndustrialWaste;
using Client.Interfaces.Service;
using Client.Interfaces.Stock.WMS;
using Client.Interfaces.Preproduction.SampleDrawing;
using Client.Interfaces.Service.Mail;
using Client.Interfaces.Production.ScalesShredder;
using Client.Interfaces.Production.ScalesShredderKsh;
using NPOI.SS.Formula.Functions;
using Client.Interfaces.Service.Printing;
using DevExpress.Xpf.Bars;
using System.Windows.Data;
using static Client.Common.Role;
using System.Windows.Media.Imaging;
using Client.Interfaces.Preproduction.PreproductionConfirmOrderLt;
using Client.Interfaces.Preproduction.PlanningOrderLt;
using Client.Interfaces.Production.MoldedContainer;
using Client.Interfaces.Preproduction.Rig;
using Client.Interfaces.Production.Corrugator.CorrugatorMachineOperator;
using Client.Interfaces.Production.Corrugator.TaskPlanningKashira;
using Client.Interfaces.Production.Corrugator.TaskPlannings;
using Client.Interfaces.Production.ProductTestTrials;
using Client.Interfaces.Sales.Edi;
using Client.Interfaces.Sales.NewOrderLt;
using Client.Interfaces.Supply;
using Client.Interfaces.Service.Sessions;
using Client.Interfaces.Сounterparty.Customers;
using Client.Interfaces.Service.Jobs;
using Client.Interfaces.Service.Servers;
using Client.Interfaces.Sources;
using Client.Interfaces.Service.Sources;
using Client.Interfaces.Production.Corrugator.CorrugatorMachineOperatorKsh;
using Client.Interfaces.ProductionCatalog;
using Client.Interfaces.Orders;
using Client.Interfaces.Preproduction.PlannedDowntime;
using Client.Interfaces.Production.ProductTestTrialsKsh;
using Client.Interfaces.Preproduction.Rig.RigMonitorKsh;
using Client.Interfaces.Service.Storages;
using Client.Interfaces.Stock.PalletBinding;
using Client.Interfaces.Stock.RawMaterialResidueMonitor;

namespace Client.Common
{
    /// <summary>
    /// Вспомогательный класс для обслуживания главного меню приложения и работы со ссылками
    /// http://192.168.3.237/developer/l-pack-erp/client/infra/navigation
    /// </summary> 
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public class Navigator
    {
        public Navigator()
        {
            Items = new List<NavigationItem>();
            Address = new NavigationAddress();
            RoleLevelTest = AccessMode.None;
        }

        /// <summary>
        /// Структура главного меню, рекурсивный список элементов
        /// </summary>
        public List<NavigationItem> Items { get; set; }

        /// <summary>        
        /// Структура адреса. При вызове ProcessURL, переданный в нее Url парсится в 
        /// эту структуру для дальнейшей работы 
        /// </summary>
        public NavigationAddress Address { get; private set; }

        /// <summary>
        /// Объект главного меню
        /// </summary>
        public Menu MainMenu { get; set; }
        public Menu RightMenu { get; set; }

        public DevExpress.Xpf.Bars.MainMenuControl MainMenu2 { get; set; }
        public DevExpress.Xpf.Bars.MainMenuControl RightMenu2 { get; set; }

        public Role.AccessMode RoleLevelTest { get; set; }


        /// <summary>
        /// Инициализация структуры главного меню. Здесь создаются собственно пункты меню
        /// </summary>
        public void Init()
        {
            /*
                Структура меню, разделы и подразделы и обработчики.
                Сначала вложенные пункты, затем внешние.
                Type="element" -- конечный элемент, требующий действия
                Type="section" -- раздел, содержащий другие разделы
                    (для таких объектов обработчик клика не назначается)

               
             */

            /*
                заготовка элемента меню: 

                subItems.Add(
                    new NavigationItem()
                    {
                        Name="tasks_appeals_ctl",
                        Title="Управление задачами и обращениями",
                        Type="element",
                        Action=new DelegateCommand<string>(
                            action =>
                            {        
                                var i = new TasksAppealsCtlInterface();
                            }
                        ),
                        AllowedRoles=new List<String>
                        { 
                            "[f]pm_head"
                        },
                        AllowedUsers=new List<int>
                        { 
                            193
                        },
                    }
                ); 

             */

            List<NavigationItem> subItems;

            // Заявки (orders)
            {
                Items.Add(
                    new NavigationItem
                    {
                        Name = "orders",
                        Title = "Заявки",
                        Type = "section",
                        SubItems = new List<NavigationItem>
                        {
                            new NavigationItem
                            {
                                Name = "online_store",
                                Title = "Интернет-магазин",
                                Action = new DelegateCommand<string>(
                                    action => {
                                        var i = new OnlineStoreInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]online_store_assortment",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string> {
                                    "[erp]online_store_assortment",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "order",
                                Title = "Заявки на ЛТ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new MoldedContainerOrderInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]molded_contnr_order",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "new_order_lt",
                                Title = "Согласование заявок на ЛТ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new NewOrderLtInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]new_order_lt",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string> {
                                    "[erp]molded_contnr_order",
                                    "[erp]new_order_lt",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "scrap_paper_ksh",
                                Title = "Макулатура КШ",
                                Action = new DelegateCommand<string>(
                                    action => {
                                        var i = new ScrapPaperKshInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]order_scrap_paper_ksh",
                                }
                            },
                        }
                    }
                );
            }

            // Контрагенты (counterparty)
            {
                Items.Add(
                    new NavigationItem
                    {
                        Name = "counterparty",
                        Title = "Контрагенты",
                        Type = "section",
                        SubItems = new List<NavigationItem>
                        {
                            new NavigationItem
                            {
                                Name = "customers",
                                Title = "Потребители",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new CustomersInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]customers",
                                }
                            },
                        }
                    });
            }

            // Экономика
            {
                Items.Add(
                    new NavigationItem
                    {
                        Name = "economics",
                        Title = "Экономика",
                        Type = "section",
                        SubItems = new List<NavigationItem>
                        {
                            new NavigationItem
                            {
                                Name = "molded_cntnr_specification",
                                Title = "Спецификации ЛТ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new MoldedContainerSpecificationInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]molded_contnr_specification",
                                }
                            },
                        }
                    }
                );
            }

            // Снабжение
            {
                Items.Add(
                    new NavigationItem
                    {
                        Name = "supply",
                        Title = "Снабжение",
                        Type = "section",
                        SubItems = new List<NavigationItem>
                        {
                            new NavigationItem
                            {
                                Name = "arrival_invoice",
                                Title = "Приходные накладные",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ArrivalInvoiceInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]arrival_invoice",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "claim_stock_rolls",
                                Title = "Претензии по складу рулонов",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ClaimStockRollsInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]claim_stock_rolls",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "delivery_shippings",
                                Title = "Загрузки",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new Interfaces.Delivery.Shippings.Interface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]delivery_shippings",
                                }
                            },
                        }
                    }
                );
            }

            // Продукция и материалы (sources)
            {
                Items.Add(
                    new NavigationItem
                    {
                        Name = "sources",
                        Title = "Продукция и материалы",
                        Type = "section",
                        SubItems = new List<NavigationItem>
                        {
                            new NavigationItem
                            {
                                Name = "product_assortment",
                                Title = "Ассортимент",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new AssortmentInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]reference_assortment",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "sets",
                                Title = "Комплекты",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new Interfaces.Sources.SourcesInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]sets",
                                },

                            },
                            new NavigationItem
                            {
                                Name = "interlayer",
                                Title = "Перестил",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new Interfaces.Sources.InterlayerInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]interlayer",
                                },

                            },
                            new NavigationItem
                            {
                                Name = "technological_map",
                                Title = "Техкарты",
                                Type = "section",
                                AllowedRoles = new List<string> {
                                    "[erp]gasket_technological_map",
                                },
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem
                                    {
                                        Name = "gasket",
                                        Title = "Техкарты",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new TechnologicalMapInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string> {
                                            "[erp]gasket_technological_map",
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "technological_map_sets",
                                        Title = "Комплекты техкарт",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                new TechnologicalMapSetsInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<String>
                                        {
                                            "[erp]technological_map_sets",
                                        },
                                    },
                                },
                            },
                            new NavigationItem
                            {
                                Name = "web_technological_map",
                                Title = "Веб-техкарты",
                                Type = "section",
                                AllowedRoles = new List<string> {
                                    "[erp]constructor_web_tech_map",
                                    "[erp]designer_web_tech_map",
                                    "[erp]manager_web_tech_map",
                                    "[erp]engineer_web_tech_map",
                                    "[erp]tk_file_request",
                                },
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem
                                    {
                                        Name = "engineer_web_tech_map",
                                        Title = "Инженеры",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new EngineerWebTechnologicalMapInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string> {
                                            "[erp]engineer_web_tech_map",
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "engineer_web_tech_map_test",
                                        Title = "Инженеры (тест)",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new EngineerWebTechnologicalMapTestInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string> {
                                            "[erp]engineer_web_tech_map",
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "constructor_web_tech_map",
                                        Title = "Конструкторы",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new ConstructorWebTechnologicalMapInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string> {
                                            "[erp]constructor_web_tech_map",
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "designer_web_tech_map",
                                        Title = "Дизайнеры",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new DesignerWebTechnologicalMapInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string> {
                                            "[erp]designer_web_tech_map",
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "manager_web_tech_map",
                                        Title = "Менеджеры",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new ManagerWebTechnologicalMapInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string> {
                                            "[erp]manager_web_tech_map",
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "tk_file_request",
                                        Title = "Заявки на файлы",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new TkFileRequestInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string> {
                                            "[erp]tk_file_request",
                                        },
                                    },
                                },
                            },
                            new NavigationItem
                            {
                                Name = "molded_contnr_techcard",
                                Title = "Техкарты ЛТ",
                                Type = "section",
                                AllowedRoles = new List<string> {
                                            "[erp]molded_contnr_engineer",
                                            "[erp]molded_contnr_designer",
                                            "[erp]molded_contnr_manager",
                                },
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem
                                    {
                                        Name = "molded_contnr_engineer",
                                        Title = "Инженеры",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                new MoldedContainerTechCardInterfaceEngineers();
                                            }
                                        ),
                                        AllowedRoles = new List<string> {
                                            "[erp]molded_contnr_engineer",
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "molded_contnr_designer",
                                        Title = "Дизайнер",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                new MoldedContainerTechCardInterfaceDesigners();
                                            }
                                        ),
                                        AllowedRoles = new List<String>
                                        {
                                            "[erp]molded_contnr_designer",
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "molded_contnr_manager",
                                        Title = "Менеджер",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                new MoldedContainerTechCardInterfaceManagers();

                                            }
                                        ),
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]molded_contnr_manager",
                                        }
                                    },
                                },
                            },
                            new NavigationItem
                            {
                                Name = "reports_technological_map",
                                Title = "Отчёты по техкартам",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ProductionPreparationDepartmentReportsInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]reports_technological_map",
                                },
                            },
                        }
                    }
                );
            }

            // Продажи (sales)
            {
                Items.Add(
                    new NavigationItem
                    {
                        Name = "sales",
                        Title = "Продажи",
                        Type = "section",
                        SubItems = new List<NavigationItem>
                        {
                            new NavigationItem
                            {
                                Name = "sale",
                                Title = "Расходные накладные",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action => {
                                        var i = new SaleInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]sales_manager",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "edm",
                                Title = "ЭДО",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ShipmentEdmInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]shipment_edm",
                                },
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string> {
                                    "[erp]sales_manager",
                                    "[erp]shipment_edm",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "reports",
                                Title = "Продукция не отгружаемая 90 дней",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action => {
                                        var i = new SalesReportUnshippedInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]sales_report_unshipped",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "reports2",
                                Title = "Отчет по продажам",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action => {
                                        var i = new SalesReportInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]sales_report",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "secondary",
                                Title = "Вторичные продажи",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action => {
                                        var i = new SalesReportSecondaryInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]sales_report_secondary",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "mutual_settlement",
                                Title = "Взаиморасчёты",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action => {
                                        var i = new MutualSettlementInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]mutual_settlement",
                                }
                            },
                        }
                    }
                );
            }

            // Планирование (planning)
            {
                Items.Add(
                    new NavigationItem
                    {
                        Name = "planning",
                        Title = "Планирование",
                        Type = "section",
                        SubItems = new List<NavigationItem>
                        {
                            new NavigationItem
                            {
                                Name = "order_confirm",
                                Title = "Подтверждение заявок",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new PreproductionConfirmOrderInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]preproduction_confirm_order",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "production_tasks_planning",
                                Title = "Планирование ПЗ на ГА",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action => {
                                        var i = new TaskPlanningInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]production_task_planning",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "production_tasks_planning_kashira",
                                Title = "Планирование ПЗ на ГА КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action => {
                                        var i = new TaskPlanningKashiraInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]prod_task_plan_kashira",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string> {
                                    "[erp]preproduction_confirm_order",
                                    "[erp]production_task_planning",
                                    "[erp]prod_task_plan_kashira",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "order_confirm_lt",
                                Title = "Подтверждение заявок на ЛТ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new PreproductionConfirmOrderLtInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]confirm_order_lt",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "production_tasks_planning_lt",
                                Title = "Планирование ПЗ на ЛТ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new PlanningOrderLtInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]planning_order_lt",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string> {
                                    "[erp]confirm_order_lt",
                                    "[erp]planning_order_lt",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "production_tasks",
                                Title = "ПЗ на ГА",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action => {
                                        var i = new ProductionTaskCMInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]production_task_cm",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "production_tasks_creating",
                                Title = "Автораскрой",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action => {
                                        var i = new ProductionTaskCMCreateInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]production_task_cm_create",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "production_tasks_rework",
                                Title = "Раскрой по ПЗ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action => {
                                        var i = new ProductionTaskCMReworkInterface(1);
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]production_task_cm_rework",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "production_tasks_rework_ksh",
                                Title = "Раскрой по ПЗ КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action => {
                                        var i = new ProductionTaskCMReworkInterface(2);
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]production_task_cm_rwk_ksh",
                                },
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string> {
                                    "[erp]production_task_cm",
                                    "[erp]production_task_cm_create",
                                    "[erp]production_task_cm_rework",
                                    "[erp]production_task_cm_rwk_ksh",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "raw_material_group_planner",
                                Title = "Планировщик сырьевых групп",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new RawMaterialGroupPlannerInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]raw_material_group_planner",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "production_task",
                                Title = "ПЗ на ЛТ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new MoldedContainerProductionTaskInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]molded_contnr_productn_task",
                                }
                            },
                            
                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string> {
                                    "[erp]production_task_cm",
                                    "[erp]production_task_cm_create",
                                    "[erp]production_task_cm_rework",
                                    "[erp]production_task_cm_rwk_ksh",
                                    "[erp]raw_material_group_planner",
                                    "[erp]molded_contnr_productn_task",
                                }
                            },
                            
                            new NavigationItem
                            {
                                Name = "planner_downtime",
                                Title = "Плановые простои",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new PlannedDowntimeInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]planned_downtime",
                                },
                            }
                        }
                    }
                );
            }

            // Подготовка производства (preproduction)
            {
                Items.Add(
                    new NavigationItem
                    {
                        Name = "preproduction",
                        Title = "Подготовка производства",
                        Type = "section",
                        SubItems = new List<NavigationItem>
                        {
                            new NavigationItem
                            {
                                Name = "samples",
                                Title = "Образцы",
                                Type = "section",
                                AllowedRoles = new List<string> {
                                    "[erp]sample",
                                    "[erp]sample_cardboard",
                                    "[erp]sample_task_planner",
                                    "[erp]sample_task",
                                    "[erp]sample_task_ksh",
                                    "[erp]sample_accounting",
                                    "[erp]sample_drawing",
                                    "[erp]sample_laboratory",
                                },
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem
                                    {
                                        Name = "sample",
                                        Title = "Образцы",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new SampleInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string> {
                                            "[erp]sample",
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "sample_carton",
                                        Title = "Картон для образцов",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new SampleCardboardInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string> {
                                            "[erp]sample_cardboard",
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "sample_task_planner",
                                        Title = "Планировщик образцов",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new SampleTaskPlannerInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string> {
                                            "[erp]sample_task_planner",
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "sample_task",
                                        Title = "Задания на образцы",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new SampleTaskInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string> {
                                            "[erp]sample_task",
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "sample_task_ksh",
                                        Title = "Задания на образцы КШ",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new SampleTaskKshInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string> {
                                            "[erp]sample_task_ksh",
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "sample_accounting",
                                        Title = "Учет образцов",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new SampleAccountingInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string> {
                                            "[erp]sample_accounting",
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "sample_drawing",
                                        Title = "Образцы для конструктора",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new SampleDrawingInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string> {
                                            "[erp]sample_drawing",
                                        },
                                    },
                                    new NavigationItem()
                                    {
                                        Name = "sample_laboratory",
                                        Title = "Образцы для лаборатории",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                new SampleLaboratoryInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<String>
                                        {
                                            "[erp]sample_laboratory",
                                        },
                                    },
                                },
                            },
                            new NavigationItem
                            {
                                Name = "samples_reference",
                                Title = "Эталонные образцы",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new SampleReferenceInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]sample_reference",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "samples_customer",
                                Title = "Образцы от клиента",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new PatternOrdersInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]pattern_orders",
                                },
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string> {
                                    "[erp]sample",
                                    "[erp]sample_cardboard",
                                    "[erp]sample_task_planner",
                                    "[erp]sample_task",
                                    "[erp]sample_accounting",
                                    "[erp]sample_drawing",
                                    "[erp]sample_laboratory",
                                    "[erp]sample_reference",
                                    "[erp]pattern_orders",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "rig_control",
                                Title = "Оснастка",
                                Type = "section",
                                AllowedRoles = new List<string>
                                {
                                    "[erp]rig_order_contnr",
                                    "[erp]rig_contner_control",
                                    "[erp]rig_cutting_stamp_order",
                                    "[erp]rig_cutting_stamp_accnt",
                                    "[erp]rig_cutting_stamp_keep",
                                    "[erp]rig_cutting_stamp_keep_ksh",
                                },
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem
                                    {
                                        Name = "rig_order_container",
                                        Title = "Заказ клише ЛТ",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new RigOrderInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]rig_order_contnr",
                                        }
                                    },
                                    new NavigationItem
                                    {
                                        Name = "container_cliche",
                                        Title = "Операции с клише ЛТ",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new ContainerClicheInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]rig_contner_control",
                                        }
                                    },
                                    new NavigationItem
                                    {
                                        Name = "rig_cutting_stamp_order",
                                        Title = "Заказ штанцформ",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new CuttingStampOrderInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]rig_cutting_stamp_order",
                                        }
                                    },
                                    new NavigationItem
                                    {
                                        Name = "rig_cutting_stamp_keeping",
                                        Title = "Учет и хранение штанцформ",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new CuttingStampKeepingInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]rig_cutting_stamp_keep",
                                        }
                                    },
                                    new NavigationItem
                                    {
                                        Name = "rig_cutting_stamp_keeping_ksh",
                                        Title = "Учет и хранение штанцформ КШ",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new CuttingStampKeepingKshInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]rig_cutting_stamp_keep_ksh",
                                        }
                                    },
                                    new NavigationItem
                                    {
                                        Name = "rig_cutting_stamp_accnt",
                                        Title = "Техкарты со штанцформами",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new CuttingStampAccountingInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]rig_cutting_stamp_accnt",
                                        }
                                    },
                                },
                            },
                            new NavigationItem
                            {
                                Name = "rig_movement",
                                Title = "Учёт оснастки",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new RigAccountingInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]rig_movement",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "rig_management",
                                Title = "Управление оснасткой",
                                Type = "element",
                                AllowedRoles = new List<string>
                                {
                                    "[erp]rig_management",
                                },
                                Action = new DelegateCommand<string>(action =>
                                {
                                    new RigManagementInterface();
                                })
                            },
                            new NavigationItem
                            {
                                Name = "rig_calculation_task",
                                Title = "Расчет оснастки",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new RigCalculationTaskInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]rig_calculation_task",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "rig_monitor",
                                Title = "Монитор оснастки",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new RigMonitorInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]rig_monitor",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string> {
                                    "[erp]rig_order_contnr",
                                    "[erp]rig_contner_control",
                                    "[erp]rig_cutting_stamp_order",
                                    "[erp]rig_cutting_stamp_accnt",
                                    "[erp]rig_cutting_stamp_keep",
                                    "[erp]rig_cutting_stamp_keep_ksh",
                                    "[erp]rig_movement",
                                    "[erp]rig_management",
                                    "[erp]rig_calculation_task",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "paints",
                                Title = "Краски",
                                Type = "element",
                                AllowedRoles = new List<string>
                                {
                                    "[erp]color",
                                },
                                Action = new DelegateCommand<string>(action =>
                                {
                                    new ColorInterface();
                                })
                            },
                            new NavigationItem
                            {
                                Name = "paint_sample",
                                Title = "Заявки на выкрасы",
                                Type = "element",
                                AllowedRoles = new List<string>
                                {
                                    "[erp]paint_sample",
                                },
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new PaintSampleInterface();
                                    }
                                ),
                            },
                            new NavigationItem
                            {
                                Name = "molded_container_sticker",
                                Title = "Этикетки ЛТ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new MoldedContainerStickerInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]molded_contnr_sticker",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "production_scheme",
                                Title = "Схемы производства",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new Interfaces.Sources.ProductionSchemeInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]production_scheme",
                                },

                            },
                            new NavigationItem
                            {
                                Name = "production_scheme2",
                                Title = "Схемы производства 2",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new Interfaces.Sources.ProductionScheme2.Interface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]production_scheme",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "production_catalog",
                                Title = "Справочники производства",
                                Action = new DelegateCommand<string>(
                                    action => {
                                        var i = new ProductionCatalogInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]production_catalog",
                                }
                            },
                        }
                    }
                );
            }

            // Производство (production)
            {
                Items.Add(
                    new NavigationItem
                    {
                        Name = "production",
                        Title = "Производство",
                        Type = "section",
                        SubItems = new List<NavigationItem>
                        {
                            new NavigationItem
                            {
                                Name = "scales_press",
                                Title = "Весы шредера",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new IndustrialWasteInterface(1);
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]industrial_waste",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "scales_shredder",
                                Title = "Весы макулатурного пресса",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ScalesShredderInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]scales_shredder",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "goods_testing",
                                Title = "Тестирование изделий",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new Interfaces.Production.Testing.ProductionTestingInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]production_testing",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "production_testing_trial",
                                Title = "Тестовые испытания изделия",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ProductTestTrialsInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]production_testing_trial",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "prod_repairs",
                                Title = "Ремонты",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new Interfaces.Production.Repairs.Interface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]prod_repairs",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string>
                                {
                                    "[erp]industrial_waste",
                                    "[erp]industrial_waste_ksh",
                                    "[erp]scales_shredder",
                                    "[erp]production_testing",
                                    "[erp]production_testing_trial",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "scales_press_ksh",
                                Title = "Весы шредера переработки КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new IndustrialWasteInterface(2);
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]industrial_waste_ksh",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "scales_shredder_ksh",
                                Title = "Весы макулатурного пресса КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ScalesShredderInterfaceKsh();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]scales_shredder_ksh",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "goods_testing_ksh",
                                Title = "Тестирование изделий КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new Interfaces.Production.Testing.ProductionTestingKshInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]production_testing_ksh",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "production_testing_trial_ksh",
                                Title = "Тестовые испытания изделия КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ProductTestTrialsKshInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]prod_testing_trial_ksh",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "prod_repairs_ksh",
                                Title = "Ремонты КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new Interfaces.Production.RepairsKsh.Interface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]prod_repairs_ksh",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string>
                                {
                                    "[erp]scales_shredder_ksh",
                                    "[erp]production_testing_ksh",
                                    "[erp]prod_testing_trial_ksh",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "energy_resources",
                                Title = "Отчет по энергоресурсам",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new EnergyResourcesInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]energy_resources",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "complectation",
                                Title = "Комплектация",
                                Type = "section",
                                AllowedRoles = new List<string>
                                {
                                    "[erp]complectation_cm",
                                    "[erp]complectation_pm",
                                    "[erp]complectation_stock",
                                    "[erp]complectation_list",
                                    "[erp]compl_molded_contnr",
                                    "[erp]recomplectation",

                                    "[erp]complectation_cm_ksh",
                                    "[erp]complectation_pm_ksh",
                                    "[erp]complectation_stock_ksh",
                                    "[erp]complectation_list_ksh",
                                },
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem
                                    {
                                        Name = "list",
                                        Title = "Список комплектаций",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]complectation_list",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            new ProductionComplectationListInterface();
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "cm",
                                        Title = "Комплектация ГА",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]complectation_cm",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            new ProductionComplectationCMInterface();
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "cm_stock",
                                        Title = "Комплектация ГА на СГП",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]complectation_cm",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            new ProductionComplectationCorrugatorInStockInterface();
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "pm",
                                        Title = "Комплектация переработка",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]complectation_pm",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            new ProductionComplectationPMInterface();
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "stock",
                                        Title = "Комплектация СГП",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]complectation_stock",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            new ProductionComplectationStockInterface();
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "molded_contnr",
                                        Title = "Комплектация ЛТ",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]compl_molded_contnr",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            new ProductionComplectationMoldedContainerInterface();
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "recomplectation",
                                        Title = "Перекомплектация",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]recomplectation",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            new ProductionRecomplectationInterface();
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "separator",
                                        Title = "",
                                        Type = "separator",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]complectation_cm",
                                            "[erp]complectation_pm",
                                            "[erp]complectation_stock",
                                            "[erp]complectation_list",
                                            "[erp]compl_molded_contnr",
                                            "[erp]recomplectation",
                                        }
                                    },
                                    new NavigationItem
                                    {
                                        Name = "list_kh",
                                        Title = "Список комплектаций КШ",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]complectation_list_ksh",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            new ComplectationListKshInterface();
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "cm_ksh",
                                        Title = "Комплектация ГА КШ",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]complectation_cm_ksh",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            new ComplectationCorrugatorKshInterface();
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "pm_ksh",
                                        Title = "Комплектация переработка КШ",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]complectation_pm_ksh",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            new ComplectationProcessingKshInterface();
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "stock_ksh",
                                        Title = "Комплектация СГП КШ",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]complectation_stock_ksh",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            new ComplectationStockKshInterface();
                                        })
                                    },
                                }
                            },
                        }
                    }
                );
            }

            // БДМ (paper_production)
            {
                Items.Add(
                    new NavigationItem
                    {
                        Name = "paper_production",
                        Title = "БДМ",
                        Type = "section",
                        SubItems = new List<NavigationItem>
                        {
                            new NavigationItem
                            {
                                Name = "paper_making_slitter",
                                Title = "Управление ПРС на БДМ",
                                Type = "element",
                                AllowedRoles = new List<string> {
                                    "[erp]slitter_bdm1",
                                    "[erp]slitter_bdm2",
                                },
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem
                                    {
                                        Name = "11",
                                        Title = "БДМ-1",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]slitter_bdm1",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            var i = new PaperMakingSlitter1Interface();
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "12",
                                        Title = "БДМ-2",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]slitter_bdm2",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            var i = new PaperMakingSlitter2Interface();
                                        })
                                    },
                                },
                            },
                            new NavigationItem
                            {
                                Name = "paper_making_machine",
                                Title = "Мониторинг БДМ",
                                Type = "element",
                                AllowedRoles = new List<string> {
                                   "[erp]bdm_1_control",
                                   "[erp]bdm_2_control",
                                },
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem
                                    {
                                        Name = "1",
                                        Title = "БДМ-1",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]bdm_1_control",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            var i = new PaperMakingMachineInterface(1);
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "2",
                                        Title = "БДМ-2",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                             "[erp]bdm_2_control",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            var i = new PaperMakingMachineInterface(2);
                                        })
                                    },
                                },
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string>
                                {
                                    "[erp]slitter_bdm1",
                                    "[erp]slitter_bdm2",
                                    "[erp]bdm_1_control",
                                    "[erp]bdm_2_control",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "driver_registration",
                                Title = "Регистрация водителей",
                                Type = "element",
                                AllowedRoles = new List<string> {
                                   "[erp]bdm_1_driver_registration",
                                   "[erp]bdm_2_driver_registration",
                                },
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem
                                    {
                                        Name = "1",
                                        Title = "БДМ-1",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]bdm_1_driver_registration",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            var i = new DriverRegistrationInterface(1);
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "2",
                                        Title = "БДМ-2",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                             "[erp]bdm_2_driver_registration",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            var i = new DriverRegistrationInterface(2);
                                        })
                                    },
                                },
                            },
                            new NavigationItem
                            {
                                    Name = "manager_weight_bdm_2",
                                    Title = "Весовая БДМ2",
                                    Type = "element",
                                    AllowedRoles = new List<string>
                                    {
                                          "[erp]bdm_2_manager_weight",
                                    },
                                    Action = new DelegateCommand<string>(action =>
                                    {
                                        var i = new _ManagerWeightBdm2Interface();
                                    })
                            },
                            new NavigationItem
                            {
                                    Name = "scrap_paper_bdm",
                                    Title = "(Прием макулатуры)",
                                    Type = "element",
                                    AllowedRoles = new List<string>
                                    {
                                          "[erp]scrap_paper_bdm1",
                                          "[erp]scrap_paper_bdm2",
                                          "[erp]scrap_paper_molded_contner",
                                    },
                                    SubItems = new List<NavigationItem>
                                    {
                                        new NavigationItem
                                        {
                                            Name = "1",
                                            Title = "БДМ-1",
                                            Type = "element",
                                            AllowedRoles = new List<string>
                                            {
                                                 "[erp]scrap_paper_bdm1",
                                            },
                                            Action = new DelegateCommand<string>(action =>
                                            {
                                                 var i = new ScrapPaperInterface(1);
                                            })
                                        },
                                        new NavigationItem
                                        {
                                            Name = "2",
                                            Title = "БДМ-2",
                                            Type = "element",
                                            AllowedRoles = new List<string>
                                            {
                                                 "[erp]scrap_paper_bdm2",
                                            },
                                            Action = new DelegateCommand<string>(action =>
                                            {
                                                  var i = new ScrapPaperInterface(2);
                                            })
                                        },
                                        new NavigationItem
                                        {
                                            Name = "3",
                                            Title = "ЛТ",
                                            Type = "element",
                                            AllowedRoles = new List<string>
                                            {
                                                 "[erp]scrap_paper_molded_contner",
                                            },
                                            Action = new DelegateCommand<string>(action =>
                                            {
                                                  var i = new ScrapPaperInterface(3);
                                            })
                                        },

                                },
                            },
                            new NavigationItem
                            {
                                    Name = "scrap_paper_report_bdm",
                                    Title = "Отчеты по макулатуре",
                                    Type = "element",
                                    AllowedRoles = new List<string>
                                    {
                                          "[erp]scrap_paper_report_bdm",
                                    },
                                    Action = new DelegateCommand<string>(action =>
                                    {
                                        var i = new ScrapPaperReportInterface();
                                    })
                            },
                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string>
                                {
                                    "[erp]bdm_1_driver_registration",
                                    "[erp]bdm_2_driver_registration",
                                    "[erp]bdm_2_manager_weight",
                                    "[erp]scrap_paper_bdm1",
                                    "[erp]scrap_paper_bdm2",
                                    "[erp]scrap_paper_molded_contner",
                                    "[erp]scrap_paper_report_bdm",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "operators_log",
                                Title = "Журнал оператора",
                                Type = "element",
                                AllowedRoles = new List<string> {
                                   "[erp]bdm1_operators_log",
                                   "[erp]bdm2_operators_log",
                                },
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem
                                    {
                                        Name = "11",
                                        Title = "БДМ-1",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]bdm1_operators_log",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            var i = new OperatorsLogInterface();
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "12",
                                        Title = "БДМ-2",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                             "[erp]bdm2_operators_log",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            var i = new OperatorsLogInterface2();
                                        })
                                    },
                                },
                            },
                            new NavigationItem
                            {
                            Name = "idles_log",
                            Title = "Простои БДМ",
                            Type = "element",
                            Action = new DelegateCommand<string>(
                                action =>
                                {
                                    var i = new IdlesLogInterface();
                                }
                            ),
                            AllowedRoles = new List<string> {
                                "[erp]bdm1_downtime",
                                "[erp]bdm2_downtime",
                            },
                            },

                        }
                    }
                );
            }

            // Гофропроизводство (corrugator)
            {
                Items.Add(
                    new NavigationItem
                    {
                        Name = "corrugator",
                        Title = "Гофропроизводство",
                        Type = "section",
                        SubItems = new List<NavigationItem>
                        {
                            new NavigationItem
                            {
                                Name = "corrugator_operator",
                                Title = "Оператор ГА",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new CorrugatorMachineOperatorInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]corrugator_operator",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "rolls_control",
                                Title = "Управление раскатом",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ReelControlInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]reel_control",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "machine_stacker",
                                Title = "Стекер ГА",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new StackerInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]corrugator_stacker",
                                },
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem
                                    {
                                        Name = "cm_2_num_1",
                                        Title = "ГА-1 стекер 1",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]corrugator_stacker",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            string url = "l-pack://l-pack_erp/corrugator/machine_stacker?machine_id=2&stacker_number=1";
                                            Central.Navigator.ProcessURL(url);
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "cm_2_num_2",
                                        Title = "ГА-1 стекер 2",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]corrugator_stacker",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            string url = "l-pack://l-pack_erp/corrugator/machine_stacker?machine_id=2&stacker_number=2";
                                            Central.Navigator.ProcessURL(url);
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "cm_21_num_1",
                                        Title = "ГА-2 стекер 1",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]corrugator_stacker",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            string url = "l-pack://l-pack_erp/corrugator/machine_stacker?machine_id=21&stacker_number=1";
                                            Central.Navigator.ProcessURL(url);
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "cm_21_num_2",
                                        Title = "ГА-2 стекер 2",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]corrugator_stacker",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            string url = "l-pack://l-pack_erp/corrugator/machine_stacker?machine_id=21&stacker_number=2";
                                            Central.Navigator.ProcessURL(url);
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "cm_22_num_1",
                                        Title = "ГА-3 стекер 1",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]corrugator_stacker",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            string url = "l-pack://l-pack_erp/corrugator/machine_stacker?machine_id=22&stacker_number=1";
                                            Central.Navigator.ProcessURL(url);
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "cm_22_num_2",
                                        Title = "ГА-3 стекер 2",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]corrugator_stacker",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            string url = "l-pack://l-pack_erp/corrugator/machine_stacker?machine_id=22&stacker_number=2";
                                            Central.Navigator.ProcessURL(url);
                                        })
                                    },
                                },
                            },
                            new NavigationItem
                            {
                                Name = "label_manually_print",
                                Title = "Ручная печать ярлыков",
                                Type = "element",
                                AllowedRoles = new List<string> {
                                   "[erp]corrugator_stacker",
                                },
                                Action = new DelegateCommand<string>(action =>
                                {
                                    var i = new StackerManuallyPrintInterface();
                                }),
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem
                                    {
                                        Name = "corrugator_machine_2",
                                        Title = "ГА-1",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]corrugator_stacker",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            string url = "l-pack://l-pack_erp/corrugator/label_manually_print?machine_id=2&stacker_number=1&read_only=1";
                                            Central.Navigator.ProcessURL(url);
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "corrugator_machine_21",
                                        Title = "ГА-2",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                             "[erp]corrugator_stacker",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            string url = "l-pack://l-pack_erp/corrugator/label_manually_print?machine_id=21&stacker_number=1&read_only=1";
                                            Central.Navigator.ProcessURL(url);
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "corrugator_machine_22",
                                        Title = "ГА-3",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                             "[erp]corrugator_stacker",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            string url = "l-pack://l-pack_erp/corrugator/label_manually_print?machine_id=22&stacker_number=1&read_only=1";
                                            Central.Navigator.ProcessURL(url);
                                        })
                                    },
                                },
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string>
                                {
                                    "[erp]corrugator_stacker",
                                    "[erp]reel_control",
                                    "[erp]corrugator_operator",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "corrugator_operator_ksh",
                                Title = "Оператор ГА КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new CorrugatorMachineOperatorInterfaceKsh();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]corrugator_operator_ksh",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "reel_control_ksh",
                                Title = "Управление раскатом КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ReelControlKshInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]reel_control_ksh",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "machine_stacker_ksh",
                                Title = "Стекер ГА КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new StackerInterfaceKsh();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]corrugator_stacker_ksh",
                                },
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem
                                    {
                                        Name = "cm_23_num_1",
                                        Title = "ГА-1 стекер 1 КШ",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]corrugator_stacker_ksh",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            string url = "l-pack://l-pack_erp/corrugator/machine_stacker_ksh?machine_id=23&stacker_number=1";
                                            Central.Navigator.ProcessURL(url);
                                        })
                                    },
                                    new NavigationItem
                                    {
                                        Name = "cm_23_num_2",
                                        Title = "ГА-1 стекер 2 КШ",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]corrugator_stacker_ksh",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            string url = "l-pack://l-pack_erp/corrugator/machine_stacker_ksh?machine_id=23&stacker_number=2";
                                            Central.Navigator.ProcessURL(url);
                                        })
                                    },
                                },
                            },
                            new NavigationItem
                            {
                                Name = "label_manually_print_ksh",
                                Title = "Ручная печать ярлыков КШ",
                                Type = "element",
                                AllowedRoles = new List<string> {
                                   "[erp]corrugator_stacker_ksh",
                                },
                                Action = new DelegateCommand<string>(action =>
                                {
                                    var i = new StackerManuallyPrintKshInterface();
                                }),
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem
                                    {
                                        Name = "corrugator_machine_23",
                                        Title = "ГА-1 КШ",
                                        Type = "element",
                                        AllowedRoles = new List<string>
                                        {
                                             "[erp]corrugator_stacker_ksh",
                                        },
                                        Action = new DelegateCommand<string>(action =>
                                        {
                                            string url = "l-pack://l-pack_erp/corrugator/label_manually_print_ksh?machine_id=23&stacker_number=1&read_only=1";
                                            Central.Navigator.ProcessURL(url);
                                        })
                                    },
                                },
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string>
                                {
                                    "[erp]corrugator_operator_ksh",
                                    "[erp]reel_control_ksh",
                                    "[erp]corrugator_stacker_ksh",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "monitors",
                                Title = "Мониторы",
                                Type = "section",
                                AllowedRoles = new List<string> {
                                    "[erp]production_task_cm_map1",
                                    "[erp]production_task_cm_map2",
                                },
                                SubItems = new List<NavigationItem>{

                                    new NavigationItem
                                    {
                                        Name = "current_task",
                                        Title = "ПЗ",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new ProductionTaskMonitorInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string> {
                                            "[erp]production_task_cm_map1",
                                            "[erp]production_task_cm_map2",
                                        },
                                    },

                                    new NavigationItem
                                    {
                                        Name = "current_task1",
                                        Title = "ПЗ ГА1",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {///corrugator/monitors/current_task1
                                                string url = "l-pack://l-pack_erp/corrugator/monitors/current_task/1?machine_id=2";
                                                Central.Navigator.ProcessURL(url);
                                            }
                                        ),
                                        AllowedRoles = new List<string> {
                                            "[erp]production_task_cm_map1",
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "current_task2",
                                        Title = "ПЗ ГА2",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {///corrugator/monitors/current_task2
                                                string url = "l-pack://l-pack_erp/corrugator/monitors/current_task/2?machine_id=21";
                                                Central.Navigator.ProcessURL(url);
                                            }
                                        ),
                                        AllowedRoles = new List<string> {
                                            "[erp]production_task_cm_map2",
                                        },
                                    },

                            },
                            },
                            new NavigationItem
                            {
                                Name = "transport_system_fosber",
                                Title = "Транспортная система Fosber",
                                Type = "element",

                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new FosberTransportSystemInterface();
                                    }
                                ),
                                 AllowedRoles = new List<string> {
                                    "[erp]fosber_transport_system",
                                },

                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string>
                                {
                                    "[erp]production_task_cm_map1",
                                    "[erp]production_task_cm_map2",
                                    "[erp]fosber_transport_system",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "rolls_diagramm",
                                Title = "Учет рулонов на ГА",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new RollRegistrationInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]roll_registration",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "machine_operation",
                                Title = "Работа ГА",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new MachineOperationInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]corrugator_work_log",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "machine_report",
                                Title = "Отчёты ГА",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new CorrugatorMachineReportInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]machine_report",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "prod_corr_idle",
                                Title = "Простои гофропроизводства",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new Interfaces.Production.Corrugator.Idles.Interface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]prod_corr_idle",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "ksh_prod_corr_idle",
                                Title = "Простои гофропроизводства КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new Interfaces.Production.Corrugator.IdlesKsh.Interface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]prod_corr_idle_ksh",
                                }
                            },
                        }
                    }
                );
            }

            // Гофропереработка (converting)
            {
                Items.Add(
                    new NavigationItem
                    {
                        Name = "converting",
                        Title = "Гофропереработка",
                        Type = "section",
                        SubItems = new List<NavigationItem>
                        {
                            new NavigationItem
                            {
                                Name = "production_tasks",
                                Title = "ПЗ на переработку",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action => {
                                        var i = new ProductionTaskPRInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]production_task_pr",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "pillory",
                                Title = "Монитор мастера",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new PilloryInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]pillory",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "prod_conv_idle",
                                Title = "Простои гофропереработки",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new Interfaces.Production.Converting.Idles.Interface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]prod_conv_idle",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "strapper_monitor",
                                Title = "Монитор упаковщиков",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new StrapperMonitorInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]strapper_monitor",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string>
                                {
                                    "[erp]production_task_pr",
                                    "[erp]pillory",
                                    "[erp]prod_conv_idle",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "production_tasks_ksh",
                                Title = "ПЗ на переработку КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action => {
                                        var i = new ProductionTaskPRKshInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]production_task_pr_ksh",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "pillory_ksh",
                                Title = "Монитор мастера КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new PilloryKshInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]pillory_ksh",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "ksh_prod_conv_idle",
                                Title = "Простои гофропереработки КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new Interfaces.Production.Converting.IdlesKsh.Interface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]prod_conv_idle_ksh",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "strapper_ksh",
                                Title = "Упаковщик КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new StrapperKshInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]strapper_ksh",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "strapper_monitor_ksh",
                                Title = "Монитор упаковщиков КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new StrapperMonitorKshInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]strapper_monitor_ksh",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string>
                                {
                                    "[erp]production_task_pr_ksh",
                                    "[erp]pillory_ksh",
                                    "[erp]prod_conv_idle_ksh",
                                    "[erp]strapper_ksh",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "production_tasks_diagram",
                                Title = "Диаграмма ПЗ на переработке",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ProductionTaskPRDiagramInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]production_task_pr_diagram",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "techological_maps",
                                Title = "Техкарта по ПЗ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new TechnologicalMapExcelInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]technological_map_excel",
                                },
                            },
                        }
                    }
                );
            }

            // Литая тара (molded_container)
            {
                Items.Add(
                    new NavigationItem
                    {
                        Name = "molded_container",
                        Title = "ЛТ",
                        Type = "section",
                        SubItems = new List<NavigationItem>
                        {
                            new NavigationItem
                            {
                                Name = "operator",
                                Title = "Оператор ВФМ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new MoldedContainerMachineInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]molded_contnr_operator",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "operator_vfm",
                                Title = "ВФМ ЛТ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new MoldedContainerRecyclingVacuumFormingMachineInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]molded_contnr_operator",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "recycling",
                                Title = "Переработка ЛТ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                       var i = new MoldedContainerRecyclingInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]molded_contnr_converting",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "molded_contnr_monitoring",
                                Title = "Мониторинг ЛТ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                       var i = new MoldedContainerMonitoringInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]molded_contnr_monitoring",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string>
                                {
                                    "[erp]molded_contnr_operator",
                                    "[erp]molded_contnr_converting",
                                    "[erp]molded_contnr_monitoring",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "idles_report",
                                Title = "Простои ЛТ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                       var i = new MoldedContainerRecyclingIdlesInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]molded_contnr_idles",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "converting_report",
                                Title = "Отчеты по ЛТ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                       var i = new MoldedContainerReportInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]molded_contnr_productn_repo",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "molded_container_turnover",
                                Title = "Оборотная ведомость",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                       var i = new MoldedContainerTurnoverInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]molded_contnr_turnover",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string>
                                {
                                    "[erp]molded_contnr_idles",
                                    "[erp]molded_contnr_productn_repo",
                                    "[erp]molded_contnr_turnover",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "molded_container_consumption_to_production",
                                Title = "Списание в производство",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new MoldedContainerConsumptionToProductionInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]molded_contnr_consumpt_task",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "rearrival_molded_container",
                                Title = "Переоприходование",
                                Type = "section",
                                AllowedRoles = new List<string>
                                {
                                    "[erp]rearrival_scrap_paper",
                                    "[erp]rearrival_blank",
                                },
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem
                                    {
                                        Name = "rearrival_scrap_paper",
                                        Title = "Переоприходование макулатуры",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new RearrivalScrapPaperInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]rearrival_scrap_paper",
                                        }
                                    },
                                    new NavigationItem
                                    {
                                        Name = "rearrival_blank",
                                        Title = "Переоприходование заготовок",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new RearrivalBlankInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]rearrival_blank",
                                        }
                                    },
                                }
                            },
                        }
                    }
                );
            }

            // Склад (stock)
            {
                Items.Add(
                    new NavigationItem
                    {
                        Name = "stock",
                        Title = "Склад",
                        Type = "section",
                        SubItems = new List<NavigationItem>
                        {

                            new NavigationItem
                            {
                                Name = "shipment_control",
                                Title = "Управление отгрузками",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ShipmentControlInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]shipment_control",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "shipment_statistics",
                                Title = "Статистика отгрузок",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ShipmentStatisticsInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]shipment_statistics",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "forklift_drivers",
                                Title = "Погрузчики",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ForkliftDriversInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]forklift_drivers",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "reports",
                                Title = "Отчёты по складу",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new WarehouseReportsInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]warehouse_report",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string> {
                                    "[erp]shipment_control",
                                    "[erp]shipment_statistics",
                                    "[erp]forklift_drivers",
                                    "[erp]warehouse_report",
                                },
                            },

                            new NavigationItem
                            {
                                Name = "shipment_ksh",
                                Title = "Управление отгрузками КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ShipmentKshInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]shipment_control_ksh",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "shipment_statistics_ksh",
                                Title = "Статистика отгрузок КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ShipmentStatisticsKshInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]shipment_statistics_ksh",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "forklift_drivers_ksh",
                                Title = "Погрузчики КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ForkliftDriversKshInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]forklift_drivers_ksh",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "report_ksh",
                                Title = "Отчёты по складу КШ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new WarehouseReportKshInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]warehouse_report_ksh",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string> {
                                    "[erp]shipment_control_ksh",
                                    "[erp]shipment_statistics_ksh",
                                    "[erp]forklift_drivers_ksh",
                                    "[erp]warehouse_report_ksh",
                                },
                            },

                            new NavigationItem
                            {
                                Name = "molded_container_shipment",
                                Title = "Отгрузка ЛТ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new MoldedContainerShipmentInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]molded_contnr_shipment",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "molded_container_warehouse",
                                Title = "Склад ЛТ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new MoldedContainerWarehouseInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]molded_contnr_warehouse",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string> {
                                    "[erp]molded_contnr_shipment",
                                    "[erp]molded_contnr_warehouse",
                                },
                            },

                            new NavigationItem
                            {
                                Name = "operations",
                                Title = "Список списаний",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new WarehouseOperationsInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]warehouse_operations",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "pallets",
                                Title = "Поддоны",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new PalletInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]pallet",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "pallet_dispose",
                                Title = "Утилизация паллета",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new PalletDisposeInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]pallet_dispose",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "pallet_search",
                                Title = "Поиск паллета",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new PalletSearcherInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]pallet_search",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "cell_visualization",
                                Title = "Визуализация ячеек",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new CellVisualizationInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]cell_visualization",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "condition",
                                Title = "Состояние склада",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new WarehouseConditionInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]warehouse_condition",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "responsible_stock",
                                Title = "Склад ответственного хранения",
                                Action = new DelegateCommand<string>(
                                    action => {
                                        var i = new ResponsibleStockInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]responsible_stock",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "separator",
                                Title = "",
                                Type = "separator",
                                AllowedRoles = new List<string> {
                                    "[erp]warehouse_operations",
                                    "[erp]pallet",
                                    "[erp]pallet_dispose",
                                    "[erp]pallet_search",
                                    "[erp]cell_visualization",
                                    "[erp]warehouse_condition",
                                    "[erp]responsible_stock",
                                },
                            },

                            new NavigationItem
                            {
                                Name = "warehouse_control",
                                Title = "Управление складом",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new WarehouseControlInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]warehouse_control",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "catalog",
                                Title = "Справочник склада",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new WarehouseCatalogInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]warehouse_catalog",
                                    "[erp]warehouse_directory",
                                }
                            },
                            new NavigationItem
                            {
                                Name = "pallet_binding",
                                Title = "Привязка поддонов",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new PalletBindingInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]pallet_binding"
                                }
                            },
                            new NavigationItem
                            {
                                Name = "raw_material_monitor",
                                Title = "Монитор остатков сырья",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new RawMaterialResidueMonitorInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]raw_material_monitor"
                                }
                            }
                        }
                    }
                );
            }

            // Доставка (delivery)
            {
                Items.Add(
                    new NavigationItem
                    {
                        Name = "delivery",
                        Title = "Логистика",
                        Type = "section",
                        SubItems = new List<NavigationItem>
                        {
                            new NavigationItem
                            {
                                Name = "delivery_addresses",
                                Title = "Адреса доставки",
                                Action = new DelegateCommand<string>(
                                    action => {
                                        var i = new DeliveryAddressesInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]delivery_addresses",
                                }
                            },
                        }
                    }
                );
            }

            // Сервис (service)
            {
                Items.Add(
                    new NavigationItem
                    {
                        Name = "service",
                        Title = "Сервис",
                        Type = "section",
                        SubItems = new List<NavigationItem>
                        {
                            new NavigationItem
                            {
                                Name = "change_password",
                                Title = "Изменить пароль",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i=new PasswordForm("edit", Central.User.AccountId.ToString() );
                                    }
                                ),
                                AllowedRoles = new List<String>
                                {
                                    "[erp]password",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "screenshots",
                                Title = "Скриншоты",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ScreenShotsInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]screenshots",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "email",
                                Title = "Почта",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new MailInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]mail",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "printing",
                                Title = "Печать",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new PrintingInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]printing",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "transport_access",
                                Title = "Допуск автотранспорта",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new StockGateInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]access_transport",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "fire_plan_room",
                                Title = "Пожарные датчики на БДМ2",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new FirePlanRoomInterface();
                                    }
                                ),
                                AllowedRoles = new List<string>
                                {
                                    "[erp]accounts",
                                }
                            },

                            new NavigationItem
                            {
                                Name = "molded_contnr_security",
                                Title = "Управление воротами ЛТ",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new MoldedContainerGateInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]molded_contnr_security",
                                },
                            },
                        },
                    }
                );
            }

            //Администрирование
            {
                Items.Add(
                    new NavigationItem
                    {
                        Name = "administration",
                        Title = "Администрирование",
                        Type = "section",
                        SubItems = new List<NavigationItem>
                        {
                            new NavigationItem
                            {
                                Name = "accounts",
                                Title = "Учетные записи",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new AccountsInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]accounts",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "messages",
                                Title = "Сообщения",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new MessagesInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]messages",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "clients",
                                Title = "Клиенты",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ClientInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]client",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "sessions",
                                Title = "Сессии",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new SessionsInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]session",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "jobs",
                                Title = "Джобы",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new JobsInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]job",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "servers",
                                Title = "Серверы",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new ServersInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]server",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "sources",
                                Title = "Ресурсы",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new Interfaces.Service.Sources.SourcesInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]server",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "storages",
                                Title = "Хранилища",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i = new StoragesInterface();
                                    }
                                ),
                                AllowedRoles = new List<string> {
                                    "[erp]server",
                                },
                            },
                        },
                    }
                );
            }

            // Справка (documentation)
            {
                Items.Add(
                    new NavigationItem
                    {
                        Name = "documentation",
                        Title = "Справка",
                        Type = "section",
                        AllowedRoles = new List<string>
                        {
                            "*",
                        },
                        SubItems = new List<NavigationItem>
                        {
                            new NavigationItem
                            {
                                Name = "update",
                                Title = "Обновить",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        if (Central.DebugMode)
                                        {
                                            {
                                                string msg = "";
                                                msg += $"В отладочном режиме обновление недоступно";

                                                string dsc = "";

                                                var d = new DialogWindow(msg, "Обновление", dsc, DialogWindowButtons.OKCancel);
                                                d.ShowDialog();
                                            }
                                        }
                                        else
                                        {
                                            Central.Updater.CheckUpdate(false, true);
                                        }
                                    }
                                ),
                                AllowedRoles = new List<String>
                                {
                                    "*"
                                },
                            },
                            new NavigationItem()
                            {
                                Name = "config",
                                Title = "Конфигурация",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var filePath = "application.config";
                                        Central.OpenFile(filePath);
                                    }
                                ),
                                AllowedRoles = new List<String>
                                {
                                    "*"
                                },
                            },
                            new NavigationItem
                            {
                                Name = "manual",
                                Title = "Документация",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action => { Central.ShowHelp("/doc/l-pack-erp"); }
                                ),
                                AllowedRoles = new List<String>
                                {
                                    "*"
                                },
                            },
                            new NavigationItem
                            {
                                Name = "about",
                                Title = "О программе",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var info = new AssemblyInfo();

                                        string str = "";
                                        str += $"{info.Title}";
                                        str += $"\nВерсия: {info.AssemblyVersion} ";
                                        str += $"\nСборка: {info.Description}";
                                        str += $"\nОкно: {Central.WindowSize}";

                                        var e = new DialogWindow(str, "Информация о программе");
                                        e.ShowDialog();
                                    }
                                ),
                                AllowedRoles = new List<String>
                                {
                                    "*"
                                },
                            }
                        }
                    }
                );
            }

            // Отладка (debug)
            {
                Items.Add(
                    new NavigationItem
                    {
                        Name = "debug",
                        Title = "Отладка",
                        Type = "section",
                        AllowedRoles = new List<string>
                        {
                            "[erp]debug",
                        },
                        SubItems = new List<NavigationItem>
                        {
                            new NavigationItem()
                            {
                                Name = "info",
                                Title = "Информация",
                                Type = "element",
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem()
                                    {
                                        Name = "client",
                                        Title = "Клиент",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                string msg2 = "";
                                                msg2 += $"Server:";

                                                foreach (KeyValuePair<string, string> i in Central.ServerInfo)
                                                {
                                                    msg2 += $"\n{i.Key}={i.Value} ";
                                                }

                                                string dsc2 = "";
                                                var e2 = new DialogWindow(msg2, "Server", dsc2, DialogWindowButtons.OKCancel);
                                                var result2 = e2.ShowDialog();
                                            }
                                        ),
                                        AllowedRoles = new List<String>
                                        {
                                            "*"
                                        },
                                    },
                                    new NavigationItem()
                                    {
                                        Name = "server",
                                        Title = "Сервер",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                string msg2 = "";
                                                msg2 += $"Server:";

                                                foreach (KeyValuePair<string, string> i in Central.ServerInfo)
                                                {
                                                    msg2 += $"\n{i.Key}={i.Value} ";
                                                }

                                                if (Central.ServerLocks.Count > 0)
                                                {
                                                    msg2 += $"\n\n";
                                                    msg2 += $"Locks:";
                                                    foreach (KeyValuePair<string, string> i in Central.ServerLocks)
                                                    {
                                                        msg2 += $"\n{i.Key}={i.Value} ";
                                                    }
                                                }

                                                string dsc2 = "";
                                                var e2 = new DialogWindow(msg2, "Server", dsc2, DialogWindowButtons.OKCancel);
                                                var result2 = e2.ShowDialog();
                                            }
                                        ),
                                        AllowedRoles = new List<String>
                                        {
                                            "*"
                                        },
                                    },
                                },
                                AllowedRoles = new List<String>
                                {
                                    "*"
                                },
                            },
                            new NavigationItem()
                            {
                                Name = "development",
                                Title = "Интерфейсы в разработке",
                                Type = "element",
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem
                                    {
                                        Name = "stacker_drop",
                                        Title = "Съёмы стекера",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new StackerDropInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]corrugator_stacker",
                                        }
                                    },
                                    new NavigationItem
                                    {
                                        Name = "stacker_drop_ksh",
                                        Title = "Съёмы стекера КШ",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new StackerDropKshInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]debug",
                                        }
                                    },
                                    new NavigationItem
                                    {
                                        Name = "paper_production_task",
                                        Title = "ПЗ БДМ",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new PaperProductionTaskInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]debug",
                                        }
                                    },
                                    new NavigationItem
                                    {
                                        Name = "edi",
                                        Title = "EDI",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new EdiInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]debug",
                                        }
                                    },
                                    new NavigationItem
                                    {
                                        Name = "sample_cardboard_ksh",
                                        Title = "Картон для образцов КШ",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new SampleCardbrdKshInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]debug",
                                        }
                                    },
                                },
                                AllowedRoles = new List<String>
                                {
                                    "[erp]debug",
                                },
                            },
                            new NavigationItem()
                            {
                                Name = "tools",
                                Title = "Инструменты",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var poster = new DebugInterface();
                                    }
                                ),
                                AllowedRoles = new List<String>
                                {
                                    "*"
                                },
                            },
                            new NavigationItem()
                            {
                                Name = "commands",
                                Title = "Команды",
                                Type = "section",
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem()
                                    {
                                        Name = "hop",
                                        Title = "Сменить сервер",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {

                                                Messenger.Default.Send(new ItemMessage()
                                                {
                                                    ReceiverGroup = "Main",
                                                    ReceiverName = "MainWindow",
                                                    SenderName = "Navigator",
                                                    Action = "ChangeServer",
                                                    Message = "",
                                                });
                                            }
                                        ),
                                        AllowedRoles = new List<String>
                                        {
                                            "*"
                                        },
                                    },
                                    new NavigationItem()
                                    {
                                        Name = "restart",
                                        Title = "Перезапуск",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                Messenger.Default.Send(new ItemMessage()
                                                {
                                                    ReceiverGroup = "Main",
                                                    ReceiverName = "MainWindow",
                                                    SenderName = "Navigator",
                                                    Action = "Restart",
                                                    Message = "",
                                                });
                                            }
                                        ),
                                        AllowedRoles = new List<String>
                                        {
                                            "*"
                                        },
                                    },
                                    new NavigationItem()
                                    {
                                        Name = "gc_collect",
                                        Title = "GC Collect",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                GC.Collect();
                                            }
                                        ),
                                        AllowedRoles = new List<String>
                                        {
                                            "*"
                                        },
                                    },
                                    new NavigationItem()
                                    {
                                        Name = "exit",
                                        Title = "Выход",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                Messenger.Default.Send(new ItemMessage()
                                                {
                                                    ReceiverGroup = "Main",
                                                    ReceiverName = "MainWindow",
                                                    SenderName = "Navigator",
                                                    Action = "Exit",
                                                    Message = "",
                                                });
                                            }
                                        ),
                                        AllowedRoles = new List<String>
                                        {
                                            "*"
                                        },
                                    },
                                    new NavigationItem()
                                    {
                                        Name = "update_menu",
                                        Title = "Обновить меню",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                UpdateMainMenu();
                                            }
                                        ),
                                        AllowedRoles = new List<String>
                                        {
                                            "*"
                                        },
                                    },
                                },
                                AllowedRoles = new List<String>
                                {
                                    "*"
                                },
                            },
                            new NavigationItem()
                            {
                                Name = "test",
                                Title = "Тесты",
                                Type = "section",
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem()
                                    {
                                        Name = "window",
                                        Title = "Окно",
                                        Type = "section",
                                        SubItems = new List<NavigationItem>
                                        {
                                            new NavigationItem()
                                            {
                                                Name = "debug_resize",
                                                Title = "Размер",
                                                Type = "section",
                                                SubItems = new List<NavigationItem>
                                                {
                                                    new NavigationItem()
                                                    {
                                                        Name = "size_maximized",
                                                        Title = "максимальный",
                                                        Type = "element",
                                                        Action = new DelegateCommand<string>(
                                                            action =>
                                                            {

                                                                Messenger.Default.Send(new ItemMessage()
                                                                {
                                                                    ReceiverGroup = "Main",
                                                                    ReceiverName = "MainWindow",
                                                                    SenderName = "Navigator",
                                                                    Action = "SetScreenMode",
                                                                    Message = "maximized",
                                                                });
                                                            }
                                                        ),
                                                        AllowedRoles = new List<String>
                                                        {
                                                            "*"
                                                        },
                                                    },
                                                    new NavigationItem()
                                                    {
                                                        Name = "size_normal",
                                                        Title = "нормальный",
                                                        Type = "element",
                                                        Action = new DelegateCommand<string>(
                                                            action =>
                                                            {

                                                                Messenger.Default.Send(new ItemMessage()
                                                                {
                                                                    ReceiverGroup = "Main",
                                                                    ReceiverName = "MainWindow",
                                                                    SenderName = "Navigator",
                                                                    Action = "SetScreenMode",
                                                                    Message = "normal",
                                                                });
                                                            }
                                                        ),
                                                        AllowedRoles = new List<String>
                                                        {
                                                            "*"
                                                        },
                                                    },
                                                    new NavigationItem()
                                                    {
                                                        Name = "size_fullscreen",
                                                        Title = "полный",
                                                        Type = "element",
                                                        Action = new DelegateCommand<string>(
                                                            action =>
                                                            {

                                                                Messenger.Default.Send(new ItemMessage()
                                                                {
                                                                    ReceiverGroup = "Main",
                                                                    ReceiverName = "MainWindow",
                                                                    SenderName = "Navigator",
                                                                    Action = "SetScreenMode",
                                                                    Message = "fullscreen",
                                                                });
                                                            }
                                                        ),
                                                        AllowedRoles = new List<String>
                                                        {
                                                            "*"
                                                        },
                                                    },
                                                    new NavigationItem()
                                                    {
                                                        Name = "size_nofullscreen",
                                                        Title = "неполный",
                                                        Type = "element",
                                                        Action = new DelegateCommand<string>(
                                                            action =>
                                                            {

                                                                Messenger.Default.Send(new ItemMessage()
                                                                {
                                                                    ReceiverGroup = "Main",
                                                                    ReceiverName = "MainWindow",
                                                                    SenderName = "Navigator",
                                                                    Action = "SetScreenMode",
                                                                    Message = "nofullscreen",
                                                                });
                                                            }
                                                        ),
                                                        AllowedRoles = new List<String>
                                                        {
                                                            "*"
                                                        },
                                                    },
                                                    new NavigationItem()
                                                    {
                                                        Name = "debug_resize_0",
                                                        Title = "800x600",
                                                        Type = "element",
                                                        Action = new DelegateCommand<string>(
                                                            action =>
                                                            {
                                                                Messenger.Default.Send(new ItemMessage()
                                                                {
                                                                    ReceiverGroup = "All",
                                                                    SenderName = "Navigator",
                                                                    Action = "Resize",
                                                                    Message = "800x600",
                                                                });
                                                            }
                                                        ),
                                                        AllowedRoles = new List<String>
                                                        {
                                                            "*"
                                                        },
                                                    },
                                                    new NavigationItem()
                                                    {
                                                        Name = "debug_resize_1",
                                                        Title = "1024x768",
                                                        Type = "element",
                                                        Action = new DelegateCommand<string>(
                                                            action =>
                                                            {
                                                                Messenger.Default.Send(new ItemMessage()
                                                                {
                                                                    ReceiverGroup = "All",
                                                                    SenderName = "Navigator",
                                                                    Action = "Resize",
                                                                    Message = "1024x768",
                                                                });
                                                            }
                                                        ),
                                                        AllowedRoles = new List<String>
                                                        {
                                                            "*"
                                                        },
                                                    },
                                                    new NavigationItem()
                                                    {
                                                        Name = "debug_resize_2",
                                                        Title = "1280x768",
                                                        Type = "element",
                                                        Action = new DelegateCommand<string>(
                                                            action =>
                                                            {
                                                                Messenger.Default.Send(new ItemMessage()
                                                                {
                                                                    ReceiverGroup = "All",
                                                                    SenderName = "Navigator",
                                                                    Action = "Resize",
                                                                    Message = "1280x768",
                                                                });
                                                            }
                                                        ),
                                                        AllowedRoles = new List<String>
                                                        {
                                                            "*"
                                                        },
                                                    },
                                                    new NavigationItem()
                                                    {
                                                        Name = "debug_resize_4",
                                                        Title = "1296x768",
                                                        Type = "element",
                                                        Action = new DelegateCommand<string>(
                                                            action =>
                                                            {
                                                                Messenger.Default.Send(new ItemMessage()
                                                                {
                                                                    ReceiverGroup = "All",
                                                                    SenderName = "Navigator",
                                                                    Action = "Resize",
                                                                    Message = "1296x768",
                                                                });
                                                            }
                                                        ),
                                                        AllowedRoles = new List<String>
                                                        {
                                                            "*"
                                                        },
                                                    },
                                                    new NavigationItem()
                                                    {
                                                        Name = "debug_resize_3",
                                                        Title = "1360x768",
                                                        Type = "element",
                                                        Action = new DelegateCommand<string>(
                                                            action =>
                                                            {
                                                                Messenger.Default.Send(new ItemMessage()
                                                                {
                                                                    ReceiverGroup = "All",
                                                                    SenderName = "Navigator",
                                                                    Action = "Resize",
                                                                    Message = "1360x768",
                                                                });
                                                            }
                                                        ),
                                                        AllowedRoles = new List<String>
                                                        {
                                                            "*"
                                                        },
                                                    },
                                                },
                                                AllowedRoles = new List<String>
                                                {
                                                    "*"
                                                },
                                            },
                                            new NavigationItem()
                                            {
                                                Name = "debug_mode_single",
                                                Title = "SDI",
                                                Type = "element",
                                                Action = new DelegateCommand<string>(
                                                    action =>
                                                    {
                                                        Messenger.Default.Send(new ItemMessage()
                                                        {
                                                            ReceiverGroup = "All",
                                                            SenderName = "Navigator",
                                                            Action = "SetDisplayMode",
                                                            Message = "Single",
                                                        });
                                                    }
                                                ),
                                                AllowedRoles = new List<String>
                                                {
                                                    "*"
                                                },
                                            },
                                            new NavigationItem()
                                            {
                                                Name = "debug_mode_multi",
                                                Title = "MDI",
                                                Type = "element",
                                                Action = new DelegateCommand<string>(
                                                    action =>
                                                    {
                                                        Messenger.Default.Send(new ItemMessage()
                                                        {
                                                            ReceiverGroup = "All",
                                                            SenderName = "Navigator",
                                                            Action = "SetDisplayMode",
                                                            Message = "Multi",
                                                        });
                                                    }
                                                ),
                                                AllowedRoles = new List<String>
                                                {
                                                    "*"
                                                },
                                            },
                                        },
                                        AllowedRoles = new List<String>
                                        {
                                            "*"
                                        },
                                    },
                                    new NavigationItem()
                                    {
                                        Name = "dialogs",
                                        Title = "Диалоги",
                                        Type = "element",
                                        SubItems = new List<NavigationItem>
                                        {
                                            new NavigationItem()
                                            {
                                                Name = "dbgdialog1",
                                                Title = "Диалог 1",
                                                Type = "element",
                                                Action = new DelegateCommand<string>(
                                                    action =>
                                                    {
                                                        string msg2 = "";
                                                        msg2 += $"Доступна новая версия программы: ";
                                                        msg2 += $"\nВы используете версию: ";

                                                        string dsc2 = "";
                                                        dsc2 += $"deserunt mollit anim id est laborum.";
                                                        dsc2 += $"Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor";

                                                        var e2 = new DialogWindow(msg2, "Обновление", dsc2, DialogWindowButtons.OKCancel);
                                                        var result2 = e2.ShowDialog();

                                                    }
                                                ),
                                                AllowedRoles = new List<String>
                                                {
                                                    "*"
                                                },
                                            },
                                            new NavigationItem()
                                            {
                                                Name = "dbgdialog2",
                                                Title = "Диалог 2",
                                                Type = "element",
                                                Action = new DelegateCommand<string>(
                                                    action =>
                                                    {
                                                        string msg = "";
                                                        msg += $"Доступна новая версия программы: ";
                                                        msg += $"\nВы используете версию: ";
                                                        msg += $"\nНажмите Да для начала обновления, после обновления программа перезапустится.";

                                                        string dsc = "";
                                                        dsc += $"Доступна новая версия программы: ";
                                                        dsc += $"Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor";
                                                        dsc += $"incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla";
                                                        dsc += $"exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute ";
                                                        dsc += $"irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla";
                                                        dsc += $"pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia ";
                                                        dsc += $"deserunt mollit anim id est laborum.";
                                                        dsc += $"incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla";
                                                        dsc += $"exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute ";
                                                        dsc += $"irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla";
                                                        dsc += $"pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia ";
                                                        dsc += $"deserunt mollit anim id est laborum.";

                                                        var e = new DialogWindow(msg, "Обновление", dsc);
                                                        var result = e.ShowDialog();

                                                    }
                                                ),
                                                AllowedRoles = new List<String>
                                                {
                                                    "*"
                                                },
                                            },
                                        },
                                        AllowedRoles = new List<String>
                                        {
                                            "*"
                                        },
                                    },
                                    new NavigationItem()
                                    {
                                        Name = "interface",
                                        Title = "Лаборатория интерфейсов",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new UiLabInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<String>
                                        {
                                            "*"
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "gridbox4",
                                        Title = "Тестирование гридов",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new GridBox4Interface();
                                            }
                                        ),
                                        AllowedRoles = new List<string>
                                        {
                                            "[erp]debug",
                                        }
                                    },
                                    new NavigationItem
                                    {
                                        Name = "barcode_generator",
                                        Title = "Генератор штрих-кода",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                var i = new BarcodeGeneratorInterface();
                                            }
                                        ),
                                        AllowedRoles = new List<string> {
                                            "*",
                                        },
                                    },
                                },
                                AllowedRoles = new List<String>
                                {
                                    "*"
                                },
                            },
                        }
                    }
                );
            }



            {
                Items.Add(
                   new NavigationItem()
                   {
                       Name = "user",
                       Title = "user@server",
                       Type = "element",
                       SubItems = new List<NavigationItem>
                       {
                            new NavigationItem
                            {
                                Name = "change_password",
                                Title = "Изменить пароль",
                                Type = "element",
                                Action = new DelegateCommand<string>(
                                    action =>
                                    {
                                        var i=new PasswordForm("edit", Central.User.AccountId.ToString() );
                                    }
                                ),
                                AllowedRoles = new List<String>
                                {
                                    "[erp]password",
                                },
                            },
                            new NavigationItem
                            {
                                Name = "access_level",
                                Title = "Тестовый доступ",
                                Type = "element",
                                SubItems = new List<NavigationItem>
                                {
                                    new NavigationItem
                                    {
                                        Name = "none",
                                        Title = "Нет",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                SetRoleLevelTest(AccessMode.None);
                                            }
                                        ),
                                        AllowedRoles = new List<String>
                                        {
                                            "*"
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "deny",
                                        Title = "Запрещен",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                SetRoleLevelTest(AccessMode.Deny);
                                            }
                                        ),
                                        AllowedRoles = new List<String>
                                        {
                                            "*"
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "readonly",
                                        Title = "Только чтение",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                SetRoleLevelTest(AccessMode.ReadOnly);
                                            }
                                        ),
                                        AllowedRoles = new List<String>
                                        {
                                            "*"
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "allowall",
                                        Title = "Полный доступ",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                SetRoleLevelTest(AccessMode.FullAccess);
                                            }
                                        ),
                                        AllowedRoles = new List<String>
                                        {
                                            "*"
                                        },
                                    },
                                    new NavigationItem
                                    {
                                        Name = "special",
                                        Title = "Спецправа",
                                        Type = "element",
                                        Action = new DelegateCommand<string>(
                                            action =>
                                            {
                                                SetRoleLevelTest(AccessMode.Special);
                                            }
                                        ),
                                        AllowedRoles = new List<String>
                                        {
                                            "*"
                                        },
                                    },
                                },
                                AllowedRoles = new List<String>
                                {
                                    "[erp]debug",
                                },
                            },
                       },
                       Align = "Right",
                       AllowedRoles = new List<String>
                       {
                            "*"
                       },
                   }
               );
            }

            {
                Items.Add(
                    new NavigationItem()
                    {
                        Name = "notes",
                        Title = "",
                        Type = "element",
                        Align = "Right",
                        AllowedRoles = new List<String>
                        {
                            "*"
                        },
                        Action = new DelegateCommand<string>(
                            action =>
                            {
                                Messenger.Default.Send(new ItemMessage()
                                {
                                    ReceiverGroup = "Main",
                                    ReceiverName = "Notifications",
                                    SenderName = "NotificationItems",
                                    Action = "Show",
                                });
                            }
                        ),
                        MinWidth = 25,
                        Style = "buttonSectionNoteStyle",
                    }
                );
            }

            PrepareItems();
            PrepareRoles();
        }

        /// <summary>
        ///  На первом уровне меню, если список ролей не задан,
        ///  он будет вычислен, как объединенние ролей всех разделов второго уровня.
        /// </summary>
        public void PrepareRoles()
        {
            if (Items.Count > 0)
            {
                //proc level1                               
                var items1 = Items;
                foreach (NavigationItem i1 in items1)
                {
                    var roles = new List<String>();
                    if (i1.SubItems.Count > 0)
                    {
                        //proc level2
                        var items2 = i1.SubItems;
                        foreach (NavigationItem i2 in items2)
                        {
                            if (i2.AllowedRoles.Count > 0)
                            {
                                foreach (string r in i2.AllowedRoles)
                                {
                                    if (!roles.Contains(r))
                                    {
                                        roles.Add(r);
                                    }
                                }
                            }
                        }
                    }

                    if (i1.AllowedRoles.Count == 0)
                    {
                        if (roles.Count > 0)
                        {
                            foreach (string r in roles)
                            {
                                if (!i1.AllowedRoles.Contains(r))
                                {
                                    i1.AllowedRoles.Add(r);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Предварительная подготовка структуры меню. Перед отрисовкой производятся
        /// некоторые предварительные вычисления (вычисление полного адреса элемента)
        /// </summary>
        public void PrepareItems()
        {
            if (Items.Count > 0)
            {
                //proc level1                               
                var items1 = Items;
                foreach (NavigationItem i1 in items1)
                {
                    i1.Address = $"/{i1.Name}";
                    i1.Level = 1;

                    if (i1.SubItems.Count > 0)
                    {
                        //proc level2
                        var items2 = i1.SubItems;
                        foreach (NavigationItem i2 in items2)
                        {
                            i2.Address = $"/{i1.Name}/{i2.Name}";
                            i2.Level = 2;

                            if (i2.SubItems.Count > 0)
                            {
                                //proc level3
                                var items3 = i2.SubItems;
                                foreach (NavigationItem i3 in items3)
                                {
                                    i3.Address = $"/{i1.Name}/{i2.Name}/{i3.Name}";
                                    i3.Level = 3;

                                    if (i3.SubItems.Count > 0)
                                    {

                                        //proc level4
                                        var items4 = i3.SubItems;
                                        foreach (NavigationItem i4 in items4)
                                        {
                                            i4.Address = $"/{i1.Name}/{i2.Name}/{i3.Name}/{i4.Name}";
                                            i4.Level = 4;

                                            if (i4.SubItems.Count > 0)
                                            {

                                                //proc level5
                                                var items5 = i4.SubItems;
                                                foreach (NavigationItem i5 in items5)
                                                {
                                                    i5.Address = $"/{i1.Name}/{i2.Name}/{i3.Name}/{i4.Name}/{i5.Name}";
                                                    i5.Level = 5;

                                                    if (i5.SubItems.Count > 0)
                                                    {
                                                    }
                                                }

                                            }
                                        }

                                    }
                                }
                            }
                        }
                    }

                }
            }
        }

        public void UpdateMainMenu()
        {
            if (MainMenu != null)
            {
                foreach (var item in MainMenu.Items)
                {
                    var m = item as MenuItem;
                    m.Background = "#ff00cc00".ToBrush();
                    var r0 = item;
                }
            }
        }

        public void RenderMainMenu2(DevExpress.Xpf.Bars.MainMenuControl menu, DevExpress.Xpf.Bars.MainMenuControl menuRight)
        {
            MainMenu2 = menu;
            RightMenu2 = menuRight;

            menu.Items.Clear();
            menuRight.Items.Clear();
            var items = Central.Navigator.Items;

            var buttonItemStyle = (Style)menu.TryFindResource("buttonItemStyle");
            var buttonItemTriggers = (BarItemTriggerCollection)menu.TryFindResource("buttonItemTriggers");
            var buttonSectionTriggers = (BarItemTriggerCollection)menu.TryFindResource("buttonSectionTriggers");

            if (items.Count > 0)
            {
                var m1 = new DevExpress.Xpf.Bars.BarSubItem();
                foreach (NavigationItem item in items)
                {
                    if (CheckItemPermissions(item))
                    {
                        var s = RenderMainMenu2CreateSection(item);
                        if (item.SubItems.Count > 0)
                        {
                            RenderMainMenu2ProcessItems(item.SubItems, s);
                        }

                        if (item.Align.ToLower() == "right")
                        {
                            menuRight.Items.Add(s);
                        }
                        else
                        {
                            menu.Items.Add(s);
                        }
                    }
                }
            }
            RoleLevelUpdate();
        }

        private void RenderMainMenu2ProcessItems(List<NavigationItem> items, DevExpress.Xpf.Bars.BarSubItem section)
        {
            foreach (NavigationItem item in items)
            {
                if (CheckItemPermissions(item))
                {
                    if (item.Type == "separator")
                    {
                        var separator = new DevExpress.Xpf.Bars.BarItemSeparator();
                        section.Items.Add(separator);
                    }
                    else if (item.SubItems.Count > 0)
                    {
                        var x = RenderMainMenu2CreateSection(item);
                        RenderMainMenu2ProcessItems(item.SubItems, x);
                        if (item.Visible)
                        {
                            section.Items.Add(x);
                        }
                    }
                    else
                    {
                        var x = RenderMainMenu2CreateItem(item);
                        if (item.Visible)
                        {
                            section.Items.Add(x);
                        }
                    }
                }
            }
        }

        private DevExpress.Xpf.Bars.BarSubItem RenderMainMenu2CreateSection(NavigationItem item)
        {
            var result = new DevExpress.Xpf.Bars.BarSubItem();
            var menu = MainMenu2;

            var buttonSectionStyle = (Style)menu.TryFindResource("buttonSectionStyle");
            if (!item.Style.IsNullOrEmpty())
            {
                buttonSectionStyle = (Style)menu.TryFindResource(item.Style);
            }

            var buttonItemTriggers = (BarItemTriggerCollection)menu.TryFindResource("buttonItemTriggers");
            var buttonSectionTriggers = (BarItemTriggerCollection)menu.TryFindResource("buttonSectionTriggers");

            bool show = false;
            show = CheckItemPermissions(item);
            if (show)
            {
                result = new DevExpress.Xpf.Bars.BarSubItem();
                result.Content = item.Title;
                result.Triggers = buttonSectionTriggers;
                result.Style = buttonSectionStyle;
                result.CommandParameter = item.Address;
                if (Central.DebugMode)
                {
                    var a = item.Address;
                    result.ToolTip = $"s:{a}";
                }
                result.ItemClick += MenuSection2ClickHandler;
            }

            return result;
        }

        private DevExpress.Xpf.Bars.BarButtonItem RenderMainMenu2CreateItem(NavigationItem item)
        {
            var result = new DevExpress.Xpf.Bars.BarButtonItem();
            var menu = MainMenu2;

            var buttonItemStyle = (Style)menu.TryFindResource("buttonItemStyle");
            if (!item.Style.IsNullOrEmpty())
            {
                buttonItemStyle = (Style)menu.TryFindResource(item.Style);
            }

            var buttonItemTriggers = (BarItemTriggerCollection)menu.TryFindResource("buttonItemTriggers");
            var buttonSectionTriggers = (BarItemTriggerCollection)menu.TryFindResource("buttonSectionTriggers");

            bool show = false;
            show = CheckItemPermissions(item);
            if (show)
            {
                result = new DevExpress.Xpf.Bars.BarButtonItem();
                result.Content = item.Title;
                result.Triggers = buttonItemTriggers;
                result.Style = buttonItemStyle;
                result.CommandParameter = item.Address;
                if (Central.DebugMode)
                {
                    var a = item.Address;
                    result.ToolTip = $"i:{a}";
                }
                result.ItemClick += MenuItem2ClickHandler;
            }

            return result;
        }

        /// <summary>
        /// Отрисовка галвного меню, используется структура меню.
        /// На вход подяется контрол меню из главного окна приложения.
        /// </summary>
        public void RenderMainMenu(Menu menu, Menu menuRight)
        {
            MainMenu = menu;
            RightMenu = menuRight;

            menu.Items.Clear();
            menuRight.Items.Clear();

            var items = Central.Navigator.Items;
            var style = "MainMenuItem2";

            if (items.Count > 0)
            {
                /*
                    Простой процедурный шаблонный рендер. 
                    Если элемент имеет тип section, отрабатываем вложенные элементы.
                    Поиск вложенных ведем по координатам Left-Right относительно
                    предыдущего элемента.
                 */

                //render level1               
                var m1 = new MenuItem();
                var items1 = items;
                foreach (NavigationItem i1 in items1)
                {
                    bool show1 = false;
                    show1 = CheckItemPermissions(i1);

                    if (show1)
                    {
                        m1 = new MenuItem();
                        m1.Header = i1.Title;
                        m1.MinWidth = i1.MinWidth;
                        m1.Style = (Style)m1.TryFindResource(style);

                        if (!string.IsNullOrEmpty(i1.Style))
                        {
                            m1.Style = (Style)m1.TryFindResource(i1.Style);
                        }

                        //m1.HorizontalAlignment = HorizontalAlignment.Left;
                        //Grid.SetRow(n, rowIndex);
                        //Grid.SetColumn(m1, 0);

                        if (Central.DebugMode)
                        {
                            m1.ToolTip = i1.Address;
                        }

                        if (i1.Type == "element")
                        {
                            m1.Click += MenuItemClickHandler;
                            m1.CommandParameter = i1.Address;
                        }

                        if (i1.Align.ToLower() == "right")
                        {
                            m1.HorizontalAlignment = HorizontalAlignment.Right;
                            //Central.Dbg($"R: {m1.Header}");
                            //Grid.SetColumn(m1, 1);
                        }



                        if (i1.Align.ToLower() == "right")
                        {
                            menuRight.Items.Add(m1);
                        }
                        else
                        {
                            menu.Items.Add(m1);
                        }
                        //menu.Items.Add(m1);

                        if (i1.SubItems.Count > 0)
                        {

                            //render level2
                            var m2 = new MenuItem();
                            var items2 = i1.SubItems;
                            foreach (NavigationItem i2 in items2)
                            {
                                bool show2 = false;
                                show2 = CheckItemPermissions(i2);

                                if (show2)
                                {
                                    m2 = new MenuItem();
                                    m2.Header = i2.Title;
                                    m2.MinWidth = i2.MinWidth;
                                    m2.Style = (Style)m2.TryFindResource(style);
                                    m2.HorizontalAlignment = HorizontalAlignment.Stretch;
                                    if (Central.DebugMode)
                                    {
                                        m2.ToolTip = i2.Address;
                                    }

                                    if (i2.Type == "element")
                                    {
                                        m2.Click += MenuItemClickHandler;
                                        m2.CommandParameter = i2.Address;
                                    }

                                    if (i2.SubItems.Count > 0)
                                    {
                                        //render level3
                                        var m3 = new MenuItem();
                                        var items3 = i2.SubItems;
                                        foreach (NavigationItem i3 in items3)
                                        {
                                            bool show3 = false;
                                            show3 = CheckItemPermissions(i3);

                                            if (show3)
                                            {
                                                m3 = new MenuItem();
                                                m3.Header = i3.Title;
                                                m3.MinWidth = i3.MinWidth;
                                                m3.Style = (Style)m3.TryFindResource(style);
                                                m3.HorizontalAlignment = HorizontalAlignment.Stretch;
                                                if (Central.DebugMode)
                                                {
                                                    m3.ToolTip = i3.Address;
                                                }

                                                if (i3.Type == "element")
                                                {
                                                    m3.Click += MenuItemClickHandler;
                                                    m3.CommandParameter = i3.Address;
                                                }


                                                if (i3.SubItems.Count > 0)
                                                {
                                                    //render level4
                                                    var m4 = new MenuItem();
                                                    var items4 = i3.SubItems;
                                                    foreach (NavigationItem i4 in items4)
                                                    {
                                                        bool show4 = false;
                                                        show4 = CheckItemPermissions(i4);

                                                        if (show4)
                                                        {
                                                            m4 = new MenuItem();
                                                            m4.Header = i4.Title;
                                                            m4.MinWidth = i4.MinWidth;
                                                            m4.Style = (Style)m4.TryFindResource(style);
                                                            m4.HorizontalAlignment = HorizontalAlignment.Stretch;
                                                            if (Central.DebugMode)
                                                            {
                                                                m4.ToolTip = i4.Address;
                                                            }

                                                            if (i4.Type == "element")
                                                            {
                                                                m4.Click += MenuItemClickHandler;
                                                                m4.CommandParameter = i4.Address;
                                                            }

                                                            if (i4.SubItems.Count > 0)
                                                            {

                                                                //render level5
                                                                var m5 = new MenuItem();
                                                                var items5 = i4.SubItems;
                                                                foreach (NavigationItem i5 in items5)
                                                                {
                                                                    bool show5 = false;
                                                                    show5 = CheckItemPermissions(i5);

                                                                    if (show5)
                                                                    {
                                                                        m5 = new MenuItem();
                                                                        m5.Header = i5.Title;
                                                                        m5.MinWidth = i5.MinWidth;
                                                                        m5.Style = (Style)m5.TryFindResource(style);
                                                                        m5.HorizontalAlignment = HorizontalAlignment.Stretch;
                                                                        if (Central.DebugMode)
                                                                        {
                                                                            m5.ToolTip = i5.Address;
                                                                        }

                                                                        if (i5.Type == "element")
                                                                        {
                                                                            m5.Click += MenuItemClickHandler;
                                                                            m5.CommandParameter = i5.Address;
                                                                        }

                                                                        m4.Items.Add(m5);
                                                                    }
                                                                }

                                                            }

                                                            m3.Items.Add(m4);
                                                        }
                                                    }
                                                }

                                                m2.Items.Add(m3);
                                            }
                                        }
                                    }

                                    m1.Items.Add(m2);
                                }


                            }

                        }

                    }
                }
            }

        }

        /// <summary>
        /// выгон структуры в плоский список
        /// </summary>
        public List<Dictionary<string, string>> ExportMainMenu()
        {
            var result = new List<Dictionary<string, string>>();
            int i = 0;
            var items = Central.Navigator.Items;

            if (items.Count > 0)
            {
                //render level1               
                var m1 = new MenuItem();
                var items1 = items;
                foreach (NavigationItem i1 in items1)
                {
                    bool show1 = true;
                    //show1 = CheckItemPermissions(i1);

                    if (show1)
                    {
                        m1 = new MenuItem();
                        i++;
                        i1.Id = i;
                        result.Add(i1.GetDict());

                        if (i1.SubItems.Count > 0)
                        {

                            //render level2
                            var m2 = new MenuItem();
                            var items2 = i1.SubItems;
                            foreach (NavigationItem i2 in items2)
                            {
                                bool show2 = true;
                                //show2 = CheckItemPermissions(i2);

                                if (show2)
                                {
                                    m2 = new MenuItem();
                                    i++;
                                    i2.Id = i;
                                    result.Add(i2.GetDict());

                                    if (i2.SubItems.Count > 0)
                                    {
                                        //render level3
                                        var m3 = new MenuItem();
                                        var items3 = i2.SubItems;
                                        foreach (NavigationItem i3 in items3)
                                        {
                                            bool show3 = true;
                                            //show3 = CheckItemPermissions(i3);

                                            if (show3)
                                            {
                                                m3 = new MenuItem();
                                                i++;
                                                i3.Id = i;
                                                result.Add(i3.GetDict());

                                                if (i3.SubItems.Count > 0)
                                                {
                                                    //render level4
                                                    var m4 = new MenuItem();
                                                    var items4 = i3.SubItems;
                                                    foreach (NavigationItem i4 in items4)
                                                    {
                                                        bool show4 = true;
                                                        //show4 = CheckItemPermissions(i4);

                                                        if (show4)
                                                        {
                                                            m4 = new MenuItem();
                                                            i++;
                                                            i4.Id = i;
                                                            result.Add(i4.GetDict());

                                                            if (i4.SubItems.Count > 0)
                                                            {

                                                                //render level5
                                                                var m5 = new MenuItem();
                                                                var items5 = i4.SubItems;
                                                                foreach (NavigationItem i5 in items5)
                                                                {
                                                                    bool show5 = true;
                                                                    //show5 = CheckItemPermissions(i5);

                                                                    if (show5)
                                                                    {
                                                                        m5 = new MenuItem();
                                                                        i++;
                                                                        i5.Id = i;
                                                                        result.Add(i5.GetDict());

                                                                        if (i5.SubItems.Count > 0)
                                                                        {
                                                                        }

                                                                        m4.Items.Add(m5);
                                                                    }
                                                                }
                                                            }

                                                            m3.Items.Add(m4);
                                                        }
                                                    }
                                                }

                                                m2.Items.Add(m3);
                                            }
                                        }
                                    }

                                    m1.Items.Add(m2);
                                }
                            }
                        }

                        //menu.Items.Add(m1);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Проверка правил доступа для данного пункта меню.
        /// Проверяется на базе заложенных в структкрк на этапе формирования правил.
        /// Проверка производится для текущего пользователя.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool CheckItemPermissions(NavigationItem item)
        {
            bool result = false;
            bool resume = true;

            //if(item.Name == "change_password")
            //{
            //    var rr = 0;
            //}

            //проверка по списку ролей
            if (resume)
            {
                if (item.AllowedRoles.Count > 0)
                {
                    foreach (string r in item.AllowedRoles)
                    {
                        if (resume)
                        {
                            string role = r;
                            role = role.Trim();
                            role = role.ToLower();

                            //если одна из ролей "all" или "*", разрешаем
                            if (role == "*" || role == "all")
                            {
                                result = true;
                                resume = false;
                            }

                            //если у пользователя есть одна из требуемых ролей, разрешаем
                            if (Central.User.Roles.Count > 0)
                            {
                                foreach (KeyValuePair<string, Role> ur in Central.User.Roles)
                                {
                                    string userRole = ur.Value.Code;
                                    userRole = userRole.Trim();
                                    userRole = userRole.ToLower();

                                    if (userRole == role)
                                    {
                                        result = true;
                                        resume = false;
                                    }
                                }
                            }

                        }
                    }
                }
            }

            //проверка по списку id пользователя
            if (resume)
            {
                if (item.AllowedUsers.Count > 0)
                {
                    foreach (int id in item.AllowedUsers)
                    {
                        if (resume)
                        {
                            if (Central.User.AccountId == id)
                            {
                                result = true;
                                resume = false;
                            }
                        }
                    }
                }
            }

            //проверка по списку login пользователя
            if (resume)
            {
                if (item.AllowedLogins.Count > 0)
                {
                    foreach (string login in item.AllowedLogins)
                    {
                        if (resume)
                        {
                            if (Central.User.Login == login)
                            {
                                result = true;
                                resume = false;
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Обработчик нажатия пункта меню.
        /// При клике мышью на пункте меню вызывается общий обработчик.
        /// Этот обработчик извлекает адрес пункта меню и запускает главный обработчик.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItemClickHandler(object sender, RoutedEventArgs e)
        {
            var m = (MenuItem)sender;
            var p = m.CommandParameter.ToString();
            Central.Navigator.HandleAction(p);
        }

        private void MenuItem2ClickHandler(object sender, RoutedEventArgs e)
        {
            var m = (DevExpress.Xpf.Bars.BarButtonItem)sender;
            var p = m.CommandParameter.ToString();
            Central.Navigator.HandleAction(p);
        }

        private void MenuSection2ClickHandler(object sender, RoutedEventArgs e)
        {
            var m = (DevExpress.Xpf.Bars.BarSubItem)sender;
            var p = m.CommandParameter.ToString();
            Central.Navigator.HandleAction(p);
        }

        /// <summary>
        /// Поиск элемента в структуре меню по его полному адресу
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public NavigationItem FindItemByAddress(string address = "")
        {
            NavigationItem result = null;

            if (!string.IsNullOrEmpty(address))
            {
                if (Items.Count > 0)
                {
                    //proc level1               
                    var items1 = Items;
                    foreach (NavigationItem i1 in items1)
                    {
                        if (i1.Address == address)
                        {
                            result = i1;
                        }

                        if (i1.SubItems.Count > 0)
                        {
                            //proc level2
                            var items2 = i1.SubItems;
                            foreach (NavigationItem i2 in items2)
                            {
                                if (i2.Address == address)
                                {
                                    result = i2;
                                }

                                if (i2.SubItems.Count > 0)
                                {
                                    //proc level3
                                    var items3 = i2.SubItems;
                                    foreach (NavigationItem i3 in items3)
                                    {
                                        if (i3.Address == address)
                                        {
                                            result = i3;
                                        }

                                        if (i3.SubItems.Count > 0)
                                        {
                                            //proc level4
                                            var items4 = i3.SubItems;
                                            foreach (NavigationItem i4 in items4)
                                            {
                                                if (i4.Address == address)
                                                {
                                                    result = i4;
                                                }

                                                if (i4.SubItems.Count > 0)
                                                {

                                                    //proc level5
                                                    var items5 = i4.SubItems;
                                                    foreach (NavigationItem i5 in items5)
                                                    {
                                                        if (i5.Address == address)
                                                        {
                                                            result = i5;
                                                        }

                                                        if (i5.SubItems.Count > 0)
                                                        {

                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Ищет и возвращает объект меню по его адресу
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public MenuItem GetMenuItemByAddress(string address = "", string menu = "main")
        {
            var result = new MenuItem();

            if (!string.IsNullOrEmpty(address))
            {
                var mn = MainMenu;
                if (menu == "right")
                {
                    mn = RightMenu;
                }

                if (mn != null)
                {
                    if (mn.Items.Count > 0)
                    {
                        var menuItems = mn.Items.Cast<MenuItem>().ToArray();
                        foreach (var m in menuItems)
                        {
                            if (m.CommandParameter != null)
                            {
                                string addr = m.CommandParameter.ToString();
                                if (addr == address)
                                {
                                    result = m;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        public void UpdateUserItem()
        {
            //string serverLabel = Central.GetServerLabel();
            var userMenuLabel = "";
            {
                var userName = $"{Central.User.Surname} {Central.User.Name} {Central.User.MiddleName}";
                userName = userName.SurnameInitials();
                userMenuLabel = userName;
            }


            //var memoryUsedMb = Central.GetUsedMemory();
            var tooltip = "";
            tooltip = tooltip.Append($"SERVER: {Central.LPackClient.CurrentConnection.Host}", true);
            tooltip = tooltip.Append($"HOST: {Central.SystemInfo.CheckGet("SYSTEM_HOSTNAME")}", true);
            tooltip = tooltip.Append($"USER: {Central.SystemInfo.CheckGet("SYSTEM_USER_NAME2")}", true);
            tooltip = tooltip.Append($"VERSION: {Central.SystemInfo.CheckGet("VERSION")}", true);
            tooltip = tooltip.Append($"SCREEN: {Central.SystemInfo.CheckGet("SYSTEM_SCREEN_RESOLUTION")}", true);

            tooltip = tooltip.Trim();

            foreach (DevExpress.Xpf.Bars.BarSubItem item in RightMenu2.Items)
            {
                var p = item.CommandParameter.ToString();
                if (p == "/user")
                {
                    item.Content = $"{userMenuLabel}";
                    item.ToolTip = tooltip;
                }
            }
        }

        public void UpdateNoteItem(string content = "", string tooltip = "")
        {
            foreach (DevExpress.Xpf.Bars.BarSubItem item in RightMenu2.Items)
            {
                var p = item.CommandParameter.ToString();
                if (p == "/notes")
                {
                    item.Content = content;
                    item.ToolTip = tooltip;
                }
            }
        }

        public void UpdateNoteItemActivity(bool active = false)
        {
            var menu = MainMenu2;
            var styleName = "buttonSectionNoteStyle";

            if (active)
            {
                styleName = "buttonSectionNoteActiveStyle";
            }
            else
            {
                styleName = "buttonSectionNoteStyle";
            }

            var buttonSectionStyle = (Style)menu.TryFindResource(styleName);

            foreach (DevExpress.Xpf.Bars.BarSubItem item in RightMenu2.Items)
            {
                var p = item.CommandParameter.ToString();
                if (p == "/notes")
                {
                    item.Style = buttonSectionStyle;
                }
            }
        }

        /// <summary>
        /// Главный обработчик меню.
        /// Он находит нужный пункт меню в структуре меню по адресу и выполняет его
        /// рабочую функцию (задается в процессе формирования в виде виртуальных функций)
        /// </summary>
        /// <param name="address"></param>
        public void HandleAction(string address = "")
        {
            if (string.IsNullOrEmpty(address))
            {
                return;
            }

            Central.Dbg($"Navigator: Menu: [{address}]");

            var i = FindItemByAddress(address);
            if (i != null)
            {
                i.Action?.Execute("");
            }
        }

        /// <summary>
        /// Обработка адреса.
        /// Производится парсинг адреса и вызывается соответствующий ему интерфейс.
        /// Далее вызывается внутренний обработчик интерфейса, для отработки некоторых
        /// параметров запроса
        /// </summary>
        /// <param name="url"></param>
        public void ProcessURL(string url = "")
        {
            /*
                Обработка Url производится в две фазы:
                -- предварительная обработка (здесь): находится нужный интерфейс и инициализируется
                -- окончательная обработка (внутри целевого интерфейса)
            
                Входящий Url парсится в структуру NavigationAddress.
                По схеме определяем дальнейшее поведение.

                Для схемы file открываем файл, путь к которому записан
                в переменной AddressRaw (если не пустой)
                
                Для схемы action выполняется вызов экшена на сервере для получения файла.
                Значения Module, Object, Action должны сожержаться в url
                Например:
                    action://Module/Object/Action/?PARAM1=value1&PARAM2=value2
                Будет вызван экшен, в который будут переданы параметры PARAM1 и PARAM2
                В ответе от сервера система ожидает получить файл, который сразу будет открыт

                Для схемы l-pack производится попытка найти интерфейс с указанным адресом.
                    /service/task_control/tasks
                Если адрес не находится, от него (адреса) отрезается слово справа
                и производится повторная попытка.
                Отрезанные слова складываются в специальный стек и будут затем
                обрабатываться внутренним обработчиком навигации интерфейса.

                Например, для адреса:
                    l-pack://l-pack_erp/service/tasks_appeals_ctl/tasks?id=1142&a=123
                
                Будет найден ближайший адрес интерфейса:
                    /service/tasks_appeals_ctl
                Слово "tasks" будет отрезано и попадет в стек параметров адреса.
                Итоговая структура будет выглядеть так:

                    Schema      ="l-pack"
                    Programm    ="pack_erp"
                    Address     ="/service/tasks_appeals_ctl";
                    AddressInner={
                        "tasks"
                    }
                    AddressRaw  ="/service/tasks_appeals_ctl/tasks"
                    Params      ={
                        "id"=>"1142",
                        "a"=>"123",
                    }
                    Anchor      =""

                    Address      -- адрес, которому найдено соответствие
                    AddressRaw   -- сырой адрес, содержащий все
                    AddressInner -- параметры для обработки во внутреннем обработчике

                    (1) Будет найден интерфейс "Управление задачами и обращениями" (Address)
                    (2) В нем будет открыта вкладка "Задачи" (AddressInner)
                    (3) Далее будет загружена форма редактирования задачи с задачей 1142

                    AddressRaw содержит и собственно адрес и параметрическую часть:
                    подраздел, подподраздел, если нужно. Чтобы корректно привести нашу систему
                    в соответствие с общепринятым представлением об адресной части URL (RFC),
                    мы примешиваем к адресу дополнительные идентификаторы справа при необходимости.

                    Эти части адреса обрабатываются логикой целевого интерфейса.
                    (поскольку, нет никакого представления, что с этим делать снаружи,
                    только интерфейс в курсе, ка это отработать). В этом суть роутинга.

             */

            bool result = false;

            Central.Dbg($"Navigator: ProcessURL: [{url}]");

            Address = new NavigationAddress();

            //взводим флажок, обращаем внимание на эту структуру, только если флажок взведен
            //по окончанию навигации, флажок опустим
            Address.Processed = false;

            NavigationItem item = null;

            //парсим адрес в удобную нам структуру
            var parseResult = Address.Parse(url);

            if (parseResult)
            {
                string a = Address.AddressRaw;
                bool resume = true;
                int maxIterations = 10;
                int iterationCounter = 0;
                if (Address.Schema == "file")
                {
                    Central.OpenFile(Address.AddressRaw);
                    Address.Processed = true;
                }
                else if (Address.Schema == "action")
                {
                    var data = new Dictionary<string, string>();
                    string[] addr = Address.AddressRaw.Split('/');
                    //addr должна содержать Module, Object, Action
                    if (addr.Length > 2)
                    {
                        data.Add("Module", addr[0]);
                        data.Add("Object", addr[1]);
                        data.Add("Action", addr[2]);

                        if (Address.Params.Count > 0)
                        {
                            foreach (var param in Address.Params)
                            {
                                data.Add(param.Key.ToUpper(), param.Value);
                            }
                        }

                        Central.GetFileFromAction(data);
                        Address.Processed = true;
                    }
                    else
                    {
                        Central.Dbg($"    Not enough elements for action");
                    }
                }
                else if (Address.Schema == "l-pack")
                {
                    //пытаемся найти элемент по адресу
                    //если не находим, отрезаем слово справа и повторяем попытку
                    while (resume)
                    {
                        //Central.Dbg($"    Finding [{a}]");
                        item = FindItemByAddress(a);

                        if (item != null)
                        {
                            //Central.Dbg($"    Found [{item.Name}]");
                            result = true;
                            resume = false;
                        }
                        else
                        {
                            var lost = "";
                            var pos = a.LastIndexOf("/");
                            if (pos > -1)
                            {
                                // /service/task_control/tasks
                                // >...................< >...<
                                lost = a.Substring((pos + 1), (a.Length - (pos + 1)));
                                a = a.Substring(0, pos);
                                if (!string.IsNullOrEmpty(lost))
                                {
                                    Address.AddressInner.Add(lost);
                                }
                                //Central.Dbg($"    Lost=[{lost}]");
                            }
                        }

                        if (iterationCounter >= maxIterations)
                        {
                            resume = false;
                        }
                        iterationCounter++;
                    }

                    if (Address.AddressInner.Count > 0)
                    {
                        // /service/task_control/tasks/new
                        // >...................< >...< >.<
                        //после обработки слова tasks, new попадут в стек параметров
                        //нам нужно, чтобы они шли слева направо (а отрезали мы их спарва налево)
                        Address.AddressInner.Reverse();
                    }

                    //если элемент найден, запускаем его обработчик
                    if (item != null)
                    {
                        Central.Dbg($"    Element:{item.Name}");

                        if (CheckItemPermissions(item))
                        {
                            item.Action?.Execute("");
                            Address.Processed = true;
                        }
                        else
                        {
                            var message = "";
                            message += $"Доступ запрещен.";

                            var description = $"";
                            description += $"\nУ вас нет разрешения для доступа к разделу:";
                            description += $"\n{item.Title}";
                            description += $"\n";
                            description += $"\nДополнительные сведения:";
                            description += $"\nname=[{item.Name}]";

                            var dialogWindow = new DialogWindow(message, "Проверка доступа", description);
                            dialogWindow.ShowDialog();

                            var r0 = Central.User;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// поиск роли у пользователя и возврат уровня доступа
        /// </summary>
        /// <returns></returns>
        public Role.AccessMode GetRoleLevel(string roleCode = "")
        {
            var result = Role.AccessMode.None;

            if (RoleLevelTest != Role.AccessMode.None)
            {
                result = RoleLevelTest;
            }
            else
            {
                if (!roleCode.IsNullOrEmpty())
                {
                    roleCode = roleCode.Trim();
                    if (Central.User != null)
                    {
                        if (Central.User.Roles.Count > 0)
                        {
                            if (Central.User.Roles.ContainsKey(roleCode))
                            {
                                var role = Central.User.Roles[roleCode];
                                switch (role.Mode)
                                {
                                    case 0:
                                        result = Role.AccessMode.Deny;
                                        break;

                                    case 1:
                                        result = Role.AccessMode.ReadOnly;
                                        break;

                                    case 2:
                                        result = Role.AccessMode.FullAccess;
                                        break;

                                    case 3:
                                        result = Role.AccessMode.Special;
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// изменение уровня доступа для текущего пользователя
        /// </summary>
        /// <param name="mode"></param>
        private void SetRoleLevelTest(AccessMode mode)
        {
            RoleLevelTest = mode;
            RoleLevelUpdate(mode.ToString());
            Central.Msg.SendMessage(new ItemMessage()
            {
                ReceiverGroup = "",
                ReceiverName = "Commander",
                SenderName = "Navigator",
                Action = "SetRoleLevelTest",
                Message = mode.ToString(),
            });
        }

        private void RoleLevelUpdate(string name = "")
        {
            foreach (DevExpress.Xpf.Bars.BarSubItem item in RightMenu2.Items)
            {
                var p = item.CommandParameter.ToString();
                if (
                    p == "/user"
                )
                {
                    foreach (var item2 in item.Items)
                    {
                        if (item2.GetType() == typeof(DevExpress.Xpf.Bars.BarSubItem))
                        {
                            var item2a = (DevExpress.Xpf.Bars.BarSubItem)item2;
                            var p2 = item2a.CommandParameter.ToString();
                            if (
                                p2 == "/user/access_level"
                            )
                            {
                                foreach (var item3 in item2a.Items)
                                {
                                    if (item3.GetType() == typeof(DevExpress.Xpf.Bars.BarButtonItem))
                                    {
                                        var item3a = (DevExpress.Xpf.Bars.BarButtonItem)item3;
                                        var p3 = item3a.CommandParameter.ToString();
                                        if (
                                            p3 == "/user/access_level/none"
                                            || p3 == "/user/access_level/deny"
                                            || p3 == "/user/access_level/readonly"
                                            || p3 == "/user/access_level/allowall"
                                            || p3 == "/user/access_level/special"
                                        )
                                        {
                                            {
                                                var m = item3a;
                                                var n = p3.Replace("/user/access_level/", "");
                                                //var l = " ";

                                                if (name.IsNullOrEmpty())
                                                {
                                                    name = "none";
                                                }
                                                else
                                                {
                                                    name = name.ToLower();
                                                }

                                                var active = false;
                                                if (n == name)
                                                {
                                                    active = true;
                                                }

                                                if (active)
                                                {
                                                    var uri = new Uri("pack://application:,,,/Assets/Icons/appbar.control.play.png");
                                                    var bitmap = new BitmapImage(uri);
                                                    m.Glyph = bitmap;
                                                }
                                                else
                                                {
                                                    m.Glyph = null;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
