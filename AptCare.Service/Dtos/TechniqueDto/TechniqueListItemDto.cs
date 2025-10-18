using AptCare.Repository.Enum;

namespace AptCare.Service.Dtos.TechniqueDto
{
    public class TechniqueListItemDto
    {
        public int TechniqueId { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string Status { get; set; } = "Active";
        public int IssueCount { get; set; }
    }
}
