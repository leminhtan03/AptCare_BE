using AptCare.Service.Dtos.FeedbackDtos;

namespace AptCare.Service.Services.Interfaces
{
    public interface IFeedbackService
    {
        Task<FeedbackResponse> CreateFeedbackAsync(CreateFeedbackRequest request);
        Task<FeedbackThreadResponse> GetFeedbackThreadAsync(int repairRequestId);
        Task<FeedbackResponse> GetFeedbackByIdAsync(int feedbackId);
        Task<bool> DeleteFeedbackAsync(int feedbackId);
    }
}