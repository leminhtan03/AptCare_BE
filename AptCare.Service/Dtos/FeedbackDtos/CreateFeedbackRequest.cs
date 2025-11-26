namespace AptCare.Service.Dtos.FeedbackDtos
{
    public class CreateFeedbackRequest
    {
        public int RepairRequestId { get; set; }
        public int? ParentFeedbackId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}
