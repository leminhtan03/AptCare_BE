using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.ApproveReportDtos;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AptCare.Service.Services.Implements
{
    public class ReportApprovalService : BaseService<ReportApprovalService>, IReportApprovalService
    {
        private readonly IUserContext _userContext;
        public ReportApprovalService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, IUserContext userContext, ILogger<ReportApprovalService> logger, IMapper mapper) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
        }

        public async Task<string> ApproveReportAsync(ApproveReportCreateDto dto)
        {
            var ReportApprovalRepo = _unitOfWork.GetRepository<ReportApproval>();
            var userId = _userContext.CurrentUserId;
            var role = Enum.Parse<AccountRole>(_userContext.Role);
            try
            {
                if (dto.ReportType == "InspectionReport")
                {
                    var InspecRepo = _unitOfWork.GetRepository<InspectionReport>();
                    var inspectionReport = await InspecRepo.SingleOrDefaultAsync(predicate: ir => ir.InspectionReportId == dto.ReportId);
                    if (inspectionReport == null)
                    {
                        throw new Exception("Inspection report not found.");
                    }
                    var reportApproval = new ReportApproval
                    {
                        InspectionReportId = dto.ReportId,
                        UserId = userId,
                        Role = role,
                        Status = dto.Status,
                        Comment = dto.Comment,
                        CreatedAt = DateTime.UtcNow.AddHours(7)
                    };
                    await ReportApprovalRepo.InsertAsync(reportApproval);
                    inspectionReport.Status = dto.Status;
                    InspecRepo.UpdateAsync(inspectionReport);
                    await _unitOfWork.CommitAsync();
                    return "Inspection report approved successfully.";
                }
                else if (dto.ReportType == "RepairReport")
                {
                    var RepairRepo = _unitOfWork.GetRepository<RepairReport>();
                    var repairReport = await RepairRepo.SingleOrDefaultAsync(predicate: rr => rr.RepairReportId == dto.ReportId);
                    if (repairReport == null)
                    {
                        throw new Exception("Repair report not found.");
                    }
                    var reportApproval = new ReportApproval
                    {
                        RepairReportId = dto.ReportId,
                        UserId = userId,
                        Role = role,
                        Status = dto.Status,
                        Comment = dto.Comment,
                        CreatedAt = DateTime.UtcNow.AddHours(7)
                    };
                    await ReportApprovalRepo.InsertAsync(reportApproval);
                    repairReport.Status = dto.Status;
                    RepairRepo.UpdateAsync(repairReport);
                    await _unitOfWork.CommitAsync();
                    return "Repair report approved successfully.";
                }
                else
                {
                    throw new Exception("Invalid report type.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("ApproveReportAsync", ex);
            }
        }
    }
}
