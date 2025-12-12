

namespace AptCare.Service.Dtos
{
    public class GetSystemUserFilterDto
    {
        public string? SearchQuery { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
    public class GetResidentDataFilterDto
    {
        public string? SearchQuery { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

    }

}
