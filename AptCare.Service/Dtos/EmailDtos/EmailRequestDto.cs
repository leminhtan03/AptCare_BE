namespace AptCare.Service.Dtos.EmailDtos
{
    public class EmailRequestDto
    {
        public string ToEmail { get; set; }
        public string Subject { get; set; }
        public string TemplateName { get; set; }
        public Dictionary<string, string> Replacements { get; set; }
    }

    public class BulkEmailRequestDto
    {
        public List<EmailRequestDto> Emails { get; set; }
    }
}