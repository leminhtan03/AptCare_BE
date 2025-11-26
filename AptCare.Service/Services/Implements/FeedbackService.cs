using AptCare.Repository;
using AptCare.Repository.Entities;
using AptCare.Repository.Enum.AccountUserEnum;
using AptCare.Repository.UnitOfWork;
using AptCare.Service.Dtos.FeedbackDtos;
using AptCare.Service.Exceptions;
using AptCare.Service.Services.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace AptCare.Service.Services.Implements
{
    public class FeedbackService : BaseService<FeedbackService>, IFeedbackService
    {
        private readonly IUserContext _userContext;

        public FeedbackService(IUnitOfWork<AptCareSystemDBContext> unitOfWork, ILogger<FeedbackService> logger, IMapper mapper, IUserContext userContext) : base(unitOfWork, logger, mapper)
        {
            _userContext = userContext;
        }

        public async Task<FeedbackResponse> CreateFeedbackAsync(CreateFeedbackRequest request)
        {
            try
            {
                var feedbackRepo = _unitOfWork.GetRepository<Feedback>();
                var repairRequestRepo = _unitOfWork.GetRepository<RepairRequest>();
                var userRepo = _unitOfWork.GetRepository<User>();
                var userId = _userContext.CurrentUserId;
                var userRole = _userContext.Role;

                await _unitOfWork.BeginTransactionAsync();
                var repairRequest = await repairRequestRepo.SingleOrDefaultAsync(
                    predicate: r => r.RepairRequestId == request.RepairRequestId);

                if (repairRequest == null)
                {
                    throw new AppValidationException("Yêu cầu sửa chữa không tồn tại.", StatusCodes.Status404NotFound);
                }
                if (userRole == nameof(AccountRole.Resident))
                {

                    var userCreatingFeedback = await userRepo.SingleOrDefaultAsync(
                    predicate: u => u.UserId == userId,
                    include: i => i.Include(u => u.Account)
                                   .Include(u => u.UserApartments)
                    );
                    if (userCreatingFeedback == null || !userCreatingFeedback.UserApartments
                            .Any(ua => ua.ApartmentId == repairRequest.ApartmentId))
                        throw new AppValidationException("Bạn không có quyền tạo feedback cho yêu cầu sửa chữa này.", StatusCodes.Status403Forbidden);
                }
                if (request.ParentFeedbackId.HasValue)
                {
                    var parentFeedback = await feedbackRepo.SingleOrDefaultAsync(
                        predicate: f => f.FeedbackId == request.ParentFeedbackId.Value);

                    if (parentFeedback == null)
                    {
                        throw new AppValidationException(
                            "Feedback gốc không tồn tại.",
                            StatusCodes.Status404NotFound);
                    }
                    if (parentFeedback.RepairRequestId != request.RepairRequestId)
                    {
                        throw new AppValidationException(
                            "Feedback gốc không thuộc cùng yêu cầu sửa chữa.",
                            StatusCodes.Status400BadRequest);
                    }
                    request.Rating = 0;
                }
                else
                {
                    if (request.Rating < 1 || request.Rating > 5)
                    {
                        throw new AppValidationException("Đánh giá phải từ 1 đến 5 sao.", StatusCodes.Status400BadRequest);
                    }
                }
                var feedback = _mapper.Map<Feedback>(request);
                feedback.UserId = userId;

                await feedbackRepo.InsertAsync(feedback);
                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "User {UserId} created feedback {FeedbackId} for repair request {RepairRequestId}",
                    userId, feedback.FeedbackId, request.RepairRequestId);

                return await GetFeedbackByIdAsync(feedback.FeedbackId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi khi tạo feedback cho repair request {RepairRequestId}", request.RepairRequestId);
                throw new Exception(ex.Message);
            }
        }

        public async Task<FeedbackThreadResponse> GetFeedbackThreadAsync(int repairRequestId)
        {
            var feedbackRepo = _unitOfWork.GetRepository<Feedback>();

            var allFeedbacks = await feedbackRepo.GetListAsync(
                predicate: f => f.RepairRequestId == repairRequestId,
                include: i => i.Include(f => f.User)
                               .ThenInclude(u => u.Account),
                orderBy: q => q.OrderBy(f => f.CreatedAt));

            if (!allFeedbacks.Any())
            {
                return new FeedbackThreadResponse
                {
                    RepairRequestId = repairRequestId,
                    RootFeedbacks = new List<FeedbackResponse>()
                };
            }

            var feedbackList = allFeedbacks.ToList();

            var rootFeedbacks = feedbackList
                .Where(f => !f.ParentFeedbackId.HasValue)
                .Select(f => MapToFeedbackResponse(f, feedbackList))
                .ToList();

            return new FeedbackThreadResponse
            {
                RepairRequestId = repairRequestId,
                RootFeedbacks = rootFeedbacks
            };
        }

        public async Task<FeedbackResponse> GetFeedbackByIdAsync(int feedbackId)
        {
            var feedbackRepo = _unitOfWork.GetRepository<Feedback>();

            var feedback = await feedbackRepo.SingleOrDefaultAsync(
                predicate: f => f.FeedbackId == feedbackId,
                include: i => i.Include(f => f.User)
                               .ThenInclude(u => u.Account));

            if (feedback == null)
            {
                throw new AppValidationException("Feedback không tồn tại.", StatusCodes.Status404NotFound);
            }

            var allFeedbacksInThread = await feedbackRepo.GetListAsync(
                predicate: f => f.RepairRequestId == feedback.RepairRequestId,
                include: i => i.Include(f => f.User)
                               .ThenInclude(u => u.Account));

            return MapToFeedbackResponse(feedback, allFeedbacksInThread.ToList());
        }

        public async Task<bool> DeleteFeedbackAsync(int feedbackId)
        {
            try
            {
                var feedbackRepo = _unitOfWork.GetRepository<Feedback>();
                var userId = _userContext.CurrentUserId;
                var userRole = _userContext.Role;
                await _unitOfWork.BeginTransactionAsync();

                var feedback = await feedbackRepo.SingleOrDefaultAsync(
                    predicate: f => f.FeedbackId == feedbackId,
                    include: i => i.Include(f => f.User)
                                   .ThenInclude(u => u.Account));

                if (feedback == null)
                {
                    throw new AppValidationException("Feedback không tồn tại.", StatusCodes.Status404NotFound);
                }

                if (feedback.UserId != userId)
                {
                    if (userRole != nameof(AccountRole.Resident) && userRole != nameof(AccountRole.Receptionist))
                        throw new AppValidationException("Bạn chỉ có thể xóa feedback của mình.", StatusCodes.Status403Forbidden);
                }
                await DeleteFeedbackRecursiveAsync(feedbackId);

                await _unitOfWork.CommitAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "User {UserId} deleted feedback {FeedbackId} and its replies",
                    userId, feedbackId);

                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Lỗi khi xóa feedback {FeedbackId}", feedbackId);
                throw new Exception(ex.Message);
            }
        }

        private async Task DeleteFeedbackRecursiveAsync(int feedbackId)
        {
            var feedbackRepo = _unitOfWork.GetRepository<Feedback>();

            var childFeedbacks = await feedbackRepo.GetListAsync(
                predicate: f => f.ParentFeedbackId == feedbackId);

            foreach (var child in childFeedbacks)
            {
                await DeleteFeedbackRecursiveAsync(child.FeedbackId);
            }

            var feedback = await feedbackRepo.SingleOrDefaultAsync(
                predicate: f => f.FeedbackId == feedbackId);

            if (feedback != null)
            {
                feedbackRepo.DeleteAsync(feedback);
            }
        }

        private FeedbackResponse MapToFeedbackResponse(Feedback feedback, List<Feedback> allFeedbacks)
        {
            var response = _mapper.Map<FeedbackResponse>(feedback);
            response.Replies = allFeedbacks
                .Where(f => f.ParentFeedbackId == feedback.FeedbackId)
                .OrderBy(f => f.CreatedAt)
                .Select(f => MapToFeedbackResponse(f, allFeedbacks))
                .ToList();

            return response;
        }
    }
}