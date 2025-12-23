using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using Microsoft.EntityFrameworkCore;

namespace AptCare.Repository.Seeds
{

    public static class MaintenanceRepairRequestSeed
    {
        #region Scenario Definitions

        private enum MaintenanceScenario
        {
            Completed_AllTasksOK,
            Completed_SomeNeedRepair,
        }

        private class MaintenanceScenarioConfig
        {
            public MaintenanceScenario Scenario { get; set; }
            public DateTime BaseDate { get; set; }
            public int Count { get; set; }
            public RequestStatus FinalStatus { get; set; }
            public bool HasInspectionReport { get; set; }
            public bool HasRepairReport { get; set; }
            public TaskCompletionStatus TaskStatus { get; set; } = TaskCompletionStatus.Pending;
            public string Description { get; set; } = string.Empty;
        }

        private class MaintenanceCounterContext
        {
            public int RequestId { get; set; } = 1;
            public int AppointmentId { get; set; } = 1;
            public int TaskId { get; set; } = 1;
        }

        #endregion

        public static async Task SeedAsync(AptCareSystemDBContext context, int startRequestId, int startAppointmentId)
        {
            // Kiem tra da co maintenance repair request chua
            var hasMaintenanceRequests = await context.RepairRequests
                .AnyAsync(r => r.MaintenanceScheduleId != null);

            if (hasMaintenanceRequests) return;

            var seedData = await LoadSeedDataAsync(context);
            if (!seedData.IsValid) return;

            var scenarios = BuildScenarioConfigs();
            var allData = new MaintenanceSeedCollections();
            var counter = new MaintenanceCounterContext
            {
                RequestId = startRequestId,
                AppointmentId = startAppointmentId
            };

            foreach (var scenario in scenarios)
            {
                for (int i = 0; i < scenario.Count; i++)
                {
                    var createdAt = scenario.BaseDate.AddDays(i * 2).AddHours(7 + (i % 4));
                    var scheduleIndex = (counter.RequestId - startRequestId) % seedData.MaintenanceSchedules.Count;
                    var schedule = seedData.MaintenanceSchedules[scheduleIndex];

                    CreateMaintenanceRepairRequest(
                        scenario,
                        seedData,
                        allData,
                        counter,
                        schedule,
                        createdAt,
                        i
                    );
                }
            }

            await SaveAllDataAsync(context, allData);
        }

        #region Scenario Configurations

        private static List<MaintenanceScenarioConfig> BuildScenarioConfigs()
        {
            var today = DateTime.Now;
            var configs = new List<MaintenanceScenarioConfig>();

            if (today >= new DateTime(2025, 2, 1))
            {
                configs.Add(new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_AllTasksOK,
                    BaseDate = new DateTime(2025, 1, 10),
                    Count = 4,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri dinh ky thang 1"
                });
            }

            if (today >= new DateTime(2025, 3, 1))
            {
                configs.Add(new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_SomeNeedRepair,
                    BaseDate = new DateTime(2025, 2, 5),
                    Count = 3,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri thang 2 - Co phat sinh"
                });
            }

            // Thang 3/2025 - Completed
            if (today >= new DateTime(2025, 4, 1))
            {
                configs.Add(new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_AllTasksOK,
                    BaseDate = new DateTime(2025, 3, 8),
                    Count = 4,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri dinh ky thang 3"
                });
            }

            // Thang 4/2025 - Completed
            if (today >= new DateTime(2025, 5, 1))
            {
                configs.Add(new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_AllTasksOK,
                    BaseDate = new DateTime(2025, 4, 12),
                    Count = 4,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri dinh ky thang 4"
                });
            }

            // Thang 5/2025 - Completed
            if (today >= new DateTime(2025, 6, 1))
            {
                configs.Add(new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_SomeNeedRepair,
                    BaseDate = new DateTime(2025, 5, 6),
                    Count = 3,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri thang 5 - Thay the linh kien"
                });
            }

            // Thang 6/2025 - Completed
            if (today >= new DateTime(2025, 7, 1))
            {
                configs.Add(new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_AllTasksOK,
                    BaseDate = new DateTime(2025, 6, 15),
                    Count = 4,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri dinh ky thang 6"
                });
            }

            // Thang 7/2025 - Chi tao neu da qua
            if (today >= new DateTime(2025, 7, 15))
            {
                configs.Add(new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_AllTasksOK,
                    BaseDate = new DateTime(2025, 7, 8),
                    Count = 3,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri dinh ky thang 7"
                });
            }

            return configs;
        }

        #endregion

        #region Data Loading

        private class MaintenanceSeedDataContext
        {
            public List<MaintenanceSchedule> MaintenanceSchedules { get; set; } = new();
            public List<MaintenanceTask> MaintenanceTasks { get; set; } = new();
            public List<User> Technicians { get; set; } = new();
            public User? TechLeadUser { get; set; }
            public User? ManagerUser { get; set; }
            public bool IsValid => MaintenanceSchedules.Any() && Technicians.Any() && TechLeadUser != null && ManagerUser != null;
        }

        private static async Task<MaintenanceSeedDataContext> LoadSeedDataAsync(AptCareSystemDBContext context)
        {
            return new MaintenanceSeedDataContext
            {
                MaintenanceSchedules = await context.MaintenanceSchedules
                    .Include(ms => ms.CommonAreaObject)
                        .ThenInclude(cao => cao.CommonArea)
                    .Include(ms => ms.CommonAreaObject)
                        .ThenInclude(cao => cao.CommonAreaObjectType)
                            .ThenInclude(t => t.MaintenanceTasks)
                    .Where(ms => ms.Status == ActiveStatus.Active)
                    .ToListAsync(),

                MaintenanceTasks = await context.MaintenanceTasks
                    .Where(mt => mt.Status == ActiveStatus.Active)
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
                    .FirstOrDefaultAsync(u => u.Account.Role == AccountRole.Manager)
            };
        }

        #endregion

        #region Data Collections

        private class MaintenanceSeedCollections
        {
            public List<RepairRequest> RepairRequests { get; } = new();
            public List<RequestTracking> RequestTrackings { get; } = new();
            public List<RepairRequestTask> RepairRequestTasks { get; } = new();
            public List<Appointment> Appointments { get; } = new();
            public List<AppointmentTracking> AppointmentTrackings { get; } = new();
            public List<AppointmentAssign> AppointmentAssigns { get; } = new();
            public List<InspectionReport> InspectionReports { get; } = new();
            public List<RepairReport> RepairReports { get; } = new();
            public List<ReportApproval> ReportApprovals { get; } = new();
        }

        #endregion

        #region Request Creation

        private static void CreateMaintenanceRepairRequest(
            MaintenanceScenarioConfig config,
            MaintenanceSeedDataContext seedData,
            MaintenanceSeedCollections data,
            MaintenanceCounterContext counter,
            MaintenanceSchedule schedule,
            DateTime createdAt,
            int index)
        {
            int currentRequestId = counter.RequestId;

            // 1. Tao RepairRequest tu MaintenanceSchedule
            var request = new RepairRequest
            {
                UserId = seedData.ManagerUser!.UserId,
                MaintenanceScheduleId = schedule.MaintenanceScheduleId,
                Object = schedule.CommonAreaObject.Name,
                Description = $"Bao tri dinh ky: {schedule.Description}. " +
                              $"Khu vuc: {schedule.CommonAreaObject.CommonArea.Name}. " +
                              $"Chu ky: {schedule.FrequencyInDays} ngay.",
                IsEmergency = false,
                CreatedAt = createdAt,
                AcceptanceTime = DateOnly.FromDateTime(createdAt.AddDays(3))
            };

            data.RepairRequests.Add(request);

            // 2. Tao RequestTrackings - Luong dung: Pending -> WaitingManagerApproval -> Approved -> InProgress -> AcceptancePendingVerify -> Completed
            var trackings = GenerateMaintenanceRequestTrackings(
                currentRequestId,
                config.FinalStatus,
                createdAt,
                seedData.TechLeadUser!.UserId,
                seedData.ManagerUser.UserId
            );
            data.RequestTrackings.AddRange(trackings);

            // 3. Tao RepairRequestTasks tu MaintenanceTasks
            var tasks = schedule.CommonAreaObject.CommonAreaObjectType.MaintenanceTasks ?? new List<MaintenanceTask>();
            foreach (var task in tasks)
            {
                var requestTask = new RepairRequestTask
                {
                    RepairRequestId = currentRequestId,
                    MaintenanceTaskTemplateId = task.MaintenanceTaskId,
                    TaskName = task.TaskName,
                    TaskDescription = task.TaskDescription,
                    DisplayOrder = task.DisplayOrder,
                    Status = config.TaskStatus,
                    InspectionResult = config.TaskStatus == TaskCompletionStatus.Completed
                        ? GetInspectionResult(config, index)
                        : null,
                    TechnicianNote = config.TaskStatus == TaskCompletionStatus.Completed
                        ? GetTechnicianNote(config, task.TaskName)
                        : null,
                    CompletedAt = config.TaskStatus == TaskCompletionStatus.Completed
                        ? createdAt.AddHours(3)
                        : null,
                    CompletedByUserId = config.TaskStatus == TaskCompletionStatus.Completed
                        ? seedData.Technicians[index % seedData.Technicians.Count].UserId
                        : null
                };
                data.RepairRequestTasks.Add(requestTask);
                counter.TaskId++;
            }

            // 4. Tao Appointment
            var startTime = createdAt.AddDays(1).Date.AddHours(9);
            var endTime = startTime.AddHours(schedule.EstimatedDuration);
            int currentAppointmentId = counter.AppointmentId;

            var appointment = new Appointment
            {
                RepairRequestId = currentRequestId,
                StartTime = startTime,
                EndTime = endTime,
                Note = $"Lich bao tri dinh ky: {config.Description}",
                CreatedAt = createdAt.AddMinutes(30)
            };
            data.Appointments.Add(appointment);

            // AppointmentTrackings
            var apptTrackings = GenerateMaintenanceAppointmentTrackings(
                currentAppointmentId,
                config.FinalStatus,
                createdAt,
                seedData.TechLeadUser.UserId
            );
            data.AppointmentTrackings.AddRange(apptTrackings);

            // AppointmentAssign
            var techIndex = index % seedData.Technicians.Count;
            var assignedTechs = seedData.Technicians
                .Skip(techIndex)
                .Take(schedule.RequiredTechnicians)
                .ToList();

            var assignedTechnician = assignedTechs.FirstOrDefault();

            foreach (var tech in assignedTechs)
            {
                data.AppointmentAssigns.Add(new AppointmentAssign
                {
                    TechnicianId = tech.UserId,
                    AppointmentId = currentAppointmentId,
                    AssignedAt = createdAt.AddHours(1),
                    EstimatedStartTime = startTime,
                    EstimatedEndTime = endTime,
                    ActualStartTime = startTime,
                    ActualEndTime = endTime,
                    Status = WorkOrderStatus.Completed
                });
            }

            // 5. Tao InspectionReport
            if (config.HasInspectionReport && assignedTechnician != null)
            {
                CreateMaintenanceInspectionReport(
                    config,
                    data,
                    currentAppointmentId,
                    assignedTechnician.UserId,
                    seedData.TechLeadUser.UserId,
                    seedData.ManagerUser.UserId,
                    schedule,
                    createdAt.AddHours(2)
                );
            }

            // 6. Tao RepairReport
            if (config.HasRepairReport && assignedTechnician != null)
            {
                CreateMaintenanceRepairReport(
                    config,
                    data,
                    currentAppointmentId,
                    assignedTechnician.UserId,
                    seedData.TechLeadUser.UserId,
                    seedData.ManagerUser.UserId,
                    schedule,
                    createdAt.AddHours(4)
                );
            }

            counter.AppointmentId++;
            counter.RequestId++;
        }

        #endregion

        #region Reports Creation

        private static void CreateMaintenanceInspectionReport(
            MaintenanceScenarioConfig config,
            MaintenanceSeedCollections data,
            int appointmentId,
            int technicianUserId,
            int techLeadUserId,
            int managerUserId,
            MaintenanceSchedule schedule,
            DateTime createdAt)
        {
            var faultType = FaultType.BuildingFault;
            var solutionType = config.Scenario == MaintenanceScenario.Completed_SomeNeedRepair
                ? SolutionType.Repair
                : SolutionType.Repair;

            var inspectionReport = new InspectionReport
            {
                AppointmentId = appointmentId,
                UserId = technicianUserId,
                FaultOwner = faultType,
                SolutionType = solutionType,
                Description = GetMaintenanceInspectionDescription(config, schedule),
                Solution = GetMaintenanceSolutionDescription(config, schedule),
                Status = ReportStatus.Approved,
                CreatedAt = createdAt
            };
            data.InspectionReports.Add(inspectionReport);

            int reportId = data.InspectionReports.Count;

            // TechLead approve
            data.ReportApprovals.Add(new ReportApproval
            {
                InspectionReportId = reportId,
                UserId = techLeadUserId,
                Role = AccountRole.TechnicianLead,
                Status = ReportStatus.Approved,
                Comment = "Xac nhan bao cao kiem tra bao tri dinh ky",
                CreatedAt = createdAt.AddHours(1)
            });

            // Manager approve (cho maintenance)
            data.ReportApprovals.Add(new ReportApproval
            {
                InspectionReportId = reportId,
                UserId = managerUserId,
                Role = AccountRole.Manager,
                Status = ReportStatus.Approved,
                Comment = "Phe duyet bao cao bao tri dinh ky",
                CreatedAt = createdAt.AddHours(2)
            });
        }

        private static void CreateMaintenanceRepairReport(
            MaintenanceScenarioConfig config,
            MaintenanceSeedCollections data,
            int appointmentId,
            int technicianUserId,
            int techLeadUserId,
            int managerUserId,
            MaintenanceSchedule schedule,
            DateTime createdAt)
        {
            var repairReport = new RepairReport
            {
                AppointmentId = appointmentId,
                UserId = technicianUserId,
                Description = GetMaintenanceRepairDescription(config, schedule),
                Status = ReportStatus.Approved,
                CreatedAt = createdAt
            };
            data.RepairReports.Add(repairReport);

            int reportId = data.RepairReports.Count;

            // TechLead approve
            data.ReportApprovals.Add(new ReportApproval
            {
                RepairReportId = reportId,
                UserId = techLeadUserId,
                Role = AccountRole.TechnicianLead,
                Status = ReportStatus.Approved,
                Comment = "Xac nhan hoan thanh bao tri dinh ky",
                CreatedAt = createdAt.AddHours(1)
            });

            // Manager nghiem thu
            data.ReportApprovals.Add(new ReportApproval
            {
                RepairReportId = reportId,
                UserId = managerUserId,
                Role = AccountRole.Manager,
                Status = ReportStatus.Approved,
                Comment = "Nghiem thu cong viec bao tri dinh ky",
                CreatedAt = createdAt.AddHours(2)
            });
        }

        #endregion

        #region Text Helpers

        private static string GetInspectionResult(MaintenanceScenarioConfig config, int index)
        {
            if (config.Scenario == MaintenanceScenario.Completed_SomeNeedRepair)
            {
                return index % 3 == 0 ? "Need Repair" : "OK";
            }
            return "OK";
        }

        private static string GetTechnicianNote(MaintenanceScenarioConfig config, string taskName)
        {
            if (config.Scenario == MaintenanceScenario.Completed_SomeNeedRepair)
            {
                return $"Da kiem tra {taskName}. Phat hien mot so van de nho can theo doi.";
            }
            return $"Da hoan thanh kiem tra {taskName}. Hoat dong binh thuong.";
        }

        private static string GetMaintenanceInspectionDescription(MaintenanceScenarioConfig config, MaintenanceSchedule schedule)
        {
            var typeName = schedule.CommonAreaObject.CommonAreaObjectType.TypeName;
            return config.Scenario switch
            {
                MaintenanceScenario.Completed_AllTasksOK =>
                    $"Kiem tra bao tri dinh ky {typeName} tai {schedule.CommonAreaObject.CommonArea.Name}. " +
                    "Tat ca cac hang muc deu hoat dong binh thuong, khong phat hien van de.",

                MaintenanceScenario.Completed_SomeNeedRepair =>
                    $"Kiem tra bao tri dinh ky {typeName} tai {schedule.CommonAreaObject.CommonArea.Name}. " +
                    "Phat hien mot so hang muc can sua chua/thay the do hao mon tu nhien.",

                _ => $"Da kiem tra {typeName} theo lich bao tri dinh ky."
            };
        }

        private static string GetMaintenanceSolutionDescription(MaintenanceScenarioConfig config, MaintenanceSchedule schedule)
        {
            return config.Scenario switch
            {
                MaintenanceScenario.Completed_AllTasksOK =>
                    "Hoan thanh tat ca cac hang muc kiem tra theo checklist. Thiet bi hoat dong tot, de xuat tiep tuc theo doi dinh ky.",

                MaintenanceScenario.Completed_SomeNeedRepair =>
                    "Da thuc hien sua chua/thay the cac bo phan bi hao mon. De xuat rut ngan chu ky bao tri cho thiet bi nay.",

                _ => "Thuc hien bao tri theo quy trinh tieu chuan."
            };
        }

        private static string GetMaintenanceRepairDescription(MaintenanceScenarioConfig config, MaintenanceSchedule schedule)
        {
            var typeName = schedule.CommonAreaObject.CommonAreaObjectType.TypeName;
            return config.Scenario switch
            {
                MaintenanceScenario.Completed_AllTasksOK =>
                    $"Hoan thanh bao tri dinh ky {typeName}. Da thuc hien day du cac cong viec theo checklist. " +
                    "Thiet bi hoat dong on dinh, san sang cho chu ky tiep theo.",

                MaintenanceScenario.Completed_SomeNeedRepair =>
                    $"Hoan thanh bao tri {typeName} voi mot so sua chua phat sinh. " +
                    "Da thay the cac bo phan hao mon va hieu chinh lai thiet bi. Hoat dong binh thuong.",

                _ => $"Da hoan thanh bao tri {typeName}."
            };
        }

        #endregion

        #region Tracking Generators

        private static List<RequestTracking> GenerateMaintenanceRequestTrackings(
            int requestId, RequestStatus finalStatus, DateTime createdAt,
            int techLeadId, int managerId)
        {
            var trackings = new List<RequestTracking>();
            var currentTime = createdAt;

            // ⭐ LUONG DUNG CHO MAINTENANCE:
            // Pending -> WaitingManagerApproval -> Approved -> InProgress -> AcceptancePendingVerify -> Completed

            // 1. Pending - Tao boi he thong
            trackings.Add(new RequestTracking
            {
                RepairRequestId = requestId,
                Status = RequestStatus.Pending,
                Note = "Yeu cau bao tri dinh ky duoc tao tu dong tu he thong",
                UpdatedBy = managerId,
                UpdatedAt = currentTime
            });

            // 2. WaitingManagerApproval - TechLead gui len Manager
            currentTime = currentTime.AddHours(1);
            trackings.Add(new RequestTracking
            {
                RepairRequestId = requestId,
                Status = RequestStatus.WaitingManagerApproval,
                Note = "Cho Manager phe duyet lich bao tri",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            // 3. Approved - Manager duyet
            currentTime = currentTime.AddHours(2);
            trackings.Add(new RequestTracking
            {
                RepairRequestId = requestId,
                Status = RequestStatus.Approved,
                Note = "Manager da phe duyet lich bao tri",
                UpdatedBy = managerId,
                UpdatedAt = currentTime
            });

            // 4. InProgress
            currentTime = currentTime.AddDays(1);
            trackings.Add(new RequestTracking
            {
                RepairRequestId = requestId,
                Status = RequestStatus.InProgress,
                Note = "Ky thuat vien dang thuc hien bao tri",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            // 5. AcceptancePendingVerify
            currentTime = currentTime.AddHours(3);
            trackings.Add(new RequestTracking
            {
                RepairRequestId = requestId,
                Status = RequestStatus.AcceptancePendingVerify,
                Note = "Cho nghiem thu bao tri",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            // 6. Completed
            currentTime = currentTime.AddHours(4);
            trackings.Add(new RequestTracking
            {
                RepairRequestId = requestId,
                Status = RequestStatus.Completed,
                Note = "Hoan thanh bao tri dinh ky",
                UpdatedBy = managerId,
                UpdatedAt = currentTime
            });

            return trackings;
        }

        private static List<AppointmentTracking> GenerateMaintenanceAppointmentTrackings(
            int appointmentId, RequestStatus requestFinalStatus, DateTime createdAt,
            int techLeadId)
        {
            var trackings = new List<AppointmentTracking>();
            var currentTime = createdAt.AddMinutes(30);

            // ⭐ LUONG DUNG CHO APPOINTMENT:
            // Pending -> Assigned -> Confirmed -> InVisit -> InRepair -> Completed

            trackings.Add(new AppointmentTracking
            {
                AppointmentId = appointmentId,
                Status = AppointmentStatus.Pending,
                Note = "Lich bao tri duoc tao tu dong tu he thong",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            currentTime = currentTime.AddHours(2);
            trackings.Add(new AppointmentTracking
            {
                AppointmentId = appointmentId,
                Status = AppointmentStatus.Assigned,
                Note = "Da phan cong ky thuat vien",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            currentTime = currentTime.AddHours(1);
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
                Note = "Ky thuat vien dang kiem tra",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            currentTime = currentTime.AddHours(1);
            trackings.Add(new AppointmentTracking
            {
                AppointmentId = appointmentId,
                Status = AppointmentStatus.InRepair,
                Note = "Dang thuc hien bao tri",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            currentTime = currentTime.AddHours(2);
            trackings.Add(new AppointmentTracking
            {
                AppointmentId = appointmentId,
                Status = AppointmentStatus.Completed,
                Note = "Hoan thanh bao tri dinh ky",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            return trackings;
        }

        #endregion

        #region Database Save

        private static async Task SaveAllDataAsync(AptCareSystemDBContext context, MaintenanceSeedCollections data)
        {
            if (!data.RepairRequests.Any()) return;

            context.RepairRequests.AddRange(data.RepairRequests);
            await context.SaveChangesAsync();

            context.RequestTrackings.AddRange(data.RequestTrackings);
            await context.SaveChangesAsync();

            context.RepairRequestTasks.AddRange(data.RepairRequestTasks);
            await context.SaveChangesAsync();

            context.Appointments.AddRange(data.Appointments);
            await context.SaveChangesAsync();

            context.AppointmentTrackings.AddRange(data.AppointmentTrackings);
            await context.SaveChangesAsync();

            context.AppointmentAssigns.AddRange(data.AppointmentAssigns);
            await context.SaveChangesAsync();

            context.InspectionReports.AddRange(data.InspectionReports);
            await context.SaveChangesAsync();

            context.RepairReports.AddRange(data.RepairReports);
            await context.SaveChangesAsync();

            context.ReportApprovals.AddRange(data.ReportApprovals);
            await context.SaveChangesAsync();
        }

        #endregion
    }
}
