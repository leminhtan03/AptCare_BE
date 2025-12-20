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
            return new List<ScenarioConfig>
            {
                // ========== THÁNG 1/2025 ==========
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 1, 5),
                    Count = 6,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sửa chữa nội bộ tháng 1 - Lỗi tòa nhà"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_FromStock,
                    BaseDate = new DateTime(2025, 1, 12),
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
                    Description = "Sửa chữa tính phí tháng 1 - Lỗi cư dân"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Cancelled_ByResident,
                    BaseDate = new DateTime(2025, 1, 25),
                    Count = 2,
                    FinalStatus = RequestStatus.Cancelled,
                    HasInvoice = false,
                    Description = "Hủy bởi cư dân tháng 1"
                },

                // ========== THÁNG 2/2025 ==========
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 2, 3),
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
                    Description = "Sửa chữa nội bộ tháng 2"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_FromStock,
                    BaseDate = new DateTime(2025, 2, 10),
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
                    Description = "Sửa chữa tính phí tháng 2"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ExternalContractor_BuildingFault,
                    BaseDate = new DateTime(2025, 2, 18),
                    Count = 2,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Outsource,
                    InvoiceType = Enum.InvoiceType.ExternalContractor,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = false,
                    HasInvoice = true,
                    HasStockTransaction = false,
                    Description = "Thuê ngoài tháng 2 - Lỗi tòa nhà"
                },

                // ========== THÁNG 3/2025 ==========
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 3, 1),
                    Count = 6,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sửa chữa nội bộ tháng 3"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_Mixed,
                    BaseDate = new DateTime(2025, 3, 10),
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
                    Description = "Kết hợp vật tư kho và mua mới tháng 3"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Cancelled_ByTechLead,
                    BaseDate = new DateTime(2025, 3, 20),
                    Count = 2,
                    FinalStatus = RequestStatus.Cancelled,
                    HasInvoice = false,
                    Description = "Hủy bởi TechLead tháng 3"
                },

                // ========== THÁNG 4/2025 ==========
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 4, 2),
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
                    Description = "Sửa chữa nội bộ tháng 4"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_FromStock,
                    BaseDate = new DateTime(2025, 4, 10),
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
                    Description = "Sửa chữa tính phí tháng 4"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_PurchaseNew,
                    BaseDate = new DateTime(2025, 4, 18),
                    Count = 3,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Replacement,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UsePurchaseNew = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Thay thế thiết bị mới tháng 4"
                },

                // ========== THÁNG 5/2025 ==========
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 5, 1),
                    Count = 7,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sửa chữa nội bộ tháng 5"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_FromStock,
                    BaseDate = new DateTime(2025, 5, 10),
                    Count = 5,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sửa chữa tính phí tháng 5"
                },
                new ScenarioConfig
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
                    Description = "Thuê ngoài tháng 5 - Lỗi cư dân"
                },

                // ========== THÁNG 6/2025 ==========
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 6, 2),
                    Count = 6,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sửa chữa nội bộ tháng 6"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_Mixed,
                    BaseDate = new DateTime(2025, 6, 10),
                    Count = 4,
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
                    Description = "Kết hợp vật tư tháng 6"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Cancelled_ByResident,
                    BaseDate = new DateTime(2025, 6, 22),
                    Count = 2,
                    FinalStatus = RequestStatus.Cancelled,
                    HasInvoice = false,
                    Description = "Hủy bởi cư dân tháng 6"
                },

                // ========== THÁNG 7/2025 ==========
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 7, 1),
                    Count = 8,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sửa chữa nội bộ tháng 7"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_FromStock,
                    BaseDate = new DateTime(2025, 7, 10),
                    Count = 6,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sửa chữa tính phí tháng 7"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ExternalContractor_BuildingFault,
                    BaseDate = new DateTime(2025, 7, 20),
                    Count = 2,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Outsource,
                    InvoiceType = Enum.InvoiceType.ExternalContractor,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = false,
                    HasInvoice = true,
                    HasStockTransaction = false,
                    Description = "Thuê ngoài tháng 7"
                },

                // ========== THÁNG 8/2025 ==========
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 8, 1),
                    Count = 7,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sửa chữa nội bộ tháng 8"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_FromStock,
                    BaseDate = new DateTime(2025, 8, 10),
                    Count = 5,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sửa chữa tính phí tháng 8"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_PurchaseNew,
                    BaseDate = new DateTime(2025, 8, 18),
                    Count = 3,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Replacement,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UsePurchaseNew = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Thay thế thiết bị tháng 8"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Cancelled_ByTechLead,
                    BaseDate = new DateTime(2025, 8, 25),
                    Count = 2,
                    FinalStatus = RequestStatus.Cancelled,
                    HasInvoice = false,
                    Description = "Hủy bởi TechLead tháng 8"
                },

                // ========== THÁNG 9/2025 ==========
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 9, 1),
                    Count = 6,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sửa chữa nội bộ tháng 9"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_FromStock,
                    BaseDate = new DateTime(2025, 9, 8),
                    Count = 5,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sửa chữa tính phí tháng 9"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ExternalContractor_ResidentFault,
                    BaseDate = new DateTime(2025, 9, 16),
                    Count = 2,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Outsource,
                    InvoiceType = Enum.InvoiceType.ExternalContractor,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    HasInvoice = true,
                    HasStockTransaction = false,
                    Description = "Thuê ngoài tháng 9 - Lỗi cư dân"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Cancelled_ByResident,
                    BaseDate = new DateTime(2025, 9, 25),
                    Count = 2,
                    FinalStatus = RequestStatus.Cancelled,
                    HasInvoice = false,
                    Description = "Hủy bởi cư dân tháng 9"
                },

                // ========== THÁNG 10/2025 - Completed ==========
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 10, 1),
                    Count = 8,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    UsePurchaseNew = false,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Lỗi hệ thống tòa nhà, sửa chữa miễn phí, dùng vật tư từ kho"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_FromStock,
                    BaseDate = new DateTime(2025, 10, 8),
                    Count = 6,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    UseFromStock = true,
                    UsePurchaseNew = false,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Lỗi do cư dân, tính phí sửa chữa, dùng vật tư từ kho"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_PurchaseNew,
                    BaseDate = new DateTime(2025, 10, 15),
                    Count = 4,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Replacement,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = false,
                    UsePurchaseNew = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Lỗi tòa nhà, thay thế thiết bị mới, mua vật tư mới"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_Mixed,
                    BaseDate = new DateTime(2025, 10, 20),
                    Count = 5,
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
                    Description = "Lỗi cư dân, kết hợp vật tư kho và mua mới"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Cancelled_ByResident,
                    BaseDate = new DateTime(2025, 10, 25),
                    Count = 3,
                    FinalStatus = RequestStatus.Cancelled,
                    HasInvoice = false,
                    Description = "Cư dân hủy yêu cầu"
                },

                // ========== THÁNG 11/2025 - Completed + External ==========
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 11, 1),
                    Count = 10,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sửa chữa nội bộ tháng 11"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_FromStock,
                    BaseDate = new DateTime(2025, 11, 10),
                    Count = 8,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Sửa chữa tính phí tháng 11"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ExternalContractor_BuildingFault,
                    BaseDate = new DateTime(2025, 11, 15),
                    Count = 3,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Outsource,
                    InvoiceType = Enum.InvoiceType.ExternalContractor,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = false,
                    HasInvoice = true,
                    Description = "Thuê ngoài - lỗi tòa nhà"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ExternalContractor_ResidentFault,
                    BaseDate = new DateTime(2025, 11, 20),
                    Count = 2,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Outsource,
                    InvoiceType = Enum.InvoiceType.ExternalContractor,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    HasInvoice = true,
                    Description = "Thuê ngoài - lỗi cư dân"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Cancelled_ByTechLead,
                    BaseDate = new DateTime(2025, 11, 25),
                    Count = 2,
                    FinalStatus = RequestStatus.Cancelled,
                    HasInvoice = false,
                    Description = "TechLead hủy yêu cầu"
                },

                // ========== THÁNG 12/2025 - Mix trạng thái ==========
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_BuildingFault_Free_FromStock,
                    BaseDate = new DateTime(2025, 12, 1),
                    Count = 8,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Completed tháng 12"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Completed_ResidentFault_Chargeable_FromStock,
                    BaseDate = new DateTime(2025, 12, 5),
                    Count = 6,
                    FinalStatus = RequestStatus.Completed,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Paid,
                    IsChargeable = true,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Completed tính phí tháng 12"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.AcceptancePending_InvoicePaid,
                    BaseDate = new DateTime(2025, 12, 15),
                    Count = 4,
                    FinalStatus = RequestStatus.AcceptancePendingVerify,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Chờ nghiệm thu - invoice approved"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.AcceptancePending_AwaitingPayment,
                    BaseDate = new DateTime(2025, 12, 16),
                    Count = 3,
                    FinalStatus = RequestStatus.AcceptancePendingVerify,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.AwaitingPayment,
                    IsChargeable = true,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Chờ nghiệm thu - chờ thanh toán"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.InProgress_WithApprovedInvoice,
                    BaseDate = new DateTime(2025, 12, 18),
                    Count = 4,
                    FinalStatus = RequestStatus.InProgress,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Approved,
                    IsChargeable = false,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = true,
                    Description = "Đang xử lý - invoice approved (InspectionReport đã duyệt)"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.InProgress_WithDraftInvoice,
                    BaseDate = new DateTime(2025, 12, 19),
                    Count = 3,
                    FinalStatus = RequestStatus.InProgress,
                    FaultType = Enum.FaultType.ResidentFault,
                    SolutionType = Enum.SolutionType.Repair,
                    InvoiceType = Enum.InvoiceType.InternalRepair,
                    InvoiceStatus = Enum.InvoiceStatus.Draft,
                    IsChargeable = true,
                    UseFromStock = true,
                    HasInvoice = true,
                    HasStockTransaction = false,
                    Description = "Đang xử lý - invoice draft (InspectionReport chờ duyệt)"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.InProgress_NoInvoice,
                    BaseDate = new DateTime(2025, 12, 20),
                    Count = 3,
                    FinalStatus = RequestStatus.InProgress,
                    FaultType = Enum.FaultType.BuildingFault,
                    SolutionType = Enum.SolutionType.Repair,
                    HasInvoice = false,
                    Description = "Đang xử lý - chưa tạo invoice/report"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Approved_WaitingStart,
                    BaseDate = new DateTime(2025, 12, 21),
                    Count = 5,
                    FinalStatus = RequestStatus.Approved,
                    HasInvoice = false,
                    Description = "Đã duyệt - chờ bắt đầu"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Pending_New,
                    BaseDate = new DateTime(2025, 12, 22),
                    Count = 5,
                    FinalStatus = RequestStatus.Pending,
                    HasInvoice = false,
                    Description = "Yêu cầu mới chờ duyệt"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.WaitingManagerApproval,
                    BaseDate = new DateTime(2025, 12, 23),
                    Count = 3,
                    FinalStatus = RequestStatus.WaitingManagerApproval,
                    HasInvoice = false,
                    Description = "Chờ Manager duyệt"
                },
                new ScenarioConfig
                {
                    Scenario = RepairScenario.Cancelled_ByResident,
                    BaseDate = new DateTime(2025, 12, 24),
                    Count = 2,
                    FinalStatus = RequestStatus.Cancelled,
                    HasInvoice = false,
                    Description = "Hủy bởi cư dân tháng 12"
                }
            };
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

            // 1. Tạo RepairRequest
            var request = new RepairRequest
            {
                UserId = userApartment.UserId,
                ApartmentId = userApartment.ApartmentId,
                IssueId = issue.IssueId,
                Object = issue.Name,
                Description = $"[{config.Scenario}] {config.Description}. Căn hộ {userApartment.Apartment.Room}.",
                IsEmergency = false,
                CreatedAt = createdAt
            };
            data.RepairRequests.Add(request);

            // 2. Tạo RequestTrackings
            var trackings = GenerateRequestTrackings(
                currentRequestId,
                config.FinalStatus,
                createdAt,
                userApartment.UserId,
                seedData.TechLeadUser!.UserId,
                seedData.ManagerUser?.UserId ?? seedData.TechLeadUser.UserId
            );
            data.RequestTrackings.AddRange(trackings);

            // 3. Tạo Appointment nếu đã qua Pending
            if (config.FinalStatus != RequestStatus.Pending)
            {
                var appointmentStart = createdAt.AddDays(1).Date.AddHours(9);
                var appointmentEnd = appointmentStart.AddHours(issue.EstimatedDuration);
                int currentAppointmentId = counter.AppointmentId;

                var appointment = new Appointment
                {
                    RepairRequestId = currentRequestId,
                    StartTime = appointmentStart,
                    EndTime = appointmentEnd,
                    Note = $"Lịch hẹn: {config.Description}",
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

                // AppointmentAssign
                User? assignedTechnician = null;
                if (config.FinalStatus != RequestStatus.Cancelled &&
                    config.FinalStatus != RequestStatus.WaitingManagerApproval)
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

                // 4. Tạo Invoice TRƯỚC, sau đó InspectionReport (theo đúng luồng thực tế)
                if (config.FinalStatus >= RequestStatus.InProgress &&
                    config.FinalStatus != RequestStatus.Cancelled &&
                    assignedTechnician != null &&
                    config.FaultType.HasValue &&
                    config.SolutionType.HasValue)
                {
                    // ⭐ QUAN TRỌNG: Invoice được tạo TRƯỚC InspectionReport (1-2 giây)
                    // Theo logic trong InspectionReporService: x.CreatedAt < inspectionReport.CreatedAt
                    var baseTime = appointmentStart.AddHours(1);
                    var invoiceCreatedAt = baseTime; // Invoice tạo trước
                    var inspectionCreatedAt = baseTime.AddSeconds(2); // InspectionReport tạo sau 2 giây

                    // Tạo Invoice TRƯỚC
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

                    // Tạo InspectionReport SAU (với thời gian sau Invoice 2 giây)
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

                // 5. Tạo RepairReport cho Completed/AcceptancePending
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

                // 6. Tạo Feedback cho Completed
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

        #endregion

        #region Invoice Creation - TẠO TRƯỚC INSPECTION REPORT

        /// <summary>
        /// Tạo Invoice TRƯỚC InspectionReport (1-2 giây)
        /// Theo logic trong InspectionReporService và ReportApprovalService:
        /// - Invoice được match với InspectionReport qua thời gian tạo trong khoảng ±5 giây
        /// - Invoice.CreatedAt < InspectionReport.CreatedAt
        /// - Invoice phải có Status = Draft để được xử lý khi approve InspectionReport
        /// </summary>
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

            // ⭐ Invoice ban đầu luôn là Draft
            // Khi approve InspectionReport, Invoice sẽ được chuyển sang Approved/AwaitingPayment/Cancelled
            var initialInvoiceStatus = config.InvoiceStatus == InvoiceStatus.Draft
                ? InvoiceStatus.Draft
                : InvoiceStatus.Draft; // Mọi invoice ban đầu đều Draft

            var invoice = new Invoice
            {
                RepairRequestId = repairRequestId,
                IsChargeable = config.IsChargeable,
                Type = config.InvoiceType!.Value,
                CreatedAt = invoiceCreatedAt, // ⭐ TẠO TRƯỚC InspectionReport
                Status = initialInvoiceStatus
            };
            data.Invoices.Add(invoice);

            int invoiceId = data.Invoices.Count;

            // Tạo InvoiceAccessories
            if (config.InvoiceType == InvoiceType.InternalRepair)
            {
                totalAmount = CreateInternalInvoiceAccessories(
                    config, data, seedData, invoiceId, techLeadUserId, invoiceCreatedAt, index);
            }
            else if (config.InvoiceType == InvoiceType.ExternalContractor)
            {
                totalAmount = CreateExternalInvoiceItems(config, data, invoiceId, issueName, index);
            }

            // Tạo InvoiceService
            decimal servicePrice = GetServicePriceByScenario(config, index);
            totalAmount += servicePrice;

            data.InvoiceServices.Add(new InvoiceService
            {
                InvoiceId = invoiceId,
                Name = GetServiceNameByScenario(config, issueName),
                Price = servicePrice
            });

            invoice.TotalAmount = totalAmount;

            // ⭐ NẾU INSPECTION REPORT ĐÃ ĐƯỢC APPROVE -> CẬP NHẬT INVOICE STATUS
            // Điều này xảy ra khi config.InvoiceStatus != Draft
            if (config.InvoiceStatus != InvoiceStatus.Draft)
            {
                // Invoice đã được approve cùng InspectionReport
                invoice.Status = config.InvoiceStatus!.Value;

                // Tạo StockTransaction và Transaction nếu cần
                if (config.HasStockTransaction && config.InvoiceStatus >= InvoiceStatus.Approved)
                {
                    CreateStockTransactionsAfterApproval(
                        config, data, seedData, invoiceId, techLeadUserId, invoiceCreatedAt, index);
                }

                // Tạo Transaction cho payment
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

            // Vật tư từ kho (FromStock)
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

            // Vật tư cần mua (ToBePurchased)
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

            var approvalTime = invoiceCreatedAt.AddHours(2); // Thời điểm approve

            // Tạo phiếu xuất kho cho FromStock
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

            // Tạo phiếu nhập kho cho ToBePurchased
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
                Name = $"Vật tư nhà thầu - {issueName}",
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

            // Thu tiền từ cư dân (nếu tính phí)
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
            // Chi tiền từ budget (nếu không tính phí)
            else
            {
                if (config.UsePurchaseNew || config.InvoiceType == InvoiceType.ExternalContractor)
                {
                    var transactionStatus = config.InvoiceType == InvoiceType.ExternalContractor
                        ? TransactionStatus.Pending  // Thuê ngoài: chờ thanh toán thực tế
                        : TransactionStatus.Success; // Mua vật tư: trừ budget ngay

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
                            ? $"Cam kết thanh toán cho nhà thầu - Invoice #{invoiceId}"
                            : $"Mua vật tư cho sửa chữa - Invoice #{invoiceId}",
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
            // ⭐ InspectionReport status phụ thuộc vào việc đã được approve chưa
            var inspectionStatus = config.InvoiceStatus switch
            {
                InvoiceStatus.Draft => ReportStatus.Pending, // Chưa approve
                _ => ReportStatus.Approved // Đã approve
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

            // ⭐ Tạo ReportApproval theo trạng thái
            if (inspectionStatus == ReportStatus.Approved)
            {
                // Đã approve - TechLead đã duyệt
                data.ReportApprovals.Add(new ReportApproval
                {
                    InspectionReportId = inspectionReportId,
                    UserId = techLeadUserId,
                    Role = AccountRole.TechnicianLead,
                    Status = ReportStatus.Approved,
                    Comment = "Đã xác nhận báo cáo kiểm tra và phê duyệt hóa đơn",
                    CreatedAt = inspectionCreatedAt.AddHours(2)
                });
            }
            else
            {
                // Chờ approve - Pending
                data.ReportApprovals.Add(new ReportApproval
                {
                    InspectionReportId = inspectionReportId,
                    UserId = techLeadUserId,
                    Role = AccountRole.TechnicianLead,
                    Status = ReportStatus.Pending,
                    Comment = "Chờ phê duyệt báo cáo kiểm tra và hóa đơn",
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

            // TechLead approve
            data.ReportApprovals.Add(new ReportApproval
            {
                RepairReportId = repairReportId,
                UserId = techLeadUserId,
                Role = AccountRole.TechnicianLead,
                Status = ReportStatus.Approved,
                Comment = "Xác nhận hoàn thành sửa chữa",
                CreatedAt = repairCreatedAt.AddHours(1)
            });

            // Resident approve cho Completed
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
                // AcceptancePending - Chờ cư dân nghiệm thu
                data.ReportApprovals.Add(new ReportApproval
                {
                    RepairReportId = repairReportId,
                    UserId = residentUserId,
                    Role = AccountRole.Resident,
                    Status = ReportStatus.Pending,
                    Comment = "Chờ cư dân nghiệm thu",
                    CreatedAt = repairCreatedAt.AddHours(2)
                });
            }
        }

        #endregion

        #region Text Helpers

        private static string GetInspectionDescription(ScenarioConfig config, string issueName) =>
            config.FaultType switch
            {
                FaultType.BuildingFault => $"Kiểm tra {issueName}: Phát hiện lỗi do hao mòn tự nhiên của thiết bị thuộc hệ thống tòa nhà.",
                FaultType.ResidentFault => $"Kiểm tra {issueName}: Phát hiện lỗi do sử dụng không đúng cách từ phía cư dân.",
                _ => $"Đã kiểm tra {issueName}."
            };

        private static string GetSolutionDescription(ScenarioConfig config) =>
            config.SolutionType switch
            {
                SolutionType.Repair => "Tiến hành sửa chữa tại chỗ, thay thế linh kiện hỏng.",
                SolutionType.Replacement => "Cần thay thế thiết bị mới hoàn toàn.",
                SolutionType.Outsource => "Đề xuất thuê đơn vị chuyên môn bên ngoài xử lý.",
                _ => "Đang đánh giá phương án xử lý."
            };

        private static string GetRepairDescription(ScenarioConfig config, string issueName) =>
            config.SolutionType switch
            {
                SolutionType.Repair => $"Đã hoàn thành sửa chữa {issueName}. Thiết bị hoạt động bình thường.",
                SolutionType.Replacement => $"Đã thay thế thiết bị mới cho {issueName}. Đã test và nghiệm thu.",
                SolutionType.Outsource => $"Nhà thầu đã hoàn thành xử lý {issueName}. Đã nghiệm thu với cư dân.",
                _ => $"Đã xử lý xong {issueName}."
            };

        private static string GetResidentApprovalComment(ScenarioConfig config) =>
            config.IsChargeable
                ? "Đã kiểm tra và thanh toán. Hài lòng với dịch vụ!"
                : "Đã kiểm tra, thiết bị hoạt động tốt. Cảm ơn đội kỹ thuật!";

        private static string GetFeedbackByScenario(ScenarioConfig config) =>
            config.Scenario switch
            {
                RepairScenario.Completed_BuildingFault_Free_FromStock => "Sửa chữa nhanh chóng, không mất phí. Rất hài lòng!",
                RepairScenario.Completed_ResidentFault_Chargeable_FromStock => "Kỹ thuật viên tận tình, giá cả hợp lý.",
                RepairScenario.Completed_BuildingFault_Free_PurchaseNew => "Thiết bị mới hoạt động tốt. Cảm ơn!",
                RepairScenario.Completed_ResidentFault_Chargeable_Mixed => "Dịch vụ tốt, giải thích rõ ràng chi phí.",
                RepairScenario.Completed_ExternalContractor_BuildingFault => "Nhà thầu làm việc chuyên nghiệp.",
                RepairScenario.Completed_ExternalContractor_ResidentFault => "Xử lý triệt để, hài lòng với kết quả.",
                _ => "Dịch vụ tốt, sẽ tiếp tục sử dụng."
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
                InvoiceType.InternalRepair => $"Phí dịch vụ sửa chữa {issueName}",
                InvoiceType.ExternalContractor => $"Phí thuê ngoài xử lý {issueName}",
                _ => $"Phí dịch vụ {issueName}"
            };

        private static string GetTransactionDescription(ScenarioConfig config, int invoiceId) =>
            config.InvoiceType switch
            {
                InvoiceType.InternalRepair => $"Thanh toán hóa đơn sửa chữa nội bộ #{invoiceId}",
                InvoiceType.ExternalContractor => $"Thanh toán hóa đơn thuê ngoài #{invoiceId}",
                _ => $"Thanh toán hóa đơn #{invoiceId}"
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
                Note = "Yêu cầu mới được tạo",
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
                    Note = "Yêu cầu đã bị hủy",
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
                    Note = "Chờ Manager phê duyệt",
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
                Note = "Yêu cầu đã được duyệt",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            if (finalStatus == RequestStatus.Approved) return trackings;

            currentTime = currentTime.AddDays(1);
            trackings.Add(new RequestTracking
            {
                RepairRequestId = requestId,
                Status = RequestStatus.InProgress,
                Note = "Kỹ thuật viên đang xử lý",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            if (finalStatus == RequestStatus.InProgress) return trackings;

            currentTime = currentTime.AddHours(3);
            trackings.Add(new RequestTracking
            {
                RepairRequestId = requestId,
                Status = RequestStatus.AcceptancePendingVerify,
                Note = "Chờ cư dân nghiệm thu",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            if (finalStatus == RequestStatus.AcceptancePendingVerify) return trackings;

            trackings.Add(new RequestTracking
            {
                RepairRequestId = requestId,
                Status = RequestStatus.Completed,
                Note = "Hoàn thành sửa chữa",
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
                Note = "Cuộc hẹn chờ phân công",
                UpdatedBy = residentId,
                UpdatedAt = currentTime
            });

            if (requestFinalStatus == RequestStatus.Cancelled)
            {
                trackings.Add(new AppointmentTracking
                {
                    AppointmentId = appointmentId,
                    Status = AppointmentStatus.Cancelled,
                    Note = "Hủy do yêu cầu bị hủy",
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
                Note = "Đã phân công kỹ thuật viên",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            if (requestFinalStatus == RequestStatus.Approved) return trackings;

            currentTime = currentTime.AddHours(2);
            trackings.Add(new AppointmentTracking
            {
                AppointmentId = appointmentId,
                Status = AppointmentStatus.Confirmed,
                Note = "Kỹ thuật viên xác nhận",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            currentTime = currentTime.AddDays(1);
            trackings.Add(new AppointmentTracking
            {
                AppointmentId = appointmentId,
                Status = AppointmentStatus.InVisit,
                Note = "Đang kiểm tra hiện trường",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            if (requestFinalStatus == RequestStatus.InProgress) return trackings;

            currentTime = currentTime.AddHours(1);
            trackings.Add(new AppointmentTracking
            {
                AppointmentId = appointmentId,
                Status = AppointmentStatus.InRepair,
                Note = "Đang tiến hành sửa chữa",
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
                    Note = "Hoàn thành sửa chữa",
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

            // ⭐ Invoice phải được lưu TRƯỚC InspectionReport
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