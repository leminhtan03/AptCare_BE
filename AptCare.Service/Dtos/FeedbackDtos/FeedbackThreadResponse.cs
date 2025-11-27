namespace AptCare.Service.Dtos.FeedbackDtos
{
    public class FeedbackThreadResponse
    {
        public int RepairRequestId { get; set; }
        public List<FeedbackResponse> RootFeedbacks { get; set; } = new();
    }
}
