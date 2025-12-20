using AptCare.Repository.Entities;
using AptCare.Repository.Enum;
using AptCare.Repository.Enum.AccountUserEnum;
using Microsoft.EntityFrameworkCore;

namespace AptCare.Repository.Seeds
{
    /// <summary>
    /// Seed RepairRequest ???c t?o t? ??ng t? MaintenanceSchedule (b?o trì ??nh k?)
    /// ?ây là lu?ng riêng bi?t v?i RepairRequest c?a c? dân
    /// </summary>
    public static class MaintenanceRepairRequestSeed
    {
        #region Scenario Definitions

        private enum MaintenanceScenario
        {
            Completed_AllTasksOK,           // Hoàn thành, t?t c? tasks OK
            Completed_SomeNeedRepair,       // Hoàn thành, có v?n ?? c?n s?a thêm
            InProgress_Inspecting,          // ?ang ki?m tra
            InProgress_Repairing,           // ?ang s?a ch?a
            Approved_WaitingStart,          // ?ã duy?t, ch? b?t ??u
            WaitingManagerApproval,         // Ch? Manager duy?t
            Pending_New,                    // M?i t?o
            Rejected_ByManager,             // B? t? ch?i
            Cancelled                       // ?ã h?y
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
            // Ki?m tra ?ã có maintenance repair request ch?a
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
            return new List<MaintenanceScenarioConfig>
            {
                // ========== THANG 1-3/2025 - Completed ==========
                new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_AllTasksOK,
                    BaseDate = new DateTime(2025, 1, 10),
                    Count = 5,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri dinh ky thang 1 - Tat ca OK"
                },
                new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_SomeNeedRepair,
                    BaseDate = new DateTime(2025, 2, 5),
                    Count = 4,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri dinh ky thang 2 - Co phat sinh sua chua"
                },
                new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_AllTasksOK,
                    BaseDate = new DateTime(2025, 3, 8),
                    Count = 6,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri dinh ky thang 3"
                },

                // ========== THANG 4-6/2025 ==========
                new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_AllTasksOK,
                    BaseDate = new DateTime(2025, 4, 12),
                    Count = 5,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri dinh ky thang 4"
                },
                new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_SomeNeedRepair,
                    BaseDate = new DateTime(2025, 5, 6),
                    Count = 4,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri thang 5 - Phat sinh thay the linh kien"
                },
                new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_AllTasksOK,
                    BaseDate = new DateTime(2025, 6, 15),
                    Count = 5,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri dinh ky thang 6"
                },
                new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Cancelled,
                    BaseDate = new DateTime(2025, 6, 25),
                    Count = 2,
                    FinalStatus = RequestStatus.Cancelled,
                    HasInspectionReport = false,
                    HasRepairReport = false,
                    Description = "Bao tri bi huy do lich trinh"
                },

                // ========== THANG 7-9/2025 ==========
                new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_AllTasksOK,
                    BaseDate = new DateTime(2025, 7, 8),
                    Count = 6,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri dinh ky thang 7"
                },
                new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_SomeNeedRepair,
                    BaseDate = new DateTime(2025, 8, 10),
                    Count = 5,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri thang 8 - Thay the thiet bi cu"
                },
                new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_AllTasksOK,
                    BaseDate = new DateTime(2025, 9, 5),
                    Count = 5,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri dinh ky thang 9"
                },

                // ========== THANG 10-11/2025 ==========
                new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_AllTasksOK,
                    BaseDate = new DateTime(2025, 10, 3),
                    Count = 6,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri dinh ky thang 10"
                },
                new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_SomeNeedRepair,
                    BaseDate = new DateTime(2025, 10, 20),
                    Count = 4,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri thang 10 - Phat sinh van de"
                },
                new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_AllTasksOK,
                    BaseDate = new DateTime(2025, 11, 5),
                    Count = 6,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri dinh ky thang 11"
                },
                new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Rejected_ByManager,
                    BaseDate = new DateTime(2025, 11, 25),
                    Count = 2,
                    FinalStatus = RequestStatus.Rejected,
                    HasInspectionReport = false,
                    HasRepairReport = false,
                    Description = "Bao tri bi tu choi do ngan sach"
                },

                // ========== THANG 12/2025 - Mix trang thai ==========
                new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Completed_AllTasksOK,
                    BaseDate = new DateTime(2025, 12, 1),
                    Count = 5,
                    FinalStatus = RequestStatus.Completed,
                    HasInspectionReport = true,
                    HasRepairReport = true,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri hoan thanh thang 12"
                },
                new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.InProgress_Repairing,
                    BaseDate = new DateTime(2025, 12, 15),
                    Count = 3,
                    FinalStatus = RequestStatus.InProgress,
                    HasInspectionReport = true,
                    HasRepairReport = false,
                    TaskStatus = TaskCompletionStatus.Completed,
                    Description = "Bao tri dang thuc hien"
                },
                new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Approved_WaitingStart,
                    BaseDate = new DateTime(2025, 12, 20),
                    Count = 4,
                    FinalStatus = RequestStatus.Approved,
                    HasInspectionReport = false,
                    HasRepairReport = false,
                    TaskStatus = TaskCompletionStatus.Pending,
                    Description = "Bao tri da duyet cho bat dau"
                },
                new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.WaitingManagerApproval,
                    BaseDate = new DateTime(2025, 12, 22),
                    Count = 3,
                    FinalStatus = RequestStatus.WaitingManagerApproval,
                    HasInspectionReport = false,
                    HasRepairReport = false,
                    TaskStatus = TaskCompletionStatus.Pending,
                    Description = "Bao tri cho Manager duyet"
                },
                new MaintenanceScenarioConfig
                {
                    Scenario = MaintenanceScenario.Pending_New,
                    BaseDate = new DateTime(2025, 12, 24),
                    Count = 3,
                    FinalStatus = RequestStatus.Pending,
                    HasInspectionReport = false,
                    HasRepairReport = false,
                    TaskStatus = TaskCompletionStatus.Pending,
                    Description = "Bao tri moi tao"
                }
            };
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
                Description = $"[{config.Scenario}] Bao tri dinh ky: {schedule.Description}. " +
                              $"Khu vuc: {schedule.CommonAreaObject.CommonArea.Name}. " +
                              $"Chu ky: {schedule.FrequencyInDays} ngay.",
                IsEmergency = false,
                CreatedAt = createdAt
            };

            if (config.FinalStatus == RequestStatus.Completed)
            {
                request.AcceptanceTime = DateOnly.FromDateTime(createdAt.AddDays(3));
            }

            data.RepairRequests.Add(request);

            // 2. Tao RequestTrackings
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

            // 4. Tao Appointment neu khong phai Pending
            if (config.FinalStatus != RequestStatus.Pending)
            {
                var startTime = schedule.NextScheduledDate
                    .ToDateTime(TimeOnly.FromTimeSpan(schedule.TimePreference));
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
                User? assignedTechnician = null;
                if (config.FinalStatus != RequestStatus.Cancelled &&
                    config.FinalStatus != RequestStatus.WaitingManagerApproval &&
                    config.FinalStatus != RequestStatus.Rejected)
                {
                    var techIndex = index % seedData.Technicians.Count;
                    var assignedTechs = seedData.Technicians
                        .Skip(techIndex)
                        .Take(schedule.RequiredTechnicians)
                        .ToList();

                    assignedTechnician = assignedTechs.FirstOrDefault();

                    foreach (var tech in assignedTechs)
                    {
                        var workOrderStatus = config.FinalStatus switch
                        {
                            RequestStatus.Completed => WorkOrderStatus.Completed,
                            RequestStatus.InProgress => WorkOrderStatus.Working,
                            RequestStatus.Approved => WorkOrderStatus.Pending,
                            _ => WorkOrderStatus.Pending
                        };

                        data.AppointmentAssigns.Add(new AppointmentAssign
                        {
                            TechnicianId = tech.UserId,
                            AppointmentId = currentAppointmentId,
                            AssignedAt = createdAt.AddHours(1),
                            EstimatedStartTime = startTime,
                            EstimatedEndTime = endTime,
                            ActualStartTime = config.FinalStatus >= RequestStatus.InProgress ? startTime : null,
                            ActualEndTime = config.FinalStatus >= RequestStatus.Completed ? endTime : null,
                            Status = workOrderStatus
                        });
                    }
                }

                // 5. Tao InspectionReport cho maintenance
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

                // 6. Tao RepairReport cho maintenance
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
            }

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
            var faultType = config.Scenario == MaintenanceScenario.Completed_SomeNeedRepair
                ? FaultType.BuildingFault
                : FaultType.BuildingFault;

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

            // Manager approve (vi la maintenance)
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

            // Manager approve (nghiem thu cho maintenance)
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

            // Pending - Tao boi he thong
            trackings.Add(new RequestTracking
            {
                RepairRequestId = requestId,
                Status = RequestStatus.Pending,
                Note = "Yeu cau bao tri dinh ky duoc tao tu dong tu he thong",
                UpdatedBy = managerId,
                UpdatedAt = currentTime
            });

            if (finalStatus == RequestStatus.Pending) return trackings;

            if (finalStatus == RequestStatus.Cancelled)
            {
                trackings.Add(new RequestTracking
                {
                    RepairRequestId = requestId,
                    Status = RequestStatus.Cancelled,
                    Note = "Yeu cau bao tri bi huy do thay doi lich trinh",
                    UpdatedBy = managerId,
                    UpdatedAt = currentTime.AddHours(2)
                });
                return trackings;
            }

            // WaitingManagerApproval - TechLead gui len Manager
            currentTime = currentTime.AddHours(1);
            trackings.Add(new RequestTracking
            {
                RepairRequestId = requestId,
                Status = RequestStatus.WaitingManagerApproval,
                Note = "Cho Manager phe duyet lich bao tri",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            if (finalStatus == RequestStatus.WaitingManagerApproval) return trackings;

            if (finalStatus == RequestStatus.Rejected)
            {
                trackings.Add(new RequestTracking
                {
                    RepairRequestId = requestId,
                    Status = RequestStatus.Rejected,
                    Note = "Tu choi do ngan sach hoac lich trinh khong phu hop",
                    UpdatedBy = managerId,
                    UpdatedAt = currentTime.AddHours(2)
                });
                return trackings;
            }

            // Approved - Manager duyet
            currentTime = currentTime.AddHours(2);
            trackings.Add(new RequestTracking
            {
                RepairRequestId = requestId,
                Status = RequestStatus.Approved,
                Note = "Manager da phe duyet lich bao tri",
                UpdatedBy = managerId,
                UpdatedAt = currentTime
            });

            if (finalStatus == RequestStatus.Approved) return trackings;

            // InProgress
            currentTime = currentTime.AddDays(1);
            trackings.Add(new RequestTracking
            {
                RepairRequestId = requestId,
                Status = RequestStatus.InProgress,
                Note = "Ky thuat vien dang thuc hien bao tri",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            if (finalStatus == RequestStatus.InProgress) return trackings;

            // Completed
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

            trackings.Add(new AppointmentTracking
            {
                AppointmentId = appointmentId,
                Status = AppointmentStatus.Pending,
                Note = "Lich bao tri duoc tao tu dong tu he thong",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            if (requestFinalStatus == RequestStatus.Cancelled)
            {
                trackings.Add(new AppointmentTracking
                {
                    AppointmentId = appointmentId,
                    Status = AppointmentStatus.Cancelled,
                    Note = "Huy do yeu cau bao tri bi huy",
                    UpdatedBy = techLeadId,
                    UpdatedAt = currentTime.AddHours(1)
                });
                return trackings;
            }

            if (requestFinalStatus == RequestStatus.WaitingManagerApproval ||
                requestFinalStatus == RequestStatus.Rejected)
                return trackings;

            currentTime = currentTime.AddHours(2);
            trackings.Add(new AppointmentTracking
            {
                AppointmentId = appointmentId,
                Status = AppointmentStatus.Assigned,
                Note = "Da phan cong ky thuat vien",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            if (requestFinalStatus == RequestStatus.Approved) return trackings;

            currentTime = currentTime.AddDays(1);
            trackings.Add(new AppointmentTracking
            {
                AppointmentId = appointmentId,
                Status = AppointmentStatus.InVisit,
                Note = "Ky thuat vien dang kiem tra",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            if (requestFinalStatus == RequestStatus.InProgress) return trackings;

            currentTime = currentTime.AddHours(2);
            trackings.Add(new AppointmentTracking
            {
                AppointmentId = appointmentId,
                Status = AppointmentStatus.InRepair,
                Note = "Dang thuc hien bao tri",
                UpdatedBy = techLeadId,
                UpdatedAt = currentTime
            });

            if (requestFinalStatus == RequestStatus.Completed)
            {
                trackings.Add(new AppointmentTracking
                {
                    AppointmentId = appointmentId,
                    Status = AppointmentStatus.Completed,
                    Note = "Hoan thanh bao tri dinh ky",
                    UpdatedBy = techLeadId,
                    UpdatedAt = currentTime.AddHours(2)
                });
            }

            return trackings;
        }

        #endregion

        #region Database Save

        private static async Task SaveAllDataAsync(AptCareSystemDBContext context, MaintenanceSeedCollections data)
        {
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
