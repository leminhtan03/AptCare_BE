namespace AptCare.Service.Dtos.AccessoryDto
{
    public class StockInAccessoryDto
    {
        public int? AccessoryId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Note { get; set; } = string.Empty;
    }
}
