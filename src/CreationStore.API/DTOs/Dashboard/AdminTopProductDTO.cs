namespace CreationStore.API.DTOs.Admin.Dashboard
{
    public class AdminTopProductDTO
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int SoldQuantity { get; set; }

        public decimal Revenue { get; set; }
    }
}