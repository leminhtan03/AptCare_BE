namespace AptCare.Service.Dtos.FeedbackDtos
{
    public class FeedbackResponse
    {
        public int FeedbackId { get; set; }
        public int RepairRequestId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public int? ParentFeedbackId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<FeedbackResponse> Replies { get; set; } = new();
    }
}
