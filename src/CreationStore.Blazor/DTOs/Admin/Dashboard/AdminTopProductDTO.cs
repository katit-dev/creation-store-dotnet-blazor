namespace CreationStore.Blazor.DTOs.Admin.Dashboard
{
    public class AdminTopProductDTO
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int QuantitySold { get; set; }

        public int TotalQuantity { get; set; }

        public decimal Revenue { get; set; }

        public decimal TotalRevenue { get; set; }

        public int DisplayQuantitySold =>
            QuantitySold > 0 ? QuantitySold : TotalQuantity;

        public decimal DisplayRevenue =>
            TotalRevenue > 0 ? TotalRevenue : Revenue;
    }
}