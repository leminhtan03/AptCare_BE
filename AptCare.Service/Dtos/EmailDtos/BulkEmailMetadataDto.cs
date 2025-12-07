namespace AptCare.Service.Dtos.EmailDtos
{
    public class BulkEmailMetadataDto
    {
        public List<EmailRecipient> Recipients { get; set; } = new();
        public string Subject { get; set; }
        public string TemplateName { get; set; }
        public Dictionary<string, string> CommonReplacements { get; set; } = new();
    }

    public class EmailRecipient
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}