using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.Enum.Apartment;
using AptCare.Repository.Enum.TransactionEnum;
using Microsoft.EntityFrameworkCore;

namespace AptCare.Repository.Seeds
{
    public static class RepairRequestSeed
    {
        #region Scenario Definitions

        private enum RepairScenario
        {
            Completed_BuildingFault_Free_FromStock,
            Completed_ResidentFault_Chargeable_FromStock,
            Completed_BuildingFault_Free_PurchaseNew,
            Completed_ResidentFault_Chargeable_Mixed,
            Completed_ExternalContractor_BuildingFault,
            Completed_ExternalContractor_ResidentFault,
            InProgress_WithDraftInvoice,
            InProgress_WithApprovedInvoice,
            InProgress_NoInvoice,
            Pending_New,
            Approved_WaitingStart,
            WaitingManagerApproval,
            AcceptancePending_InvoicePaid,
            AcceptancePending_AwaitingPayment,
            Cancelled_ByResident,
            Cancelled_ByTechLead
        }

        private class ScenarioConfig
        {
            public RepairScenario Scenario { get; set; }
            public DateTime BaseDate { get; set; }
            public int Count { get; set; }
            public RequestStatus FinalStatus { get; set; }
            public FaultType? FaultType { get; set; }
            public SolutionType? SolutionType { get; set; }
            public InvoiceType? InvoiceType { get; set; }
            public InvoiceStatus? InvoiceStatus { get; set; }
            public bool IsChargeable { get; set; }
            public bool UseFromStock { get; set; }
            public bool UsePurchaseNew { get; set; }
            public bool HasInvoice { get; set; }
            public bool HasStockTransaction { get; set; }
            public string Description { get; set; } = string.Empty;
        }

        private class CounterContext
        {
            public int RequestId { get; set; } = 1;
            public int AppointmentId { get; set; } = 1;
        }

        #endregion

        public static async Task SeedAsync(AptCareSystemDBContext context)
        {
            if (context.RepairRequests.Any()) return;

            var seedData = await LoadSeedDataAsync(context);
            if (!seedData.IsValid) return;

            var scenarios = BuildScenarioConfigs();
            var allData = new SeedCollections();
            var counter = new CounterContext();

            foreach (var scenario in scenarios)
            {
                for (int i = 0; i < scenario.Count; i++)
                {
                    var createdAt = scenario.BaseDate.AddDays(i).AddHours(8 + (i % 8));

                    if (IsBlockedDate(createdAt))
                    {
                        continue;
                    }

                    CreateRepairRequestByScenario(
                        scenario,
                        seedData,
                        allData,
                        counter,
                        createdAt,
                        i
                    );
                }
            }

            await SaveAllDataAsync(context, allData);
        }

        #region Scenario Configurations

        private static List<ScenarioConfig> BuildScenarioConfigs()
        {
            var today = DateTime.Now;
            var configs = new List<ScenarioConfig>();

            // DU LIEU LICH SU - Chi tao cho cac thang da qua
            if (today.Year >= 2025 && today.Month >= 1)
            {
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 1, 5),
                    Count = 5,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sua chua noi bo thang 1 - Loi toa nha"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_FromStock,
                    BaseDate = new DateTime(2025, 1, 12),
                    Count = 3,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sua chua tinh phi thang 1 - Loi cu dan"
                });
            }

            if (today.Year >= 2025 && today.Month >= 2)
            {
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 2, 3),
                    Count = 4,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sua chua noi bo thang 2 - Loi toa nha"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_FromStock,
                    BaseDate = new DateTime(2025, 2, 10),
                    Count = 3,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sua chua tinh phi thang 2 - Loi cu dan"
                });
            }

            if (today.Year >= 2025 && today.Month >= 3)
            {
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 3, 1),
                    Count = 5,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sua chua noi bo thang 3 - Loi toa nha"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_Mixed,
                    BaseDate = new DateTime(2025, 3, 10),
                    Count = 2,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    UseFromStock = true,
                    UsePurchaseNew = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Ket hop vat tu kho va mua moi thang 3"
                });
            }

            if (today.Year >= 2025 && today.Month >= 4)
            {
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 4, 2),
                    Count = 4,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sua chua noi bo thang 4 - Loi toa nha"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_FromStock,
                    BaseDate = new DateTime(2025, 4, 10),
                    Count = 3,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sua chua tinh phi thang 4 - Loi cu dan"
                });
            }

            if (today.Year >= 2025 && today.Month >= 5)
            {
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 5, 1),
                    Count = 5,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sua chua noi bo thang 5 - Loi toa nha"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_FromStock,
                    BaseDate = new DateTime(2025, 5, 10),
                    Count = 4,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sua chua tinh phi thang 5 - Loi cu dan"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ExternalContractor_ResidentFault,
                    BaseDate = new DateTime(2025, 5, 20),
                    Count = 2,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Outsource,
                    InvoiceType = Enum.InvoiceType.ExternalContractor,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    HasInvoice = true,
                    HasStockTransaction = false,
                    Description = "Thue ngoai thang 5 - Loi cu dan"
                });
            }

            if (today.Year >= 2025 && today.Month >= 6)
            {
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 6, 2),
                    Count = 5,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sua chua noi bo thang 6 - Loi toa nha"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_Mixed,
                    BaseDate = new DateTime(2025, 6, 10),
                    Count = 3,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    UseFromStock = true,
                    UsePurchaseNew = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Ket hop vat tu kho va mua moi thang 6"
                });
            }

            if (today.Year >= 2025 && today.Month >= 7)
            {
                var daysInMonth = DateTime.DaysInMonth(2025, 7);
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 7, 1),
                    Count = daysInMonth >= 5 ? 5 : daysInMonth,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sua chua noi bo thang 7 - Loi toa nha"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_FromStock,
                    BaseDate = new DateTime(2025, 7, 10),
                    Count = daysInMonth >= 10 ? 5 : daysInMonth - 5,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sua chua tinh phi thang 7 - Loi cu dan"
                });
            }

            if (today.Year >= 2025 && today.Month >= 8)
            {
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 8, 1),
                    Count = 5,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sua chua noi bo thang 8 - Loi toa nha"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_FromStock,
                    BaseDate = new DateTime(2025, 8, 10),
                    Count = 4,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sua chua tinh phi thang 8 - Loi cu dan"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ExternalContractor_BuildingFault,
                    BaseDate = new DateTime(2025, 8, 20),
                    Count = 2,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Outsource,
                    InvoiceType = Enum.InvoiceType.ExternalContractor,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    HasInvoice = true,
                    HasStockTransaction = false,
                    Description = "Thue ngoai thang 8 - Loi toa nha"
                });
            }

            if (today.Year >= 2025 && today.Month >= 9)
            {
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 9, 2),
                    Count = 5,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sua chua noi bo thang 9 - Loi toa nha"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_Mixed,
                    BaseDate = new DateTime(2025, 9, 10),
                    Count = 3,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    UseFromStock = true,
                    UsePurchaseNew = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Ket hop vat tu kho va mua moi thang 9"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ExternalContractor_ResidentFault,
                    BaseDate = new DateTime(2025, 9, 18),
                    Count = 2,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Outsource,
                    InvoiceType = Enum.InvoiceType.ExternalContractor,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    HasInvoice = true,
                    HasStockTransaction = false,
                    Description = "Thue ngoai thang 9 - Loi cu dan"
                });
            }

            if (today.Year >= 2025 && today.Month >= 10)
            {
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 10, 1),
                    Count = 5,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sua chua noi bo thang 10 - Loi toa nha"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_FromStock,
                    BaseDate = new DateTime(2025, 10, 10),
                    Count = 4,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sua chua tinh phi thang 10 - Loi cu dan"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_PurchaseNew,
                    BaseDate = new DateTime(2025, 10, 20),
                    Count = 2,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Replacement,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UsePurchaseNew = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Thay the thiet bi moi thang 10 - Loi toa nha"
                });
            }

            if (today.Year >= 2025 && today.Month >= 11)
            {
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 11, 1),
                    Count = 5,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sua chua noi bo thang 11 - Loi toa nha"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_Mixed,
                    BaseDate = new DateTime(2025, 11, 10),
                    Count = 3,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    UseFromStock = true,
                    UsePurchaseNew = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Ket hop vat tu kho va mua moi thang 11"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ExternalContractor_BuildingFault,
                    BaseDate = new DateTime(2025, 11, 18),
                    Count = 2,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Outsource,
                    InvoiceType = Enum.InvoiceType.ExternalContractor,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    HasInvoice = true,
                    HasStockTransaction = false,
                    Description = "Thue ngoai thang 11 - Loi toa nha"
                });
            }

            // DU LIEU HIEN TAI - Cac trang thai dang xu ly
            // Chi tao cac yeu cau co lich hen trong tuong lai hoac gan day
            if (today.Year >= 2025 && today.Month >= 7)
            {
                // Cac yeu cau da hoan thanh gan day (lich hen da qua)
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(today.Year, today.Month, 1),
                    Count = 3,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Hoan thanh dau thang - Loi toa nha"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_FromStock,
                    BaseDate = new DateTime(today.Year, today.Month, 5),
                    Count = 2,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Hoan thanh giua thang - Loi cu dan"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(today.Year, today.Month, 10),
                    Count = 2,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Hoan thanh giua thang - Loi toa nha"
                });

                // Cac yeu cau cho nghiem thu (lich hen da qua, da suas xong, cho cu dan xac nhan)
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.AcceptancePending_InvoicePaid,
                    BaseDate = new DateTime(today.Year, today.Month, 15),
                    Count = 2,
                    FinalStatus = RequestStatus.AcceptancePendingVerify,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Cho nghiem thu - Hoa don da duyet"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.AcceptancePending_AwaitingPayment,
                    BaseDate = new DateTime(today.Year, today.Month, 17),
                    Count = 2,
                    FinalStatus = RequestStatus.AcceptancePendingVerify,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.AwaitingPayment,
                    IsChargeable = true,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Cho nghiem thu - Cho thanh toan"
                });

                // Cac yeu cau dang xu ly (lich hen la hom nay hoac hom qua)
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.InProgress_WithApprovedInvoice,
                    BaseDate = new DateTime(today.Year, today.Month, 20),
                    Count = 2,
                    FinalStatus = RequestStatus.InProgress,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Dang xu ly - Hoa don da duyet"
                });
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.InProgress_WithDraftInvoice,
                    BaseDate = new DateTime(today.Year, today.Month, 20),
                    Count = 2,
                    FinalStatus = RequestStatus.InProgress,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Draft,
                    IsChargeable = true,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = false,
                    Description = "Dang xu ly - Hoa don nhap"
                });

                // Cac yeu cau da duyet, cho bat dau - chi tao 1 yeu cau ngay 22
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.Approved_WaitingStart,
                    BaseDate = new DateTime(today.Year, today.Month, 19),
                    Count = 3,
                    FinalStatus = RequestStatus.Approved,
                    HasInvoice = false,
                    Description = "Da duyet - Cho bat dau"
                });
                
                // Cac yeu cau cho Manager phe duyet - chi tao 1 yeu cau ngay 22
                configs.Add(new ScenarioConfig
                {
                    Scenario = RepairScenario.WaitingManagerApproval,
                    BaseDate = new DateTime(today.Year, today.Month, 22),
                    Count = 1,
                    FinalStatus = RequestStatus.WaitingManagerApproval,
                    HasInvoice = false,
                    Description = "Cho Manager phe duyet"
                });
            }

            return configs;
        }

        #endregion

        #region Data Loading

        private class SeedDataContext
        {
            public List<UserApartment> UserApartments { get; set; } = new();
            public List<Issue> Issues { get; set; } = new();
            public List<User> Technicians { get; set; } = new();
            public User? TechLeadUser { get; set; }
            public User? ManagerUser { get; set; }
            public List<Accessory> Accessories { get; set; } = new();
            public bool IsValid => UserApartments.Any() && Issues.Any() && Technicians.Any() && TechLeadUser != null;
        }

        private static async Task<SeedDataContext> LoadSeedDataAsync(AptCareSystemDBContext context)
        {
            return new SeedDataContext
            {
                UserApartments = await context.UserApartments
                    .Include(ua => ua.User)
                    .Include(ua => ua.Apartment)
                    .Where(ua => ua.RoleInApartment == RoleInApartmentType.Owner && ua.Status == ActiveStatus.Active)
                    .Take(50)
                    .ToListAsync(),

                Issues = await context.Issues
                    .Where(i => i.Status == ActiveStatus.Active && !i.IsEmergency)
                    .ToListAsync(),

                Technicians = await context.Users
                    .Include(u => u.Account)
                    .Where(u => u.Account.Role == AccountRole.Technician)
                    .ToListAsync(),

                TechLeadUser = await context.Users
                    .Include(u => u.Account)
                    .FirstOrDefaultAsync(u => u.Account.Role == AccountRole.TechnicianLead),

                ManagerUser = await context.Users
                    .Include(u => u.Account)
                    .FirstOrDefaultAsync(u => u.Account.Role == AccountRole.Manager),

                Accessories = await context.Accessories.Where(a => a.Status == ActiveStatus.Active).ToListAsync()
            };
        }

        #endregion

        #region Data Collections

        private class SeedCollections
        {
            public List<RepairRequest> RepairRequests { get; } = new();
            public List<RequestTracking> RequestTrackings { get; } = new();
            public List<Appointment> Appointments { get; } = new();
            public List<AppointmentTracking> AppointmentTrackings { get; } = new();
            public List<AppointmentAssign> AppointmentAssigns { get; } = new();
            public List<InspectionReport> InspectionReports { get; } = new();
            public List<RepairReport> RepairReports { get; } = new();
            public List<ReportApproval> ReportApprovals { get; } = new();
            public List<Invoice> Invoices { get; } = new();
            public List<InvoiceAccessory> InvoiceAccessories { get; } = new();
            public List<InvoiceService> InvoiceServices { get; } = new();
            public List<AccessoryStockTransaction> StockTransactions { get; } = new();
            public List<Transaction> Transactions { get; } = new();
            public List<Feedback> Feedbacks { get; } = new();
        }

        #endregion

        #region Scenario Processing

        private static void CreateRepairRequestByScenario(
            ScenarioConfig config,
            SeedDataContext seedData,
            SeedCollections data,
            CounterContext counter,
            DateTime createdAt,
            int index)
        {
            var userApartment = seedData.UserApartments[index % seedData.UserApartments.Count];
            var issue = seedData.Issues[index % seedData.Issues.Count];
            int currentRequestId = counter.RequestId;

            var request = new RepairRequest
            {
                UserId = userApartment.UserId,
                ApartmentId = userApartment.ApartmentId,
                IssueId = issue.IssueId,
                Object = issue.Name,
                Description = $"[{config.Scenario}] {config.Description}. Can ho {userApartment.Apartment.Room}.",
                IsEmergency = false,
                CreatedAt = createdAt
            };
            data.RepairRequests.Add(request);

            var trackings = GenerateRequestTrackings(
                currentRequestId,
                config.FinalStatus,
                createdAt,
                userApartment.UserId,
                seedData.TechLeadUser!.UserId,
                seedData.ManagerUser?.UserId ?? seedData.TechLeadUser.UserId
            );
            data.RequestTrackings.AddRange(trackings);

            if (config.FinalStatus != RequestStatus.Pending)
            {
                // Tinh toan ngay hen - tranh ngay 24 de khong anh huong demo
                var appointmentStart = CalculateAppointmentStartDate(createdAt, issue.EstimatedDuration);
                var appointmentEnd = appointmentStart.AddHours(issue.EstimatedDuration);
                int currentAppointmentId = counter.AppointmentId;

                var appointment = new Appointment
                {
                    RepairRequestId = currentRequestId,
                    StartTime = appointmentStart,
                    EndTime = appointmentEnd,
                    Note = $"Lich hen: {config.Description}",
                    CreatedAt = createdAt.AddMinutes(30)
                };
                data.Appointments.Add(appointment);

                // AppointmentTrackings
                var apptTrackings = GenerateAppointmentTrackings(
                    currentAppointmentId,
                    config.FinalStatus,
                    createdAt,
                    userApartment.UserId,
                    seedData.TechLeadUser.UserId
                );
                data.AppointmentTrackings.AddRange(apptTrackings);

                // AppointmentAssign - Khong phan cong neu lich hen vao ngay 24
                User? assignedTechnician = null;
                bool shouldAssignTechnician = config.FinalStatus != RequestStatus.Cancelled &&
                    config.FinalStatus != RequestStatus.WaitingManagerApproval &&
                    !IsBlockedAppointmentDate(appointmentStart);

                if (shouldAssignTechnician)
                {
                    var techIndex = index % seedData.Technicians.Count;
                    var assignedTechs = seedData.Technicians
                        .Skip(techIndex)
                        .Take(issue.RequiredTechnician)
                        .ToList();

                    assignedTechnician = assignedTechs.FirstOrDefault();

                    foreach (var tech in assignedTechs)
                    {
                        var workOrderStatus = config.FinalStatus switch
                        {
                            RequestStatus.Completed => WorkOrderStatus.Completed,
                            RequestStatus.InProgress => WorkOrderStatus.Working,
                            RequestStatus.AcceptancePendingVerify => WorkOrderStatus.Completed,
                            _ => WorkOrderStatus.Pending
                        };

                        data.AppointmentAssigns.Add(new AppointmentAssign
                        {
                            TechnicianId = tech.UserId,
                            AppointmentId = currentAppointmentId,
                            AssignedAt = createdAt.AddHours(1),
                            EstimatedStartTime = appointmentStart,
                            EstimatedEndTime = appointmentEnd,
                            ActualStartTime = config.FinalStatus >= RequestStatus.InProgress ? appointmentStart : null,
                            ActualEndTime = config.FinalStatus >= RequestStatus.AcceptancePendingVerify ? appointmentEnd : null,
                            Status = workOrderStatus
                        });
                    }
                }

                if (config.FinalStatus >= RequestStatus.InProgress &&
                    config.FinalStatus != RequestStatus.Cancelled &&
                    assignedTechnician != null &&
                    config.FaultType.HasValue &&
                    config.SolutionType.HasValue)
                {
                    var baseTime = appointmentStart.AddHours(1);
                    var invoiceCreatedAt = baseTime;
                    var inspectionCreatedAt = baseTime.AddSeconds(2);

                    if (config.HasInvoice && config.InvoiceType.HasValue && config.InvoiceStatus.HasValue)
                    {
                        CreateInvoiceBeforeInspection(
                            config,
                            data,
                            seedData,
                            currentRequestId,
                            userApartment.UserId,
                            seedData.TechLeadUser.UserId,
                            issue.Name,
                            invoiceCreatedAt,
                            appointmentEnd,
                            index
                        );
                    }

                    CreateInspectionReport(
                        config,
                        data,
                        currentAppointmentId,
                        assignedTechnician.UserId,
                        seedData.TechLeadUser.UserId,
                        seedData.ManagerUser?.UserId ?? seedData.TechLeadUser.UserId,
                        issue.Name,
                        inspectionCreatedAt
                    );
                }

                if (config.FinalStatus == RequestStatus.Completed ||
                    config.FinalStatus == RequestStatus.AcceptancePendingVerify)
                {
                    if (assignedTechnician != null)
                    {
                        CreateRepairReport(
                            config,
                            data,
                            currentAppointmentId,
                            assignedTechnician.UserId,
                            seedData.TechLeadUser.UserId,
                            userApartment.UserId,
                            issue.Name,
                            appointmentEnd
                        );
                    }
                }
                if (config.FinalStatus == RequestStatus.Completed)
                {
                    request.AcceptanceTime = DateOnly.FromDateTime(appointmentEnd.AddDays(1));

                    data.Feedbacks.Add(new Feedback
                    {
                        RepairRequestId = currentRequestId,
                        UserId = userApartment.UserId,
                        Rating = 4 + (index % 2),
                        Comment = GetFeedbackByScenario(config),
                        CreatedAt = appointmentEnd.AddDays(1)
                    });
                }

                counter.AppointmentId++;
            }

            counter.RequestId++;
        }

        /// <summary>
        /// Tinh toan ngay bat dau lich hen
        /// Dam bao lich hen khong roi vao ngay 24 tro di
        /// </summary>
        private static DateTime CalculateAppointmentStartDate(DateTime createdAt, double estimatedDuration)
        {
            var today = DateTime.Now;
            var proposedDate = createdAt.AddDays(1).Date.AddHours(9);

            // Neu lich hen roi vao ngay 24 tro di trong thang hien tai, dat lai ngay 23
            if (proposedDate.Month == today.Month && proposedDate.Year == today.Year && proposedDate.Day >= 24)
            {
                proposedDate = new DateTime(today.Year, today.Month, 23, 9, 0, 0);
            }

            return proposedDate;
        }
        /// <summary>
        /// Kiem tra xem ngay co bi chan khong
        /// Chi tao yeu cau cho ngay 23 va truoc do
        /// Khong tao cho ngay 24 tro di (ngay hien tai va tuong lai)
        /// </summary>
        private static bool IsBlockedDate(DateTime date)
        {
            var today = DateTime.Now.Date;
            // Chi tao yeu cau cho ngay 23 va truoc do
            // Khong tao cho ngay 24 tro di (ngay hien tai va tuong lai)
            return date.Date >= today;
        }

        /// <summary>
        /// Kiem tra xem ngay lich hen co bi chan khong
        /// Khong tao lich hen vao ngay 24 tro di
        /// </summary>
        private static bool IsBlockedAppointmentDate(DateTime date)
        {
            var today = DateTime.Now.Date;
            // Khong tao lich hen vao ngay 24 tro di
            return date.Date >= today;
        }

        #endregion

        #region Invoice Creation

        private static void CreateInvoiceBeforeInspection(
            ScenarioConfig config,
            SeedCollections data,
            SeedDataContext seedData,
            int repairRequestId,
            int residentUserId,
            int techLeadUserId,
            string issueName,
            DateTime invoiceCreatedAt,
            DateTime appointmentEnd,
            int index)
        {
            decimal totalAmount = 0;

            var initialInvoiceStatus = config.InvoiceStatus == InvoiceStatus.Draft
                ? InvoiceStatus.Draft
                : InvoiceStatus.Draft;

            var invoice = new Invoice
            {
                RepairRequestId = repairRequestId,
                IsChargeable = config.IsChargeable,
                Type = config.InvoiceType!.Value,
                CreatedAt = invoiceCreatedAt,
                Status = initialInvoiceStatus
            };
            data.Invoices.Add(invoice);

            int invoiceId = data.Invoices.Count;

            if (config.InvoiceType == InvoiceType.InternalRepair)
            {
                totalAmount = CreateInternalInvoiceAccessories(
                    config, data, seedData, invoiceId, techLeadUserId, invoiceCreatedAt, index);
            }
            else if (config.InvoiceType == InvoiceType.ExternalContractor)
            {
                totalAmount = CreateExternalInvoiceItems(config, data, invoiceId, issueName, index);
            }

            decimal servicePrice = GetServicePriceByScenario(config, index);
            totalAmount += servicePrice;

            data.InvoiceServices.Add(new InvoiceService
            {
                InvoiceId = invoiceId,
                Name = GetServiceNameByScenario(config, issueName),
                Price = servicePrice
            });

            invoice.TotalAmount = totalAmount;

            if (config.InvoiceStatus != InvoiceStatus.Draft)
            {
                invoice.Status = config.InvoiceStatus!.Value;

                if (config.HasStockTransaction && config.InvoiceStatus >= InvoiceStatus.Approved)
                {
                    CreateStockTransactionsAfterApproval(
                        config, data, seedData, invoiceId, techLeadUserId, invoiceCreatedAt, index);
                }

                if (config.InvoiceStatus == InvoiceStatus.Paid ||
                    config.InvoiceStatus == InvoiceStatus.AwaitingPayment ||
                    config.InvoiceStatus == InvoiceStatus.Approved)
                {
                    CreateTransactionForInvoice(
                        config, data, invoiceId, residentUserId, techLeadUserId, totalAmount, invoiceCreatedAt, appointmentEnd);
                }
            }
        }

        private static decimal CreateInternalInvoiceAccessories(
            ScenarioConfig config,
            SeedCollections data,
            SeedDataContext seedData,
            int invoiceId,
            int techLeadUserId,
            DateTime invoiceCreatedAt,
            int index)
        {
            decimal total = 0;
            var accessoryCount = seedData.Accessories.Count;
            if (accessoryCount == 0) return total;

            if (config.UseFromStock)
            {
                var stockAccessories = seedData.Accessories
                    .Skip(index % accessoryCount)
                    .Take(1 + (index % 2))
                    .ToList();

                foreach (var acc in stockAccessories)
                {
                    int qty = 1 + (index % 3);
                    decimal price = acc.Price * qty;
                    total += price;

                    data.InvoiceAccessories.Add(new InvoiceAccessory
                    {
                        InvoiceId = invoiceId,
                        AccessoryId = acc.AccessoryId,
                        Name = acc.Name,
                        Quantity = qty,
                        Price = acc.Price,
                        SourceType = InvoiceAccessorySourceType.FromStock
                    });
                }
            }

            if (config.UsePurchaseNew)
            {
                var purchaseAccessory = seedData.Accessories[(index + 3) % accessoryCount];
                int qty = 1 + (index % 2);
                decimal purchasePrice = purchaseAccessory.Price * 1.1m;
                total += purchasePrice * qty;

                data.InvoiceAccessories.Add(new InvoiceAccessory
                {
                    InvoiceId = invoiceId,
                    AccessoryId = purchaseAccessory.AccessoryId,
                    Name = purchaseAccessory.Name,
                    Quantity = qty,
                    Price = purchasePrice,
                    SourceType = InvoiceAccessorySourceType.ToBePurchased
                });
            }

            return total;
        }

        private static void CreateStockTransactionsAfterApproval(
            ScenarioConfig config,
            SeedCollections data,
            SeedDataContext seedData,
            int invoiceId,
            int techLeadUserId,
            DateTime invoiceCreatedAt,
            int index)
        {
            var accessoryCount = seedData.Accessories.Count;
            if (accessoryCount == 0) return;

            var approvalTime = invoiceCreatedAt.AddHours(2);

            if (config.UseFromStock)
            {
                var stockAccessories = seedData.Accessories
                    .Skip(index % accessoryCount)
                    .Take(1 + (index % 2))
                    .ToList();

                foreach (var acc in stockAccessories)
                {
                    int qty = 1 + (index % 3);
                    decimal price = acc.Price * qty;

                    data.StockTransactions.Add(new AccessoryStockTransaction
                    {
                        AccessoryId = acc.AccessoryId,
                        Quantity = qty,
                        Type = StockTransactionType.Export,
                        Status = StockTransactionStatus.Approved,
                        Note = $"Xuất kho tự động khi approve InspectionReport - Invoice #{invoiceId}",
                        CreatedBy = techLeadUserId,
                        CreatedAt = approvalTime,
                        ApprovedBy = techLeadUserId,
                        ApprovedAt = approvalTime,
                        UnitPrice = acc.Price,
                        TotalAmount = price,
                        InvoiceId = invoiceId
                    });
                }
            }

            if (config.UsePurchaseNew)
            {
                var purchaseAccessory = seedData.Accessories[(index + 3) % accessoryCount];
                int qty = 1 + (index % 2);
                decimal purchasePrice = purchaseAccessory.Price * 1.1m;

                data.StockTransactions.Add(new AccessoryStockTransaction
                {
                    AccessoryId = purchaseAccessory.AccessoryId,
                    Quantity = qty,
                    Type = StockTransactionType.Import,
                    Status = StockTransactionStatus.Approved,
                    Note = $"Nhập kho vật tư mua mới khi approve InspectionReport - Invoice #{invoiceId}",
                    CreatedBy = techLeadUserId,
                    CreatedAt = approvalTime,
                    ApprovedBy = techLeadUserId,
                    ApprovedAt = approvalTime,
                    UnitPrice = purchasePrice,
                    TotalAmount = purchasePrice * qty,
                    InvoiceId = invoiceId
                });
            }
        }

        private static decimal CreateExternalInvoiceItems(
            ScenarioConfig config,
            SeedCollections data,
            int invoiceId,
            string issueName,
            int index)
        {
            decimal total = 0;
            var materialPrice = 200000m + (index * 50000m);
            total += materialPrice;

            data.InvoiceAccessories.Add(new InvoiceAccessory
            {
                InvoiceId = invoiceId,
                Name = $"Contractor materials - {issueName}",
                Quantity = 1,
                Price = materialPrice,
                SourceType = InvoiceAccessorySourceType.ToBePurchased
            });

            return total;
        }

        private static void CreateTransactionForInvoice(
            ScenarioConfig config,
            SeedCollections data,
            int invoiceId,
            int residentUserId,
            int techLeadUserId,
            decimal amount,
            DateTime invoiceCreatedAt,
            DateTime appointmentEnd)
        {
            var approvalTime = invoiceCreatedAt.AddHours(2);

            if (config.IsChargeable)
            {
                var transactionStatus = config.InvoiceStatus switch
                {
                    InvoiceStatus.Paid => TransactionStatus.Success,
                    InvoiceStatus.AwaitingPayment => TransactionStatus.Pending,
                    _ => TransactionStatus.Pending
                };

                data.Transactions.Add(new Transaction
                {
                    UserId = residentUserId,
                    InvoiceId = invoiceId,
                    TransactionType = TransactionType.Payment,
                    Status = transactionStatus,
                    Provider = PaymentProvider.PayOS,
                    Direction = TransactionDirection.Income,
                    Amount = amount,
                    Description = GetTransactionDescription(config, invoiceId),
                    CreatedAt = approvalTime,
                    PaidAt = transactionStatus == TransactionStatus.Success ? appointmentEnd.AddDays(1) : null
                });
            }
            else
            {
                if (config.UsePurchaseNew || config.InvoiceType == InvoiceType.ExternalContractor)
                {
                    var transactionStatus = config.InvoiceType == InvoiceType.ExternalContractor
                        ? TransactionStatus.Pending
                        : TransactionStatus.Success;

                    data.Transactions.Add(new Transaction
                    {
                        UserId = techLeadUserId,
                        InvoiceId = invoiceId,
                        TransactionType = TransactionType.Cash,
                        Status = transactionStatus,
                        Provider = PaymentProvider.Budget,
                        Direction = TransactionDirection.Expense,
                        Amount = amount,
                        Description = config.InvoiceType == InvoiceType.ExternalContractor
                            ? $"Commit payment for contractor - Invoice #{invoiceId}"
                            : $"Purchase materials for repair - Invoice #{invoiceId}",
                        CreatedAt = approvalTime,
                        PaidAt = transactionStatus == TransactionStatus.Success ? approvalTime : null
                    });
                }
            }
        }

        #endregion

        #region InspectionReport Creation

        private static void CreateInspectionReport(
            ScenarioConfig config,
            SeedCollections data,
            int appointmentId,
            int technicianUserId,
            int techLeadUserId,
            int managerUserId,
            string issueName,
            DateTime inspectionCreatedAt)
        {
            var inspectionStatus = config.InvoiceStatus switch
            {
                InvoiceStatus.Draft => ReportStatus.Pending,
                _ => ReportStatus.Approved
            };

            var inspectionReport = new InspectionReport
            {
                AppointmentId = appointmentId,
                UserId = technicianUserId,
                FaultOwner = config.FaultType!.Value,
                SolutionType = config.SolutionType!.Value,
                Description = GetInspectionDescription(config, issueName),
                Solution = GetSolutionDescription(config),
                Status = inspectionStatus,
                CreatedAt = inspectionCreatedAt
            };
            data.InspectionReports.Add(inspectionReport);

            int inspectionReportId = data.InspectionReports.Count;

            if (inspectionStatus == ReportStatus.Approved)
            {
                data.ReportApprovals.Add(new ReportApproval
                {
                    InspectionReportId = inspectionReportId,
                    UserId = techLeadUserId,
                    Role = AccountRole.TechnicianLead,
                    Status = ReportStatus.Approved,
                    Comment = "Da phe duyet bao cao kiem tra va hoa don",
                    CreatedAt = inspectionCreatedAt.AddHours(2)
                });
            }
            else
            {
                data.ReportApprovals.Add(new ReportApproval
                {
                    InspectionReportId = inspectionReportId,
                    UserId = techLeadUserId,
                    Role = AccountRole.TechnicianLead,
                    Status = ReportStatus.Pending,
                    Comment = "Cho phe duyet bao cao kiem tra va hoa don",
                    CreatedAt = inspectionCreatedAt.AddMinutes(5)
                });
            }
        }

        #endregion

        #region RepairReport Creation

        private static void CreateRepairReport(
            ScenarioConfig config,
            SeedCollections data,
            int appointmentId,
            int technicianUserId,
            int techLeadUserId,
            int residentUserId,
            string issueName,
            DateTime appointmentEnd)
        {
            var repairCreatedAt = appointmentEnd.AddMinutes(-30);
            var repairStatus = config.FinalStatus == RequestStatus.Completed
                ? ReportStatus.ResidentApproved
                : ReportStatus.Approved;

            var repairReport = new RepairReport
            {
                AppointmentId = appointmentId,
                UserId = technicianUserId,
                Description = GetRepairDescription(config, issueName),
                Status = repairStatus,
                CreatedAt = repairCreatedAt
            };
            data.RepairReports.Add(repairReport);

            int repairReportId = data.RepairReports.Count;

            data.ReportApprovals.Add(new ReportApproval
            {
                RepairReportId = repairReportId,
                UserId = techLeadUserId,
                Role = AccountRole.TechnicianLead,
                Status = ReportStatus.Approved,
                Comment = "Xac nhan hoan thanh sua chua",
                CreatedAt = repairCreatedAt.AddHours(1)
            });

            if (config.FinalStatus == RequestStatus.Completed)
            {
                data.ReportApprovals.Add(new ReportApproval
                {
                    RepairReportId = repairReportId,
                    UserId = residentUserId,
                    Role = AccountRole.Resident,
                    Status = ReportStatus.ResidentApproved,
                    Comment = GetResidentApprovalComment(config),
                    CreatedAt = repairCreatedAt.AddDays(1)
                });
            }
            else
            {
                data.ReportApprovals.Add(new ReportApproval
                {
                    RepairReportId = repairReportId,
                    UserId = residentUserId,
                    Role = AccountRole.Resident,
                    Status = ReportStatus.Pending,
                    Comment = "Cho cu dan nghiem thu",
                    CreatedAt = repairCreatedAt.AddHours(2)
                });
            }
        }

        #endregion

        #region Text Helpers

        private static string GetInspectionDescription(ScenarioConfig config, string issueName) =>
            config.FaultType switch
            {
                FaultType.BuildingFault => $"Kiem tra {issueName}: Phat hien loi do hao mon tu nhien cua thiet bi thuoc he thong toa nha.",
                FaultType.ResidentFault => $"Kiem tra {issueName}: Phat hien loi do su dung khong dung cach tu phia cu dan.",
                _ => $"Da kiem tra {issueName}."
            };

        private static string GetSolutionDescription(ScenarioConfig config) =>
            config.SolutionType switch
            {
                SolutionType.Repair => "Tien hanh sua chua tai cho, thay the linh kien hong.",
                SolutionType.Replacement => "Can thay the thiet bi moi hoan toan.",
                SolutionType.Outsource => "De xuat thue don vi chuyen mon ben ngoai xu ly.",
                _ => "Dang danh gia phuong an xu ly."
            };

        private static string GetRepairDescription(ScenarioConfig config, string issueName) =>
            config.SolutionType switch
            {
                SolutionType.Repair => $"Da hoan thanh sua chua {issueName}. Thiet bi hoat dong binh thuong.",
                SolutionType.Replacement => $"Da thay the thiet bi moi cho {issueName}. Da test va nghiem thu.",
                SolutionType.Outsource => $"Nha thau da hoan thanh xu ly {issueName}. Da nghiem thu voi cu dan.",
                _ => $"Da xu ly xong {issueName}."
            };

        private static string GetResidentApprovalComment(ScenarioConfig config) =>
            config.IsChargeable
                ? "Da kiem tra va thanh toan. Hai long voi dich vu!"
                : "Da kiem tra, thiet bi hoat dong tot. Cam on doi ky thuat!";

        private static string GetFeedbackByScenario(ScenarioConfig config) =>
            config.Scenario switch
            {
                RepairScenario.Completed_BuildingFault_Free_FromStock => "Sua chua nhanh chong, khong mat phi. Rat hai long!",
                RepairScenario.Completed_ResidentFault_Chargeable_FromStock => "Ky thuat vien tan tinh, gia ca hop ly.",
                RepairScenario.Completed_BuildingFault_Free_PurchaseNew => "Thiet bi moi hoat dong tot. Cam on!",
                RepairScenario.Completed_ResidentFault_Chargeable_Mixed => "Dich vu tot, giai thich ro rang chi phi.",
                RepairScenario.Completed_ExternalContractor_BuildingFault => "Nha thau lam viec chuyen nghiep.",
                RepairScenario.Completed_ExternalContractor_ResidentFault => "Xu ly triet de, hai long voi ket qua.",
                _ => "Dich vu tot, se tiep tuc su dung."
            };

        private static decimal GetServicePriceByScenario(ScenarioConfig config, int index) =>
            (config.InvoiceType switch
            {
                InvoiceType.InternalRepair => 150000m,
                InvoiceType.ExternalContractor => 500000m,
                _ => 100000m
            }) + (index * 25000m);

        private static string GetServiceNameByScenario(ScenarioConfig config, string issueName) =>
            config.InvoiceType switch
            {
                InvoiceType.InternalRepair => $"Phi dich vu sua chua {issueName}",
                InvoiceType.ExternalContractor => $"Phi thue ngoai xu ly {issueName}",
                _ => $"Phi dich vu {issueName}"
            };

        private static string GetTransactionDescription(ScenarioConfig config, int invoiceId) =>
            config.InvoiceType switch
            {
                InvoiceType.InternalRepair => $"Thanh toan hoa don sua chua noi bo #{invoiceId}",
                InvoiceType.ExternalContractor => $"Thanh toan hoa don thue ngoai #{invoiceId}",
                _ => $"Thanh toan hoa don #{invoiceId}"
            };

        #endregion

        #region Tracking Generators

        private static List<RequestTracking> GenerateRequestTrackings(
            int requestId, RequestStatus finalStatus, DateTime createdAt,
            int residentId, int techLeadId, int managerId)
        {
            var trackings = new List<RequestTracking>();
            var currentTime = createdAt;

            trackings.Add(new RequestTracking
            {
                RepairRequestId = requestId,
                Status = RequestStatus.Pending,
                Note = "Yeu cau moi duoc tao",
                UpdatedBy = residentId,
                UpdatedAt = currentTime
            });

            if (finalStatus == RequestStatus.Pending) return trackings;

            if (finalStatus == RequestStatus.Cancelled)
            {
                trackings.Add(new RequestTracking
                {
                    RepairRequestId = requestId,
                    Status = RequestStatus.Cancelled,
                    Note = "Yeu cau da bi huy",
                    UpdatedBy = residentId,
                    UpdatedAt = currentTime.AddHours(2)
                });
                return trackings;
            }

            if (finalStatus == RequestStatus.WaitingManagerApproval)
            {
                trackings.Add(new RequestTracking
                {
                    RepairRequestId = requestId,
                    Status = RequestStatus.WaitingManagerApproval,
                    Note = "Cho Manager phe duyet",
                    UpdatedBy = techLeadId,
                    UpdatedAt = currentTime.AddHours(1)
                });
                return trackings;
            }

            currentTime = currentTime.AddHours(1);
            trackings.Add(new RequestTracking
            {
                RepairRequestId = requestId,
                Status = RequestStatus.Approved,
                Note = "Yeu cau da duoc duyet",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            if (finalStatus == RequestStatus.Approved) return trackings;

            currentTime = currentTime.AddDays(1);
            trackings.Add(new RequestTracking
            {
                RepairRequestId = requestId,
                Status = RequestStatus.InProgress,
                Note = "Ky thuat vien dang xu ly",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            if (finalStatus == RequestStatus.InProgress) return trackings;

            currentTime = currentTime.AddHours(3);
            trackings.Add(new RequestTracking
            {
                RepairRequestId = requestId,
                Status = RequestStatus.AcceptancePendingVerify,
                Note = "Cho cu dan nghiem thu",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            if (finalStatus == RequestStatus.AcceptancePendingVerify) return trackings;

            trackings.Add(new RequestTracking
            {
                RepairRequestId = requestId,
                Status = RequestStatus.Completed,
                Note = "Hoan thanh sua chua",
                UpdatedBy = residentId,
                UpdatedAt = currentTime.AddDays(1)
            });

            return trackings;
        }

        private static List<AppointmentTracking> GenerateAppointmentTrackings(
            int appointmentId, RequestStatus requestFinalStatus, DateTime createdAt,
            int residentId, int techLeadId)
        {
            var trackings = new List<AppointmentTracking>();
            var currentTime = createdAt.AddMinutes(30);

            trackings.Add(new AppointmentTracking
            {
                AppointmentId = appointmentId,
                Status = AppointmentStatus.Pending,
                Note = "Cuoc hen cho phan cong",
                UpdatedBy = residentId,
                UpdatedAt = currentTime
            });

            if (requestFinalStatus == RequestStatus.Cancelled)
            {
                trackings.Add(new AppointmentTracking
                {
                    AppointmentId = appointmentId,
                    Status = AppointmentStatus.Cancelled,
                    Note = "Huy do yeu cau bi huy",
                    UpdatedBy = techLeadId,
                    UpdatedAt = currentTime.AddHours(1)
                });
                return trackings;
            }

            if (requestFinalStatus == RequestStatus.WaitingManagerApproval)
                return trackings;

            currentTime = currentTime.AddHours(1);
            trackings.Add(new AppointmentTracking
            {
                AppointmentId = appointmentId,
                Status = AppointmentStatus.Assigned,
                Note = "Da phan cong ky thuat vien",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            if (requestFinalStatus == RequestStatus.Approved) return trackings;

            currentTime = currentTime.AddHours(2);
            trackings.Add(new AppointmentTracking
            {
                AppointmentId = appointmentId,
                Status = AppointmentStatus.Confirmed,
                Note = "Ky thuat vien xac nhan",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            currentTime = currentTime.AddDays(1);
            trackings.Add(new AppointmentTracking
            {
                AppointmentId = appointmentId,
                Status = AppointmentStatus.InVisit,
                Note = "Dang kiem tra hien truong",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            if (requestFinalStatus == RequestStatus.InProgress) return trackings;

            currentTime = currentTime.AddHours(1);
            trackings.Add(new AppointmentTracking
            {
                AppointmentId = appointmentId,
                Status = AppointmentStatus.InRepair,
                Note = "Dang tien hanh sua chua",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            if (requestFinalStatus == RequestStatus.Completed ||
                requestFinalStatus == RequestStatus.AcceptancePendingVerify)
            {
                trackings.Add(new AppointmentTracking
                {
                    AppointmentId = appointmentId,
                    Status = AppointmentStatus.Completed,
                    Note = "Hoan thanh sua chua",
                    UpdatedBy = techLeadId,
                    UpdatedAt = currentTime.AddHours(2)
                });
            }

            return trackings;
        }

        #endregion

        #region Database Save

        private static async Task SaveAllDataAsync(AptCareSystemDBContext context, SeedCollections data)
        {
            context.RepairRequests.AddRange(data.RepairRequests);
            await context.SaveChangesAsync();

            context.RequestTrackings.AddRange(data.RequestTrackings);
            await context.SaveChangesAsync();

            context.Appointments.AddRange(data.Appointments);
            await context.SaveChangesAsync();

            context.AppointmentTrackings.AddRange(data.AppointmentTrackings);
            await context.SaveChangesAsync();

            context.AppointmentAssigns.AddRange(data.AppointmentAssigns);
            await context.SaveChangesAsync();

            context.Invoices.AddRange(data.Invoices);
            await context.SaveChangesAsync();

            context.InvoiceAccessories.AddRange(data.InvoiceAccessories);
            await context.SaveChangesAsync();

            context.InvoiceServices.AddRange(data.InvoiceServices);
            await context.SaveChangesAsync();

            context.InspectionReports.AddRange(data.InspectionReports);
            await context.SaveChangesAsync();

            context.RepairReports.AddRange(data.RepairReports);
            await context.SaveChangesAsync();

            context.ReportApprovals.AddRange(data.ReportApprovals);
            await context.SaveChangesAsync();

            context.AccessoryStockTransactions.AddRange(data.StockTransactions);
            await context.SaveChangesAsync();

            context.Transactions.AddRange(data.Transactions);
            await context.SaveChangesAsync();

            context.Feedbacks.AddRange(data.Feedbacks);
            await context.SaveChangesAsync();
        }

        #endregion
    }
}